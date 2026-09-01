using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
/// 宣言的 Boss 振る舞いパイプライン（Fluent API）の定義エントリーポイントです。
/// </summary>
public static class BossPipeline
{
    /// <summary>
    /// 指定された Boss 種別の新しい宣言的パイプラインの構築を開始します。
    /// </summary>
    /// <param name="bossType">Boss 種別。</param>
    /// <param name="name">パイプラインの名前（企画・デバッグ用識別名）。</param>
    /// <returns>Boss パイプラインビルダー。</returns>
    public static BossPipelineBuilder Define(BossType bossType, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new BossPipelineBuilder(bossType, name);
    }
}
