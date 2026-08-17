using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー弾および Boss 弾幕の等速直線運動および画面外デスポーン（消滅）境界判定を制御する ECS System。
/// </summary>
public static class BulletPhysicsSystem
{
    /// <summary>
    /// 弾物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System<Transform, Velocity>("PlayerBulletMovementSystem")
             .With<PlayerBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel) =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;
                 if (trans.X < arenaCfg.BulletDespawnMinX || trans.X > arenaCfg.BulletDespawnMaxX ||
                     trans.Y < arenaCfg.BulletDespawnMinY || trans.Y > arenaCfg.BulletDespawnMaxY)
                 {
                     e.Destruct();
                 }
             });

        world.System<Transform, Velocity>("BossBulletMovementSystem")
             .With<BossBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel) =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;
                 if (trans.X < arenaCfg.BulletDespawnMinX || trans.X > arenaCfg.BulletDespawnMaxX ||
                     trans.Y < arenaCfg.BulletDespawnMinY || trans.Y > arenaCfg.BulletDespawnMaxY)
                 {
                     e.Destruct();
                 }
             });
    }
}
