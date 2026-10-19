using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     衝突枠（CollisionBox）、攻撃判定枠（Hitbox）、被弾枠（Hurtbox）、およびナビゲーショングラフを描画する ECS システム。
///     render.toml の DebugMode かつ IsActive 有効時にワールド空間（カメラオフセット適用中）に Gizmos を描画します。
/// </summary>
public static class DebugGizmosRenderSystem
{
    private static Query<Transform, CollisionBox> _platformQuery;
    private static Query<Transform, Hitbox> _hitboxQuery;
    private static Query<Transform, Hurtbox> _hurtboxQuery;
    private static Query<TileMap> _tileMapQuery;
    private static TileMap? _cachedTileMap;
    private static DebugRenderConfig _currentDbgCfg = null!;

    /// <summary>
    ///     デバッグギズモ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugGizmosRenderSystem(this World world)
    {
        _platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        _hitboxQuery = world.QueryBuilder<Transform, Hitbox>()
            .Build();

        _hurtboxQuery = world.QueryBuilder<Transform, Hurtbox>()
            .Build();

        _tileMapQuery = world.QueryBuilder<TileMap>().Build();

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

        _currentDbgCfg = dbgCfg;
        _cachedTileMap = null;
        _tileMapQuery.Each(FindTileMap);
        var tileMap = _cachedTileMap;
        _cachedTileMap = null;

        DrawCollisionBoxes(_platformQuery);
        DrawHurtboxes(_hurtboxQuery, dbgCfg);
        DrawHitboxes(_hitboxQuery, dbgCfg);
        DrawPlatformNavGraph(tileMap, dbgCfg);
    }

    private static void FindTileMap(ref TileMap tm) => _cachedTileMap = tm;

    private static void DrawCollisionBoxes(Query<Transform, CollisionBox> colQuery)
    {
        colQuery.Each(DrawSingleCollisionBox);
    }

    private static void DrawSingleCollisionBox(Entity e, ref Transform trans, ref CollisionBox col)
    {
        var dbgCfg = _currentDbgCfg;
        var halfSize = col.Size * 0.5f;
        var p1 = (Vec2i)(trans.Position - halfSize);
        var p2 = (Vec2i)(trans.Position + halfSize);

        var boxColor = e.Has<PlatformTag>() ? dbgCfg.PlatformHitboxColor : dbgCfg.PlayerTerrainBoxColor;
        GameEngine.DrawBox(p1, p2, boxColor, false);

        if (e.Has<PlayerTag>())
            GameEngine.DrawString(p1 + dbgCfg.PlayerInfoOffset, $"P({(int)trans.Position.X},{(int)trans.Position.Y}) [Grounded:{col.IsGrounded}]",
                boxColor);
    }

    private static void DrawHurtboxes(Query<Transform, Hurtbox> hurtboxQuery, DebugRenderConfig dbgCfg)
    {
        hurtboxQuery.Each(DrawSingleHurtbox);
    }

    private static void DrawSingleHurtbox(Entity e, ref Transform trans, ref Hurtbox hurtbox)
    {
        if (e.Has<Health>() && e.Get<Health>().Current <= 0.0f) return;

        var dbgCfg = _currentDbgCfg;
        var hurtColor = hurtbox.Faction switch
        {
            Faction.Friendly => dbgCfg.PlayerHurtboxColor,
            Faction.Hostile => dbgCfg.BossHitboxColor,
            Faction.Neutral => dbgCfg.NeutralHitboxColor,
            _ => GetFallbackFactionColor(hurtbox.Faction, dbgCfg.BossHitboxColor)
        };

        var worldCenter = trans.Position + hurtbox.Shape.Offset;
        DrawCollisionShape(hurtbox.Shape, worldCenter, hurtColor, dbgCfg);
    }

    private static void DrawHitboxes(Query<Transform, Hitbox> hitboxQuery, DebugRenderConfig dbgCfg)
    {
        hitboxQuery.Each(DrawSingleHitbox);
    }

    private static void DrawSingleHitbox(Entity e, ref Transform trans, ref Hitbox hitbox)
    {
        if (!hitbox.IsActive) return;

        var dbgCfg = _currentDbgCfg;
        var hitColor = hitbox.Faction switch
        {
            Faction.Friendly => dbgCfg.PlayerBulletHitboxColor,
            Faction.Hostile => dbgCfg.BossBulletHitboxColor,
            Faction.Neutral => dbgCfg.NeutralHitboxColor,
            _ => GetFallbackFactionColor(hitbox.Faction, dbgCfg.BossBulletHitboxColor)
        };

        var worldCenter = trans.Position + hitbox.Shape.Offset;
        DrawCollisionShape(hitbox.Shape, worldCenter, hitColor, dbgCfg);
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
