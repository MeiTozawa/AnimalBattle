using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// FrenzyCharge の壁激突後に Boss2 を短時間停止する State_Module です。
/// </summary>
/// <param name="state">Boss2 共有実行時状態。</param>
public sealed class Boss2WallCrashStunModule(Boss2BehaviorState state) : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.WallCrashStun;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["MovementPort", "ObservationService"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = false;
        context.Movement.Stop();
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        context.Movement.Stop();
        _state.StateElapsed += context.DeltaTime;
        if (_state.StateElapsed >= ConfigManager.Boss2.WallCrashStunDuration)
        {
            _state.RequestTransition(Boss2State.Recovery, new(ReasonKind.TimerElapsed, skill: _state.CurrentSkill));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }
}
