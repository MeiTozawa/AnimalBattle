using System.Diagnostics;
using AnimalBattle.Core;
using AnimalBattle.Features.Items.Components;
using AnimalBattle.Features.Items.Configs;

namespace AnimalBattle.Features.Items.Spawners;

/// <summary>
///     フィールド上へドロップアイテムエンティティを動的に生成・破棄するスポナークラス。
/// </summary>
public static class ItemSpawner
{
    /// <summary>
    ///     指定位置に血包（Health Pack）エンティティを放出生成します。
    /// </summary>
    /// <param name="world">Flecs.NET ワールドインスタンス</param>
    /// <param name="spawnPos">出現初期座標</param>
    /// <param name="cfg">血包の設定データ</param>
    /// <returns>生成されたエンティティ</returns>
    public static Entity SpawnHealthPack(World world, Vec2f spawnPos, HealthPackConfig cfg)
    {
        Debug.Assert(cfg != null, "HealthPackConfig が null です。");

        // 放出時の水平ランダム速度および上向き初速の決定
        var vx = Random.Shared.NextSingle() * (cfg.PopVelocityXMax - cfg.PopVelocityXMin) + cfg.PopVelocityXMin;
        var vy = cfg.PopVelocityY;

        ref var director = ref world.GetMut<ItemDirectorState>();
        director.ActiveHealthPackCount++;
        director.IcdRemaining = cfg.IcdDuration;

        // ドロップ効果音を再生
        SoundService.PlaySfx(SfxType.ItemDrop);

        return world.Entity()
            .Add<ItemTag>()
            .Add<HealthPackTag>()
            .Set(new Transform(spawnPos, Vec2f.One))
            .Set(new ItemDropPhysics
            {
                Velocity = new Vec2f(vx, vy),
                IsGrounded = false,
                RestY = spawnPos.Y,
                FloatTimer = 0f
            })
            .Set(new ItemLifetime
            {
                Remaining = cfg.Lifetime,
                BlinkThreshold = cfg.BlinkThreshold
            })
            .Set(new ItemPickup
            {
                Radius = cfg.PickupRadius,
                HealAmount = cfg.HealAmount
            });
    }

    /// <summary>
    ///     バトル開始・終了時に既存アイテムを全消去しディレクター状態を初期化します。
    /// </summary>
    /// <param name="world">Flecs.NET ワールドインスタンス</param>
    public static void ResetItems(World world)
    {
        world.Set(new ItemDirectorState
        {
            IcdRemaining = 0f,
            ActiveHealthPackCount = 0
        });

        using var query = world.QueryBuilder().With<ItemTag>().Build();
        query.Each(e => e.Destruct());
    }
}
