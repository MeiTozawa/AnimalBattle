using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の選択済み攻撃を共通 Telegraph lifecycle へ渡す State_Module です。</summary>
public sealed class Boss1TelegraphModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration;
    private readonly Boss1BehaviorState _state;
    private readonly Dictionary<BossSkill, TelegraphModule> _telegraphs = new();
    private TelegraphModule? _activeTelegraph;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.Telegraph;
    /// <summary>現在 Telegraph が完了しているかどうかを取得します。</summary>
    public bool IsComplete => _activeTelegraph is not null && _activeTelegraph.IsComplete;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "EffectPort", "MovementPort", "ObservationService"];

    /// <summary>Boss1 の攻撃 pattern と共有実行値を注入します。</summary>
    public Boss1TelegraphModule(IBoss1Configuration configuration, Boss1BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        BossSkill[] boss1Attacks = [BossSkill.Aimed3Shot, BossSkill.RingBurst, BossSkill.DoubleSpiral, BossSkill.RainDrop, BossSkill.CrossChaos, BossSkill.FastSniper, BossSkill.DiveRam];
        foreach (BossSkill skill in boss1Attacks)
        {
            AttackDefinition definition = configuration.GetAttack(skill, out Boss1AttackPattern pattern)
                ? pattern.Definition
                : new AttackDefinition(skill, 0, 0, 0, 0);
            _telegraphs[skill] = new TelegraphModule(Boss1StateIds.Telegraph, definition);
        }
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph = null;
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation)
            || !_state.CurrentSkill.IsValid()
            || !_telegraphs.TryGetValue(_state.CurrentSkill, out _activeTelegraph))
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の Telegraph 対象が不正です。", _state.CurrentSkill);
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new Core.Physics.Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _activeTelegraph.Enter(ref context, in invocation);
        if (!_activeTelegraph.IsActive && !_activeTelegraph.IsComplete)
        {
            _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidConfiguration));
        }
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_activeTelegraph is null || !_activeTelegraph.IsActive)
        {
            return;
        }

        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            _state.AttackTargetPosition = new Core.Physics.Vec2f(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
            if (context.Phase is Phase.Phase1 or Phase.Phase2 or Phase.Phase3)
            {
                Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
                float shake = settings.TelegraphShakeIntensity;
                float offsetX = (Random.Shared.NextSingle() - 0.5f) * shake;
                float offsetY = (Random.Shared.NextSingle() - 0.5f) * shake;
                float sx = _state.AttackStartPosition.X + offsetX;
                float sy = _state.AttackStartPosition.Y + offsetY;
                _state.CurrentPosition = new Core.Physics.Vec2f(sx, sy);
                if (context.Movement.MoveTo(sx, sy).IsError)
                {
                    Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "DiveRam の予兆振動移動 command を発行できませんでした。", _state.CurrentSkill, AttackStage.Telegraph);
                }
            }
        }
        else
        {
            Boss1ModuleSupport.ApplyHoverMovement(ref context, in invocation, _configuration, _state, _state.CurrentSkill, AttackStage.Telegraph);
            _state.AttackStartPosition = _state.CurrentPosition;
        }

        _activeTelegraph.Update(ref context, in invocation);
        if (_activeTelegraph.IsComplete)
        {
            BossState executeState = Boss1StateIds.GetExecuteState(_state.CurrentSkill);
            if (!executeState.IsValid())
            {
                _state.RequestTransition(Boss1StateIds.Hover, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
                return;
            }


            _state.RequestTransition(executeState, new TransitionReason(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _activeTelegraph?.Exit(ref context, in invocation);
        _activeTelegraph = null;
    }

}
