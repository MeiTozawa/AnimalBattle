using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の俯衝突進移動を Execute lifecycle として処理する State_Module です。</summary>
public sealed class DiveRamExecuteModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss1BehaviorState _state;
    private readonly Boss1AttackPattern _pattern;
    private readonly ExecuteGate _gate;
    private float _elapsed;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.DiveRamExecute;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "AttackPort", "MovementPort"];

    /// <summary>Boss1 の設定と Entity 実行値を注入します。</summary>
    public DiveRamExecuteModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _pattern = configuration.GetAttack(Boss1AttackIds.DiveRam, out Boss1AttackPattern pattern)
            ? pattern
            : new Boss1AttackPattern(Boss1AttackIds.DiveRam, Boss1AttackKind.DiveRam, 0, 0, 0);
        _gate = new ExecuteGate(_pattern.Definition);
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _elapsed = 0.0f;
        _state.StateElapsed = 0.0f;
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _gate.Reset();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead)
        {
            return;
        }

        ExecuteResult result = _gate.Execute(ref context, in invocation);
        if (!result.Started)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.SafetyFallback, attackId: _pattern.AttackId, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        float duration = _pattern.Definition.ExecuteDuration;
        float progress = duration > 0 ? _elapsed / duration : 1.0f;
        float eased = Boss1ModuleSupport.Ease(progress, _pattern.EaseType);
        Vec2f position = _state.AttackStartPosition
            + (_state.AttackTargetPosition - _state.AttackStartPosition) * eased;
        _state.CurrentPosition = position;
        if (context.Movement.MoveTo(position.X, position.Y).IsError)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam の移動 command を発行できませんでした。", _pattern.AttackId, AttackStage.Execute);
        }

        _elapsed = MathF.Min(duration, _elapsed + context.DeltaTime);
        _state.StateElapsed = _elapsed;
        if (result.IsComplete || _elapsed >= duration)
        {
            _state.RequestTransition(Boss1StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, attackId: _pattern.AttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate.Reset();
        _elapsed = 0.0f;
    }
}
