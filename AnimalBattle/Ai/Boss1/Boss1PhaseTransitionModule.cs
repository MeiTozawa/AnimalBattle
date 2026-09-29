using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 の PhaseTransition と項目別安全適用を処理する State_Module です。
/// </summary>
/// <param name="configuration">Boss1 設定境界。</param>
/// <param name="state">Boss1 共有実行時状態。</param>
public sealed class Boss1PhaseTransitionModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private float _elapsed;

    /// <inheritdoc />
    public BossState HandledState => Boss1State.PhaseTransition;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "MovementPort", "EffectPort"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _state.StateElapsed = 0.0f;
        _state.PhaseTransitionData = new(
            context.Phase,
            new(ReasonKind.PhaseThreshold));
        _state.PhaseTransitionData.IsActive = true;
        ApplyItems(ref context, in invocation);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        ApplyItems(ref context, in invocation);
        Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
        _elapsed = MathF.Min(settings.TransitionDuration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        Vec2f curPos = _state.CurrentPosition;
        Vec2f targetBase = new(_state.BaseX, settings.HoverBaseY);
        Vec2f newPos = PhysicsService.CalculateBossReturnPosition(
            curPos,
            targetBase,
            settings.TransitionFloatSpeed,
            context.DeltaTime);
        _state.CurrentPosition = newPos;
        if (context.Movement.MoveTo(newPos.X, newPos.Y).IsError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "PhaseTransition の帰還 command を発行できませんでした。");
        }

        if (_elapsed >= settings.TransitionDuration && !_state.IsDead)
        {
            _state.PhaseTransitionData.IsActive = false;
            _state.IsInvulnerable = false;
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.PhaseThreshold, threshold: 0));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _state.PhaseTransitionData.IsActive = false;
    }

    private void ApplyItems(ref StateContext context, in LifecycleInvocation invocation)
    {
        ref BossPhaseTransitionData data = ref _state.PhaseTransitionData;
        _state.IsInvulnerable = true;
        data.AppliedMask |= PhaseTransitionApplyMask.Invulnerability;

        if (!data.AppliedMask.HasFlag(PhaseTransitionApplyMask.MovementStop))
        {
            if (!context.Movement.Stop().IsError)
            {
                data.AppliedMask |= PhaseTransitionApplyMask.MovementStop;
            }
            else
            {
                RecordFailure(ref context, in invocation, PhaseTransitionApplyMask.MovementStop, "PhaseTransition の移動停止に失敗しました。");
            }
        }

        if (!data.AppliedMask.HasFlag(PhaseTransitionApplyMask.Effect))
        {
            if (!context.Effect.RequestTelegraph(_state.CurrentPosition.X, _state.CurrentPosition.Y).IsError)
            {
                data.AppliedMask |= PhaseTransitionApplyMask.Effect;
            }
            else
            {
                RecordFailure(ref context, in invocation, PhaseTransitionApplyMask.Effect, "PhaseTransition の演出 command を発行できませんでした。");
            }
        }
    }

    private void RecordFailure(ref StateContext context, in LifecycleInvocation invocation, PhaseTransitionApplyMask item, string message)
    {
        if (_state.PhaseTransitionData.FailedMask.HasFlag(item))
        {
            return;
        }

        _state.PhaseTransitionData.FailedMask |= item;
        Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, message);
    }

}
