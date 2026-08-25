using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の死亡終端状態を保持し、新規攻撃と移動を停止する State_Module です。</summary>
public sealed class Boss1DeadModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss1BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss1StateIds.Dead;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "AttackPort"];

    /// <summary>Boss1 の共有実行値を注入して Dead module を生成します。</summary>
    public Boss1DeadModule(Boss1BehaviorState state)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.IsDead = true;
        _state.IsAttackActive = false;
        _state.IsInvulnerable = false;
        try
        {
            context.Movement.Stop();
            context.Attack.ClearBossBullets();
        }
        catch
        {
            Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "Boss1 の死亡停止処理に失敗しました。", stage: AttackStage.None);
        }

        Boss1ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "Boss1 は Dead 終端状態へ移行しました。", stage: AttackStage.None);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_state.IsDead)
        {
            return;
        }

        context.Movement.Stop();
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        // Dead からの通常状態への遷移は TransitionService が拒否します。
    }
}
