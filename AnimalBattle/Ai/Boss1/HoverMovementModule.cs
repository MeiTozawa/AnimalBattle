using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の Phase 別浮遊移動を port command へ変換する State_Module です。</summary>
/// <remarks>Boss1 の設定と Entity 実行値を注入して Hover module を生成します。</remarks>
public sealed class HoverMovementModule(IBoss1Configuration configuration, Boss1BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState Id => BossState.Boss1_Hover;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort"];

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
        _state.StateElapsed += context.DeltaTime;

        Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state);

        if (_state.StateElapsed >= settings.HoverDuration && context.PlayerObservation.IsAlive)
        {
            _state.RequestTransition(
                BossState.Boss1_AttackSelection,
                new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
        }

    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.ResetStateTimer();
    }
}
