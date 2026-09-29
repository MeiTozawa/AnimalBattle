using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2 の全近接・突進・跳躍攻撃の Execute ライフサイクルを一元処理する実行モジュールです。
/// </summary>
public sealed class Boss2ExecuteModule : Boss2ExecuteModuleBase {
    /// <summary>
    ///     Boss2 Execute モジュールを初期化します。
    /// </summary>
    /// <param name="configuration">Boss2 攻撃設定。</param>
    /// <param name="state">Boss2 実行時状態。</param>
    public Boss2ExecuteModule(IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(Boss2State.Execute, configuration, state)
    {
    }

    /// <summary>
    ///     状態識別値を明示指定して Boss2 Execute モジュールを初期化します。
    /// </summary>
    /// <param name="handledState">担当状態。</param>
    /// <param name="configuration">Boss2 攻撃設定。</param>
    /// <param name="state">Boss2 実行時状態。</param>
    public Boss2ExecuteModule(BossState handledState, IBoss2Configuration configuration, Boss2BehaviorState state)
        : base(handledState, configuration, state)
    {
    }
}