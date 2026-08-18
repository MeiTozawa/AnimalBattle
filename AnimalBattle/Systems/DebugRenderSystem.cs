using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

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
    public static void Register(World world)
    {
        world.System("DebugRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 DebugRenderConfig dbgCfg = renderCfg.Debug;

                 bool isToggleKey = GameEngine.CheckHitKey(0x3B) || GameEngine.CheckHitKey(0x23);
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
                              Faction.Hostile => dbgCfg.BossHitboxColor,
                              Faction.Neutral => ColorPalette.Gray,
                              _ => GetFallbackFactionColor(hurtbox.Faction, dbgCfg.BossHitboxColor)
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
                                  break;

                              case CollisionShape.Box box:
                                  int x1 = (int)(worldCenterX - box.Width / 2.0f);
                                  int y1 = (int)(worldCenterY - box.Height / 2.0f);
                                  int x2 = (int)(worldCenterX + box.Width / 2.0f);
                                  int y2 = (int)(worldCenterY + box.Height / 2.0f);
                                  GameEngine.DrawBox(x1, y1, x2, y2, hurtColor, false);
                                  break;

                              case CollisionShape.Point:
                                  GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 2, hurtColor, false);
                                  break;

                              default:
                                  Console.WriteLine($"[DebugRender 警告] 未知の CollisionShape 型です: {hurtbox.Shape?.GetType().Name}");
                                  break;
                          }
                      });

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
                              Faction.Hostile => dbgCfg.BossBulletHitboxColor,
                              Faction.Neutral => ColorPalette.Gray,
                              _ => GetFallbackFactionColor(hitbox.Faction, dbgCfg.BossBulletHitboxColor)
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
                                  break;

                              case CollisionShape.Box box:
                                  int x1 = (int)(worldCenterX - box.Width / 2.0f);
                                  int y1 = (int)(worldCenterY - box.Height / 2.0f);
                                  int x2 = (int)(worldCenterX + box.Width / 2.0f);
                                  int y2 = (int)(worldCenterY + box.Height / 2.0f);
                                  GameEngine.DrawBox(x1, y1, x2, y2, hitColor, false);
                                  break;

                              case CollisionShape.Point:
                                  GameEngine.DrawCircle((int)worldCenterX, (int)worldCenterY, 2, hitColor, false);
                                  break;

                              default:
                                  Console.WriteLine($"[DebugRender 警告] 未知の CollisionShape 型です: {hitbox.Shape?.GetType().Name}");
                                  break;
                          }
                      });
             });
    }

    private static Color GetFallbackFactionColor(Faction faction, Color defaultColor)
    {
        Console.WriteLine($"[DebugRender 警告] 未知の Faction です: {faction}。デフォルト色を使用します。");
        return defaultColor;
    }
}
