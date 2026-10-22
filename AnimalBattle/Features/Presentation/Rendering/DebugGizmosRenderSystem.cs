using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     World 単位で独立して保持されるデバッグギズモ描画システム用コンテキスト。
/// </summary>
internal sealed class DebugGizmosRenderContext
{
    public required Query<Transform, CollisionBox> PlatformQuery { get; init; }
    public required Query<Transform, CollisionBox> PlayerQuery { get; init; }
    public required Query<Transform, Hitbox> HitboxQuery { get; init; }
    public required Query<Transform, Hurtbox> HurtboxQuery { get; init; }
    public required Query<TileMap> TileMapQuery { get; init; }
}

/// <summary>
///     衝突枠（CollisionBox）、攻撃判定枠（Hitbox）、被弾枠（Hurtbox）、およびナビゲーショングラフを描画する ECS システム。
///     render.toml の DebugMode かつ IsActive 有効時にワールド空間（カメラオフセット適用中）に Gizmos を描画します。
/// </summary>
public static class DebugGizmosRenderSystem
{
    /// <summary>
    ///     デバッグギズモ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugGizmosRenderSystem(this World world)
    {
        // 足場地形および自機のコリジョンボックスクエリを With タグにより厳格分離
        var platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        var playerQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlayerTag>()
            .Build();

        var hitboxQuery = world.QueryBuilder<Transform, Hitbox>()
            .Build();

        // 死亡済みユニットの Hurtbox 描画をクエリ層で除外
        var hurtboxQuery = world.QueryBuilder<Transform, Hurtbox>()
            .Without<DeadTag>()
            .Build();

        var tileMapQuery = world.QueryBuilder<TileMap>().Build();

        world.Set(new DebugGizmosRenderContext
        {
            PlatformQuery = platformQuery,
            PlayerQuery = playerQuery,
            HitboxQuery = hitboxQuery,
            HurtboxQuery = hurtboxQuery,
            TileMapQuery = tileMapQuery
        });

        world.System("DebugGizmosRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteDebugGizmosRender);

        return world;
    }

    /// <summary>
    ///     デバッグギズモ描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteDebugGizmosRender(Iter it)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;

        // DebugMode が無効または非アクティブな場合は空間ギズモ描画をスキップ
        if (!dbgCfg.DebugMode || !dbgCfg.IsActive) return;

        var ctx = w.Get<DebugGizmosRenderContext>();

        TileMap? tileMap = null;
        ctx.TileMapQuery.Each((ref TileMap tm) => tileMap = tm);

        DrawPlatformCollisionBoxes(ctx.PlatformQuery, dbgCfg);
        DrawPlayerCollisionBoxes(ctx.PlayerQuery, dbgCfg);
        DrawHurtboxes(ctx.HurtboxQuery, dbgCfg);
        DrawHitboxes(ctx.HitboxQuery, dbgCfg);
        DrawPlatformNavGraph(tileMap, dbgCfg);
    }

    private static void DrawPlatformCollisionBoxes(Query<Transform, CollisionBox> platformQuery, DebugRenderConfig dbgCfg)
    {
        // 足場地形枠描画（クエリ分離により Entity.Has による分岐を完全排除）
        platformQuery.Each((ref Transform trans, ref CollisionBox col) =>
        {
            var halfSize = col.Size * 0.5f;
            var p1 = (Vec2i)(trans.Position - halfSize);
            var p2 = (Vec2i)(trans.Position + halfSize);
            GameEngine.DrawBox(p1, p2, dbgCfg.PlatformHitboxColor, false);
        });
    }

    private static void DrawPlayerCollisionBoxes(Query<Transform, CollisionBox> playerQuery, DebugRenderConfig dbgCfg)
    {
        // 自機地形枠および接地状態情報描画
        playerQuery.Each((ref Transform trans, ref CollisionBox col) =>
        {
            var halfSize = col.Size * 0.5f;
            var p1 = (Vec2i)(trans.Position - halfSize);
            var p2 = (Vec2i)(trans.Position + halfSize);
            GameEngine.DrawBox(p1, p2, dbgCfg.PlayerTerrainBoxColor, false);
            GameEngine.DrawString(p1 + dbgCfg.PlayerInfoOffset, $"P({(int)trans.Position.X},{(int)trans.Position.Y}) [Grounded:{col.IsGrounded}]",
                dbgCfg.PlayerTerrainBoxColor);
        });
    }

