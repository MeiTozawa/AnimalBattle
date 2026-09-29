using System;
using System.Drawing;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 衝突・被弾・攻撃判定枠およびナビゲーションメッシュをワイヤーフレーム描画するデバッグ表示システム。
/// F1 または H キーでリアルタイムに ON / OFF を切り替え可能です。
/// </summary>
/// <remarks>
/// <see cref="DebugRenderSystem"/> の新しいインスタンスを初期化し、判定枠クエリを構築します。
/// </remarks>
/// <param name="world">登録先の Flecs.NET ワールド。</param>
public sealed class DebugRenderSystem(World world) : IDisposable
{
    private static bool _prevToggleKey;
    private readonly Query<Transform, Hurtbox> _hurtboxQuery = world.QueryBuilder<Transform, Hurtbox>()
                             .Build();
    private readonly Query<Transform, Hitbox> _hitboxQuery = world.QueryBuilder<Transform, Hitbox>()
                            .Build();
    private bool _isDisposed;

    /// <summary>
    /// デバッグ衝突枠およびナビゲーショングラフを描画します。
    /// </summary>
    /// <param name="colQuery">地形・プラットフォームコライダークエリ。</param>
    /// <param name="tileMap">現在のアリーナタイルマップ。</param>
    public void Render(Query<Transform, CollisionBox> colQuery, TileMap? tileMap)
    {
        RenderConfig renderCfg = ConfigManager.Render;
        DebugRenderConfig dbgCfg = renderCfg.Debug;

        ProcessToggleInput(dbgCfg);

        if (!dbgCfg.Enabled)
        {
            return;
        }

        DrawCollisionBoxes(colQuery, dbgCfg);
        DrawHurtboxes(_hurtboxQuery, dbgCfg);
        DrawHitboxes(_hitboxQuery, dbgCfg);
        DrawPlatformNavGraph(tileMap);
    }

    private static void ProcessToggleInput(DebugRenderConfig dbgCfg)
    {
        bool isToggleKey = GameEngine.CheckHitKey(0x3B) || GameEngine.CheckHitKey(0x23);
        bool toggleJustPressed = isToggleKey && !_prevToggleKey;
        _prevToggleKey = isToggleKey;

        if (toggleJustPressed)
        {
            dbgCfg.Enabled = !dbgCfg.Enabled;
        }
    }

    private static void DrawCollisionBoxes(Query<Transform, CollisionBox> colQuery, DebugRenderConfig dbgCfg)
    {
        colQuery.Each((Entity e, ref Transform trans, ref CollisionBox col) =>
        {
            int x1 = (int)(trans.X - col.Width / 2.0f);
            int y1 = (int)(trans.Y - col.Height / 2.0f);
            int x2 = (int)(trans.X + col.Width / 2.0f);
            int y2 = (int)(trans.Y + col.Height / 2.0f);

            Color boxColor = e.Has<PlatformTag>() ? dbgCfg.PlatformHitboxColor : dbgCfg.PlayerTerrainBoxColor;
            GameEngine.DrawBox(x1, y1, x2, y2, boxColor, false);

            if (e.Has<PlayerTag>())
            {
                GameEngine.DrawString(x1 - 10, y1 - 16, $"P({(int)trans.X},{(int)trans.Y}) [Grounded:{col.IsGrounded}]", boxColor);
            }
        });
    }

    private static void DrawHurtboxes(Query<Transform, Hurtbox> hurtboxQuery, DebugRenderConfig dbgCfg)
    {
        hurtboxQuery.Each((Entity e, ref Transform trans, ref Hurtbox hurtbox) =>
        {
            if (e.Has<Health>())
            {
                ref readonly Health h = ref e.Get<Health>();
                if (!h.IsAlive)
                {
                    return;
                }
            }

            Color hurtColor = hurtbox.Faction switch
            {
                Faction.Friendly => dbgCfg.PlayerHurtboxColor,
                Faction.Hostile => dbgCfg.BossHitboxColor,
                Faction.Neutral => ColorPalette.Gray,
                _ => GetFallbackFactionColor(hurtbox.Faction, dbgCfg.BossHitboxColor)
            };

            float worldCenterX = trans.X + hurtbox.Shape.OffsetX;
            float worldCenterY = trans.Y + hurtbox.Shape.OffsetY;
            DrawCollisionShape(hurtbox.Shape, worldCenterX, worldCenterY, hurtColor);
        });
    }

