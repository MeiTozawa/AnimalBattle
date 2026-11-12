namespace AnimalBattle.Features.Items.Spawners;

using System;
using System.Diagnostics;
using AnimalBattle.Player.Components;
using Core;
using Components;
using Configs;
using Systems;
using Flecs.NET.Core;

/// <summary>
///     フィールド上へドロップアイテムエンティティを動的に生成・破棄するスポナークラス。
/// </summary>
public static class ItemSpawner
{
    /// <summary>
    ///     指定位置に回復アイテム（ヘルスパック）エンティティを生成・射出します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールドインスタンス</param>
    /// <param name="spawnPos">出現時のワールド座標</param>
    /// <param name="cfg">回復アイテムの設定データ</param>
    /// <returns>生成されたエンティティ</returns>
    public static Entity SpawnHealthPack(World world, Vec2f spawnPos, HealthPackConfig cfg)
    {
        Debug.Assert(cfg != null, "HealthPackConfig が null です。");

        // プレイヤーの位置を取得し、プレイヤーが存在する方向へ優先して放出（敵の足下敷きや障害物重複を防止）
        var targetDir = 0.0f;
        if (world.Has<ItemPickupContext>())
        {
            var pCtx = world.Get<ItemPickupContext>();
            pCtx.PlayerQuery.Each((ref tr, ref hp) =>
            {
                if (hp.Current > 0.0f)
                {
                    targetDir = tr.Position.X < spawnPos.X ? -1.0f : 1.0f;
                }
            });
        }
        else
        {
            using var playerQuery = world.QueryBuilder<Transform>().With<PlayerTag>().Without<DeadTag>().Build();
            playerQuery.Each((ref tr) =>
            {
                targetDir = tr.Position.X < spawnPos.X ? -1.0f : 1.0f;
            });
        }

        if (targetDir == 0.0f)
        {
            targetDir = Random.Shared.Next(2) == 0 ? -1.0f : 1.0f;
        }

        // 水平初速度の大きさを計算（真下への落下を排除し、敵から確実に離れた位置へ放物射出）
        var minSpeed = MathF.Abs(cfg.PopVelocityXMin);
        var maxSpeed = MathF.Abs(cfg.PopVelocityXMax);
        if (minSpeed > maxSpeed)
        {
            (minSpeed, maxSpeed) = (maxSpeed, minSpeed);
        }

        var speedX = Random.Shared.NextSingle() * (maxSpeed - minSpeed) + minSpeed;
        var vx = targetDir * speedX;
        var vy = cfg.PopVelocityY;

        ref var director = ref world.GetMut<ItemDirectorState>();
        director.ActiveHealthPackCount++;
        director.IcdRemaining = cfg.IcdDuration;

        // ドロップイベントを発行
        EventBus.Publish(new ItemSpawnedEvent(spawnPos));

        return world.Entity()
            .Add<ItemTag>()
            .Add<HealthPackTag>()
            .Set(new Transform(spawnPos, Vec2f.One))
            .Set(new ItemDropPhysics
            {
                Velocity = new Vec2f(vx, vy),
                IsGrounded = false,
                RestY = spawnPos.Y,
                FloatTimer = 0.0f
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
    ///     バトル開始・終了および戦闘リセット時に既存のアイテムエンティティを全消去し、ディレクター状態を初期化します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールドインスタンス</param>
    public static void ResetItems(World world)
    {
        var isDeferred = world.IsDeferred();
        if (isDeferred) world.DeferSuspend();
        try
        {
            world.Set(new ItemDirectorState
            {
                IcdRemaining = 0.0f,
                ActiveHealthPackCount = 0
            });

            using var query = world.QueryBuilder().With<ItemTag>().Build();
            query.Each(static e => e.Destruct());
        }
        finally
        {
            if (isDeferred) world.DeferResume();
        }
    }
}
