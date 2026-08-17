using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// 弾幕 Boss 戦における移動・跳躍・射撃・Boss AI・弾物理・当たり判定・描画システムを
/// Flecs.NET ECS 常駐 Routine として一括登録するプラグイン。
/// 各種単一責任の System クラスを順次登録します。
/// </summary>
public class BulletPlugin : IPlugin
{
    /// <summary>
    /// 全戦闘・描画関連の ECS 常駐 Routine (System) をワールドへ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        PlayerMovementSystem.Register(app.World);
        PlayerShootSystem.Register(app.World);
        BossAiSystem.Register(app.World);
        BulletPhysicsSystem.Register(app.World);
        CombatCollisionSystem.Register(app.World);
        RestartSystem.Register(app.World);
        WorldRenderSystem.Register(app.World);
        HudRenderSystem.Register(app.World);
    }
}
