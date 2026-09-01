using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;

namespace AnimalBattle.Ai.Dsl.Modules;

/// <summary>
/// 宣言的パイプラインで使用される汎用の攻撃実行（ExecuteGate）状態モジュールです。
/// </summary>
public sealed class DslExecuteModule : Boss2ExecuteModuleBase
{
    /// <summary>
    /// 汎用 Execute モジュールを初期化します。
    /// </summary>
    /// <param name="stateId">この実行状態の識別子。</param>
    /// <param name="configuration">Boss2 攻撃設定。</param>
    /// <param name="state">Boss2 実行時状態。</param>
    public DslExecuteModule(BossState stateId, IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(stateId, configuration, state)
    {
    }
}
