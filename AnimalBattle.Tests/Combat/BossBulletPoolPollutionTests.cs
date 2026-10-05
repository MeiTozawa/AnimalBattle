using System.Drawing;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Components;
using AnimalBattle.Core;
using AnimalBattle.Systems;
using Flecs.NET.Core;
using Xunit;

namespace AnimalBattle.Tests.Combat;

/// <summary>
///     Boss 弾幕エンティティプールの汚染防止および GroundStomp 衝撃波生成時の変調解除を検証するテストクラス。
/// </summary>
public sealed class BossBulletPoolPollutionTests
{
    /// <summary>
    ///     特殊変調弾幕（BulletModifier）がプール返却後に再貸出された際、残存変調が正常に破棄されることを検証します。
    /// </summary>
    [Fact]
    public void RecycledBossBullet_WhenRentedAsGroundShockwave_DoesNotRetainBulletModifier()
    {
        using var world = World.Create();
        world.GetCombatEntityPool();

        // 1. 特殊弾幕（黑弾 / カウンター弾）を生成
        CombatEffectHelper.SpawnBossSpecialBullet(
            world,
            100.0f,
            200.0f,
            50.0f,
            50.0f,
            8.0f,
            BulletType.Black);

        Entity? specialBullet = null;
        using (var q = world.QueryBuilder<Transform, Bullet>().With<BossBulletTag>().Build())
        {
            q.Each((Entity e, ref Transform t, ref Bullet b) =>
            {
                if (e.Has<BulletModifier>())
                {
                    specialBullet = e;
                }
            });
        }

        Assert.NotNull(specialBullet);
        Assert.True(specialBullet.Value.Has<BulletModifier>());

        // 2. 特殊弾幕をプールへ返却（画面外消滅または被弾消滅をシミュレート）
        CombatEntityPoolHelper.Recycle(world, specialBullet.Value);

        // 3. Boss 2 の GroundStomp 衝撃波を左右に発射
        CombatEffectHelper.SpawnGroundShockwave(
            world,
            300.0f,
            550.0f,
            -420.0f,
            13.0f,
            1.0f,
            Color.Orange,
            Color.Yellow);

        CombatEffectHelper.SpawnGroundShockwave(
            world,
            360.0f,
            550.0f,
            420.0f,
            13.0f,
            1.0f,
            Color.Orange,
            Color.Yellow);

        // 4. 生成された衝撃波弾幕が BulletModifier を保持していないことを検証
        var foundShockwaves = 0;
        using (var q = world.QueryBuilder<Transform, Velocity, Bullet>().With<BossBulletTag>().Build())
        {
            q.Each((Entity e, ref Transform t, ref Velocity v, ref Bullet b) =>
            {
                if (!e.IsAlive() || !e.Enabled()) return;

                foundShockwaves++;
                Assert.False(
                    e.Has<BulletModifier>(),
                    "リサイクルされた BossBullet が BulletModifier を残存させたまま地面衝撃波として再利用されています。");
            });
        }

        Assert.Equal(2, foundShockwaves);
    }
}
