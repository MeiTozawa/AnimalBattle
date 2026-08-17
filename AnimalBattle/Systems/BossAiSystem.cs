using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss の階層状態機 (HFSM: Phase -> Hovering -> Telegraphing -> Casting -> Recovering)、
/// 浮遊運動、各技能の弾幕発射サイクルを制御する ECS System。
/// </summary>
public static class BossAiSystem
{
    /// <summary>
    /// Boss AI および弾幕制御システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System<Transform, Health, BossStats, BossStateMachine, Sprite>("BossAiSystem")
             .With<BossTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Health bHealth, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PatternConfig patternCfg = ConfigManager.Pattern;
                 float dt = it.DeltaTime();

                 float playerX = 640.0f;
                 float playerY = 600.0f;
                 bool isPlayerAlive = false;

                 world.QueryBuilder<Transform, Health>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref Health pHealth) =>
                      {
                          playerX = pTrans.X;
                          playerY = pTrans.Y;
                          isPlayerAlive = pHealth.IsAlive;
                      });

                 if (stats.HitFlashTimer > 0.0f)
                 {
                     stats.HitFlashTimer = Math.Max(0.0f, stats.HitFlashTimer - dt);
                 }

                 if (!bHealth.IsAlive)
                 {
                     bHealth.Current = 0.0f;
                     return;
                 }

                 float hpRatio = bHealth.Ratio;
                 int targetPhase = hpRatio > bCfg.Phase2.HpThreshold ? 1 : (hpRatio > bCfg.Phase3.HpThreshold ? 2 : 3);

                 if (targetPhase > stats.Phase && fsm.CurrentState != BossStateKind.PhaseTransition)
                 {
                     stats.Phase = targetPhase;
                     fsm.CurrentState = BossStateKind.PhaseTransition;
                     fsm.StateTimer = 0.0f;
                     fsm.StateDuration = bCfg.Phase1.TransitionDuration;
                     fsm.IsInvulnerable = true;
                     CombatEffectHelper.ClearAllBossBullets(world);
                     CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
                 }

                 PhaseFsmConfig currentPhaseCfg = stats.Phase switch
                 {
                     1 => bCfg.Phase1,
                     2 => bCfg.Phase2,
                     _ => bCfg.Phase3
                 };

                 stats.HoverTimer += dt;
                 float targetBaseX = stats.BaseX + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedX) * currentPhaseCfg.HoverAmpX;
                 float targetBaseY = stats.BaseY + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedY) * currentPhaseCfg.HoverAmpY;
                 float lerpFactor = MathF.Min(1.0f, currentPhaseCfg.HoverLerpSpeed * dt);
                 trans.X += (targetBaseX - trans.X) * lerpFactor;
                 trans.Y += (targetBaseY - trans.Y) * lerpFactor;

                 fsm.StateTimer += dt;

                 switch (fsm.CurrentState)
                 {
                     case BossStateKind.Hovering:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration && isPlayerAlive)
                         {
                             if (currentPhaseCfg.Skills.Count > 0)
                             {
                                 fsm.CurrentSkillIndex = (fsm.CurrentSkillIndex + 1) % currentPhaseCfg.Skills.Count;
                                 fsm.CurrentSkillName = currentPhaseCfg.Skills[fsm.CurrentSkillIndex];
                             }
                             else
                             {
                                 fsm.CurrentSkillName = "Aimed3Shot";
                             }

                             float windUp = GetSkillWindUpTime(fsm.CurrentSkillName, patternCfg);
                             Color telegraphColor = GetSkillTelegraphColor(fsm.CurrentSkillName, patternCfg);

                             fsm.CurrentState = BossStateKind.Telegraphing;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = windUp;
                             fsm.SubTimer = 0.0f;
                             CombatEffectHelper.SpawnTelegraphSpark(world, trans.X, trans.Y, telegraphColor);
                         }
                         break;

                     case BossStateKind.Telegraphing:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float castDuration = GetSkillCastDuration(fsm.CurrentSkillName, patternCfg);
                             fsm.CurrentState = BossStateKind.Casting;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = castDuration;
                             fsm.SubTimer = 0.0f;
                             fsm.AngleOffset = 0.0f;
                         }
                         break;

                     case BossStateKind.Casting:
                         fsm.IsInvulnerable = false;
                         ExecuteBossSkill(world, ref fsm, patternCfg, trans.X, trans.Y, playerX, playerY, dt);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float recoveryTime = GetSkillRecoveryTime(fsm.CurrentSkillName, patternCfg);
                             fsm.CurrentState = BossStateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = recoveryTime;
                         }
                         break;

                     case BossStateKind.Recovering:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;

                     case BossStateKind.PhaseTransition:
                         fsm.IsInvulnerable = true;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.IsInvulnerable = false;
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;
                 }
             });
    }

    private static float GetSkillWindUpTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.WindUpTime,
        "RingBurst" => patternCfg.RingBurst.WindUpTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.WindUpTime,
        "RainDrop" => patternCfg.RainDrop.WindUpTime,
        "CrossChaos" => patternCfg.CrossChaos.WindUpTime,
        "FastSniper" => patternCfg.FastSniper.WindUpTime,
        _ => 0.5f
    };

    private static Color GetSkillTelegraphColor(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.TelegraphColor,
        "RingBurst" => patternCfg.RingBurst.TelegraphColor,
        "DoubleSpiral" => patternCfg.DoubleSpiral.TelegraphColor,
        "RainDrop" => patternCfg.RainDrop.TelegraphColor,
        "CrossChaos" => patternCfg.CrossChaos.TelegraphColor,
        "FastSniper" => patternCfg.FastSniper.TelegraphColor,
        _ => Color.FromArgb(0xFF, 0xFF, 0x88, 0x88)
    };

    private static float GetSkillCastDuration(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.CastDuration,
        "RingBurst" => patternCfg.RingBurst.CastDuration,
        "DoubleSpiral" => patternCfg.DoubleSpiral.CastDuration,
        "RainDrop" => patternCfg.RainDrop.CastDuration,
        "CrossChaos" => patternCfg.CrossChaos.CastDuration,
        "FastSniper" => patternCfg.FastSniper.CastDuration,
        _ => 1.0f
    };

    private static float GetSkillRecoveryTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.RecoveryTime,
        "RingBurst" => patternCfg.RingBurst.RecoveryTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.RecoveryTime,
        "RainDrop" => patternCfg.RainDrop.RecoveryTime,
        "CrossChaos" => patternCfg.CrossChaos.RecoveryTime,
        "FastSniper" => patternCfg.FastSniper.RecoveryTime,
        _ => 0.5f
    };

    private static void ExecuteBossSkill(World world, ref BossStateMachine fsm, PatternConfig patternCfg, float bx, float by, float px, float py, float dt)
    {
        if (fsm.SubTimer > 0.0f)
        {
            fsm.SubTimer = Math.Max(0.0f, fsm.SubTimer - dt);
        }

        switch (fsm.CurrentSkillName)
        {
            case "Aimed3Shot":
                if (fsm.SubTimer <= 0.0f)
                {
                    Aimed3ShotConfig p = patternCfg.Aimed3Shot;
                    fsm.SubTimer = p.Interval;
                    float baseAngle = MathF.Atan2(py - by, px - bx);
                    for (int i = -1; i <= 1; i++)
                    {
                        float angle = baseAngle + i * p.SpreadAngle;
                        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                }
                break;

            case "RingBurst":
                if (fsm.SubTimer <= 0.0f)
                {
                    RingBurstConfig p = patternCfg.RingBurst;
                    fsm.SubTimer = p.Interval;
                    int count = Math.Max(4, p.BulletCount);
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i / (float)count) * MathF.PI * 2.0f + fsm.AngleOffset;
                        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "DoubleSpiral":
                if (fsm.SubTimer <= 0.0f)
                {
                    DoubleSpiralConfig p = patternCfg.DoubleSpiral;
                    fsm.SubTimer = p.Interval;
                    float angle1 = fsm.AngleOffset;
                    float angle2 = fsm.AngleOffset + MathF.PI;

                    CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle1) * p.BulletSpeed, MathF.Sin(angle1) * p.BulletSpeed, p.BulletRadius, p.FillColor1, p.OutlineColor1);
                    CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle2) * p.BulletSpeed, MathF.Sin(angle2) * p.BulletSpeed, p.BulletRadius, p.FillColor2, p.OutlineColor2);

                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "RainDrop":
                if (fsm.SubTimer <= 0.0f)
                {
                    RainDropConfig p = patternCfg.RainDrop;
                    fsm.SubTimer = p.Interval;
                    Random random = Random.Shared;
                    int minX = (int)p.MinX;
                    int maxX = (int)p.MaxX;
                    float rainX1 = random.Next(minX, maxX);
                    float rainX2 = random.Next(minX, maxX);
                    CombatEffectHelper.SpawnBossBullet(world, rainX1, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    CombatEffectHelper.SpawnBossBullet(world, rainX2, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                }
                break;

            case "CrossChaos":
                if (fsm.SubTimer <= 0.0f)
                {
                    CrossChaosConfig p = patternCfg.CrossChaos;
                    fsm.SubTimer = p.Interval;
                    for (int k = 0; k < 4; k++)
                    {
                        float angle = fsm.AngleOffset + k * (MathF.PI / 2.0f);
                        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "FastSniper":
                if (fsm.SubTimer <= 0.0f)
                {
                    FastSniperConfig p = patternCfg.FastSniper;
                    fsm.SubTimer = p.Interval;
                    float aimAngle = MathF.Atan2(py - by, px - bx);
                    CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(aimAngle) * p.BulletSpeed, MathF.Sin(aimAngle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                }
                break;
        }
    }
}
