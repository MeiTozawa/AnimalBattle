using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃予兆（Telegraph）状態モジュールです。
/// </summary>
public sealed class DslTelegraphModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly BossState _stateId;
    private readonly Boss2TelegraphRunner _runner;

    /// <inheritdoc />
    public BossState Id => _stateId;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "EffectPort", "ObservationService"];

    /// <summary>
    /// 予兆状態モジュールを初期化します。
    /// </summary>
    /// <param name="stateId">この予兆状態の識別子。</param>
    /// <param name="configuration">Boss2 攻撃設定。</param>
    /// <param name="state">Boss2 実行時状態。</param>
    public DslTelegraphModule(BossState stateId, IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(state);
        _stateId = stateId;
        _runner = new Boss2TelegraphRunner(stateId, configuration, state);
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation) => _runner.Enter(ref context, in invocation);

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation) => _runner.Update(ref context, in invocation);

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation) => _runner.Exit(ref context, in invocation);
}
