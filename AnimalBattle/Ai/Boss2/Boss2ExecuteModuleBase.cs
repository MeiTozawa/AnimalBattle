using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の Execute module に共通する lifecycle 実装です。</summary>
public abstract class Boss2ExecuteModuleBase : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;
    private ExecuteGate? _gate;

    /// <summary>State_Id、Boss2 設定、共有実行値を注入します。</summary>
    protected Boss2ExecuteModuleBase(BossState stateId, IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        HandledState = stateId;
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <summary>Execute 担当状態です。</summary>
    public BossState HandledState { get; }
    /// <summary>Execute 状態が必要とする依存関係です。</summary>
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "AttackPort", "ObservationService"];
    /// <summary>派生 module が参照する Boss2 共有実行値です。</summary>
    protected Boss2BehaviorState State => _state;
    /// <summary>派生 module が参照する Boss2 設定です。</summary>
    protected IBoss2Configuration Configuration => _configuration;
    /// <summary>現在の共通 Execute gate です。</summary>
    protected ExecuteGate? Gate => _gate;

    /// <summary>Execute 状態へ入場します。</summary>
    public virtual void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate = null;
        if (!Boss2ModuleSupport.GetPattern(_configuration, _state.CurrentSkill, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.SafetyFallback, skill: _state.CurrentSkill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }


        float forwardDir = _state.FacingRight ? 1.0f : -1.0f;
        _gate = new ExecuteGate(pattern.Definition, forwardDir);
        _state.StateElapsed = 0.0f;
        _state.AttackSubTimer = 0.0f;
        _state.IsAttackActive = true;
        _gate.Start(ref context, in invocation);
    }

    /// <summary>Execute 状態を一回更新します。</summary>
    public virtual void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_gate is null)
        {
            return;
        }

        ExecuteResult result = _gate.Execute(
            ref context,
            in invocation,
            _state.IsGrounded,
            _state.CurrentPosition.X,
            _state.CurrentPosition.Y);
        _state.StateElapsed = _gate.Elapsed;
        if (result.IsComplete)
        {
            _state.IsAttackActive = false;
            _state.RequestTransition(Boss2State.Recovery, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
        }
    }


    /// <summary>Execute 状態から退場します。</summary>
    public virtual void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _gate = null;
        _state.StateElapsed = 0.0f;
    }
}
