using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2 の攻撃予兆（Telegraph）状態モジュールです。
///     攻撃前予兆・予備動作および目標方向への指向を処理します。
/// </summary>
public sealed class Boss2TelegraphModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider {
    private readonly IBoss2Configuration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private TelegraphModule? _activeTelegraph;

    /// <inheritdoc />
    public BossState HandledState => Boss2State.Telegraph;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.AttackTargetPosition =
            new Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.FacingRight = context.PlayerObservation.PositionX >= _state.CurrentPosition.X;
        _state.StateElapsed = 0.0f;

        var skill = _state.CurrentSkill;
        if (!skill.IsValid())
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss2State.Idle,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        if (!_configuration.GetAttack(skill, out var pattern))
        {
            _activeTelegraph?.Exit(ref context, in invocation);
            _state.RequestTransition(Boss2State.Idle,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill,
                    errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        if (_activeTelegraph is null)
            _activeTelegraph = new TelegraphModule(Boss2State.Telegraph, pattern.Definition);
        else
            _activeTelegraph.Reset(Boss2State.Telegraph, pattern.Definition);

        _activeTelegraph.Enter(ref context, in invocation);

        if (!_activeTelegraph.IsActive && !_activeTelegraph.IsComplete)
            _state.RequestTransition(Boss2State.Idle,
                new TransitionReason(ReasonKind.SafetyFallback, skill: skill,
                    errorCode: ErrorCode.InvalidConfiguration));
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeTelegraph is null || !_activeTelegraph.IsActive) return;

        _activeTelegraph.Update(ref context, in invocation);
        _state.StateElapsed = _activeTelegraph.Elapsed;

        if (_activeTelegraph.IsComplete)
            _state.RequestTransition(Boss2State.Execute,
                new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph?.Exit(ref context, in invocation);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } =
        ["Boss2Configuration", "EffectPort", "MovementPort"];
}