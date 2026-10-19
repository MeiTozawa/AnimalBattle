using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     衝突枠（CollisionBox）、攻撃判定枠（Hitbox）、被弾枠（Hurtbox）およびナビゲーショングラフのデバッグ枠線を描画する ECS システム。
///     F1 または H キーによるトグル切り替えに対応します。
/// </summary>
public static class DebugGizmosRenderSystem
{
    private static bool _prevToggleKey;
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

        var renderCfg = ConfigManager.Render;
        var dbgCfg = renderCfg.Debug;

        ProcessToggleInput(dbgCfg);

        if (!dbgCfg.Enabled) return;

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

    private static void ProcessToggleInput(DebugRenderConfig dbgCfg)
    {
        var isToggleKey = GameEngine.CheckHitKey(0x3B) || GameEngine.CheckHitKey(0x23);
        var toggleJustPressed = isToggleKey && !_prevToggleKey;
        _prevToggleKey = isToggleKey;

        if (toggleJustPressed) dbgCfg.Enabled = !dbgCfg.Enabled;
    }

    private static void DrawCollisionBoxes(Query<Transform, CollisionBox> colQuery)
    {
        colQuery.Each(DrawSingleCollisionBox);
    }

    private static void DrawSingleCollisionBox(Entity e, ref Transform trans, ref CollisionBox col)
    {
        var dbgCfg = _currentDbgCfg;
        var x1 = (int)(trans.X - col.Width / 2.0f);
        var y1 = (int)(trans.Y - col.Height / 2.0f);
        var x2 = (int)(trans.X + col.Width / 2.0f);
        var y2 = (int)(trans.Y + col.Height / 2.0f);

        var boxColor = e.Has<PlatformTag>() ? dbgCfg.PlatformHitboxColor : dbgCfg.PlayerTerrainBoxColor;
        GameEngine.DrawBox(x1, y1, x2, y2, boxColor, false);

        if (e.Has<PlayerTag>())
            GameEngine.DrawString(x1 + dbgCfg.PlayerInfoOffsetX, y1 + dbgCfg.PlayerInfoOffsetY, $"P({(int)trans.X},{(int)trans.Y}) [Grounded:{col.IsGrounded}]",
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

        var worldCenterX = trans.X + hurtbox.Shape.OffsetX;
        var worldCenterY = trans.Y + hurtbox.Shape.OffsetY;
        DrawCollisionShape(hurtbox.Shape, worldCenterX, worldCenterY, hurtColor, dbgCfg);
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

        var worldCenterX = trans.X + hitbox.Shape.OffsetX;
        var worldCenterY = trans.Y + hitbox.Shape.OffsetY;
        DrawCollisionShape(hitbox.Shape, worldCenterX, worldCenterY, hitColor, dbgCfg);
    }

    private static void DrawCollisionShape(CollisionShape shape, float worldCenterX, float worldCenterY, Color color, DebugRenderConfig dbgCfg)
    {
        switch (shape.Type)
        {
            case CollisionShapeType.Circle:
                var cx = (int)worldCenterX;
                var cy = (int)worldCenterY;
                var r = (int)shape.Radius;
                GameEngine.DrawCircle(cx, cy, r, color, false);
                break;

            case CollisionShapeType.Box:
                var x1 = (int)(worldCenterX - shape.Width / 2.0f);
                var y1 = (int)(worldCenterY - shape.Height / 2.0f);
                var x2 = (int)(worldCenterX + shape.Width / 2.0f);
                var y2 = (int)(worldCenterY + shape.Height / 2.0f);
                GameEngine.DrawBox(x1, y1, x2, y2, color, false);
                break;

            case CollisionShapeType.Point:
                GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, dbgCfg.PointShapeRadius, color, false);
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
            var nx = (int)node.CenterX;
            var ny = (int)node.TopY;
            GameEngine.DrawCircle(nx, ny, dbgCfg.NavNodeRadius, nodeColor, true);
        }

        foreach (var link in graph.Links)
        {
            var fx = (int)link.TakeoffX;
            var fy = (int)link.FromNode.TopY;
            var tx = (int)link.LandingX;
            var ty = (int)link.ToNode.TopY;
            var edgeColor = link.Action == NavActionType.Drop ? edgeDropColor : edgeJumpColor;
            GameEngine.DrawLine(fx, fy, tx, ty, edgeColor);
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
