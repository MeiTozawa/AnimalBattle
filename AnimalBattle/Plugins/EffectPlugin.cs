using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// フェード・ヒットフラッシュ等の演出エフェクトシステム群を一括構築するドメインプラグイン。
/// </summary>
public class EffectPlugin : IPlugin
{
    /// <summary>
    /// スラッシュ演出・フェード・ヒットフラッシュのエフェクトシステムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        EffectSystems.RegisterSystems(app.World);
    }
}
