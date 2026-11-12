namespace AnimalBattle.Features.Items.Systems;

using System;
using System.Collections.Generic;
using Core;
using Components;
using Configs;
using Maps;
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
        world.SetDisposable(new ItemPhysicsContext
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
                var nextPos = trans.Position + phys.Velocity * dt;
                nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

                // プラットフォーム足場およびアリーナ床面との片道衝突判定
                var hitPlatform = PhysicsService.ResolvePlatformAndFloorCollision(
                    trans.Position.Y,
                    nextPos,
                    itemSize,
                    phys.Velocity.Y,
                    tileMap,
                    arenaCfg,
                    ctx.CandidatePlatforms,
                    out var adjustedPosY,
                    out _);

                if (hitPlatform)
                {
                    phys.IsGrounded = true;
                    phys.RestY = adjustedPosY;
                    phys.Velocity = Vec2f.Zero;
                    trans.Position = new Vec2f(nextPos.X, adjustedPosY);
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
