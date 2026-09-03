using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の死亡理由と終端 data を保持し、新規攻撃と移動を停止する State_Module です。</summary>
public sealed class Boss2DeadModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState HandledState => Boss2State.Dead;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "AttackPort"];

    /// <summary>共有実行値を注入して Dead module を生成します。</summary>
    public Boss2DeadModule(Boss2BehaviorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.IsDead = true;
        _state.IsAttackActive = false;
        _state.IsInvulnerable = false;
        TransitionReason reason = _state.TerminalData.IsTerminal
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
            Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Boss2 の死亡停止処理に失敗しました。", stage: AttackStage.None);
        }

        Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "Boss2 は Dead 終端状態へ移行しました。", stage: AttackStage.None);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_state.IsDead)
        {
            return;
        }

        _state.IsAttackActive = false;
        _state.TerminalData.AttackGenerationStopped = true;
        _state.TerminalData.MovementStopped = !context.Movement.Stop().IsError;
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        // Dead からの通常状態への遷移は TransitionService が拒否します。
    }
}
