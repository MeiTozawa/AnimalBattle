using ErrorOr;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// Boss1 用の移動 command port です。
/// </summary>
/// <remarks>Boss1 の移動 command port を生成します。</remarks>
/// <param name="buffer">Entity 用に事前確保された移動 command buffer。</param>
public sealed class Boss1MovementPort(
    PreallocatedCommandBuffer<MovementCommand> buffer) : IMovementPort
{
    private readonly BufferedMovementPort _buffered = new(buffer);

    /// <inheritdoc />
    public ErrorOr<Success> SetVelocity(float x, float y)
    {
        return _buffered.SetVelocity(x, y);
    }

    /// <inheritdoc />
    public ErrorOr<Success> MoveTo(float x, float y)
    {
        return _buffered.MoveTo(x, y);
    }

    /// <inheritdoc />
    public ErrorOr<Success> ApplyGravity(float gravity, float maxFallSpeed)
    {
        return _buffered.ApplyGravity(gravity, maxFallSpeed);
    }

    /// <inheritdoc />
    public ErrorOr<Success> Stop()
    {
        return _buffered.Stop();
    }
}
