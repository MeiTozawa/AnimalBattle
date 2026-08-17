using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Bevy スタイルのコンポーネント駆動 (Component-Driven) デバッグ判定枠可視化システム。
/// 代数的データ型 <see cref="CollisionShape"/> のパターンマッチングにより、
/// 各種判定バリアント (Circle, Box, Point) の世界座標ワイヤーフレームをリアルタイム描画します。
/// F1 または H キーでリアルタイムに ON / OFF を切り替え可能です。
/// </summary>
public static class DebugRenderSystem
{
    private static bool _prevToggleKey;

    /// <summary>
    /// デバッグ衝突枠描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System("DebugRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 DebugRenderConfig dbgCfg = renderCfg.Debug;

                 // 1. F1 (0x3B) / H (0x23) キーによるリアルタイム表示切替
                 bool isToggleKey = GameEngine.CheckHitKey(0x3B) != 0 || GameEngine.CheckHitKey(0x23) != 0;
                 bool toggleJustPressed = isToggleKey && !_prevToggleKey;
                 _prevToggleKey = isToggleKey;

                 if (toggleJustPressed)
                 {
                     dbgCfg.Enabled = !dbgCfg.Enabled;
                 }

                 if (!dbgCfg.Enabled)
                 {
                     return;
                 }

                 // 2. 地形・足場衝突ボックス (CollisionBox) の汎用描画
                 world.QueryBuilder<Transform, CollisionBox>()
                      .Build()
                      .Each((Entity e, ref Transform trans, ref CollisionBox col) =>
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

                 // 3. 全ての受撃判定ボックス (Hurtbox) のパターンマッチング描画（OffsetX/Y 反映）
                 world.QueryBuilder<Transform, Hurtbox>()
                      .Build()
                      .Each((Entity e, ref Transform trans, ref Hurtbox hurtbox) =>
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
                              _ => dbgCfg.BossHitboxColor
                          };

                          float worldCenterX = trans.X + hurtbox.Shape.OffsetX;
                          float worldCenterY = trans.Y + hurtbox.Shape.OffsetY;

                          switch (hurtbox.Shape)
                          {
                              case CollisionShape.Circle circle:
                                  int cx = (int)worldCenterX;
                                  int cy = (int)worldCenterY;
                                  int r = (int)circle.Radius;
                                  GameEngine.DrawCircle(cx, cy, r, hurtColor, false);
                                  GameEngine.DrawCircle(cx, cy, 2, hurtColor, true);

                                  if (MathF.Abs(circle.OffsetX) > 0.1f || MathF.Abs(circle.OffsetY) > 0.1f)
                                  {
                                      GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 2, ColorPalette.White, false);
                                  }
                                  break;

                              case CollisionShape.Box box:
                                  int x1 = (int)(worldCenterX - box.Width / 2.0f);
                                  int y1 = (int)(worldCenterY - box.Height / 2.0f);
                                  int x2 = (int)(worldCenterX + box.Width / 2.0f);
                                  int y2 = (int)(worldCenterY + box.Height / 2.0f);
                                  GameEngine.DrawBox(x1, y1, x2, y2, hurtColor, false);
                                  GameEngine.DrawBox(x1 + 1, y1 + 1, x2 - 1, y2 - 1, hurtColor, false);
                                  break;

                              case CollisionShape.Point:
                                  GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 2, hurtColor, true);
                                  break;
                          }
                      });

                 // 4. 全ての攻撃判定ボックス (Hitbox) のパターンマッチング描画（OffsetX/Y 反映）
                 world.QueryBuilder<Transform, Hitbox>()
                      .Build()
                      .Each((Entity e, ref Transform trans, ref Hitbox hitbox) =>
                      {
                          if (!hitbox.IsActive)
                          {
                              return;
                          }

                          Color hitColor = hitbox.Faction switch
                          {
                              Faction.Friendly => dbgCfg.PlayerBulletHitboxColor,
                              _ => dbgCfg.BossBulletHitboxColor
                          };

                          float worldCenterX = trans.X + hitbox.Shape.OffsetX;
                          float worldCenterY = trans.Y + hitbox.Shape.OffsetY;

                          switch (hitbox.Shape)
                          {
                              case CollisionShape.Circle circle:
                                  int cx = (int)worldCenterX;
                                  int cy = (int)worldCenterY;
                                  int r = (int)circle.Radius;
                                  GameEngine.DrawCircle(cx, cy, r, hitColor, false);
                                  GameEngine.DrawCircle(cx, cy, 1, hitColor, true);
                                  break;

                              case CollisionShape.Box box:
                                  int x1 = (int)(worldCenterX - box.Width / 2.0f);
                                  int y1 = (int)(worldCenterY - box.Height / 2.0f);
                                  int x2 = (int)(worldCenterX + box.Width / 2.0f);
                                  int y2 = (int)(worldCenterY + box.Height / 2.0f);
                                  GameEngine.DrawBox(x1, y1, x2, y2, hitColor, false);
                                  break;

                              case CollisionShape.Point:
                                  GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 1, hitColor, true);
                                  break;
                          }
                      });
             });
    }
}
