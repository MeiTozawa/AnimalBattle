using ErrorOr;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// Boss1 用の移動 command port です。
/// 軌道計算は <see cref="Boss1NativeTrajectoryPort" /> に隔離し、State_Module には command buffer だけを公開します。
/// </summary>
/// <remarks>Boss1 の移動 command port を生成します。</remarks>
/// <param name="buffer">Entity 用に事前確保された移動 command buffer。</param>
/// <param name="nativeTrajectory">native 軌道計算境界。省略時は新規生成します。</param>
public sealed class Boss1MovementPort(
    PreallocatedCommandBuffer<MovementCommand> buffer,
    Boss1NativeTrajectoryPort? nativeTrajectory = null) : IMovementPort
{
    private readonly BufferedMovementPort _buffered = new(buffer);

    /// <summary>native extension の軌道計算境界を取得します。</summary>
    public Boss1NativeTrajectoryPort NativeTrajectory { get; } = nativeTrajectory ?? new();

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
