using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の Phase 攻撃順から次の弾幕または DiveRam を選択する State_Module です。</summary>
/// <remarks>Boss1 の設定と Entity 実行値を注入して攻撃選択 module を生成します。</remarks>
public sealed class Boss1AttackSelectionModule(IBoss1Configuration configuration, Boss1BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss1State.AttackSelection;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss1Configuration", "PlayerObservation"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss1ModuleSupport.IsBoss1(in context, in invocation) || _state.IsDead)
        {
            return;
        }

        if (context.Phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.SafetyFallback));
            return;
        }

        Boss1PhaseSettings settings = _configuration.GetPhase(context.Phase);
        if (settings.AttackOrder.Count == 0)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "Boss1 の攻撃順が空のため Hover へ復帰します。");
            _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.SafetyFallback, errorCode: ErrorCode.InvalidConfiguration));
            return;
        }

        int nextIndex = (_state.CurrentAttackIndex + 1) % settings.AttackOrder.Count;
        _state.CurrentAttackIndex = nextIndex;
        BossSkill skill = settings.AttackOrder[nextIndex];
        if (!_configuration.GetAttack(skill, out Boss1AttackPattern pattern) || !pattern.IsValid)
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidBossSkill, "Boss1 の攻撃設定が不正なため Hover へ復帰します。", skill);
            _state.RequestTransition(Boss1State.Hover, new(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }


        _state.CurrentSkill = skill;
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.AttackSubTimer = 0.0f;
        _state.AngleOffset = 0.0f;
        _state.IsAttackActive = true;
        _state.RequestTransition(
            Boss1State.Telegraph,
            new(ReasonKind.PlayerObservation, skill: skill, originalInput: context.PlayerObservation.EntityId > long.MaxValue ? long.MaxValue : (long)context.PlayerObservation.EntityId));

    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        // 攻撃選択は Enter で完了し、Recovery 後まで再評価しません。
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
    }
}
