using ErrorOr;

namespace AnimalBattle.Ai.Ports;

/// <summary>
///     Boss2 の移動 command の ECS 境界です。
///     State_Module には IMovementPort の値 command だけを積みます。
/// </summary>
/// <param name="buffer">Entity 用に事前確保された移動 command buffer。</param>
public sealed class Boss2MovementPort(PreallocatedCommandBuffer<MovementCommand> buffer) : IMovementPort {
    private readonly PreallocatedCommandBuffer<MovementCommand> _buffer =
        buffer ?? throw new ArgumentNullException(nameof(buffer));

    /// <inheritdoc />
    public ErrorOr<Success> SetVelocity(float x, float y)
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.SetVelocity, x, y), CommandPriority.Safety);
    }

    /// <inheritdoc />
    public ErrorOr<Success> MoveTo(float x, float y)
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.MoveTo, x, y), CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> ApplyGravity(float gravity, float maxFallSpeed)
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.ApplyGravity, gravity, maxFallSpeed),
            CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> Stop()
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.Stop, 0.0f, 0.0f), CommandPriority.Safety);
    }
}