    private static void DrawHurtboxes(Query<Transform, Hurtbox> hurtboxQuery, DebugRenderConfig dbgCfg)
    {
        // 被弾判定枠描画（クエリで Without<DeadTag> を適用済みなためループ内の死亡判定は不要）
        hurtboxQuery.Each((ref Transform trans, ref Hurtbox hurtbox) =>
        {
            var hurtColor = hurtbox.Faction switch
            {
                Faction.Friendly => dbgCfg.PlayerHurtboxColor,
                Faction.Hostile => dbgCfg.BossHitboxColor,
                Faction.Neutral => dbgCfg.NeutralHitboxColor,
                _ => GetFallbackFactionColor(hurtbox.Faction, dbgCfg.BossHitboxColor)
            };

            var worldCenter = trans.Position + hurtbox.Shape.Offset;
            DrawCollisionShape(hurtbox.Shape, worldCenter, hurtColor, dbgCfg);
        });
    }

    private static void DrawHitboxes(Query<Transform, Hitbox> hitboxQuery, DebugRenderConfig dbgCfg)
    {
        hitboxQuery.Each((Entity e, ref Transform trans, ref Hitbox hitbox) =>
        {
            if (!hitbox.IsActive) return;

            var hitColor = hitbox.Faction switch
            {
                Faction.Friendly => dbgCfg.PlayerBulletHitboxColor,
                Faction.Hostile => dbgCfg.BossBulletHitboxColor,
                Faction.Neutral => dbgCfg.NeutralHitboxColor,
                _ => GetFallbackFactionColor(hitbox.Faction, dbgCfg.BossBulletHitboxColor)
            };

            var worldCenter = trans.Position + hitbox.Shape.Offset;
            DrawCollisionShape(hitbox.Shape, worldCenter, hitColor, dbgCfg);
        });
    }

    private static void DrawCollisionShape(CollisionShape shape, Vec2f worldCenter, Color color, DebugRenderConfig dbgCfg)
    {
        switch (shape.Type)
        {
            case CollisionShapeType.Circle:
                var center = (Vec2i)worldCenter;
                var r = (int)shape.Radius;
                GameEngine.DrawCircle(center, r, color, false);
                break;

            case CollisionShapeType.Box:
                var halfSize = shape.Size * 0.5f;
                var p1 = (Vec2i)(worldCenter - halfSize);
                var p2 = (Vec2i)(worldCenter + halfSize);
                GameEngine.DrawBox(p1, p2, color, false);
                break;

            case CollisionShapeType.Point:
                GameEngine.DrawCircle((Vec2i)worldCenter, dbgCfg.PointShapeRadius, color, false);
                break;

            default:
                Debug.Fail($"[DebugRender 警告] 未知の CollisionShape 型です: {shape.Type}");
                break;
        }
    }

    private static void DrawPlatformNavGraph(TileMap? tileMap, DebugRenderConfig dbgCfg)
    {
        if (tileMap == null || tileMap.NavGraph.Nodes.Count == 0) return;

        var graph = tileMap.NavGraph;
        var nodeColor = dbgCfg.NavNodeColor;
        var edgeDropColor = dbgCfg.NavEdgeDropColor;
        var edgeJumpColor = dbgCfg.NavEdgeJumpColor;

        foreach (var node in graph.Nodes)
        {
            var center = new Vec2i((int)node.CenterX, (int)node.TopY);
            GameEngine.DrawCircle(center, dbgCfg.NavNodeRadius, nodeColor, true);
        }

        foreach (var link in graph.Links)
        {
            var p1 = new Vec2i((int)link.TakeoffX, (int)link.FromNode.TopY);
            var p2 = new Vec2i((int)link.LandingX, (int)link.ToNode.TopY);
            var edgeColor = link.Action == NavActionType.Drop ? edgeDropColor : edgeJumpColor;
            GameEngine.DrawLine(p1, p2, edgeColor);
        }
    }

    private static Color GetFallbackFactionColor(Faction faction, Color defaultColor)
    {
        return faction switch
        {
            Faction.Friendly => ColorPalette.Green,
            Faction.Hostile => ColorPalette.Red,
            _ => AssertHelper.FailWithDefault($"[DebugRenderer 警告] 未知の Faction: {faction}", defaultColor)
        };
    }
}
