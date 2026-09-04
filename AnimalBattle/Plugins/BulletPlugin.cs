using AnimalBattle.Systems;

namespace AnimalBattle.Plugins;

/// <summary>
/// 弾幕 Boss 戦におけるスプライトアニメーション、弾物理（SIMD 計算委譲）および
/// 戦闘幾何衝突判定システムを Flecs.NET ECS 常駐 Routine として一括登録するプラグイン。
/// 表現層（描画・UI）や入力制御を排除し、純粋な物理・衝突シミュレーションに特化します。
/// </summary>
public class BulletPlugin : IPlugin
{
    /// <summary>
    /// 全戦闘物理・衝突関連の ECS 常駐 Routine (System) をワールドへ登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ。</param>
    public void Build(App app)
    {
        app.World
           .AddSpriteAnimationSystem()
           .AddBulletPhysicsSystem()
           .AddCombatCollisionSystem();
    }
}
