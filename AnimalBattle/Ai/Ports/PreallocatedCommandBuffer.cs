using AnimalBattle.Ai.Contracts;
using ErrorOr;

namespace AnimalBattle.Ai.Ports;

/// <summary>初期化時に配列を確保し、Frame_Update 間で再利用する command buffer です。</summary>
public sealed class PreallocatedCommandBuffer<T> : ICommandBuffer<T> where T : struct {
    private readonly T[] _commands;
    private readonly ICommandBufferDiagnosticSink? _diagnosticSink;
    private readonly CommandOneShotKey[] _keys;
    private readonly CommandPriority[] _priorities;
    private readonly CommandOneShotKey[] _seenOneShotKeys;

    /// <summary>指定容量の command buffer を初期化します。</summary>
    /// <param name="capacity">同時に保持できる command 数。</param>
    /// <param name="diagnosticSink">容量超過と重複を受け取る診断境界。</param>
    /// <param name="oneShotKeyCapacity">Frame_Update 中に記憶する key の最大数。省略時は command 容量の二倍です。</param>
    public PreallocatedCommandBuffer(
        int capacity,
        ICommandBufferDiagnosticSink? diagnosticSink = null,
        int oneShotKeyCapacity = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        ArgumentOutOfRangeException.ThrowIfNegative(oneShotKeyCapacity);

        var defaultKeyCapacity = capacity > int.MaxValue / 2 ? capacity : capacity * 2;
        var keyCapacity = oneShotKeyCapacity == 0 ? defaultKeyCapacity : oneShotKeyCapacity;
        if (keyCapacity < capacity)
            throw new ArgumentOutOfRangeException(nameof(oneShotKeyCapacity), "one-shot key 容量は command 容量以上にしてください。");

        _commands = new T[capacity];
        _priorities = new CommandPriority[capacity];
        _keys = new CommandOneShotKey[capacity];
        _seenOneShotKeys = new CommandOneShotKey[keyCapacity];
        _diagnosticSink = diagnosticSink;
    }

    /// <summary>Frame_Update 中に記憶できる one-shot key の最大数です。</summary>
    public int OneShotKeyCapacity => _seenOneShotKeys.Length;

    /// <summary>現在記憶している one-shot key 数です。</summary>
    public int OneShotKeyCount { get; private set; }

    /// <summary>容量超過などで破棄した command 数です。</summary>
    public long DroppedCount { get; private set; }

    /// <summary>診断記録に失敗した回数です。</summary>
    public long DiagnosticFailureCount { get; private set; }

    /// <summary>直近の overflow 診断です。</summary>
    public CommandBufferDiagnostic LastDiagnostic { get; private set; }

    /// <summary>現在の command 数です。</summary>
    public int Count { get; private set; }

    /// <summary>最大 command 数です。</summary>
    public int Capacity => _commands.Length;

    /// <summary>通常優先度で command を追加します。</summary>
    public ErrorOr<Success> Add(in T command)
    {
        var res = AddCommand(in command, CommandPriority.Normal);
        return res.IsAccepted ? Result.Success : Error.Failure("CommandBuffer.CapacityExceeded", "コマンドバッファの容量を超過しました。");
    }

    /// <summary>指定優先度で command を追加します。</summary>
    public ErrorOr<Success> Add(in T command, CommandPriority priority)
    {
        var res = AddCommand(in command, priority);
        return res.IsAccepted ? Result.Success : Error.Failure("CommandBuffer.CapacityExceeded", "コマンドバッファの容量を超過しました。");
    }

    /// <summary>指定した one-shot key を重複なく command とともに記録します。</summary>
    public CommandWriteResult AddOneShot(in T command, in CommandOneShotKey key,
        CommandPriority priority = CommandPriority.Attack)
    {
        return AddCommand(in command, priority, key);
    }

    /// <summary>指定位置に保持された one-shot key を取得します。</summary>
    public bool GetOneShotKey(int index, out CommandOneShotKey key)
    {
        if ((uint)index >= (uint)Count)
        {
            key = default;
            return false;
        }

        key = _keys[index];
        return key.IsValid;
    }

    /// <summary>次の Frame_Update 用に command と key を破棄します。</summary>
    public void Clear()
    {
        Count = 0;
        OneShotKeyCount = 0;
    }

    /// <summary>格納済み command を index で読み取ります。</summary>
    public ref readonly T this[int index] {
        get
        {
            if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));

