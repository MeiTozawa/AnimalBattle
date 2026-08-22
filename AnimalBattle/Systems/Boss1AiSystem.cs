using System;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Bindings;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Systems;

/// <summary>
/// Boss 1 (Ancient Cat Overlord) の階層状態機 (HFSM: Phase -> Hovering -> Telegraphing -> Casting -> Recovering)、
/// 8 字・波状・追従を含む立体浮遊移動、俯衝突進（Dive Ram）、および各技能の弾幕発射サイクルを制御する ECS System。
/// 全ての座標・速度・時間・色パラメータは外部 TOML 設定ファイルから読み出されます。
/// </summary>
public static class Boss1AiSystem
{
    /// <summary>
    /// Boss 1 AI および弾幕制御システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddBoss1AiSystem(this World world)
    {
        Query<Transform, Health> playerQuery =
            world.QueryBuilder<Transform, Health>()
                 .With<PlayerTag>()
                 .Build();

        world.System<Transform, Health, Boss1Stats, Boss1StateMachine, Sprite>("Boss1AiSystem")
             .With<Boss1Tag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Health bHealth, ref Boss1Stats stats, ref Boss1StateMachine fsm, ref Sprite sprite) =>
             {
                 Boss1Config bCfg = ConfigManager.Boss1;
                 PatternConfig patternCfg = ConfigManager.Pattern;
                 float dt = it.DeltaTime();

                 FetchPlayerTarget(playerQuery, out float playerX, out float playerY, out bool isPlayerAlive);

                 if (stats.HitFlashTimer > 0.0f)
                 {
                     stats.HitFlashTimer = Math.Max(0.0f, stats.HitFlashTimer - dt);
                 }

                 if (!bHealth.IsAlive)
                 {
                     bHealth.Current = 0.0f;
                     return;
                 }

                 EvaluateBoss1Phase(world, ref bHealth, ref stats, ref fsm, trans.Position, bCfg);

                 Boss1PhaseFsmConfig currentPhaseCfg = stats.Phase switch
                 {
                     1 => bCfg.Phase1,
                     2 => bCfg.Phase2,
                     3 => bCfg.Phase3,
                     _ => GetFallbackPhaseConfig(stats.Phase, bCfg)
                 };

                 stats.HoverTimer += dt;
                 fsm.StateTimer += dt;

                 UpdateBoss1HoverPosition(ref trans, ref stats, ref fsm, currentPhaseCfg, playerX, playerY, dt);
                 UpdateBoss1StateMachine(world, ref trans, ref stats, ref fsm, currentPhaseCfg, patternCfg, playerX, playerY, isPlayerAlive, dt);
             });

        return world;
    }

    private static void FetchPlayerTarget(Query<Transform, Health> playerQuery, out float playerX, out float playerY, out bool isPlayerAlive)
    {
        PlayerConfig pCfg = ConfigManager.Player;
        float px = pCfg.SpawnX;
        float py = pCfg.SpawnY;
        bool alive = false;

        playerQuery.Each((ref Transform pTrans, ref Health pHealth) =>
        {
            px = pTrans.X;
            py = pTrans.Y;
            alive = pHealth.IsAlive;
        });

        playerX = px;
        playerY = py;
        isPlayerAlive = alive;
    }

    private static void EvaluateBoss1Phase(World world, ref Health bHealth, ref Boss1Stats stats, ref Boss1StateMachine fsm, Vec2f pos, Boss1Config bCfg)
    {
        float hpRatio = bHealth.Ratio;
        int targetPhase = hpRatio > bCfg.Phase2.HpThreshold ? 1 : (hpRatio > bCfg.Phase3.HpThreshold ? 2 : 3);

        if (targetPhase > stats.Phase && fsm.CurrentState != Boss1StateKind.PhaseTransition)
        {
            stats.Phase = targetPhase;
            fsm.CurrentState = Boss1StateKind.PhaseTransition;
            fsm.StateTimer = 0.0f;
            fsm.StateDuration = bCfg.Phase1.TransitionDuration;
            fsm.IsInvulnerable = true;
            CombatEffectHelper.ClearAllBossBullets(world);
            CombatEffectHelper.SpawnShockwave(world, pos.X, pos.Y);
        }
    }

    private static void UpdateBoss1HoverPosition(ref Transform trans, ref Boss1Stats stats, ref Boss1StateMachine fsm, Boss1PhaseFsmConfig currentPhaseCfg, float playerX, float playerY, float dt)
    {
        bool isDiving = fsm.CurrentSkillName == "DiveRam" && (fsm.CurrentState == Boss1StateKind.Casting || fsm.CurrentState == Boss1StateKind.Recovering);
        if (isDiving)
        {
            return;
        }

        Vec2f curPos = trans.Position;
        Vec2f basePos = new(stats.BaseX, currentPhaseCfg.HoverBaseY);
        Vec2f playerPos = new(playerX, playerY);

        NativeExtensionBindings.Extension_CalculateBossHoverPosition(
            in curPos,
            in basePos,
            in playerPos,
            stats.HoverTimer,
            currentPhaseCfg.HoverSpeedX,
            currentPhaseCfg.HoverSpeedY,
            currentPhaseCfg.HoverAmpX,
            currentPhaseCfg.HoverAmpY,
            currentPhaseCfg.PlayerPursuitWeight,
            currentPhaseCfg.HoverLerpSpeed,
            dt,
            out trans.Position);
    }

    private static void UpdateBoss1StateMachine(
        World world,
        ref Transform trans,
        ref Boss1Stats stats,
        ref Boss1StateMachine fsm,
        Boss1PhaseFsmConfig currentPhaseCfg,
        PatternConfig patternCfg,
        float playerX,
        float playerY,
        bool isPlayerAlive,
        float dt)
    {
        switch (fsm.CurrentState)
        {
            case Boss1StateKind.Hovering:
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
                        Console.WriteLine($"[Boss1AI 警告] Phase {stats.Phase} の技能リストが空です。boss1.toml の Skills を確認してください。デフォルト 'Aimed3Shot' を使用します。");
                        fsm.CurrentSkillName = "Aimed3Shot";
                    }

                    float windUp = GetSkillWindUpTime(fsm.CurrentSkillName, patternCfg);
                    Color telegraphColor = GetSkillTelegraphColor(fsm.CurrentSkillName, patternCfg);

                    fsm.CurrentState = Boss1StateKind.Telegraphing;
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

            case Boss1StateKind.Telegraphing:
                fsm.IsInvulnerable = false;
                if (fsm.CurrentSkillName == "DiveRam")
                {
                    fsm.TargetX = playerX;
                    fsm.TargetY = playerY;
                    float shakeIntensity = currentPhaseCfg.TelegraphShakeIntensity;
                    trans.X += (Random.Shared.NextSingle() - 0.5f) * shakeIntensity;
                    trans.Y += (Random.Shared.NextSingle() - 0.5f) * shakeIntensity;
                }

                if (fsm.StateTimer >= fsm.StateDuration)
                {
                    float castDuration = GetSkillCastDuration(fsm.CurrentSkillName, patternCfg);
                    fsm.CurrentState = Boss1StateKind.Casting;
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

            case Boss1StateKind.Casting:
                fsm.IsInvulnerable = false;
                ExecuteBoss1Skill(world, ref fsm, patternCfg, ref trans, playerX, playerY, dt);

                if (fsm.StateTimer >= fsm.StateDuration)
                {
                    float recoveryTime = GetSkillRecoveryTime(fsm.CurrentSkillName, patternCfg);
                    fsm.CurrentState = Boss1StateKind.Recovering;
                    fsm.StateTimer = 0.0f;
                    fsm.StateDuration = recoveryTime;

                    if (fsm.CurrentSkillName == "DiveRam")
                    {
                        CombatEffectHelper.SpawnShockwave(world, trans.X, trans.Y);
                    }
                }
                break;

            case Boss1StateKind.Recovering:
                fsm.IsInvulnerable = false;
                if (fsm.CurrentSkillName == "DiveRam" && fsm.StateTimer >= fsm.StateDuration * currentPhaseCfg.RecoverDelayRatio)
                {
                    Vec2f curPos = trans.Position;
                    Vec2f targetBase = new(stats.BaseX, currentPhaseCfg.HoverBaseY);
                    NativeExtensionBindings.Extension_CalculateBossReturnPosition(
                        in curPos,
                        in targetBase,
                        currentPhaseCfg.RecoverFlybackSpeed,
                        dt,
                        out trans.Position);
                }

                if (fsm.StateTimer >= fsm.StateDuration)
                {
                    fsm.CurrentState = Boss1StateKind.Hovering;
                    fsm.StateTimer = 0.0f;
                    fsm.StateDuration = currentPhaseCfg.HoverDuration;
                }
                break;

            case Boss1StateKind.PhaseTransition:
                fsm.IsInvulnerable = true;
                Vec2f transPos = trans.Position;
                Vec2f transBase = new(stats.BaseX, currentPhaseCfg.HoverBaseY);
                NativeExtensionBindings.Extension_CalculateBossReturnPosition(
                    in transPos,
                    in transBase,
                    currentPhaseCfg.TransitionFloatSpeed,
                    dt,
                    out trans.Position);

                if (fsm.StateTimer >= fsm.StateDuration)
                {
                    fsm.CurrentState = Boss1StateKind.Hovering;
                    fsm.StateTimer = 0.0f;
                    fsm.StateDuration = currentPhaseCfg.HoverDuration;
                    fsm.IsInvulnerable = false;
                }
                break;

            default:
                Console.WriteLine($"[Boss1AI エラー] 未知の Boss 状態種別です: {fsm.CurrentState}。Hovering 状態に強制復帰します。");
                fsm.CurrentState = Boss1StateKind.Hovering;
                fsm.StateTimer = 0.0f;
                fsm.StateDuration = currentPhaseCfg.HoverDuration;
                break;
        }
    }

    private static Boss1PhaseFsmConfig GetFallbackPhaseConfig(int phase, Boss1Config bCfg)
    {
        Console.WriteLine($"[Boss1AI 警告] 無効な Boss フェーズ番号です: {phase} (有効値: 1, 2, 3)。Phase 1 を安全に使用します。");
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
        Console.WriteLine($"[Boss1AI 警告] 未知の技能名です: '{skillName}' (WindUpTime)。デフォルトの Aimed3Shot を使用します。");
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
        Console.WriteLine($"[Boss1AI 警告] 未知の技能名です: '{skillName}' (TelegraphColor)。デフォルト色を使用します。");
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
        Console.WriteLine($"[Boss1AI 警告] 未知の技能名です: '{skillName}' (CastDuration)。デフォルトの Aimed3Shot を使用します。");
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
        Console.WriteLine($"[Boss1AI 警告] 未知の技能名です: '{skillName}' (RecoveryTime)。デフォルトの Aimed3Shot を使用します。");
        return patternCfg.Aimed3Shot.RecoveryTime;
    }

    private static void ExecuteBoss1Skill(World world, ref Boss1StateMachine fsm, PatternConfig patternCfg, ref Transform trans, float px, float py, float dt)
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
                ExecuteDiveRam(ref fsm, patternCfg, ref trans);
                break;

            case "Aimed3Shot":
                ExecuteAimed3Shot(world, ref fsm, patternCfg.Aimed3Shot, bx, by, px, py);
                break;

            case "RingBurst":
                ExecuteRingBurst(world, ref fsm, patternCfg.RingBurst, bx, by);
                break;

            case "DoubleSpiral":
                ExecuteDoubleSpiral(world, ref fsm, patternCfg.DoubleSpiral, bx, by);
                break;

            case "RainDrop":
                ExecuteRainDrop(world, ref fsm, patternCfg.RainDrop);
                break;

            case "CrossChaos":
                ExecuteCrossChaos(world, ref fsm, patternCfg.CrossChaos, bx, by);
                break;

            case "FastSniper":
                ExecuteFastSniper(world, ref fsm, patternCfg.FastSniper, bx, by, px, py);
                break;

            default:
                Console.WriteLine($"[Boss1AI エラー] 技能 '{fsm.CurrentSkillName}' に対する弾幕実行ロジックが存在しません！boss1.toml の Skills 名を確認してください。");
                break;
        }
    }

    private static void ExecuteDiveRam(ref Boss1StateMachine fsm, PatternConfig patternCfg, ref Transform trans)
    {
        Vec2f startPos = new(fsm.StartX, fsm.StartY);
        Vec2f targetPos = new(fsm.TargetX, fsm.TargetY);
        NativeExtensionBindings.Extension_CalculateBossDiveRamPosition(
            in startPos,
            in targetPos,
            fsm.StateTimer,
            fsm.StateDuration,
            patternCfg.DiveRam.EaseType,
            out trans.Position);
    }

    private static void ExecuteAimed3Shot(World world, ref Boss1StateMachine fsm, Aimed3ShotConfig p, float bx, float by, float px, float py)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        float baseAngle = MathF.Atan2(py - by, px - bx);
        for (int i = -1; i <= 1; i++)
        {
            float angle = baseAngle + i * p.SpreadAngle;
            CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
        }
    }

    private static void ExecuteRingBurst(World world, ref Boss1StateMachine fsm, RingBurstConfig p, float bx, float by)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        int count = Math.Max(4, p.BulletCount);
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * MathF.PI * 2.0f + fsm.AngleOffset;
            CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
        }
        fsm.AngleOffset += p.AngleStep;
    }

    private static void ExecuteDoubleSpiral(World world, ref Boss1StateMachine fsm, DoubleSpiralConfig p, float bx, float by)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        float angle1 = fsm.AngleOffset;
        float angle2 = fsm.AngleOffset + MathF.PI;

        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle1) * p.BulletSpeed, MathF.Sin(angle1) * p.BulletSpeed, p.BulletRadius, p.FillColor1, p.OutlineColor1);
        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle2) * p.BulletSpeed, MathF.Sin(angle2) * p.BulletSpeed, p.BulletRadius, p.FillColor2, p.OutlineColor2);

        fsm.AngleOffset += p.AngleStep;
    }

    private static void ExecuteRainDrop(World world, ref Boss1StateMachine fsm, RainDropConfig p)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        Random random = Random.Shared;
        int minX = (int)p.MinX;
        int maxX = (int)p.MaxX;
        float rainX1 = random.Next(minX, maxX);
        float rainX2 = random.Next(minX, maxX);
        CombatEffectHelper.SpawnBossBullet(world, rainX1, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
        CombatEffectHelper.SpawnBossBullet(world, rainX2, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
    }

    private static void ExecuteCrossChaos(World world, ref Boss1StateMachine fsm, CrossChaosConfig p, float bx, float by)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        for (int k = 0; k < 4; k++)
        {
            float angle = fsm.AngleOffset + k * (MathF.PI / 2.0f);
            CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
        }
        fsm.AngleOffset += p.AngleStep;
    }

    private static void ExecuteFastSniper(World world, ref Boss1StateMachine fsm, FastSniperConfig p, float bx, float by, float px, float py)
    {
        if (fsm.SubTimer > 0.0f)
        {
            return;
        }

        fsm.SubTimer = p.Interval;
        float aimAngle = MathF.Atan2(py - by, px - bx);
        CombatEffectHelper.SpawnBossBullet(world, bx, by, MathF.Cos(aimAngle) * p.BulletSpeed, MathF.Sin(aimAngle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
    }
}
