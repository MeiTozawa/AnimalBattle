using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// 4 人対戦での攻撃ヒット・ノックバック・ダメージ蓄積システムを一括構築するドメインプラグイン。
/// </summary>
public class CombatPlugin : IPlugin
{
    /// <summary>
    /// 戦闘判定システムを App へ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        app.AddSystem(CombatSystems.CombatSystem);
    }
}