            return ref _commands[index];
        }
    }

    /// <summary>command を優先度付きで追加し、結果を返します。</summary>
    public CommandWriteResult AddCommand(in T command, CommandPriority priority, CommandOneShotKey key = default)
    {
        var hasKey = key != default;
        if (hasKey && !key.IsValid)
            return Reject(CommandBufferFailureReason.InvalidOneShotKey, priority, CommandPriority.Normal, key,
                ErrorCode.ExecutionFailed);

        if (hasKey && ContainsSeenKey(key))
            return Reject(CommandBufferFailureReason.DuplicateOneShot, priority, FindExistingPriority(key), key,
                ErrorCode.ExecutionFailed);

        if (Count < _commands.Length)
        {
            if (hasKey && !RememberKey(key))
                return Reject(CommandBufferFailureReason.CapacityExceeded, priority, CommandPriority.Normal, key,
                    ErrorCode.ExecutionFailed);

            Store(Count, in command, priority, key);
            Count++;
            return Accepted(key);
        }

        var evictionIndex = FindLowestPriorityIndex(out var lowestPriority);
        if (priority <= lowestPriority)
            return Reject(CommandBufferFailureReason.CapacityExceeded, priority, lowestPriority, key,
                ErrorCode.ExecutionFailed);

        if (hasKey && !RememberKey(key))
            return Reject(CommandBufferFailureReason.CapacityExceeded, priority, lowestPriority, key,
                ErrorCode.ExecutionFailed);

        Store(evictionIndex, in command, priority, key);
        DroppedCount++;
        RecordDiagnostic(new CommandBufferDiagnostic(
            CommandBufferFailureReason.CapacityExceeded,
            priority,
            lowestPriority,
            key,
            ErrorCode.ExecutionFailed));
        return Accepted(key);
    }

    /// <summary>バッファの状態および診断情報を初期状態にリセットします。</summary>
    public void Reset()
    {
        Clear();
        DroppedCount = 0;
        DiagnosticFailureCount = 0;
        LastDiagnostic = default;
    }


    private void Store(int index, in T command, CommandPriority priority, CommandOneShotKey key)
    {
        _commands[index] = command;
        _priorities[index] = priority;
        _keys[index] = key;
    }

    private bool ContainsSeenKey(CommandOneShotKey key)
    {
        for (var i = 0; i < OneShotKeyCount; i++)
            if (_seenOneShotKeys[i] == key)
                return true;

        return false;
    }

    private CommandPriority FindExistingPriority(CommandOneShotKey key)
    {
        for (var i = 0; i < Count; i++)
            if (_keys[i] == key)
                return _priorities[i];

        return CommandPriority.Normal;
    }

    private bool RememberKey(CommandOneShotKey key)
    {
        if (OneShotKeyCount >= _seenOneShotKeys.Length) return false;

        _seenOneShotKeys[OneShotKeyCount++] = key;
        return true;
    }

    private int FindLowestPriorityIndex(out CommandPriority priority)
    {
        var index = 0;
        priority = _priorities[0];
        for (var i = 1; i < Count; i++)
            if (_priorities[i] < priority)
            {
                index = i;
                priority = _priorities[i];
            }

        return index;
    }

    private static CommandWriteResult Accepted(CommandOneShotKey key)
    {
        return new CommandWriteResult(true, key, CommandBufferFailureReason.None, ErrorCode.None);
    }

    private CommandWriteResult Reject(CommandBufferFailureReason reason, CommandPriority incoming,
        CommandPriority existing, CommandOneShotKey key, ErrorCode errorCode)
    {
        DroppedCount++;
        CommandBufferDiagnostic diagnostic = new(reason, incoming, existing, key, errorCode);
        var diagnosticRecorded = RecordDiagnostic(diagnostic);
        if (!diagnosticRecorded)
            return new CommandWriteResult(false, key, CommandBufferFailureReason.DiagnosticFailed,
                ErrorCode.ObservationFailed);

        return new CommandWriteResult(false, key, reason, errorCode);
    }

    private bool RecordDiagnostic(in CommandBufferDiagnostic diagnostic)
    {
        LastDiagnostic = diagnostic;
        if (_diagnosticSink is null) return true;

        try
        {
            if (_diagnosticSink.Record(in diagnostic).IsError)
            {
                DiagnosticFailureCount++;
                return false;
            }

            return true;
        }
        catch
        {
            DiagnosticFailureCount++;
            return false;
        }
    }
}

