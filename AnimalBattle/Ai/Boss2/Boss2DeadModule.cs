using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の死亡終端状態を保持し、新規攻撃と移動を停止する State_Module です。</summary>
public sealed class Boss2DeadModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.Dead;
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
        context.Movement.Stop();
        context.Attack.ClearBossBullets();
        Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "Boss2 は Dead 終端状態へ移行しました。");
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            context.Movement.Stop();
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        // Dead からの通常状態への遷移は TransitionService が拒否します。
    }
}
