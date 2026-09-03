using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の着地衝撃後の短い硬直を処理する State_Module です。</summary>
public sealed class Boss2LandingRecoveryModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state;
    private readonly IBoss2Configuration? _configuration;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.LandingRecovery;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "EffectPort", "Boss2Configuration"];

    /// <summary>共有実行値および設定を注入して着地 recovery module を生成します。</summary>
    public Boss2LandingRecoveryModule(Boss2BehaviorState state, IBoss2Configuration? configuration = null)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _configuration = configuration;
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        context.Movement.Stop();
        context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        context.Movement.Stop();
        _state.StateElapsed += context.DeltaTime;
        float recoveryDuration = _configuration?.GetPhase(context.Phase).LandingRecoveryDuration ?? 0.10f;
        if (_state.StateElapsed >= recoveryDuration)
        {
            _state.RequestTransition(Boss2StateIds.PatrolChase, new TransitionReason(ReasonKind.TimerElapsed));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
