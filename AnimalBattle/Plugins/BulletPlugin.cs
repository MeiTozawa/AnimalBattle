using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// 弾幕 Boss 戦における移動・跳躍・射撃・Boss AI・弾物理・当たり判定・描画システムを
/// Flecs.NET ECS 常駐 Routine として一括登録するプラグイン。
/// </summary>
public class BulletPlugin : IPlugin
{
    /// <summary>
    /// 全戦闘・描画関連の ECS 常駐 Routine (System) をワールドへ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        CombatSystems.RegisterSystems(app.World);
        RenderSystems.RegisterSystems(app.World);
    }
}
