using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Ports;

/// <summary>初期化時に配列を確保し、Frame_Update 間で再利用する command buffer です。</summary>
public sealed class PreallocatedCommandBuffer<T> : ICommandBuffer<T> where T : struct
{
    private readonly T[] _commands;
    private readonly CommandPriority[] _priorities;
    private readonly CommandOneShotKey[] _keys;
    private readonly ICommandBufferDiagnosticSink? _diagnosticSink;
    private int _count;

    /// <summary>現在の command 数です。</summary>
    public int Count => _count;
    /// <summary>最大 command 数です。</summary>
    public int Capacity => _commands.Length;
    /// <summary>容量超過などで破棄した command 数です。</summary>
    public long DroppedCount { get; private set; }
    /// <summary>診断記録に失敗した回数です。</summary>
    public long DiagnosticFailureCount { get; private set; }
    /// <summary>直近の overflow 診断です。</summary>
    public CommandBufferDiagnostic LastDiagnostic { get; private set; }

    /// <summary>指定容量の command buffer を初期化します。</summary>
    public PreallocatedCommandBuffer(int capacity, ICommandBufferDiagnosticSink? diagnosticSink = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _commands = new T[capacity];
        _priorities = new CommandPriority[capacity];
        _keys = new CommandOneShotKey[capacity];
        _diagnosticSink = diagnosticSink;
    }

    /// <summary>通常優先度で command を追加します。</summary>
    public bool TryAdd(in T command) => TryAddCommand(in command, CommandPriority.Normal).IsAccepted;

    /// <summary>指定優先度で command を追加します。</summary>
    public bool TryAdd(in T command, CommandPriority priority) => TryAddCommand(in command, priority).IsAccepted;

    /// <summary>指定した one-shot key を重複なく command とともに記録します。</summary>
    public CommandWriteResult TryAddOneShot(in T command, in CommandOneShotKey key, CommandPriority priority = CommandPriority.Attack)
    {
        if (!key.IsValid)
        {
            return Reject(CommandBufferFailureReason.InvalidOneShotKey, priority, CommandPriority.Normal, key, ErrorCode.ExecutionFailed);
        }

        for (int i = 0; i < _count; i++)
        {
            if (_keys[i] == key)
            {
                return Reject(CommandBufferFailureReason.DuplicateOneShot, priority, _priorities[i], key, ErrorCode.ExecutionFailed);
            }
        }

        return TryAddCommand(in command, priority, key);
    }

