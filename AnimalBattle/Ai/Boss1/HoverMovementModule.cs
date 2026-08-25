using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の Phase 別浮遊移動を port command へ変換する State_Module です。</summary>
public sealed class HoverMovementModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.Hover;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort"];

    /// <summary>Boss1 の設定と Entity 実行値を注入して Hover module を生成します。</summary>
    public HoverMovementModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.ResetStateTimer();
        _state.IsInvulnerable = false;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead || context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return;
        }

        Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
        float deltaTime = context.DeltaTime;
        _state.StateElapsed += deltaTime;
        _state.HoverTimer += deltaTime;

        float x = _state.BaseX
            + MathF.Sin(_state.HoverTimer * settings.HoverSpeedX) * settings.HoverAmpX;
        x += (context.PlayerObservation.PositionX - _state.BaseX) * settings.PlayerPursuitWeight;
        float y = settings.HoverBaseY
            + MathF.Sin(_state.HoverTimer * settings.HoverSpeedY) * settings.HoverAmpY;

        if (!context.Movement.MoveTo(x, y))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Boss1 の浮遊移動 command を発行できませんでした。");
        }

        if (_state.StateElapsed >= settings.HoverDuration && context.PlayerObservation.IsAlive)
        {
            _state.RequestTransition(
                Boss1StateIds.AttackSelection,
                new TransitionReason(ReasonKind.TimerElapsed, attackId: _state.CurrentAttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.ResetStateTimer();
    }
}
