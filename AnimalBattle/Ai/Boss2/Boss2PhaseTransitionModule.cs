using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 の PhaseTransition と無敵・停止・弾整理・演出を項目別に処理する State_Module です。
/// </summary>
/// <param name="configuration">Boss2 設定境界。</param>
/// <param name="state">Boss2 共有実行時状態。</param>
public sealed class Boss2PhaseTransitionModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.PhaseTransition;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "AttackPort", "EffectPort"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
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
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        _state.StateElapsed = MathF.Min(settings.TransitionDuration, _state.StateElapsed + context.DeltaTime);
        if (_state.StateElapsed >= settings.TransitionDuration)
        {
            _state.PhaseTransitionData.IsActive = false;
            _state.IsInvulnerable = false;
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.PhaseThreshold, threshold: 0));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.PhaseTransitionData.IsActive = false;
        _state.IsInvulnerable = false;
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

        if (!data.AppliedMask.HasFlag(PhaseTransitionApplyMask.BulletCleanup))
        {
            if (!context.Attack.ClearBossBullets().IsError)
            {
                data.AppliedMask |= PhaseTransitionApplyMask.BulletCleanup;
            }
            else
            {
                RecordFailure(ref context, in invocation, PhaseTransitionApplyMask.BulletCleanup, "PhaseTransition の弾整理に失敗しました。");
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
        Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, message);
    }
}
