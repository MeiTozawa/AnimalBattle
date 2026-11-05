namespace AnimalBattle.Features.Items.Systems;

using System;
using System.Collections.Generic;
using AnimalBattle.Core;
using AnimalBattle.Features.Items.Components;
using AnimalBattle.Features.Items.Configs;
using AnimalBattle.Maps;
using Flecs.NET.Core;

/// <summary>
///     ドロップアイテム物理演算用のクエリおよび作業バッファを保持するコンテキストリソース。
/// </summary>
internal sealed class ItemPhysicsContext : IDisposable
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public TileMap? CachedTileMap;
    public readonly List<int> CandidatePlatforms = new(16);

    public void Dispose()
    {
        TileMapQuery.Dispose();
    }
}

/// <summary>
///     ドロップアイテムの放物線運動、接地検出、上下浮遊および自然消滅タイマーを統括するシステム。
/// </summary>
public static class ItemPhysicsSystem
{
    /// <summary>
    ///     アイテム物理演算システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddItemPhysicsSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();
        world.Set(new ItemPhysicsContext
        {
            TileMapQuery = tileMapQuery
        });

        world.System<Transform, ItemDropPhysics, ItemLifetime>("ItemPhysicsSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(Execute);

        return world;
    }

    private static void Execute(
        Iter it,
        Field<Transform> transforms,
        Field<ItemDropPhysics> physicsField,
        Field<ItemLifetime> lifetimes)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        var dt = it.DeltaTime();
        if (dt <= 0.0f) return;

        var itemCfg = world.Get<ItemConfig>();
        var cfg = itemCfg.HealthPack;
        ref readonly var arenaCfg = ref world.Get<ArenaConfig>();
        var ctx = world.Get<ItemPhysicsContext>();

        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(static (Iter tileIt, Field<TileMap> maps) =>
        {
            if (tileIt.Any())
            {
                tileIt.World().Get<ItemPhysicsContext>().CachedTileMap = maps[0];
            }
        });
        var tileMap = ctx.CachedTileMap;
        ctx.CachedTileMap = null;

        var itemSize = cfg.CollisionSize;

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref var trans = ref transforms[i];
            ref var phys = ref physicsField[i];
            ref var life = ref lifetimes[i];

            // 寿命カウントダウン
            life.Remaining -= dt;
            if (life.Remaining <= 0.0f)
            {
                ref var director = ref world.GetMut<ItemDirectorState>();
                director.ActiveHealthPackCount = Math.Max(0, director.ActiveHealthPackCount - 1);
                entity.Destruct();
                continue;
            }

            if (!phys.IsGrounded)
            {
                // 空中放物線移動
                phys.Velocity = new Vec2f(phys.Velocity.X, phys.Velocity.Y + cfg.Gravity * dt);
                var currentY = trans.Position.Y;
                var nextPos = trans.Position + phys.Velocity * dt;
                nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

                Box2D entityBox = new(nextPos, itemSize);

                // プラットフォーム足場との衝突判定
                var hitPlatform = false;
                var platformGroundY = float.MaxValue;

                if (tileMap != null)
                {
                    var sweepTop = MathF.Min(currentY, nextPos.Y) - itemSize.Y * 0.5f;
                    var sweepBottom = MathF.Max(currentY, nextPos.Y) + itemSize.Y * 0.5f;
                    Box2D sweepQueryBox = new(new Vec2f(nextPos.X, (sweepTop + sweepBottom) * 0.5f), new Vec2f(itemSize.X, sweepBottom - sweepTop));

                    ctx.CandidatePlatforms.Clear();
                    tileMap.SpatialHash.Query(sweepQueryBox, ctx.CandidatePlatforms);

                    foreach (var platformIdx in ctx.CandidatePlatforms)
                    {
                        if (tileMap.SpatialHash.TryGetBox(platformIdx, out var platformBox))
                        {
                            if (PhysicsService.CheckPlatformGround(currentY, nextPos.Y, entityBox, platformBox, out var groundY, out _))
                            {
                                if (!hitPlatform || groundY < platformGroundY)
                                {
                                    hitPlatform = true;
                                    platformGroundY = groundY;
                                }
                            }
                        }
                    }
                }

                // 競技場底面フロア制限の適用
                var floorLimitY = arenaCfg.FloorLimitY - itemSize.Y * 0.5f;
                if (!hitPlatform && nextPos.Y >= floorLimitY)
                {
                    hitPlatform = true;
                    platformGroundY = floorLimitY;
                }

                if (hitPlatform)
                {
                    phys.IsGrounded = true;
                    phys.RestY = platformGroundY;
                    phys.Velocity = Vec2f.Zero;
                    trans.Position = new Vec2f(nextPos.X, platformGroundY);
                }
                else
                {
                    trans.Position = nextPos;
                }
            }
            else
            {
                // 接地後の正弦波上下微浮遊
                phys.FloatTimer += dt;
                trans.Position = new Vec2f(trans.Position.X, phys.RestY + MathF.Sin(phys.FloatTimer * cfg.FloatSpeed) * cfg.FloatAmplitude);
            }
        }
    }
}
