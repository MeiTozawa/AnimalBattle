using System.Buffers;
using AnimalBattle.Bindings;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     プレイヤー弾および Boss 弾幕の等速直線運動および画面外デスポーン（消滅）境界判定を一括制御する ECS System。
///     Flecs.NET の Iter から連続コンポーネント配列を取得し、C++ SIMD バッチ物理計算を一括実行します。
/// </summary>
public static class BulletPhysicsSystem {
    /// <summary>
    ///     弾物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddBulletPhysicsSystem(this World world)
    {
        world.System<Transform, Velocity>("PlayerBulletMovementSystem")
           .With<PlayerBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBulletMovement);

        world.System<Transform, Velocity>("BossBulletMovementSystem")
           .With<BossBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter(RunBatchBulletMovement);

        return world;
    }

    private static void RunBatchBulletMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        if (count <= 0) return;

        var arenaCfg = ConfigManager.Arena;
        const int stackallocThreshold = 1024;
        var rentedArray = count > stackallocThreshold ? ArrayPool<byte>.Shared.Rent(count) : null;
        var despawnFlags = rentedArray != null ? rentedArray.AsSpan(0, count) : stackalloc byte[count];

        try
        {
            unsafe
            {
                var pTrans = (Transform*)trans.Data;
                var pVel = (Velocity*)vel.Data;

                fixed (byte* pFlags = despawnFlags)
                {
                    NativeExtensionBindings.Extension_BatchBulletMovement(
                        pTrans,
                        pVel,
                        count,
                        it.DeltaTime(),
                        arenaCfg.BulletDespawnMinX,
                        arenaCfg.BulletDespawnMaxX,
                        arenaCfg.BulletDespawnMinY,
                        arenaCfg.BulletDespawnMaxY,
                        pFlags);
                }
            }

            var w = it.World();
            for (var i = 0; i < count; ++i)
                if (despawnFlags[i] != 0)
                    CombatEntityPoolHelper.Recycle(w, it.Entity(i));
        }
        finally
        {
            if (rentedArray != null) ArrayPool<byte>.Shared.Return(rentedArray);
        }
    }
}