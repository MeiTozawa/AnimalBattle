using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の PhaseTransition と無敵・停止・弾整理を処理する State_Module です。</summary>
public sealed class Boss2PhaseTransitionModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.PhaseTransition;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "AttackPort", "EffectPort"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2PhaseTransitionModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsInvulnerable = true;
        context.Movement.Stop();
        context.Attack.ClearBossBullets();
        context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead || context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return;
        }

        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        context.Movement.Stop();
        _state.StateElapsed = MathF.Min(settings.TransitionDuration, _state.StateElapsed + context.DeltaTime);
        if (_state.StateElapsed >= settings.TransitionDuration)
        {
            _state.IsInvulnerable = false;
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.PhaseThreshold));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsInvulnerable = false;
    }
}
