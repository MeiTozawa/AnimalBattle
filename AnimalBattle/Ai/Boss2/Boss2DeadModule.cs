using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2 の死亡理由と終端 data を保持し、新規攻撃と移動を停止する State_Module です。
/// </summary>
/// <param name="state">Boss2 共有実行時状態。</param>
public sealed class Boss2DeadModule(Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider {
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.Dead;

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.IsDead = true;
        _state.IsAttackActive = false;
        _state.IsInvulnerable = false;
        var reason = _state.TerminalData.IsTerminal
            ? _state.TerminalData.DeathReason
            : PhaseDeathController.CreateDeathReason(0.0f);
        _state.TerminalData.Capture(
            Boss2State.Dead,
            context.Phase,
            in reason,
            invocation.FrameUpdateNumber,
            _state.CurrentSkill,
            AttackStage.None,
            0.0f);


        try
        {
            _state.TerminalData.MovementStopped = !context.Movement.Stop().IsError;
            _state.TerminalData.AttackGenerationStopped = true;
            context.Attack.ClearBossBullets();
        }
        catch
        {
            _state.TerminalData.AttackGenerationStopped = true;
            Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError,
                ErrorCode.ExecutionFailed, "Boss2 の死亡停止処理に失敗しました。", stage: AttackStage.None);
        }

        Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None,
            "Boss2 は Dead 終端状態へ移行しました。", stage: AttackStage.None);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_state.IsDead) return;

        _state.IsAttackActive = false;
        _state.TerminalData.AttackGenerationStopped = true;
        _state.TerminalData.MovementStopped = !context.Movement.Stop().IsError;
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        // Dead からの通常状態への遷移は TransitionService が拒否します。
    }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "AttackPort"];
}