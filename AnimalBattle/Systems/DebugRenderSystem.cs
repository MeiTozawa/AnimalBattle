using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Scenes;

namespace AnimalBattle.Systems;

/// <summary>
/// Bevy スタイルのコンポーネント駆動 (Component-Driven) デバッグ判定枠可视化システム。
/// 代数的データ型 <see cref="CollisionShape"/> のパターンマッチングにより、
/// 実際の物理・受撃・攻撃判定の輪郭線（ワイヤーフレーム）のみをシンプルにリアルタイム描画します。
/// F1 または H キーでリアルタイムに ON / OFF を切り替え可能です。
/// </summary>
public static class DebugRenderSystem
{
    private static bool _prevToggleKey;

    /// <summary>
    /// デバッグ衝突枠描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddDebugRenderSystem(this World world)
    {
        Query<Transform, CollisionBox> colQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .Build();

        Query<Transform, Hurtbox> hurtboxQuery =
            world.QueryBuilder<Transform, Hurtbox>()
                 .Build();

        Query<Transform, Hitbox> hitboxQuery =
            world.QueryBuilder<Transform, Hitbox>()
                 .Build();

        world.System("DebugRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 DebugRenderConfig dbgCfg = renderCfg.Debug;

                 ProcessToggleInput(dbgCfg);

                 if (!dbgCfg.Enabled)
                 {
                     return;
                 }

                 DrawCollisionBoxes(colQuery, dbgCfg);
                 DrawHurtboxes(hurtboxQuery, dbgCfg);
                 DrawHitboxes(hitboxQuery, dbgCfg);
                 DrawPlatformNavGraph(BattleScene.CurrentTileMap);
             });

        return world;
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
        switch (shape)
        {
            case CollisionShape.Circle circle:
                int cx = (int)worldCenterX;
                int cy = (int)worldCenterY;
                int r = (int)circle.Radius;
                GameEngine.DrawCircle(cx, cy, r, color, false);
                break;

            case CollisionShape.Box box:
                int x1 = (int)(worldCenterX - box.Width / 2.0f);
                int y1 = (int)(worldCenterY - box.Height / 2.0f);
                int x2 = (int)(worldCenterX + box.Width / 2.0f);
                int y2 = (int)(worldCenterY + box.Height / 2.0f);
                GameEngine.DrawBox(x1, y1, x2, y2, color, false);
                break;

            case CollisionShape.Point:
                GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 2, color, false);
                break;

            default:
                Console.WriteLine($"[DebugRender 警告] 未知の CollisionShape 型です: {shape?.GetType().Name}");
                break;
        }
    }

    private static void DrawPlatformNavGraph(TileMap? tileMap)
    {
        if (tileMap == null || tileMap.NavGraph.Nodes.Count == 0)
        {
            return;
        }

        foreach (PlatformNode node in tileMap.NavGraph.Nodes)
        {
            int nx1 = (int)node.LeftX;
            int nx2 = (int)node.RightX;
            int ny = (int)node.TopY;
            GameEngine.DrawLine(nx1, ny, nx2, ny, ColorPalette.Cyan, 3);
            GameEngine.DrawString(nx1 + 5, ny - 16, $"Plat #{node.Id} (W:{node.Width:F0})", ColorPalette.Cyan);
        }

        Boss2Config b2Cfg = ConfigManager.Boss2;
        float gravity = b2Cfg.Gravity > 0.0f ? b2Cfg.Gravity : 980.0f;

        foreach (PlatformLink link in tileMap.NavGraph.Links)
        {
            float tx = link.TakeoffX;
            float ty = link.FromNode.TopY;
            float lx = link.LandingX;
            float ly = link.ToNode.TopY;

            Color linkColor = link.Action == NavActionType.Jump ? ColorPalette.Green : ColorPalette.Amber;

            if (link.Action == NavActionType.Jump)
            {
                float vx = link.RequiredJumpVx;
                float vy = link.RequiredJumpVy;
                float a = 0.5f * gravity;
                float b = vy;
                float c = ty - ly;
                float disc = b * b - 4.0f * a * c;
                float tTotal = disc >= 0.0f ? (-b + MathF.Sqrt(disc)) / (2.0f * a) : (MathF.Abs(lx - tx) / MathF.Max(1.0f, MathF.Abs(vx)));
                if (tTotal <= 0.01f)
                {
                    tTotal = 0.5f;
                }

                int segments = 12;
                float prevX = tx;
                float prevY = ty;
                for (int s = 1; s <= segments; s++)
                {
                    float t = tTotal * (s / (float)segments);
                    float curX = tx + vx * t;
                    float curY = ty + vy * t + 0.5f * gravity * t * t;
                    GameEngine.DrawLine((int)prevX, (int)prevY, (int)curX, (int)curY, linkColor, 1);
                    prevX = curX;
                    prevY = curY;
                }
            }
            else
            {
                float dropHeight = MathF.Max(1.0f, ly - ty);
                float tFall = MathF.Sqrt(2.0f * dropHeight / gravity);
                float vx = link.RequiredJumpVx;

                int segments = 10;
                float prevX = tx;
                float prevY = ty;
                for (int s = 1; s <= segments; s++)
                {
                    float t = tFall * (s / (float)segments);
                    float curX = tx + vx * t;
                    float curY = ty + 0.5f * gravity * t * t;
                    GameEngine.DrawLine((int)prevX, (int)prevY, (int)curX, (int)curY, linkColor, 1);
                    prevX = curX;
                    prevY = curY;
                }
            }

            GameEngine.DrawCircle((int)tx, (int)ty, 4, linkColor, true);
            GameEngine.DrawCircle((int)lx, (int)ly, 3, ColorPalette.White, true);
        }
    }

    private static Color GetFallbackFactionColor(Faction faction, Color defaultColor)
    {
        Console.WriteLine($"[DebugRender 警告] 未知の Faction です: {faction}。デフォルト色を使用します。");
        return defaultColor;
    }
}
