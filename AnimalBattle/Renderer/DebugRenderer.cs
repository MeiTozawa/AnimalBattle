using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Renderer;

/// <summary>
///     衝突・被弾・攻撃判定枠およびナビゲーションメッシュをワイヤーフレーム描画するデバッグ表示システム。
///     F1 または H キーでリアルタイムに ON / OFF を切り替え可能です。
/// </summary>
/// <remarks>
///     <see cref="DebugRenderer" /> の新しいインスタンスを初期化し、判定枠クエリを構築します。
/// </remarks>
/// <param name="world">登録先の Flecs.NET ワールド。</param>
public sealed class DebugRenderer(World world) : IDisposable {
    private static bool _prevToggleKey;

    private readonly Query<Transform, Hitbox> _hitboxQuery = world.QueryBuilder<Transform, Hitbox>()
       .Build();

    private readonly Query<Transform, Hurtbox> _hurtboxQuery = world.QueryBuilder<Transform, Hurtbox>()
       .Build();

    private bool _isDisposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _hurtboxQuery.Dispose();
            _hitboxQuery.Dispose();
            _isDisposed = true;
        }
    }

    /// <summary>
    ///     デバッグ衝突枠およびナビゲーショングラフを描画します。
    /// </summary>
    /// <param name="colQuery">地形・プラットフォームコライダークエリ。</param>
    /// <param name="tileMap">現在のアリーナタイルマップ。</param>
    public void Render(Query<Transform, CollisionBox> colQuery, TileMap? tileMap)
    {
        var renderCfg = ConfigManager.Render;
        var dbgCfg = renderCfg.Debug;

        ProcessToggleInput(dbgCfg);

        if (!dbgCfg.Enabled) return;

        DrawCollisionBoxes(colQuery, dbgCfg);
        DrawHurtboxes(_hurtboxQuery, dbgCfg);
        DrawHitboxes(_hitboxQuery, dbgCfg);
        DrawPlatformNavGraph(tileMap, dbgCfg);
    }

    private static void ProcessToggleInput(DebugRenderConfig dbgCfg)
    {
        var isToggleKey = GameEngine.CheckHitKey(0x3B) || GameEngine.CheckHitKey(0x23);
        var toggleJustPressed = isToggleKey && !_prevToggleKey;
        _prevToggleKey = isToggleKey;

        if (toggleJustPressed) dbgCfg.Enabled = !dbgCfg.Enabled;
    }

    private static void DrawCollisionBoxes(Query<Transform, CollisionBox> colQuery, DebugRenderConfig dbgCfg)
    {
        colQuery.Each((e, ref trans, ref col) =>
        {
            var x1 = (int)(trans.X - col.Width / 2.0f);
            var y1 = (int)(trans.Y - col.Height / 2.0f);
            var x2 = (int)(trans.X + col.Width / 2.0f);
            var y2 = (int)(trans.Y + col.Height / 2.0f);

            var boxColor = e.Has<PlatformTag>() ? dbgCfg.PlatformHitboxColor : dbgCfg.PlayerTerrainBoxColor;
            GameEngine.DrawBox(x1, y1, x2, y2, boxColor, false);

            if (e.Has<PlayerTag>())
                GameEngine.DrawString(x1 + dbgCfg.PlayerInfoOffsetX, y1 + dbgCfg.PlayerInfoOffsetY, $"P({(int)trans.X},{(int)trans.Y}) [Grounded:{col.IsGrounded}]",
                    boxColor);
        });
    }

    private static void DrawHurtboxes(Query<Transform, Hurtbox> hurtboxQuery, DebugRenderConfig dbgCfg)
    {
        hurtboxQuery.Each((e, ref trans, ref hurtbox) =>
        {
            if (e.Has<Health>() && e.Get<Health>().Current <= 0.0f) return;

            var hurtColor = hurtbox.Faction switch {
                Faction.Friendly => dbgCfg.PlayerHurtboxColor,
                Faction.Hostile => dbgCfg.BossHitboxColor,
                Faction.Neutral => dbgCfg.NeutralHitboxColor,
                _ => GetFallbackFactionColor(hurtbox.Faction, dbgCfg.BossHitboxColor)
            };

            var worldCenterX = trans.X + hurtbox.Shape.OffsetX;
            var worldCenterY = trans.Y + hurtbox.Shape.OffsetY;
            DrawCollisionShape(hurtbox.Shape, worldCenterX, worldCenterY, hurtColor, dbgCfg);
        });
    }

    private static void DrawHitboxes(Query<Transform, Hitbox> hitboxQuery, DebugRenderConfig dbgCfg)
    {
        hitboxQuery.Each((e, ref trans, ref hitbox) =>
        {
            if (!hitbox.IsActive) return;

            var hitColor = hitbox.Faction switch {
                Faction.Friendly => dbgCfg.PlayerBulletHitboxColor,
                Faction.Hostile => dbgCfg.BossBulletHitboxColor,
                Faction.Neutral => dbgCfg.NeutralHitboxColor,
                _ => GetFallbackFactionColor(hitbox.Faction, dbgCfg.BossBulletHitboxColor)
            };

            var worldCenterX = trans.X + hitbox.Shape.OffsetX;
            var worldCenterY = trans.Y + hitbox.Shape.OffsetY;
            DrawCollisionShape(hitbox.Shape, worldCenterX, worldCenterY, hitColor, dbgCfg);
        });
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
        return faction switch {
            Faction.Friendly => ColorPalette.Green,
            Faction.Hostile => ColorPalette.Red,
            _ => AssertHelper.FailWithDefault($"[DebugRenderer 警告] 未知の Faction: {faction}", defaultColor)
        };
    }
}