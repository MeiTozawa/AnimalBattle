using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Scenes;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss 2 (Ground Crusher) の地上重力・単向プラットフォーム衝突・巡回追従、
/// 平台跳躍、近接重劈・二連撃、撼地踏撃、狂暴突進および滅地狂吼を司る ECS System。
/// </summary>
public static class Boss2AiSystem
{
    /// <summary>
    /// Boss 2 AI システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddBoss2AiSystem(this World world)
    {
        Query<Transform, Health> playerQuery =
            world.QueryBuilder<Transform, Health>()
                 .With<PlayerTag>()
                 .Build();

        Query<Transform, CollisionBox> platformQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build();

        world.System<Transform, Velocity, CollisionBox, Health, Boss2Stats, Boss2StateMachine, Sprite>("Boss2AiSystem")
             .With<Boss2Tag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health bHealth, ref Boss2Stats stats, ref Boss2StateMachine fsm, ref Sprite sprite) =>
             {
                 Boss2Config bCfg = ConfigManager.Boss2;
                 PlayerConfig pCfg = ConfigManager.Player;
                 PatternConfig patternCfg = ConfigManager.Pattern;
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 float dt = it.DeltaTime();

                 float playerX = pCfg.SpawnX;
                 float playerY = pCfg.SpawnY;
                 bool isPlayerAlive = false;

                 playerQuery.Each((ref Transform pTrans, ref Health pHealth) =>
                 {
                     playerX = pTrans.X;
                     playerY = pTrans.Y;
                     isPlayerAlive = pHealth.IsAlive;
                 });

                 if (stats.HitFlashTimer > 0.0f)
                 {
                     stats.HitFlashTimer = Math.Max(0.0f, stats.HitFlashTimer - dt);
                 }

                 if (stats.JumpCooldown > 0.0f)
                 {
                     stats.JumpCooldown = Math.Max(0.0f, stats.JumpCooldown - dt);
                 }

                 if (!bHealth.IsAlive)
                 {
                     bHealth.Current = 0.0f;
                     vel.Vx = 0.0f;
                     vel.Vy = 0.0f;
                     return;
                 }

                 float hpRatio = bHealth.Ratio;
                 int targetPhase = hpRatio > bCfg.Phase2.HpThreshold ? 1 : (hpRatio > bCfg.Phase3.HpThreshold ? 2 : 3);

                 if (targetPhase > stats.Phase && fsm.CurrentState != Boss2StateKind.PhaseTransition)
                 {
                     stats.Phase = targetPhase;
                     fsm.CurrentState = Boss2StateKind.PhaseTransition;
                     fsm.StateTimer = 0.0f;
                     fsm.StateDuration = bCfg.Phase1.TransitionDuration;
                     fsm.IsInvulnerable = true;
                     vel.Vx = 0.0f;
                     CombatEffectHelper.ClearAllBossBullets(world);
                     CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
                 }

                 Boss2PhaseConfig currentPhaseCfg = stats.Phase switch
                 {
                     1 => bCfg.Phase1,
                     2 => bCfg.Phase2,
                     3 => bCfg.Phase3,
                     _ => GetFallbackPhaseConfig(stats.Phase, bCfg)
                 };

                 fsm.StateTimer += dt;
                 stats.PatrolTimer += dt;

                 // 状態機更新
                 switch (fsm.CurrentState)
                 {
                      case Boss2StateKind.PatrolChase:
                          fsm.IsInvulnerable = false;
                          float bossFootY = trans.Y + col.Height / 2.0f;
                          float playerFootY = playerY + pCfg.HitboxHeight / 2.0f;

                          TileMap? tileMap = BattleScene.CurrentTileMap;
                          if (col.IsGrounded)
                          {
                              if (tileMap != null && tileMap.NavGraph.Nodes.Count > 0)
                              {
                                  NavPlan plan = tileMap.NavGraph.FindPath(
                                      trans.X,
                                      bossFootY,
                                      playerX,
                                      playerFootY,
                                      col.IsGrounded,
                                      currentPhaseCfg.MoveSpeed);

                                  switch (plan.Status)
                                  {
                                      case NavStatus.DirectWalk:
                                          float dir = playerX >= trans.X ? 1.0f : -1.0f;
                                          vel.Vx = dir * currentPhaseCfg.MoveSpeed;
                                          break;

                                      case NavStatus.MoveToTakeoff:
                                          vel.Vx = plan.RequiredVx;
                                          break;

                                      case NavStatus.ExecuteJump:
                                          if (stats.JumpCooldown <= 0.0f)
                                          {
                                              vel.Vx = plan.RequiredVx;
                                              vel.Vy = plan.RequiredVy != 0.0f ? plan.RequiredVy : -currentPhaseCfg.JumpForce;
                                              stats.JumpCooldown = 1.0f;
                                              col.IsGrounded = false;
                                              trans.Y -= 6.0f;
                                          }
                                          break;

                                      case NavStatus.ExecuteDrop:
                                          vel.Vx = plan.RequiredVx;
                                          break;

                                      case NavStatus.InAir:
                                      case NavStatus.NoPath:
                                      default:
                                          float defaultDir = playerX >= trans.X ? 1.0f : -1.0f;
                                          vel.Vx = defaultDir * currentPhaseCfg.MoveSpeed;
                                          break;
                                  }
                              }
                              else
                              {
                                  float dir = playerX >= trans.X ? 1.0f : -1.0f;
                                  vel.Vx = dir * currentPhaseCfg.MoveSpeed;
                              }
                          }

                          // 速度駆動の朝向更新（微小位置振動による左右反転を防ぐ死区付き）
                          if (vel.Vx > 15.0f)
                          {
                              stats.FacingRight = true;
                          }
                          else if (vel.Vx < -15.0f)
                          {
                              stats.FacingRight = false;
                          }
                          else if (MathF.Abs(playerX - trans.X) > 20.0f)
                          {
                              stats.FacingRight = playerX >= trans.X;
                          }
                          sprite.FlipX = !stats.FacingRight;

                          string pendingSkill = currentPhaseCfg.Skills.Count > 0
                              ? currentPhaseCfg.Skills[fsm.CurrentSkillIndex % currentPhaseCfg.Skills.Count]
                              : "HeavySlash";

                          NavStatus currentNavStatus = tileMap != null && tileMap.NavGraph.Nodes.Count > 0
                              ? tileMap.NavGraph.FindPath(trans.X, bossFootY, playerX, playerFootY, col.IsGrounded, currentPhaseCfg.MoveSpeed).Status
                              : NavStatus.DirectWalk;

                          bool inRange = CheckBoss2SkillRange(pendingSkill, trans.X, trans.Y, playerX, playerY, currentNavStatus);

                          // 防風箏機制 (Anti-Kiting)：プレイヤーが離れていて 2.5 秒以上接近できない場合は泰坦躍撃 LeapSmash に切り替えて強襲
                          bool isKitingTimeout = fsm.StateTimer >= 2.5f && col.IsGrounded;
                          if (isKitingTimeout && !inRange)
                          {
                              pendingSkill = "LeapSmash";
                              inRange = true;
                          }

                          // プレイヤーが射程内に入り、かつ接地中で最低巡航時間 (IdleDuration) を満たした場合に出招開始
                          if (inRange && fsm.StateTimer >= currentPhaseCfg.IdleDuration && isPlayerAlive && col.IsGrounded)
                          {
                              vel.Vx = 0.0f;
                              fsm.CurrentSkillName = pendingSkill;
                              if (currentPhaseCfg.Skills.Count > 0)
                              {
                                  fsm.CurrentSkillIndex = (fsm.CurrentSkillIndex + 1) % currentPhaseCfg.Skills.Count;
                              }

                              float windUp = GetBoss2SkillWindUpTime(fsm.CurrentSkillName, patternCfg);
                              Color telegraphColor = GetBoss2SkillTelegraphColor(fsm.CurrentSkillName, patternCfg);

                              fsm.CurrentState = Boss2StateKind.Telegraphing;
                              fsm.StateTimer = 0.0f;
                              fsm.StateDuration = windUp;
                              fsm.SubTimer = 0.0f;
                              fsm.StartX = trans.X;
                              fsm.StartY = trans.Y;
                              fsm.TargetX = playerX;
                              fsm.TargetY = playerY;

                              if (fsm.CurrentSkillName == "GroundStomp")
                              {
                                  float groundY = trans.Y + bCfg.CollisionHeight / 2.0f - 10.0f;
                                  CombatEffectHelper.SpawnTelegraphSpark(world, trans.X - 35.0f, groundY, telegraphColor);
                                  CombatEffectHelper.SpawnTelegraphSpark(world, trans.X + 35.0f, groundY, telegraphColor);
                              }
                              else
                              {
                                  float forwardDir = stats.FacingRight ? 1.0f : -1.0f;
                                  float sparkX = trans.X + forwardDir * bCfg.SlashSpawnOffsetX;
                                  float sparkY = trans.Y + bCfg.SlashSpawnOffsetY;
                                  CombatEffectHelper.SpawnTelegraphSpark(world, sparkX, sparkY, telegraphColor);
                              }
                          }
                          break;

                     case Boss2StateKind.Telegraphing:
                         fsm.IsInvulnerable = false;
                         stats.FacingRight = playerX >= trans.X;
                         sprite.FlipX = !stats.FacingRight;
                         vel.Vx = 0.0f;

                         float shake = currentPhaseCfg.TelegraphShakeIntensity;
                         if (shake > 0.0f)
                         {
                             trans.X += (Random.Shared.NextSingle() - 0.5f) * shake;
                         }

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.StartX = trans.X;
                             fsm.StartY = trans.Y;
                             fsm.TargetX = playerX;
                             fsm.TargetY = playerY;
                             fsm.SubTimer = 0.0f;

                             if (fsm.CurrentSkillName == "LeapSmash")
                             {
                                 fsm.CurrentState = Boss2StateKind.LeapJumping;
                                 fsm.StateTimer = 0.0f;
                                 fsm.StateDuration = patternCfg.LeapSmash.CastDuration;
                                 vel.Vy = -patternCfg.LeapSmash.JumpForce;
                                 col.IsGrounded = false;
                                 float horizontalDist = playerX - trans.X;
                                 vel.Vx = Math.Clamp(horizontalDist / 0.7f, -450.0f, 450.0f);
                             }
                             else
                             {
                                 float castDuration = GetBoss2SkillCastDuration(fsm.CurrentSkillName, patternCfg);
                                 fsm.CurrentState = Boss2StateKind.Casting;
                                 fsm.StateTimer = 0.0f;
                                 fsm.StateDuration = castDuration;
                                 stats.ComboIndex = 0;
                             }
                         }
                         break;

                     case Boss2StateKind.Casting:
                         fsm.IsInvulnerable = false;
                         ExecuteBoss2Skill(world, ref fsm, patternCfg, ref trans, ref vel, ref col, ref stats, playerX, playerY, dt);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             vel.Vx = 0.0f;
                             float recoveryTime = GetBoss2SkillRecoveryTime(fsm.CurrentSkillName, patternCfg);
                             fsm.CurrentState = Boss2StateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = recoveryTime;
                         }
                         break;

