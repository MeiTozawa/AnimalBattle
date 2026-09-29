using AnimalBattle.Compute;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     特殊弾幕（品紅・黄・青・黒）の周期アニメーション、法線正弦波揺動、および動的拡縮判定を
///     <see cref="NativeComputeQueue" /> による C++ 原生拡張一括同期バッチ処理で制御する ECS System。
/// </summary>
public static class SpecialBulletSystem {
    /// <summary>
    ///     特殊弾幕変調システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World" />。</returns>
    public static World AddSpecialBulletSystem(this World world)
    {
        world.System<Velocity, BulletModifier, Bullet, Hitbox>("SpecialBulletSystem")
           .With<BossBulletTag>()
           .Kind(Ecs.OnUpdate)
           .Iter((Iter it, Field<Velocity> vel, Field<BulletModifier> modifier, Field<Bullet> bullet,
                Field<Hitbox> hitbox) =>
            {
                var count = it.Count();
                if (count <= 0) return;

                var dt = it.DeltaTime();
                var queue = NativeComputeQueue.Shared;
                queue.Clear();

                // 1. 全弾幕の変調要求を一括して計算キューへ登録
                for (var i = 0; i < count; ++i)
                {
                    modifier[i].ElapsedTimer += dt;
                    queue.EnqueueSpecialBullet(
                        modifier[i].Type,
                        modifier[i].ElapsedTimer,
                        modifier[i].BaseVx,
                        modifier[i].BaseVy,
                        modifier[i].BaseRadius);
                }

                // 2. 1 回の P/Invoke で C++ 原生拡張へ一括転送・並列 SIMD 実行
                queue.Dispatch();

                // 3. 計算結果（変調速度および脈動判定半径）を Entity コンポーネントへ一括反映
                for (var i = 0; i < count; ++i)
                {
                    ref readonly var res = ref queue.GetBulletResult(i);
                    vel[i].Vx = res.OutVx;
                    vel[i].Vy = res.OutVy;

                    if (modifier[i].Type == BulletType.Magenta || modifier[i].Type == BulletType.Black)
                    {
                        bullet[i].Radius = res.OutRadius;
                        hitbox[i].Shape = new CollisionShape.Circle(res.OutRadius);
                    }
                }

                queue.Clear();
            });

        return world;
    }
}