/// <summary>移動 command buffer を移動 port として公開します。</summary>
/// <remarks>buffer を指定します。</remarks>
public sealed class BufferedMovementPort(PreallocatedCommandBuffer<MovementCommand> buffer) : IMovementPort {
    private readonly PreallocatedCommandBuffer<MovementCommand> _buffer =
        buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <inheritdoc />
    public ErrorOr<Success> SetVelocity(float x, float y)
    {
        if (!IsFinite(x) || !IsFinite(y))
            return Error.Validation("BufferedMovementPort.SetVelocity", "速度値が有限値ではありません。");

        return _buffer.Add(new MovementCommand(MovementCommandKind.SetVelocity, x, y), CommandPriority.Safety);
    }

    /// <inheritdoc />
    public ErrorOr<Success> MoveTo(float x, float y)
    {
        if (!IsFinite(x) || !IsFinite(y)) return Error.Validation("BufferedMovementPort.MoveTo", "移動先座標が有限値ではありません。");

        return _buffer.Add(new MovementCommand(MovementCommandKind.MoveTo, x, y), CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> ApplyGravity(float gravity, float maxFallSpeed)
    {
        if (!IsFinite(gravity) || !IsFinite(maxFallSpeed) || gravity < 0.0f || maxFallSpeed < 0.0f)
            return Error.Validation("BufferedMovementPort.ApplyGravity", "重力または最大落下速度が不正です。");

        return _buffer.Add(new MovementCommand(MovementCommandKind.ApplyGravity, gravity, maxFallSpeed),
            CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> Stop()
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.Stop, 0.0f, 0.0f), CommandPriority.Safety);
    }

    private static bool IsFinite(float value)
    {
        return float.IsFinite(value);
    }
}

/// <summary>攻撃 command buffer を攻撃 port として公開します。</summary>
/// <remarks>buffer を指定します。</remarks>
public sealed class BufferedAttackPort(PreallocatedCommandBuffer<AttackCommand> buffer) : IAttackPort {
    private readonly PreallocatedCommandBuffer<AttackCommand> _buffer =
        buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <inheritdoc />
    public ErrorOr<Success> EmitBullet(BossSkill skill, float x, float y)
    {
        if (!CanEmit(skill, x, y)) return Error.Validation("BufferedAttackPort.EmitBullet", "技能または弾丸座標が不正です。");

        return _buffer.Add(new AttackCommand(AttackCommandKind.EmitBullet, skill, x, y), CommandPriority.Attack);
    }

    /// <inheritdoc />
    public ErrorOr<Success> CreateHitbox(BossSkill skill, float x, float y)
    {
        if (!CanEmit(skill, x, y)) return Error.Validation("BufferedAttackPort.CreateHitbox", "技能または判定座標が不正です。");

        return _buffer.Add(new AttackCommand(AttackCommandKind.CreateHitbox, skill, x, y), CommandPriority.Safety);
    }

    /// <inheritdoc />
    public ErrorOr<Success> ClearBossBullets()
    {
        return _buffer.Add(new AttackCommand(AttackCommandKind.ClearBossBullets), CommandPriority.Safety);
    }

    private static bool CanEmit(BossSkill skill, float x, float y)
    {
        return skill.IsValid() && float.IsFinite(x) && float.IsFinite(y);
    }
}

/// <summary>演出 command buffer を演出 port として公開します。</summary>
/// <remarks>buffer を指定します。</remarks>
public sealed class BufferedEffectPort(PreallocatedCommandBuffer<EffectCommand> buffer) : IEffectPort {
    private readonly PreallocatedCommandBuffer<EffectCommand> _buffer =
        buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <inheritdoc />
    public ErrorOr<Success> RequestTelegraph(float x, float y)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y))
            return Error.Validation("BufferedEffectPort.RequestTelegraph", "予告表示座標が有限値ではありません。");

        return _buffer.Add(new EffectCommand(EffectCommandKind.RequestTelegraph, x, y), CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> TriggerScreenShake(float intensity, float duration)
    {
        if (!float.IsFinite(intensity) || !float.IsFinite(duration))
            return Error.Validation("BufferedEffectPort.TriggerScreenShake", "振動パラメータが有限値ではありません。");

        return _buffer.Add(new EffectCommand(EffectCommandKind.ScreenShake, intensity, duration), CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> PlaySoundCue(int soundId, float volume)
    {
        if (!float.IsFinite(volume))
            return Error.Validation("BufferedEffectPort.PlaySoundCue", "音量パラメータが有限値ではありません。");

        return _buffer.Add(new EffectCommand(EffectCommandKind.AudioCue, soundId, volume), CommandPriority.Normal);
    }
}