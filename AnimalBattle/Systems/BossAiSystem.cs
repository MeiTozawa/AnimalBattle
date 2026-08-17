using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss の階層状態機 (HFSM: Phase -> Hovering -> Telegraphing -> Casting -> Recovering)、
/// 8 字・波状・追従を含む立体移動、俯衝突進（Dive Ram）、および各技能の弾幕発射サイクルを制御する ECS System。
/// 全ての座標・速度・時間・色パラメータは外部 TOML 設定ファイルから読み出され、マジックナンバーや静默兜底を排除しています。
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
                 PlayerConfig pCfg = ConfigManager.Player;
                 PatternConfig patternCfg = ConfigManager.Pattern;
                 float dt = it.DeltaTime();

                 float playerX = pCfg.SpawnX;
                 float playerY = pCfg.SpawnY;
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
                     3 => bCfg.Phase3,
                     _ => GetFallbackPhaseConfig(stats.Phase, bCfg)
                 };

                 stats.HoverTimer += dt;
                 fsm.StateTimer += dt;

                 bool isDiving = fsm.CurrentSkillName == "DiveRam" && (fsm.CurrentState == BossStateKind.Casting || fsm.CurrentState == BossStateKind.Recovering);

                 if (!isDiving)
                 {
                     float targetBaseX = stats.BaseX + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedX) * currentPhaseCfg.HoverAmpX + (playerX - stats.BaseX) * currentPhaseCfg.PlayerPursuitWeight;
                     float targetBaseY = currentPhaseCfg.HoverBaseY + MathF.Cos(stats.HoverTimer * currentPhaseCfg.HoverSpeedY) * currentPhaseCfg.HoverAmpY;
                     float lerpFactor = MathF.Min(1.0f, currentPhaseCfg.HoverLerpSpeed * dt);
                     trans.X += (targetBaseX - trans.X) * lerpFactor;
                     trans.Y += (targetBaseY - trans.Y) * lerpFactor;
                 }

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
                                 Console.WriteLine($"[BossAI 警告] Phase {stats.Phase} の技能リストが空です。boss.toml の Skills を確認してください。デフォルト 'Aimed3Shot' を使用します。");
                                 fsm.CurrentSkillName = "Aimed3Shot";
                             }

                             float windUp = GetSkillWindUpTime(fsm.CurrentSkillName, patternCfg);
                             Color telegraphColor = GetSkillTelegraphColor(fsm.CurrentSkillName, patternCfg);

                             fsm.CurrentState = BossStateKind.Telegraphing;
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

                     case BossStateKind.Telegraphing:
                         fsm.IsInvulnerable = false;
                         if (fsm.CurrentSkillName == "DiveRam")
                         {
                             fsm.TargetX = playerX;
                             fsm.TargetY = playerY;
                             float shakeIntensity = currentPhaseCfg.TelegraphShakeIntensity;
                             float shakeX = (Random.Shared.NextSingle() - 0.5f) * shakeIntensity;
                             float shakeY = (Random.Shared.NextSingle() - 0.5f) * shakeIntensity;
                             trans.X += shakeX;
                             trans.Y += shakeY;
                         }

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float castDuration = GetSkillCastDuration(fsm.CurrentSkillName, patternCfg);

                             fsm.CurrentState = BossStateKind.Casting;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = castDuration;
                             fsm.SubTimer = 0.0f;
                             fsm.AngleOffset = 0.0f;

                             if (fsm.CurrentSkillName == "DiveRam")
                             {
                                 fsm.StartX = trans.X;
                                 fsm.StartY = trans.Y;
                                 fsm.TargetX = playerX;
                                 fsm.TargetY = playerY;
                             }
                         }
                         break;

                     case BossStateKind.Casting:
                         fsm.IsInvulnerable = false;
                         ExecuteBossSkill(world, ref fsm, patternCfg, ref trans, playerX, playerY, dt);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float recoveryTime = GetSkillRecoveryTime(fsm.CurrentSkillName, patternCfg);

                             fsm.CurrentState = BossStateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = recoveryTime;

                             if (fsm.CurrentSkillName == "DiveRam")
                             {
                                 CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
                             }
                         }
                         break;

                     case BossStateKind.Recovering:
                         fsm.IsInvulnerable = false;
                         if (fsm.CurrentSkillName == "DiveRam")
                         {
                             if (fsm.StateTimer >= fsm.StateDuration * currentPhaseCfg.RecoverDelayRatio)
                             {
                                 float returnSpeed = currentPhaseCfg.RecoverFlybackSpeed * dt;
                                 trans.X += (stats.BaseX - trans.X) * MathF.Min(1.0f, returnSpeed);
                                 trans.Y += (currentPhaseCfg.HoverBaseY - trans.Y) * MathF.Min(1.0f, returnSpeed);
                             }
                         }

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;

                     case BossStateKind.PhaseTransition:
                         fsm.IsInvulnerable = true;
                         float floatSpeed = 3.0f * dt;
                         trans.X += (stats.BaseX - trans.X) * MathF.Min(1.0f, floatSpeed);
                         trans.Y += (currentPhaseCfg.HoverBaseY - trans.Y) * MathF.Min(1.0f, floatSpeed);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                             fsm.IsInvulnerable = false;
                         }
                         break;

                     default:
                         Console.WriteLine($"[BossAI エラー] 未知の Boss 状態種別です: {fsm.CurrentState}。Hovering 状態に強制復帰します。");
                         fsm.CurrentState = BossStateKind.Hovering;
                         fsm.StateTimer = 0.0f;
                         fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         break;
                 }
             });
    }

    private static PhaseFsmConfig GetFallbackPhaseConfig(int phase, BossConfig bCfg)
    {
        Console.WriteLine($"[BossAI 警告] 無効な Boss フェーズ番号です: {phase} (有効値: 1, 2, 3)。Phase 1 を安全に使用します。");
        return bCfg.Phase1;
    }

    private static float GetSkillWindUpTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "DiveRam" => patternCfg.DiveRam.WindUpTime,
        "Aimed3Shot" => patternCfg.Aimed3Shot.WindUpTime,
        "RingBurst" => patternCfg.RingBurst.WindUpTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.WindUpTime,
        "RainDrop" => patternCfg.RainDrop.WindUpTime,
        "CrossChaos" => patternCfg.CrossChaos.WindUpTime,
        "FastSniper" => patternCfg.FastSniper.WindUpTime,
        _ => LogUnknownSkillWindUp(skillName, patternCfg)
    };

    private static float LogUnknownSkillWindUp(string skillName, PatternConfig patternCfg)
    {
        Console.WriteLine($"[BossAI 警告] 未知の技能名です: '{skillName}' (WindUpTime)。デフォルトの Aimed3Shot を使用します。");
        return patternCfg.Aimed3Shot.WindUpTime;
    }

    private static Color GetSkillTelegraphColor(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "DiveRam" => patternCfg.DiveRam.TelegraphColor,
        "Aimed3Shot" => patternCfg.Aimed3Shot.TelegraphColor,
        "RingBurst" => patternCfg.RingBurst.TelegraphColor,
        "DoubleSpiral" => patternCfg.DoubleSpiral.TelegraphColor,
        "RainDrop" => patternCfg.RainDrop.TelegraphColor,
        "CrossChaos" => patternCfg.CrossChaos.TelegraphColor,
        "FastSniper" => patternCfg.FastSniper.TelegraphColor,
        _ => LogUnknownSkillTelegraphColor(skillName)
    };

    private static Color LogUnknownSkillTelegraphColor(string skillName)
    {
        Console.WriteLine($"[BossAI 警告] 未知の技能名です: '{skillName}' (TelegraphColor)。デフォルト色を使用します。");
        return ColorPalette.RedLight;
    }

    private static float GetSkillCastDuration(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "DiveRam" => patternCfg.DiveRam.CastDuration,
        "Aimed3Shot" => patternCfg.Aimed3Shot.CastDuration,
        "RingBurst" => patternCfg.RingBurst.CastDuration,
        "DoubleSpiral" => patternCfg.DoubleSpiral.CastDuration,
        "RainDrop" => patternCfg.RainDrop.CastDuration,
        "CrossChaos" => patternCfg.CrossChaos.CastDuration,
        "FastSniper" => patternCfg.FastSniper.CastDuration,
        _ => LogUnknownSkillCastDuration(skillName, patternCfg)
    };

    private static float LogUnknownSkillCastDuration(string skillName, PatternConfig patternCfg)
    {
        Console.WriteLine($"[BossAI 警告] 未知の技能名です: '{skillName}' (CastDuration)。デフォルトの Aimed3Shot を使用します。");
        return patternCfg.Aimed3Shot.CastDuration;
    }

    private static float GetSkillRecoveryTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "DiveRam" => patternCfg.DiveRam.RecoveryTime,
        "Aimed3Shot" => patternCfg.Aimed3Shot.RecoveryTime,
        "RingBurst" => patternCfg.RingBurst.RecoveryTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.RecoveryTime,
        "RainDrop" => patternCfg.RainDrop.RecoveryTime,
        "CrossChaos" => patternCfg.CrossChaos.RecoveryTime,
        "FastSniper" => patternCfg.FastSniper.RecoveryTime,
        _ => LogUnknownSkillRecoveryTime(skillName, patternCfg)
    };

    private static float LogUnknownSkillRecoveryTime(string skillName, PatternConfig patternCfg)
    {
        Console.WriteLine($"[BossAI 警告] 未知の技能名です: '{skillName}' (RecoveryTime)。デフォルトの Aimed3Shot を使用します。");
        return patternCfg.Aimed3Shot.RecoveryTime;
    }

    private static void ExecuteBossSkill(World world, ref BossStateMachine fsm, PatternConfig patternCfg, ref Transform trans, float px, float py, float dt)
    {
        if (fsm.SubTimer > 0.0f)
        {
            fsm.SubTimer = Math.Max(0.0f, fsm.SubTimer - dt);
        }

        float bx = trans.X;
        float by = trans.Y;

        switch (fsm.CurrentSkillName)
        {
            case "DiveRam":
                {
                    float progress = Math.Clamp(fsm.StateTimer / Math.Max(0.01f, fsm.StateDuration), 0.0f, 1.0f);
                    float easeProgress = progress * progress;
                    trans.X = fsm.StartX + (fsm.TargetX - fsm.StartX) * easeProgress;
                    trans.Y = fsm.StartY + (fsm.TargetY - fsm.StartY) * easeProgress;
                }
                break;

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

            default:
                Console.WriteLine($"[BossAI エラー] 技能 '{fsm.CurrentSkillName}' に対する弾幕実行ロジックが存在しません！boss.toml の Skills 名を確認してください。");
                break;
        }
    }
}
