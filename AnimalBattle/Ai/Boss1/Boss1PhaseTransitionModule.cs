using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の PhaseTransition と項目別安全適用を処理する State_Module です。</summary>
public sealed class Boss1PhaseTransitionModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private float _elapsed;

    /// <inheritdoc />
    public StateId Id => Boss1StateIds.PhaseTransition;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort", "AttackPort", "EffectPort"];

    /// <summary>Boss1 の設定と共有実行値を注入します。</summary>
    public Boss1PhaseTransitionModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _state.StateElapsed = 0.0f;
        _state.IsInvulnerable = true;

        if (!context.Movement.Stop())
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "PhaseTransition の移動停止に失敗しました。");
        }

        if (!context.Attack.ClearBossBullets())
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "PhaseTransition の弾整理に失敗しました。");
        }

        if (!context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "PhaseTransition の演出 command を発行できませんでした。");
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead || context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return;
        }

        Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
        _elapsed = MathF.Min(settings.TransitionDuration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        if (!context.Movement.MoveTo(_state.BaseX, settings.HoverBaseY))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "PhaseTransition の帰還 command を発行できませんでした。");
        }

        if (_elapsed >= settings.TransitionDuration)
        {
            _state.IsInvulnerable = false;
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.PhaseThreshold, threshold: 0));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
    }
}
