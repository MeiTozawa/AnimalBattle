using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// 初期化時に配列を一度だけ確保し、Frame_Update では再利用する command buffer です。
/// </summary>
public sealed class PreallocatedCommandBuffer<T> : ICommandBuffer<T> where T : struct
{
    private readonly T[] _commands;
    private int _count;

    /// <summary>command の現在数を取得します。</summary>
    public int Count => _count;

    /// <summary>command の最大格納数を取得します。</summary>
    public int Capacity => _commands.Length;

    /// <summary>指定容量の command buffer を初期化します。</summary>
    public PreallocatedCommandBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _commands = new T[capacity];
    }

    /// <summary>容量に空きがある場合だけ command を追加します。</summary>
    public bool TryAdd(in T command)
    {
        if (_count >= _commands.Length)
        {
            return false;
        }

        _commands[_count++] = command;
        return true;
    }

    /// <summary>次の Frame_Update に向けて command 数を 0 に戻します。</summary>
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
}

/// <summary>事前確保済みの移動 command buffer を移動 port として公開します。</summary>
public sealed class BufferedMovementPort : IMovementPort
{
    private readonly PreallocatedCommandBuffer<MovementCommand> _buffer;
    /// <summary>使用する command buffer を指定します。</summary>
    public BufferedMovementPort(PreallocatedCommandBuffer<MovementCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool SetVelocity(float x, float y) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.SetVelocity, x, y));
    /// <inheritdoc />
    public bool MoveTo(float x, float y) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.MoveTo, x, y));
    /// <inheritdoc />
    public bool ApplyGravity(float gravity, float maxFallSpeed) => _buffer.TryAdd(new MovementCommand(MovementCommandKind.ApplyGravity, gravity, maxFallSpeed));
    /// <inheritdoc />
    public bool Stop() => _buffer.TryAdd(new MovementCommand(MovementCommandKind.Stop, 0, 0));
}

/// <summary>事前確保済みの攻撃 command buffer を攻撃 port として公開します。</summary>
public sealed class BufferedAttackPort : IAttackPort
{
    private readonly PreallocatedCommandBuffer<AttackCommand> _buffer;
    /// <summary>使用する command buffer を指定します。</summary>
    public BufferedAttackPort(PreallocatedCommandBuffer<AttackCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool EmitBullet(AttackId attackId, float x, float y) => _buffer.TryAdd(new AttackCommand(AttackCommandKind.EmitBullet, attackId, x, y));
    /// <inheritdoc />
    public bool CreateHitbox(AttackId attackId, float x, float y) => _buffer.TryAdd(new AttackCommand(AttackCommandKind.CreateHitbox, attackId, x, y));
    /// <inheritdoc />
    public bool ClearBossBullets() => _buffer.TryAdd(new AttackCommand(AttackCommandKind.ClearBossBullets));
}

/// <summary>事前確保済みの演出 command buffer を演出 port として公開します。</summary>
public sealed class BufferedEffectPort : IEffectPort
{
    private readonly PreallocatedCommandBuffer<EffectCommand> _buffer;
    /// <summary>使用する command buffer を指定します。</summary>
    public BufferedEffectPort(PreallocatedCommandBuffer<EffectCommand> buffer) => _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    /// <inheritdoc />
    public bool RequestTelegraph(float x, float y) => _buffer.TryAdd(new EffectCommand(EffectCommandKind.RequestTelegraph, x, y));
}
