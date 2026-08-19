using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// フェード・ヒットフラッシュ・スラッシュ等の演出エフェクトシステム群を一括構築するドメインプラグイン。
/// 各種単一責任の Effect System クラスを順次登録します。
/// </summary>
public class EffectPlugin : IPlugin
{
    /// <summary>
    /// スラッシュ演出・フェード・ヒットフラッシュのエフェクトシステムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.World
           .AddSlashEffectSystem()
           .AddFadeEffectSystem()
           .AddHitFlashEffectSystem();
    }
}