    /// <summary>command を優先度付きで追加し、結果を返します。</summary>
    public CommandWriteResult TryAddCommand(in T command, CommandPriority priority, CommandOneShotKey key = default)
    {
        bool hasKey = key != default;
        if (hasKey && !key.IsValid)
        {
            return Reject(CommandBufferFailureReason.InvalidOneShotKey, priority, CommandPriority.Normal, key, ErrorCode.ExecutionFailed);
        }

        if (hasKey)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_keys[i] == key)
                {
                    return Reject(CommandBufferFailureReason.DuplicateOneShot, priority, _priorities[i], key, ErrorCode.ExecutionFailed);
                }
            }
        }

        if (_count < _commands.Length)
        {
            Store(_count, in command, priority, key);
            _count++;
            return Accepted(key);
        }

        int evictionIndex = FindLowestPriorityIndex(out CommandPriority lowestPriority);
        if (priority <= lowestPriority)
        {
            return Reject(CommandBufferFailureReason.CapacityExceeded, priority, lowestPriority, key, ErrorCode.ExecutionFailed);
        }

        Store(evictionIndex, in command, priority, key);
        DroppedCount++;
        return Accepted(key);
    }

    /// <summary>指定位置に保持された one-shot key を取得します。</summary>
    public bool TryGetOneShotKey(int index, out CommandOneShotKey key)
    {
        if ((uint)index >= (uint)_count)
        {
            key = default;
            return false;
        }

        key = _keys[index];
        return key.IsValid;
    }

    /// <summary>次の Frame_Update 用に command と key を破棄します。</summary>
    public void Clear() => _count = 0;

    /// <summary>格納済み command を index で読み取ります。</summary>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _commands[index];
        }
    }

    private void Store(int index, in T command, CommandPriority priority, CommandOneShotKey key)
    {
        _commands[index] = command;
        _priorities[index] = priority;
        _keys[index] = key;
    }

    private int FindLowestPriorityIndex(out CommandPriority priority)
    {
        int index = 0;
        priority = _priorities[0];
        for (int i = 1; i < _count; i++)
        {
            if (_priorities[i] < priority)
            {
                index = i;
                priority = _priorities[i];
            }
        }

        return index;
    }

    private CommandWriteResult Accepted(CommandOneShotKey key) => new(true, key, CommandBufferFailureReason.None, ErrorCode.None);

    private CommandWriteResult Reject(CommandBufferFailureReason reason, CommandPriority incoming, CommandPriority existing, CommandOneShotKey key, ErrorCode errorCode)
    {
        DroppedCount++;
        LastDiagnostic = new CommandBufferDiagnostic(reason, incoming, existing, key, errorCode);
        try
        {
            CommandBufferDiagnostic diagnostic = LastDiagnostic;
            _diagnosticSink?.TryRecord(in diagnostic);
        }
        catch
        {
            DiagnosticFailureCount++;
        }

        return new CommandWriteResult(false, key, reason, errorCode);
    }
}

/// <summary>移動 command buffer を移動 port として公開します。</summary>
public sealed class BufferedMovementPort : IMovementPort
{
    private readonly PreallocatedCommandBuffer<MovementCommand> _buffer;
    /// <summary>buffer を指定します。</summary>
    public BufferedMovementPort(PreallocatedCommandBuffer<MovementCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool SetVelocity(float x, float y) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.SetVelocity, x, y), CommandPriority.Safety);
    /// <inheritdoc />
    public bool MoveTo(float x, float y) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.MoveTo, x, y), CommandPriority.Normal);
    /// <inheritdoc />
    public bool ApplyGravity(float gravity, float maxFallSpeed) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.ApplyGravity, gravity, maxFallSpeed), CommandPriority.Normal);
    /// <inheritdoc />
    public bool Stop() => _buffer.TryAdd(new MovementCommand(MovementCommandKind.Stop, 0.0f, 0.0f), CommandPriority.Safety);
}

/// <summary>攻撃 command buffer を攻撃 port として公開します。</summary>
public sealed class BufferedAttackPort : IAttackPort
{
    private readonly PreallocatedCommandBuffer<AttackCommand> _buffer;
    /// <summary>buffer を指定します。</summary>
    public BufferedAttackPort(PreallocatedCommandBuffer<AttackCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool EmitBullet(AttackId attackId, float x, float y) => _buffer.TryAdd(new AttackCommand(AttackCommandKind.EmitBullet, attackId, x, y), CommandPriority.Attack);
    /// <inheritdoc />
    public bool CreateHitbox(AttackId attackId, float x, float y) => _buffer.TryAdd(new AttackCommand(AttackCommandKind.CreateHitbox, attackId, x, y), CommandPriority.Safety);
    /// <inheritdoc />
    public bool ClearBossBullets() => _buffer.TryAdd(new AttackCommand(AttackCommandKind.ClearBossBullets), CommandPriority.Safety);
}

/// <summary>演出 command buffer を演出 port として公開します。</summary>
public sealed class BufferedEffectPort : IEffectPort
{
    private readonly PreallocatedCommandBuffer<EffectCommand> _buffer;
    /// <summary>buffer を指定します。</summary>
    public BufferedEffectPort(PreallocatedCommandBuffer<EffectCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool RequestTelegraph(float x, float y) => _buffer.TryAdd(new EffectCommand(EffectCommandKind.RequestTelegraph, x, y));
}