using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;

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
                         stats.FacingRight = playerX >= trans.X;
                         sprite.FlipX = !stats.FacingRight;

                         float moveDir = stats.FacingRight ? 1.0f : -1.0f;
                         float distanceToPlayer = MathF.Abs(playerX - trans.X);

                         if (col.IsGrounded)
                         {
                             vel.Vx = moveDir * currentPhaseCfg.MoveSpeed;

                             // プレイヤーが上層足場にいる場合の跳躍追従
                             if (playerY < trans.Y - 90.0f && distanceToPlayer < 250.0f && stats.JumpCooldown <= 0.0f)
                             {
                                 vel.Vy = -currentPhaseCfg.JumpForce;
                                 stats.JumpCooldown = 3.0f;
                                 col.IsGrounded = false;
                             }
                         }

                         if (fsm.StateTimer >= fsm.StateDuration && isPlayerAlive)
                         {
                             vel.Vx = 0.0f;
                             if (currentPhaseCfg.Skills.Count > 0)
                             {
                                 fsm.CurrentSkillIndex = (fsm.CurrentSkillIndex + 1) % currentPhaseCfg.Skills.Count;
                                 fsm.CurrentSkillName = currentPhaseCfg.Skills[fsm.CurrentSkillIndex];
                             }
                             else
                             {
                                 Console.WriteLine($"[Boss2AI 警告] Phase {stats.Phase} の技能リストが空です。デフォルト 'HeavySlash' を使用します。");
                                 fsm.CurrentSkillName = "HeavySlash";
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

                             CombatEffectHelper.SpawnTelegraphSpark(world, trans.X, trans.Y, telegraphColor);
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
                        float hitX = trans.X + forwardDir * 50.0f;
                        float hitY = trans.Y;
                        SlashDirection dir = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX, hitY, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.20f, p.SlashColor, dir);
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
                        float hitX1 = trans.X + forwardDir * 55.0f;
                        SlashDirection dir1 = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX1, trans.Y, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.18f, p.SlashColor, dir1);
                    }
                    else if (stats.ComboIndex == 1 && fsm.StateTimer >= p.SlashInterval)
                    {
                        stats.ComboIndex = 2;
                        float hitX2 = trans.X + forwardDir * 60.0f;
                        SlashDirection dir2 = stats.FacingRight ? SlashDirection.Right : SlashDirection.Left;
                        CombatEffectHelper.SpawnMeleeSlashHitbox(world, hitX2, trans.Y, p.SlashRangeX, p.SlashRangeY, p.SlashDamage, 0.18f, p.SlashColor, dir2);
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
}
