using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 弾、Boss（FSM 預警光環・無敵盾）、プレイヤー、プラットフォーム、および HUD・状態機バナーを
/// Flecs.NET ECS 常駐 Routine (System) として描画登録・管理する静的クラス。
/// </summary>
public static class RenderSystems
{
    /// <summary>
    /// 全描画関連の ECS 常駐 Routine (System) を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void RegisterSystems(World world)
    {
        RegisterWorldRenderSystem(world);
        RegisterHudRenderSystem(world);
    }

    private static void RegisterWorldRenderSystem(World world)
    {
        world.System("WorldRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 world.QueryBuilder<Transform, CollisionBox>()
                      .With<PlatformTag>()
                      .Build()
                      .Each((ref Transform trans, ref CollisionBox col) =>
                      {
                          int x1 = (int)(trans.X - col.Width / 2.0f);
                          int y1 = (int)(trans.Y - col.Height / 2.0f);
                          int x2 = (int)(trans.X + col.Width / 2.0f);
                          int y2 = (int)(trans.Y + col.Height / 2.0f);

                          GameEngine.DrawBox(x1, y1, x2, y2, 0xFF141926, true);
                          GameEngine.DrawBox(x1, y1, x2, y2, 0xFF00E5FF, false);
                          GameEngine.DrawBox(x1 + 2, y1 + 2, x2 - 2, y1 + 4, 0xFF80FFFF, true);
                      });

                 world.QueryBuilder<Transform, BossStats, BossStateMachine, Sprite>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform trans, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
                      {
                          uint auraColor = stats.Phase switch
                          {
                              1 => 0xFF00E5FF,
                              2 => 0xFFBA55D3,
                              _ => 0xFFFF1744
                          };

                          int bx = (int)trans.X;
                          int by = (int)trans.Y;

                          if (fsm.CurrentState == BossStateKind.Telegraphing)
                          {
                              float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                              int ringRadius = (int)(140.0f - progress * 60.0f);
                              GameEngine.DrawCircle(bx, by, ringRadius, 0xFFFF3333, false);
                              GameEngine.DrawCircle(bx, by, ringRadius - 2, 0xFFFFFFFF, false);
                              GameEngine.DrawCircle(bx, by, 35, 0x55FF2222, true);
                          }
                          else if (fsm.CurrentState == BossStateKind.PhaseTransition || fsm.IsInvulnerable)
                          {
                              int shieldRadius = 85 + (int)(MathF.Sin(fsm.StateTimer * 12.0f) * 6.0f);
                              GameEngine.DrawCircle(bx, by, shieldRadius, 0xFFFFD700, false);
                              GameEngine.DrawCircle(bx, by, shieldRadius - 3, 0xFFFFF275, false);
                              GameEngine.DrawCircle(bx, by, shieldRadius - 8, 0x44FFFF88, true);
                          }
                          else if (fsm.CurrentState == BossStateKind.Recovering)
                          {
                              GameEngine.DrawCircle(bx, by, 65, 0x3300E5FF, true);
                          }

                          int auraRadius = 75 + (int)(MathF.Sin(stats.HoverTimer * 4.0f) * 8.0f);
                          GameEngine.DrawCircle(bx, by, auraRadius, auraColor, false);
                          GameEngine.DrawCircle(bx, by, auraRadius - 2, auraColor, false);

                          if (sprite.IsVisible && sprite.GraphHandle >= 0)
                          {
                              double bossAngle = MathF.Sin(stats.HoverTimer * 2.0f) * 0.08;
                              GameEngine.DrawRectRotaGraph(
                                  bx,
                                  by,
                                  sprite.SrcRect.X1,
                                  sprite.SrcRect.Y1,
                                  sprite.SrcRect.Width,
                                  sprite.SrcRect.Height,
                                  2.8,
                                  bossAngle,
                                  sprite.GraphHandle,
                                  true,
                                  false);
                          }

                          if (stats.HitFlashTimer > 0.0f)
                          {
                              GameEngine.DrawCircle(bx, by, 45, 0x88FFFFFF, true);
                          }
                      });

                 world.QueryBuilder<Transform, PlayerState, Sprite>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform trans, ref PlayerState playerState, ref Sprite sprite) =>
                      {
                          if (!playerState.IsAlive || !sprite.IsVisible || sprite.GraphHandle < 0)
                          {
                              return;
                          }

                          GameEngine.DrawRectRotaGraph(
                              (int)trans.X,
                              (int)trans.Y,
                              sprite.SrcRect.X1,
                              sprite.SrcRect.Y1,
                              sprite.SrcRect.Width,
                              sprite.SrcRect.Height,
                              trans.ScaleX,
                              trans.Rotation,
                              sprite.GraphHandle,
                              true,
                              sprite.FlipX);

                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 3, 0xFFFF0033, true);
                          GameEngine.DrawCircle((int)trans.X, (int)trans.Y, 4, 0xFFFFFFFF, false);
                      });

                 world.QueryBuilder<Transform>()
                      .With<PlayerBulletTag>()
                      .Build()
                      .Each((ref Transform trans) =>
                      {
                          int px = (int)trans.X;
                          int py = (int)trans.Y;
                          GameEngine.DrawCircle(px, py, 6, 0xFF00FFFF, true);
                          GameEngine.DrawCircle(px, py, 3, 0xFFFFFFFF, true);
                          GameEngine.DrawBox(px - 7, py - 2, px + 7, py + 2, 0xFF80FFFF, true);
                      });

                 world.QueryBuilder<Transform, Bullet>()
                      .With<BossBulletTag>()
                      .Build()
                      .Each((ref Transform trans, ref Bullet bullet) =>
                      {
                          int bx = (int)trans.X;
                          int by = (int)trans.Y;
                          int radius = (int)bullet.Radius;

                          GameEngine.DrawCircle(bx, by, radius + 2, bullet.OutlineColor, true);
                          GameEngine.DrawCircle(bx, by, radius, bullet.FillColor, true);
                          int coreRadius = Math.Max(2, (int)(radius * 0.45f));
                          GameEngine.DrawCircle(bx, by, coreRadius, 0xFFFFFFFF, true);
                      });
             });
    }

    private static void RegisterHudRenderSystem(World world)
    {
        world.System("HudRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 // 1. 操作説明とタイトル
                 GameEngine.DrawString(20, 16, "=== BULLET BATTLE (TOML FSM) ===", 0xFFFFD700);
                 GameEngine.DrawString(20, 36, "[A/D/Left/Right]: Move (Instant Stop)", 0xFFCCCCCC);
                 GameEngine.DrawString(20, 54, "[W/Space/Up]: Jump & Air Double Jump", 0xFFCCCCCC);
                 GameEngine.DrawString(20, 72, "[J/Z]: Shoot Pistol", 0xFFCCCCCC);
                 GameEngine.DrawString(20, 90, "[R]: Instant Restart (1-Frame Reset)", 0xFFFF8080);

                 // 2. プレイヤージャンプ残数表示
                 world.QueryBuilder<PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref PlayerState playerState) =>
                      {
                          string jumpSymbol = playerState.JumpCount switch
                          {
                              2 => "[ ●  ● ]",
                              1 => "[ ●  ○ ]",
                              _ => "[ ○  ○ ]"
                          };
                          uint jumpColor = playerState.JumpCount > 0 ? 0xFF00E5FF : 0xFF888888;
                          GameEngine.DrawString(20, 116, $"DOUBLE JUMP: {jumpSymbol}", jumpColor);
                      });

                 world.QueryBuilder<BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref BossStats stats, ref BossStateMachine fsm) =>
                      {
                          const int barX1 = 300;
                          const int barY1 = 18;
                          const int barX2 = 980;
                          const int barY2 = 44;
                          const int totalWidth = barX2 - barX1;

                          GameEngine.DrawBox(barX1 - 4, barY1 - 4, barX2 + 4, barY2 + 4, 0xCC0A0D14, true);
                          GameEngine.DrawBox(barX1 - 4, barY1 - 4, barX2 + 4, barY2 + 4, 0xFFFFFFFF, false);

                          uint hpFillColor = stats.Phase switch
                          {
                              1 => 0xFF00E5FF,
                              2 => 0xFFBA55D3,
                              _ => 0xFFFF1744
                          };

                          float hpRatio = Math.Clamp(stats.CurrentHp / stats.MaxHp, 0.0f, 1.0f);
                          int fillWidth = (int)(totalWidth * hpRatio);
                          if (fillWidth > 0)
                          {
                              GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
                              GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + 4, 0xFFFFFFFF, true);
                          }

                          int p1Split = barX1 + totalWidth / 3;
                          int p2Split = barX1 + (totalWidth * 2) / 3;
                          GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, 0xFFFFFFFF, true);
                          GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, 0xFFFFFFFF, true);

                          string bossTitle = $"BOSS: ANCIENT CAT OVERLORD [PHASE {stats.Phase}/3] HP: {stats.CurrentHp:F0} / {stats.MaxHp:F0}";
                          GameEngine.DrawString(barX1 + 10, barY1 + 5, bossTitle, 0xFFFFFFFF);

                          string stateName = fsm.CurrentState switch
                          {
                              BossStateKind.Hovering => "[STATE: HOVERING (IDLE)]",
                              BossStateKind.Telegraphing => $"[STATE: TELEGRAPHING (WIND-UP: {fsm.CurrentSkillName})]",
                              BossStateKind.Casting => $"[STATE: CASTING ({fsm.CurrentSkillName})]",
                              BossStateKind.Recovering => "[STATE: RECOVERING (VULNERABLE)]",
                              BossStateKind.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
                              _ => "[STATE: UNKNOWN]"
                          };

                          uint stateBadgeColor = fsm.CurrentState switch
                          {
                              BossStateKind.Telegraphing => 0xFFFF5555,
                              BossStateKind.Casting => 0xFFFFD700,
                              BossStateKind.Recovering => 0xFF00FF88,
                              BossStateKind.PhaseTransition => 0xFFFF88FF,
                              _ => 0xFF88CCFF
                          };

                          GameEngine.DrawString(barX1 + 10, barY2 + 8, $"{stateName} - Time: {fsm.StateTimer:F2}s / {fsm.StateDuration:F2}s", stateBadgeColor);
                      });

                 world.QueryBuilder<PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref PlayerState playerState) =>
                      {
                          if (!playerState.IsAlive)
                          {
                              GameEngine.DrawBox(360, 260, 920, 440, 0xDD2A0505, true);
                              GameEngine.DrawBox(360, 260, 920, 440, 0xFFFF3333, false);
                              GameEngine.DrawBox(364, 264, 916, 436, 0xFFFF8888, false);

                              GameEngine.DrawString(540, 305, "=== GAME OVER ===", 0xFFFF3333);
                              GameEngine.DrawString(460, 360, "Press  [ R ]  for Instant Retry!", 0xFFFFFFFF);
                          }
                      });

                 world.QueryBuilder<BossStats>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref BossStats stats) =>
                      {
                          if (stats.CurrentHp <= 0.0f)
                          {
                              GameEngine.DrawBox(340, 250, 940, 450, 0xEE2A2A00, true);
                              GameEngine.DrawBox(340, 250, 940, 450, 0xFFFFD700, false);
                              GameEngine.DrawBox(344, 254, 936, 446, 0xFFFFF275, false);

                              GameEngine.DrawString(440, 295, "★ ★ ★ VICTORY! BOSS DEFEATED! ★ ★ ★", 0xFFFFD700);
                              GameEngine.DrawString(460, 350, "Congratulations! Press [ R ] to Play Again!", 0xFFFFFFFF);
                          }
                      });
             });
    }
}
