using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// 弾幕 Boss 戦における移動・跳躍・射撃・Boss 1 / Boss 2 AI・弾物理・当たり判定・描画システムを
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
        app.World
           .AddPlayerMovementSystem()
           .AddSpriteAnimationSystem()
           .AddPlayerShootSystem()
           .AddBoss1AiSystem()
           .AddBoss2AiSystem()
           .AddBulletPhysicsSystem()
           .AddCombatCollisionSystem()
           .AddRestartSystem()
           .AddWorldRenderSystem()
           .AddHudRenderSystem()
           .AddDebugRenderSystem();
    }
}
