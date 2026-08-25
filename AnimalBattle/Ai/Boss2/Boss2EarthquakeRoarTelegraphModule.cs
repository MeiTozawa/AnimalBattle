using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Boss2;

/// <summary>EarthquakeRoar の Telegraph State_Module です。</summary>
public sealed class Boss2EarthquakeRoarTelegraphModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly Boss2TelegraphRunner _runner;
    /// <inheritdoc />
    public BossState Id => Boss2StateIds.EarthquakeRoarTelegraph;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "EffectPort", "ObservationService"];

    /// <summary>Boss2 攻撃設定と共有実行値を注入します。</summary>
    public Boss2EarthquakeRoarTelegraphModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _runner = new Boss2TelegraphRunner(Id, configuration ?? throw new ArgumentNullException(nameof(configuration)), state ?? throw new ArgumentNullException(nameof(state)));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation) => _runner.Enter(ref context, in invocation);
    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation) => _runner.Update(ref context, in invocation);
    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation) => _runner.Exit(ref context, in invocation);
}
