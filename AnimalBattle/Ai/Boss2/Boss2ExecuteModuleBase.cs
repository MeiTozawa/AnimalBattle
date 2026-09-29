using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の Execute module に共通する lifecycle 実装です。</summary>
/// <remarks>State_Id、Boss2 設定、共有実行値を注入します。</remarks>
public abstract class Boss2ExecuteModuleBase(
    BossState stateId,
    IBoss2Configuration configuration,
    Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider {
    /// <summary>派生 module が参照する Boss2 共有実行値です。</summary>
    protected Boss2BehaviorState State { get; } = state ?? throw new ArgumentNullException(nameof(state));

    /// <summary>派生 module が参照する Boss2 設定です。</summary>
    protected IBoss2Configuration Configuration { get; } =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    /// <summary>現在の共通 Execute gate です。</summary>
    protected ExecuteGate? Gate { get; private set; }

    /// <summary>Execute 担当状態です。</summary>
    public BossState HandledState { get; } = stateId;

    /// <summary>Execute 状態へ入場します。</summary>
    public virtual void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss2ModuleSupport.GetPattern(Configuration, State.CurrentSkill, in context, in invocation,
            out var pattern))
        {
            Gate?.Reset();
            State.RequestTransition(Boss2State.Idle,
                new TransitionReason(ReasonKind.SafetyFallback, skill: State.CurrentSkill,
                    errorCode: ErrorCode.InvalidBossSkill));
            return;
        }

        var forwardDir = State.FacingRight ? 1.0f : -1.0f;
        if (Gate is null)
            Gate = new ExecuteGate(pattern.Definition, forwardDir);
        else
            Gate.Reset(pattern.Definition, forwardDir);

        State.StateElapsed = 0.0f;
        State.AttackSubTimer = 0.0f;
        State.IsAttackActive = true;
        State.IsGrounded = false;
        Gate.Start(ref context, in invocation);
    }

    /// <summary>Execute 状態を一回更新します。</summary>
    public virtual void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (Gate is null) return;

        var result = Gate.Execute(
            ref context,
            in invocation,
            State.IsGrounded,
            State.CurrentPosition.X,
            State.CurrentPosition.Y);
        State.StateElapsed = Gate.Elapsed;
        if (result.IsComplete)
        {
            State.IsAttackActive = false;
            State.RequestTransition(Boss2State.Recovery,
                new TransitionReason(ReasonKind.AttackCompleted, skill: State.CurrentSkill));
        }
    }


    /// <summary>Execute 状態から退場します。</summary>
    public virtual void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        Gate?.Reset();
        State.StateElapsed = 0.0f;
    }

    /// <summary>Execute 状態が必要とする依存関係です。</summary>
    public IReadOnlyList<string> RequiredDependencies { get; } =
        ["Boss2Configuration", "MovementPort", "AttackPort", "ObservationService"];
}