using Flecs.NET.Core;
using AnimalBattle.Bindings;
using AnimalBattle.Configs;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー弾および Boss 弾幕の等速直線運動および画面外デスポーン（消滅）境界判定を一括制御する ECS System。
/// Flecs.NET の Iter から連続コンポーネント配列を取得し、C++ SIMD バッチ物理計算を一括実行します。
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
             .Run(it =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 byte[] despawnBuffer = new byte[256];

                 while (it.Next())
                 {
                     int count = it.Count();
                     if (count <= 0)
                     {
                         continue;
                     }

                     if (despawnBuffer.Length < count)
                     {
                         despawnBuffer = new byte[count * 2];
                     }

                     Span<byte> despawnFlags = despawnBuffer.AsSpan(0, count);

                     unsafe
                     {
                         fixed (Transform* pTrans = &it.Field<Transform>(1)[0])
                         fixed (Velocity* pVel = &it.Field<Velocity>(2)[0])
                         fixed (byte* pFlags = despawnFlags)
                         {
                             NativeExtensionBindings.Extension_BatchBulletMovement(
                                 pTrans, pVel, count, it.DeltaTime(),
                                 arenaCfg.BulletDespawnMinX, arenaCfg.BulletDespawnMaxX,
                                 arenaCfg.BulletDespawnMinY, arenaCfg.BulletDespawnMaxY,
                                 pFlags);
                         }
                     }

                     for (int i = 0; i < count; ++i)
                     {
                         if (despawnFlags[i] != 0)
                         {
                             it.Entity(i).Destruct();
                         }
                     }
                 }
             });

        world.System<Transform, Velocity>("BossBulletMovementSystem")
             .With<BossBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Run(it =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 byte[] despawnBuffer = new byte[256];

                 while (it.Next())
                 {
                     int count = it.Count();
                     if (count <= 0)
                     {
                         continue;
                     }

                     if (despawnBuffer.Length < count)
                     {
                         despawnBuffer = new byte[count * 2];
                     }

                     Span<byte> despawnFlags = despawnBuffer.AsSpan(0, count);

                     unsafe
                     {
                         fixed (Transform* pTrans = &it.Field<Transform>(1)[0])
                         fixed (Velocity* pVel = &it.Field<Velocity>(2)[0])
                         fixed (byte* pFlags = despawnFlags)
                         {
                             NativeExtensionBindings.Extension_BatchBulletMovement(
                                 pTrans, pVel, count, it.DeltaTime(),
                                 arenaCfg.BulletDespawnMinX, arenaCfg.BulletDespawnMaxX,
                                 arenaCfg.BulletDespawnMinY, arenaCfg.BulletDespawnMaxY,
                                 pFlags);
                         }
                     }

                     for (int i = 0; i < count; ++i)
                     {
                         if (despawnFlags[i] != 0)
                         {
                             it.Entity(i).Destruct();
                         }
                     }
                 }
             });
    }
}
