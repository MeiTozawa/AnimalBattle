using AnimalBattle.Core.Physics;
using AnimalBattle.Maps.Navigation;
using ErrorOr;

namespace AnimalBattle.Ai.Ports;

/// <summary>
/// Boss2 の移動 command、重力、足場衝突、navigation の ECS 境界です。
/// State_Module はこの型の物理実装を呼び出さず、IMovementPort の値 command だけを積みます。
/// </summary>
/// <param name="buffer">Entity 用に事前確保された移動 command buffer。</param>
public sealed class Boss2MovementPort(PreallocatedCommandBuffer<MovementCommand> buffer) : IMovementPort
{
    private readonly PreallocatedCommandBuffer<MovementCommand> _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

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
        return _buffer.Add(new MovementCommand(MovementCommandKind.ApplyGravity, gravity, maxFallSpeed), CommandPriority.Normal);
    }

    /// <inheritdoc />
    public ErrorOr<Success> Stop()
    {
        return _buffer.Add(new MovementCommand(MovementCommandKind.Stop, 0.0f, 0.0f), CommandPriority.Safety);
    }

    /// <summary>検証済み navigation graph から Boss2 の次の移動計画を取得します。</summary>
    /// <param name="graph">ECS 境界で取得した navigation graph。</param>
    /// <param name="request">Boss とプレイヤーの位置・速度観測。</param>
    /// <param name="plan">計算された navigation 計画。</param>
    /// <returns>計画を取得できた場合は true。</returns>
    public bool CreateNavigationPlan(
        PlatformNavigationGraph graph,
        in Boss2NavigationRequest request,
        out NavPlan plan)
    {
        ArgumentNullException.ThrowIfNull(graph);
        plan = null!;
        if (!IsFinite(request.CurrentX)
            || !IsFinite(request.CurrentFootY)
            || !IsFinite(request.TargetX)
            || !IsFinite(request.TargetFootY)
            || !IsFinite(request.MoveSpeed)
            || request.MoveSpeed < 0.0f)
        {
            return false;
        }

        try
        {
            plan = graph.FindPath(
                request.CurrentX,
                request.CurrentFootY,
                request.TargetX,
                request.TargetFootY,
                request.IsGrounded,
                request.MoveSpeed);
            return plan is not null;
        }
        catch
        {
            plan = null!;
            return false;
        }
    }

    /// <summary>
    /// command buffer の重力・速度・足場衝突 command を ECS 境界で適用します。
    /// </summary>
    /// <param name="frame">適用対象の Boss2 運動データ。</param>
    /// <param name="deltaTime">経過時間（秒）。</param>
    /// <param name="graph">足場 graph。未接続時は null を指定します。</param>
    /// <param name="floorTopY">最低床の上面 Y 座標。</param>
    /// <param name="minimumX">移動可能な X の最小値。</param>
    /// <param name="maximumX">移動可能な X の最大値。</param>
    /// <returns>command がすべて検証済みで適用できた場合は true。</returns>
    public bool ApplyPhysics(
        ref Boss2MovementFrame frame,
        float deltaTime,
        PlatformNavigationGraph? graph,
        float floorTopY,
        float minimumX,
        float maximumX)
    {
        if (!IsFinite(frame.Position)
            || !IsFinite(frame.Velocity)
            || !IsFinite(frame.Width)
            || !IsFinite(frame.Height)
            || !IsFinite(deltaTime)
            || !IsFinite(floorTopY)
            || !IsFinite(minimumX)
            || !IsFinite(maximumX)
            || deltaTime < 0.0f
            || frame.Width <= 0.0f
            || frame.Height <= 0.0f
            || minimumX > maximumX)
        {
            return false;
        }

        float previousY = frame.Position.Y;
        bool hasPositionCommand = false;
        for (int index = 0; index < _buffer.Count; index++)
        {
            MovementCommand command = _buffer[index];
            switch (command.Kind)
            {
                case MovementCommandKind.SetVelocity:
                    frame.Velocity = new(command.X, command.Y);
                    break;
                case MovementCommandKind.MoveTo:
                    frame.Position = new(command.X, command.Y);
                    hasPositionCommand = true;
                    break;
                case MovementCommandKind.ApplyGravity:
                    float verticalVelocity = frame.Velocity.Y;
                    PhysicsService.ApplyGravity(
                        ref verticalVelocity,
                        command.X,
                        command.Y,
                        deltaTime);
                    frame.Velocity = new(frame.Velocity.X, verticalVelocity);
                    break;
                case MovementCommandKind.Stop:
                    frame.Velocity = new(0.0f, 0.0f);
                    break;
                default:
                    return false;
            }
        }

        if (!hasPositionCommand)
        {
            frame.Position = new(
                frame.Position.X + frame.Velocity.X * deltaTime,
                frame.Position.Y + frame.Velocity.Y * deltaTime);
        }

        frame.Position = new(
            Math.Clamp(frame.Position.X, minimumX, maximumX),
            frame.Position.Y);
        ResolvePlatforms(ref frame, previousY, graph, floorTopY);
        return IsFinite(frame.Position) && IsFinite(frame.Velocity);
    }

    private static void ResolvePlatforms(
        ref Boss2MovementFrame frame,
        float previousY,
        PlatformNavigationGraph? graph,
        float floorTopY)
    {
        float nextY = frame.Position.Y;
        float adjustedY = nextY;
        float adjustedVelocityY = frame.Velocity.Y;
        bool grounded = false;
        Box2D bossBox = new(frame.Position, new(frame.Width, frame.Height));

        if (graph is not null)
        {
            foreach (PlatformNode platform in graph.Nodes)
            {
                Box2D platformBox = new(
                    new(platform.CenterX, platform.TopY + platform.Height * 0.5f),
                    new(platform.Width, platform.Height));
                if (PhysicsService.CheckPlatformGround(
                        previousY,
                        nextY,
                        bossBox,
                        platformBox,
                        out float platformY,
                        out float platformVelocityY))
                {
                    grounded = true;
                    adjustedY = platformY;
                    adjustedVelocityY = platformVelocityY;
                }
            }
        }

        float floorLimitY = floorTopY - frame.Height * 0.5f;
        if (adjustedY >= floorLimitY)
        {
            grounded = true;
            adjustedY = floorLimitY;
            adjustedVelocityY = 0.0f;
        }

        frame.Position = new(frame.Position.X, adjustedY);
        frame.Velocity = new(frame.Velocity.X, adjustedVelocityY);
        frame.IsGrounded = grounded;
    }

    private static bool IsFinite(Vec2f value)
    {
        return IsFinite(value.X) && IsFinite(value.Y);
    }

    private static bool IsFinite(float value)
    {
        return float.IsFinite(value);
    }
}