    private static void DrawHitboxes(Query<Transform, Hitbox> hitboxQuery, DebugRenderConfig dbgCfg)
    {
        hitboxQuery.Each((Entity e, ref Transform trans, ref Hitbox hitbox) =>
        {
            if (!hitbox.IsActive)
            {
                return;
            }

            Color hitColor = hitbox.Faction switch
            {
                Faction.Friendly => dbgCfg.PlayerBulletHitboxColor,
                Faction.Hostile => dbgCfg.BossBulletHitboxColor,
                Faction.Neutral => ColorPalette.Gray,
                _ => GetFallbackFactionColor(hitbox.Faction, dbgCfg.BossBulletHitboxColor)
            };

            float worldCenterX = trans.X + hitbox.Shape.OffsetX;
            float worldCenterY = trans.Y + hitbox.Shape.OffsetY;
            DrawCollisionShape(hitbox.Shape, worldCenterX, worldCenterY, hitColor);
        });
    }

    private static void DrawCollisionShape(CollisionShape shape, float worldCenterX, float worldCenterY, Color color)
    {
        switch (shape.Type)
        {
            case CollisionShapeType.Circle:
                int cx = (int)worldCenterX;
                int cy = (int)worldCenterY;
                int r = (int)shape.Radius;
                GameEngine.DrawCircle(cx, cy, r, color, false);
                break;

            case CollisionShapeType.Box:
                int x1 = (int)(worldCenterX - shape.Width / 2.0f);
                int y1 = (int)(worldCenterY - shape.Height / 2.0f);
                int x2 = (int)(worldCenterX + shape.Width / 2.0f);
                int y2 = (int)(worldCenterY + shape.Height / 2.0f);
                GameEngine.DrawBox(x1, y1, x2, y2, color, false);
                break;

            case CollisionShapeType.Point:
                GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 2, color, false);
                break;

            default:
                Console.WriteLine($"[DebugRender 警告] 未知の CollisionShape 型です: {shape.Type}");
                break;
        }
    }

    private static void DrawPlatformNavGraph(TileMap? tileMap)
    {
        if (tileMap == null || tileMap.NavGraph.Nodes.Count == 0)
        {
            return;
        }

        PlatformNavigationGraph graph = tileMap.NavGraph;
        Color nodeColor = ColorPalette.Cyan;
        Color edgeDropColor = ColorPalette.Gold;
        Color edgeJumpColor = ColorPalette.Green;

        foreach (PlatformNode node in graph.Nodes)
        {
            int nx = (int)node.CenterX;
            int ny = (int)node.TopY;
            GameEngine.DrawCircle(nx, ny, 4, nodeColor, true);
        }

        foreach (PlatformLink link in graph.Links)
        {
            int fx = (int)link.TakeoffX;
            int fy = (int)link.FromNode.TopY;
            int tx = (int)link.LandingX;
            int ty = (int)link.ToNode.TopY;
            Color edgeColor = link.Action == NavActionType.Drop ? edgeDropColor : edgeJumpColor;
            GameEngine.DrawLine(fx, fy, tx, ty, edgeColor);
        }
    }

    private static Color GetFallbackFactionColor(Faction faction, Color defaultColor)
    {
        return faction switch
        {
            Faction.Friendly => ColorPalette.Green,
            Faction.Hostile => ColorPalette.Red,
            _ => defaultColor
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _hurtboxQuery.Dispose();
            _hitboxQuery.Dispose();
            _isDisposed = true;
        }
    }
}