                     case Boss2StateKind.LeapJumping:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer > 0.15f && col.IsGrounded)
                         {
                             vel.Vx = 0.0f;
                             LeapSmashConfig leapCfg = patternCfg.LeapSmash;
                             CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y + col.Height / 2.0f);

                             for (int k = 0; k < leapCfg.ShrapnelCount; k++)
                             {
                                 float angle = -MathF.PI * (0.15f + 0.7f * (k / (float)(leapCfg.ShrapnelCount - 1)));
                                 CombatEffectHelper.SpawnBossBullet(world, trans.X, trans.Y + col.Height / 2.0f - 15.0f,
                                     MathF.Cos(angle) * leapCfg.ShrapnelSpeed,
                                     MathF.Sin(angle) * leapCfg.ShrapnelSpeed,
                                     leapCfg.ShrapnelRadius,
                                     leapCfg.ShrapnelFillColor,
                                     leapCfg.ShrapnelOutlineColor);
                             }

                             fsm.CurrentState = Boss2StateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = leapCfg.RecoveryTime;
                         }
                         break;

                     case Boss2StateKind.Recovering:
                         fsm.IsInvulnerable = false;
                         vel.Vx = 0.0f;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = Boss2StateKind.PatrolChase;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.IdleDuration;
                             if (currentPhaseCfg.Skills.Count > 0)
                             {
                                 fsm.CurrentSkillName = currentPhaseCfg.Skills[fsm.CurrentSkillIndex % currentPhaseCfg.Skills.Count];
                             }
                         }
                         break;

                     case Boss2StateKind.PhaseTransition:
                         fsm.IsInvulnerable = true;
                         vel.Vx = 0.0f;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = Boss2StateKind.PatrolChase;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.IdleDuration;
                             fsm.IsInvulnerable = false;
                             if (currentPhaseCfg.Skills.Count > 0)
                             {
                                 fsm.CurrentSkillName = currentPhaseCfg.Skills[fsm.CurrentSkillIndex % currentPhaseCfg.Skills.Count];
                             }
                         }
                         break;
                 }

                 // 地面重力与平台物理更新
                 float currentVy = vel.Vy;
                 PhysicsService.ApplyGravity(ref currentVy, bCfg.Gravity, 850.0f, dt);
                 vel.Vy = currentVy;

                 float nextX = trans.X + vel.Vx * dt;
                 float nextY = trans.Y + vel.Vy * dt;

                 nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

                 bool isGrounded = false;
                 float adjustedPosY = nextY;
                 float adjustedVy = vel.Vy;

                 float currentY = trans.Y;
                 Box2D bossBox = new(nextX, nextY, col.Width, col.Height);

                 platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
                 {
                     Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
                     if (PhysicsService.CheckPlatformGround(
                         currentY,
                         nextY,
                         bossBox,
                         platformBox,
                         out float groundY,
                         out float vy))
                     {
                         isGrounded = true;
                         adjustedPosY = groundY;
                         adjustedVy = vy;
                     }
                 });

                 float floorLimitY = 560.0f - col.Height / 2.0f;
                 if (adjustedPosY >= floorLimitY)
                 {
                     isGrounded = true;
                     adjustedPosY = floorLimitY;
                     adjustedVy = 0.0f;
                 }

                 trans.X = nextX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;
             });

        return world;
    }

    private static Boss2PhaseConfig GetFallbackPhaseConfig(int phase, Boss2Config bCfg)
    {
        Console.WriteLine($"[Boss2AI 警告] 無効な Boss フェーズ番号です: {phase}。Phase 1 を使用します。");
        return bCfg.Phase1;
    }

    private static float GetBoss2SkillWindUpTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "HeavySlash" => patternCfg.HeavySlash.WindUpTime,
        "GroundStomp" => patternCfg.GroundStomp.WindUpTime,
        "LeapSmash" => patternCfg.LeapSmash.WindUpTime,
        "DoubleCleave" => patternCfg.DoubleCleave.WindUpTime,
        "FrenzyCharge" => patternCfg.FrenzyCharge.WindUpTime,
        "EarthquakeRoar" => patternCfg.EarthquakeRoar.WindUpTime,
        _ => patternCfg.HeavySlash.WindUpTime
    };

    private static Color GetBoss2SkillTelegraphColor(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "HeavySlash" => patternCfg.HeavySlash.TelegraphColor,
        "GroundStomp" => patternCfg.GroundStomp.TelegraphColor,
        "LeapSmash" => patternCfg.LeapSmash.TelegraphColor,
        "DoubleCleave" => patternCfg.DoubleCleave.TelegraphColor,
        "FrenzyCharge" => patternCfg.FrenzyCharge.TelegraphColor,
        "EarthquakeRoar" => patternCfg.EarthquakeRoar.TelegraphColor,
        _ => ColorPalette.RedOrange
    };

    private static float GetBoss2SkillCastDuration(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "HeavySlash" => patternCfg.HeavySlash.CastDuration,
        "GroundStomp" => patternCfg.GroundStomp.CastDuration,
        "LeapSmash" => patternCfg.LeapSmash.CastDuration,
        "DoubleCleave" => patternCfg.DoubleCleave.CastDuration,
        "FrenzyCharge" => patternCfg.FrenzyCharge.CastDuration,
        "EarthquakeRoar" => patternCfg.EarthquakeRoar.CastDuration,
        _ => patternCfg.HeavySlash.CastDuration
    };

    private static float GetBoss2SkillRecoveryTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "HeavySlash" => patternCfg.HeavySlash.RecoveryTime,
        "GroundStomp" => patternCfg.GroundStomp.RecoveryTime,
        "LeapSmash" => patternCfg.LeapSmash.RecoveryTime,
        "DoubleCleave" => patternCfg.DoubleCleave.RecoveryTime,
        "FrenzyCharge" => patternCfg.FrenzyCharge.RecoveryTime,
        "EarthquakeRoar" => patternCfg.EarthquakeRoar.RecoveryTime,
        _ => patternCfg.HeavySlash.RecoveryTime
    };

    private static void ExecuteBoss2Skill(World world, ref Boss2StateMachine fsm, PatternConfig patternCfg, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Boss2Stats stats, float px, float py, float dt)
    {
        if (fsm.SubTimer > 0.0f)
        {
            fsm.SubTimer = Math.Max(0.0f, fsm.SubTimer - dt);
        }

        Boss2Config bCfg = ConfigManager.Boss2;
        float forwardDir = stats.FacingRight ? 1.0f : -1.0f;

        switch (fsm.CurrentSkillName)
        {
            case "HeavySlash":
                {
                    HeavySlashConfig p = patternCfg.HeavySlash;
                    vel.Vx = forwardDir * p.DashSpeed;

                    if (stats.ComboIndex == 0)
                    {
                        stats.ComboIndex = 1;
                        float hitX = trans.X + forwardDir * bCfg.SlashSpawnOffsetX;
                        float hitY = trans.Y + bCfg.SlashSpawnOffsetY;
                        float slashVx = forwardDir * p.DashSpeed;
                        SlashDirection dir = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX, hitY, slashVx, 0.0f, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.20f, p.SlashColor, dir);
                    }
                }
                break;

            case "GroundStomp":
                {
                    GroundStompConfig p = patternCfg.GroundStomp;
                    vel.Vx = 0.0f;

                    if (stats.ComboIndex == 0)
                    {
                        stats.ComboIndex = 1;
                        float groundY = trans.Y + col.Height / 2.0f - 10.0f;
                        CombatEffectHelper.SpawnShockwave(world, trans.X, groundY);
                        CombatEffectHelper.SpawnGroundShockwave(world, trans.X - 30.0f, groundY, -p.ShockwaveSpeed, p.ShockwaveRadius, p.ShockwaveDamage, p.FillColor, p.OutlineColor);
                        CombatEffectHelper.SpawnGroundShockwave(world, trans.X + 30.0f, groundY, p.ShockwaveSpeed, p.ShockwaveRadius, p.ShockwaveDamage, p.FillColor, p.OutlineColor);
                    }
                }
                break;

            case "DoubleCleave":
                {
                    DoubleCleaveConfig p = patternCfg.DoubleCleave;
                    vel.Vx = forwardDir * p.DashSpeed;

                    if (stats.ComboIndex == 0 && fsm.StateTimer >= 0.05f)
                    {
                        stats.ComboIndex = 1;
                        float hitX1 = trans.X + forwardDir * bCfg.SlashSpawnOffsetX;
                        float hitY1 = trans.Y + bCfg.SlashSpawnOffsetY;
                        float slashVx1 = forwardDir * p.DashSpeed;
                        SlashDirection dir1 = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX1, hitY1, slashVx1, 0.0f, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.18f, p.SlashColor, dir1);
                    }
                    else if (stats.ComboIndex == 1 && fsm.StateTimer >= p.SlashInterval)
                    {
                        stats.ComboIndex = 2;
                        float hitX2 = trans.X + forwardDir * (bCfg.SlashSpawnOffsetX + 15.0f);
                        float hitY2 = trans.Y + bCfg.SlashSpawnOffsetY;
                        float slashVx2 = forwardDir * p.DashSpeed;
                        SlashDirection dir2 = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX2, hitY2, slashVx2, 0.0f, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.18f, p.SlashColor, dir2);
                    }
                }
                break;

            case "FrenzyCharge":
                {
                    FrenzyChargeConfig p = patternCfg.FrenzyCharge;
                    vel.Vx = forwardDir * p.ChargeSpeed;

                    // 競技場左右壁に衝突した場合の破片飛散
                    ArenaConfig arenaCfg = ConfigManager.Arena;
                    if ((trans.X <= arenaCfg.BoundaryMinX + 15.0f && forwardDir < 0.0f) ||
                        (trans.X >= arenaCfg.BoundaryMaxX - 15.0f && forwardDir > 0.0f))
                    {
                        if (stats.ComboIndex == 0)
                        {
                            stats.ComboIndex = 1;
                            CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
                            for (int k = 0; k < p.ShrapnelCount; k++)
                            {
                                float angle = -MathF.PI * (0.2f + 0.6f * (k / (float)(p.ShrapnelCount - 1)));
                                CombatEffectHelper.SpawnBossBullet(world, trans.X, trans.Y,
                                    MathF.Cos(angle) * p.ShrapnelSpeed,
                                    MathF.Sin(angle) * p.ShrapnelSpeed,
                                    p.ShrapnelRadius,
                                    p.ShrapnelFillColor,
                                    p.ShrapnelOutlineColor);
                            }
                        }
                    }
                }
                break;

            case "EarthquakeRoar":
                {
                    EarthquakeRoarConfig p = patternCfg.EarthquakeRoar;
                    vel.Vx = 0.0f;

                    if (fsm.SubTimer <= 0.0f)
                    {
                        fsm.SubTimer = p.Interval;
                        Random rand = Random.Shared;
                        for (int k = 0; k < p.RockCount; k++)
                        {
                            float rx = rand.Next((int)p.MinX, (int)p.MaxX);
                            CombatEffectHelper.SpawnBossBullet(world, rx, 0.0f, 0.0f, p.RockSpeed, p.RockRadius, p.RockFillColor, p.RockOutlineColor);
                        }
                    }
                }
                break;

            default:
                Console.WriteLine($"[Boss2AI エラー] 未知の技能名です: {fsm.CurrentSkillName}");
                break;
        }
    }

    /// <summary>
    /// 技能ごとの有効間合い・攻撃射程・プラットフォーム位置関係を判定します。
    /// </summary>
    private static bool CheckBoss2SkillRange(string skillName, float bossX, float bossY, float playerX, float playerY, NavStatus navStatus)
    {
        float dx = MathF.Abs(playerX - bossX);
        float dy = MathF.Abs(playerY - bossY);

        switch (skillName)
        {
            case "HeavySlash":
                // 近接重劈：同一プラットフォーム上または高低差が小さく、水平間合いが近接有効射程内 (dx <= 160, dy <= 60)
                return (navStatus == NavStatus.DirectWalk || dy <= 60.0f) && dx <= 160.0f;

            case "DoubleCleave":
                // 双重連撃：近接突進有効射程内 (dx <= 175, dy <= 60)
                return (navStatus == NavStatus.DirectWalk || dy <= 60.0f) && dx <= 175.0f;

            case "GroundStomp":
                // 地面踏撃衝撃波：同一高度層または地上付近 (dx <= 420, dy <= 75)
                return dy <= 75.0f && dx <= 420.0f;

            case "FrenzyCharge":
                // 狂暴突進：水平直線突進 (dx <= 480, dy <= 60)
                return dy <= 60.0f && dx <= 480.0f;

            case "LeapSmash":
                // 泰坦躍撃：長距離強襲・上層強襲 (dx <= 580)
                return dx <= 580.0f;

            case "EarthquakeRoar":
                // 天崩地裂：全画面範囲攻撃
                return true;

            default:
                return dx <= 180.0f && dy <= 70.0f;
        }
    }
}
