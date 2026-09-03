using System;
using System.Drawing;

using AnimalBattle.Ai.Contracts;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 画面上部操作ヘルプ、自機 HP・二段ジャンプ残数、および画面下部 Boss 大 HP バー・行動状態バッジを描画する ECS System。
/// Boss 1 (Ancient Cat) と Boss 2 (Ground Crusher) の両方を自動検知して動的に描画を切り替えます。
/// </summary>
public static class HudRenderSystem
{
    /// <summary>
    /// HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddHudRenderSystem(this World world)
    {
        Query<DoubleJump> doubleJumpQuery =
            world.QueryBuilder<DoubleJump>()
                 .With<PlayerTag>()
                 .Build();

        Query<Health> playerHealthQuery =
            world.QueryBuilder<Health>()
                 .With<PlayerTag>()
                 .Build();

        Query<Health, Boss1Stats, Boss1StateMachine, BossCompatibilityData> boss1StatusQuery =
            world.QueryBuilder<Health, Boss1Stats, Boss1StateMachine, BossCompatibilityData>()
                 .With<BossTag>()
                 .With<Boss1Tag>()
                 .Build();

        Query<Health, Boss2Stats, Boss2StateMachine, BossCompatibilityData> boss2StatusQuery =
            world.QueryBuilder<Health, Boss2Stats, Boss2StateMachine, BossCompatibilityData>()
                 .With<BossTag>()
                 .With<Boss2Tag>()
                 .Build();

        world.System("HudRenderSystem")
             .Kind(Ecs.OnStore)
             .Run((Iter it) =>
             {
                 RenderConfig renderCfg = ConfigManager.Render;
                 HudRenderConfig hudCfg = renderCfg.Hud;
                 BossHpBarRenderConfig hpCfg = renderCfg.BossHpBar;
                 ModalRenderConfig modalCfg = renderCfg.Modals;
                 DebugRenderConfig dbgCfg = renderCfg.Debug;

                 DrawControlHelp(hudCfg, dbgCfg);
                 DrawPlayerStatus(doubleJumpQuery, playerHealthQuery, hudCfg);
                 DrawBoss1Status(boss1StatusQuery, hpCfg, modalCfg);
                 DrawBoss2Status(boss2StatusQuery, hpCfg, modalCfg);
                 DrawGameOverModal(playerHealthQuery, modalCfg);
             });

        return world;
    }

    private static void DrawControlHelp(HudRenderConfig hudCfg, DebugRenderConfig dbgCfg)
    {
        GameEngine.DrawString(hudCfg.TitleX, hudCfg.TitleY, hudCfg.TitleText, hudCfg.TitleColor);

        int lineIndex = 1;
        foreach (HudTextItem item in hudCfg.Text)
        {
            int drawY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;

            if (item.Text.Contains("F1", StringComparison.OrdinalIgnoreCase) ||
                item.Text.Contains("Hitbox", StringComparison.OrdinalIgnoreCase))
            {
                string dbgStatus = dbgCfg.Enabled ? " [ON - H / F1 to toggle]" : " [OFF - H / F1 to toggle]";
                Color statusColor = dbgCfg.Enabled ? dbgCfg.IndicatorColor : item.Color;
                GameEngine.DrawString(hudCfg.TitleX, drawY, $"{item.Text}{dbgStatus}", statusColor);
            }
            else
            {
                GameEngine.DrawString(hudCfg.TitleX, drawY, item.Text, item.Color);
            }

            lineIndex++;
        }

        int switchGuideY = hudCfg.TitleY + hudCfg.LineSpacing * lineIndex;
        GameEngine.DrawString(hudCfg.TitleX, switchGuideY, "[1] Boss 1 (Aerial Cat)  [2] Boss 2 (Ground Crusher)", ColorPalette.GoldLight);
    }

    private static void DrawPlayerStatus(Query<DoubleJump> doubleJumpQuery, Query<Health> playerHealthQuery, HudRenderConfig hudCfg)
    {
        doubleJumpQuery.Each((ref DoubleJump doubleJump) =>
        {
            string jumpSymbol = doubleJump.RemainingJumps switch
            {
                >= 2 => "[ ●  ● ]",
                1 => "[ ●  ○ ]",
                _ => "[ ○  ○ ]"
            };
            Color jumpColor = doubleJump.RemainingJumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
            GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY, $"DOUBLE JUMP: {jumpSymbol}", jumpColor);
        });

        playerHealthQuery.Each((ref Health health) =>
        {
            int currentHp = (int)health.Current;
            string hpSymbol = currentHp switch
            {
                >= 3 => "[ ●  ●  ● ]",
                2 => "[ ●  ●  ○ ]",
                1 => "[ ●  ○  ○ ]",
                _ => "[ ○  ○  ○ ]"
            };
            Color hpColor = currentHp switch
            {
                >= 3 => ColorPalette.Green,
                2 => ColorPalette.Gold,
                1 => ColorPalette.Red,
                _ => ColorPalette.Gray
            };
            string invBadge = health.IsInvincible ? $" [INVINCIBLE {health.InvincibleTimer:F1}s]" : "";
            GameEngine.DrawString(hudCfg.TitleX, hudCfg.DoubleJumpY + 20, $"PLAYER HP:   {hpSymbol} ({currentHp}/{(int)health.Max}){invBadge}", hpColor);
        });
    }

    private static void DrawBoss1Status(Query<Health, Boss1Stats, Boss1StateMachine, BossCompatibilityData> boss1Query, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        boss1Query.Each((Entity entity, ref Health health, ref Boss1Stats stats, ref Boss1StateMachine legacyState, ref BossCompatibilityData compatibility) =>
        {
            BossDisplayData display = GetBossDisplayData(entity, in compatibility);
            string bossTitle = $"BOSS: {ConfigManager.Boss1.BossName} [PHASE {ToLegacyPhase(display.Phase)}/3]";
            GetBossStateInfo(display.State, display.Skill, hpCfg, out string stateName, out Color stateBadgeColor);
            DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, health, ToLegacyPhase(display.Phase), compatibility.LegacyStateTimer, compatibility.LegacyStateDuration);
        });
    }

    private static void DrawBoss2Status(Query<Health, Boss2Stats, Boss2StateMachine, BossCompatibilityData> boss2Query, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        boss2Query.Each((Entity entity, ref Health health, ref Boss2Stats stats, ref Boss2StateMachine legacyState, ref BossCompatibilityData compatibility) =>
        {
            BossDisplayData display = GetBossDisplayData(entity, in compatibility);
            string bossTitle = $"BOSS: {ConfigManager.Boss2.BossName} [PHASE {ToLegacyPhase(display.Phase)}/3]";
            GetBossStateInfo(display.State, display.Skill, hpCfg, out string stateName, out Color stateBadgeColor);
            DrawBossHpBar(hpCfg, modalCfg, bossTitle, stateName, stateBadgeColor, health, ToLegacyPhase(display.Phase), compatibility.LegacyStateTimer, compatibility.LegacyStateDuration);
        });
    }

    private static void DrawGameOverModal(Query<Health> playerHealthQuery, ModalRenderConfig modalCfg)
    {
        playerHealthQuery.Each((ref Health pHealth) =>
        {
            if (pHealth.Current <= 0.0f)
            {
                GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverBgColor, true);
                GameEngine.DrawBox(modalCfg.GameOverX1, modalCfg.GameOverY1, modalCfg.GameOverX2, modalCfg.GameOverY2, modalCfg.GameOverOuterBorderColor, false);
                GameEngine.DrawBox(modalCfg.GameOverX1 + 2, modalCfg.GameOverY1 + 2, modalCfg.GameOverX2 - 2, modalCfg.GameOverY2 - 2, modalCfg.GameOverInnerBorderColor, false);
                GameEngine.DrawString(modalCfg.GameOverTitleX, modalCfg.GameOverTitleY, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
                GameEngine.DrawString(modalCfg.GameOverRetryX, modalCfg.GameOverRetryY, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
            }
        });
    }

    private readonly record struct BossDisplayData(BossState State, Phase Phase, BossSkill Skill, bool IsInvulnerable, AttackStage AttackStage);

    private static BossDisplayData GetBossDisplayData(Entity entity, in BossCompatibilityData compatibility)
    {
        if (compatibility.HasDebugData && entity.Has<BossDebugData>())
        {
            ref readonly BossDebugData debugData = ref entity.Get<BossDebugData>();
            return new BossDisplayData(
                debugData.BossState,
                debugData.Phase,
                debugData.Skill,
                debugData.IsInvulnerable,
                debugData.AttackStage);
        }

        return new BossDisplayData(
            compatibility.BossState,
            compatibility.Phase,
            compatibility.Skill,
            compatibility.IsInvulnerable,
            compatibility.AttackStage);
    }

    private static int ToLegacyPhase(Phase phase)
    {
        return phase switch
        {
            Phase.Phase2 => 2,
            Phase.Phase3 => 3,
            _ => 1
        };
    }

    private static void GetBossStateInfo(BossState state, BossSkill skill, BossHpBarRenderConfig hpCfg, out string stateName, out Color badgeColor)
    {
        stateName = state switch
        {
            BossState.Boss1_Hover => "[STATE: HOVERING (IDLE)]",
            BossState.Boss1_AttackSelection => "[STATE: ATTACK SELECTION]",
            BossState.Boss1_Telegraph => $"[STATE: TELEGRAPHING (WIND-UP: {skill})]",

            BossState.Boss1_Recovery or BossState.Boss2_Recovery => "[STATE: RECOVERING (VULNERABLE)]",
            BossState.Boss1_PhaseTransition or BossState.Boss2_PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
            BossState.Boss1_Dead or BossState.Boss2_Dead => "[STATE: DEFEATED]",
            BossState.Boss2_Idle => "[STATE: IDLE / ALERT]",
            BossState.Boss2_PatrolChase => $"[STATE: CHASE (NEXT: {skill})]",
            BossState.Boss2_MoveToTakeoff => "[STATE: NAVIGATING TO TAKEOFF]",
            BossState.Boss2_PlatformJump => "[STATE: PLATFORM JUMP (AIRBORNE)]",
            BossState.Boss2_PlatformFall => "[STATE: PLATFORM FALLING]",
            BossState.Boss2_LandingRecovery => "[STATE: LANDING IMPACT]",
            BossState.Boss2_WallCrashStun => "[STATE: WALL CRASH STUNNED (VULNERABLE)]",
            _ when IsTelegraphState(state) => $"[STATE: TELEGRAPH ({skill})]",
            _ when IsExecuteState(state) => $"[STATE: EXECUTING ({skill})]",
            _ => "[STATE: UNKNOWN]"
        };



        badgeColor = state switch
        {
            BossState.Boss1_PhaseTransition or BossState.Boss2_PhaseTransition => hpCfg.StatePhaseTransitionColor,
            BossState.Boss1_Recovery or BossState.Boss2_Recovery => hpCfg.StateRecoveringColor,
            BossState.Boss1_Dead or BossState.Boss2_Dead => ColorPalette.Gray,
            BossState.Boss2_WallCrashStun => ColorPalette.Gold,
            _ when IsTelegraphState(state) => hpCfg.StateTelegraphingColor,
            _ when IsExecuteState(state) => hpCfg.StateCastingColor,
            _ => hpCfg.StateDefaultColor
        };
    }

    private static bool IsTelegraphState(BossState state)
    {
        return state is BossState.Boss1_Telegraph
            or BossState.Boss2_HeavySlashTelegraph
            or BossState.Boss2_DoubleCleaveTelegraph
            or BossState.Boss2_GroundStompTelegraph
            or BossState.Boss2_LeapSmashTelegraph
            or BossState.Boss2_FrenzyChargeTelegraph
            or BossState.Boss2_EarthquakeRoarTelegraph;
    }

    private static bool IsExecuteState(BossState state)
    {
        return state is BossState.Boss1_Aimed3ShotExecute
            or BossState.Boss1_RingBurstExecute
            or BossState.Boss1_DoubleSpiralExecute
            or BossState.Boss1_RainDropExecute
            or BossState.Boss1_CrossChaosExecute
            or BossState.Boss1_FastSniperExecute
            or BossState.Boss1_DiveRamExecute
            or BossState.Boss2_HeavySlashExecute
            or BossState.Boss2_DoubleCleaveExecute
            or BossState.Boss2_GroundStompExecute
            or BossState.Boss2_LeapSmashExecute
            or BossState.Boss2_FrenzyChargeExecute
            or BossState.Boss2_EarthquakeRoarExecute;
    }

    private static void GetBoss1StateInfo(Boss1StateMachine fsm, BossHpBarRenderConfig hpCfg, out string stateName, out Color badgeColor)
    {
        stateName = fsm.CurrentState switch
        {
            Boss1StateKind.Hovering => "[STATE: HOVERING (IDLE)]",
            Boss1StateKind.Telegraphing => $"[STATE: TELEGRAPHING (WIND-UP: {fsm.CurrentSkillName})]",
            Boss1StateKind.Casting => $"[STATE: CASTING ({fsm.CurrentSkillName})]",
            Boss1StateKind.Recovering => "[STATE: RECOVERING (VULNERABLE)]",
            Boss1StateKind.PhaseTransition => "[STATE: PHASE TRANSITION (INVULNERABLE)]",
            _ => "[STATE: UNKNOWN]"
        };

        badgeColor = fsm.CurrentState switch
        {
            Boss1StateKind.Hovering => hpCfg.StateDefaultColor,
            Boss1StateKind.Telegraphing => hpCfg.StateTelegraphingColor,
            Boss1StateKind.Casting => hpCfg.StateCastingColor,
            Boss1StateKind.Recovering => hpCfg.StateRecoveringColor,
            Boss1StateKind.PhaseTransition => hpCfg.StatePhaseTransitionColor,
            _ => hpCfg.StateDefaultColor
        };
    }

    private static void GetBoss2StateInfo(Boss2StateMachine fsm, BossHpBarRenderConfig hpCfg, out string stateName, out Color badgeColor)
    {
        stateName = fsm.CurrentState switch
        {
            Boss2StateKind.Idle => "[STATE: IDLE / ALERT]",
            Boss2StateKind.PatrolChase => $"[STATE: CHASE (NEXT: {fsm.CurrentSkillName})]",
            Boss2StateKind.MoveToTakeoff => "[STATE: NAVIGATING TO TAKEOFF]",
            Boss2StateKind.PlatformJumping => "[STATE: PLATFORM JUMP (AIRBORNE)]",
            Boss2StateKind.PlatformFalling => "[STATE: PLATFORM FALLING]",
            Boss2StateKind.LandingRecovery => "[STATE: LANDING IMPACT]",
            Boss2StateKind.TelegraphHeavySlash => "[STATE: TELEGRAPH (HEAVY SLASH)]",
            Boss2StateKind.TelegraphDoubleCleave => "[STATE: TELEGRAPH (DOUBLE CLEAVE)]",
            Boss2StateKind.TelegraphGroundStomp => "[STATE: TELEGRAPH (GROUND STOMP)]",
            Boss2StateKind.TelegraphLeapSmash => "[STATE: TELEGRAPH (LEAP SMASH)]",
            Boss2StateKind.TelegraphFrenzyCharge => "[STATE: TELEGRAPH (FRENZY CHARGE)]",
            Boss2StateKind.TelegraphEarthquakeRoar => "[STATE: TELEGRAPH (EARTHQUAKE ROAR)]",
            Boss2StateKind.ExecutingHeavySlash => "[STATE: EXECUTING (HEAVY SLASH)]",
            Boss2StateKind.ExecutingDoubleCleave => "[STATE: EXECUTING (DOUBLE CLEAVE)]",
            Boss2StateKind.ExecutingGroundStomp => "[STATE: EXECUTING (GROUND STOMP)]",
            Boss2StateKind.ExecutingLeapAscent => "[STATE: LEAP ASCENT (TITAN LEAP)]",
            Boss2StateKind.ExecutingLeapDescent => "[STATE: LEAP DESCENT (SLAM)]",
            Boss2StateKind.ExecutingFrenzyCharge => "[STATE: FRENZY CHARGE (RAM)]",
            Boss2StateKind.ExecutingEarthquakeRoar => "[STATE: EARTHQUAKE ROAR (METEOR)]",
            Boss2StateKind.WallCrashStun => "[STATE: WALL CRASH STUNNED (VULNERABLE)]",
            Boss2StateKind.SkillRecovery => "[STATE: SKILL RECOVERY (VULNERABLE)]",
            Boss2StateKind.PhaseTransition => "[STATE: ROAR TRANSITION (INVULNERABLE)]",
            Boss2StateKind.Dead => "[STATE: DEFEATED]",
            _ => "[STATE: UNKNOWN]"
        };

        badgeColor = fsm.CurrentState switch
        {
            Boss2StateKind.Idle or Boss2StateKind.PatrolChase or Boss2StateKind.MoveToTakeoff => hpCfg.StateDefaultColor,
            Boss2StateKind.PlatformJumping or Boss2StateKind.PlatformFalling or Boss2StateKind.LandingRecovery => ColorPalette.BlueSky,
            Boss2StateKind.TelegraphHeavySlash or Boss2StateKind.TelegraphDoubleCleave or Boss2StateKind.TelegraphGroundStomp
                or Boss2StateKind.TelegraphLeapSmash or Boss2StateKind.TelegraphFrenzyCharge or Boss2StateKind.TelegraphEarthquakeRoar => hpCfg.StateTelegraphingColor,
            Boss2StateKind.ExecutingHeavySlash or Boss2StateKind.ExecutingDoubleCleave or Boss2StateKind.ExecutingGroundStomp
                or Boss2StateKind.ExecutingLeapAscent or Boss2StateKind.ExecutingLeapDescent or Boss2StateKind.ExecutingFrenzyCharge
                or Boss2StateKind.ExecutingEarthquakeRoar => hpCfg.StateCastingColor,
            Boss2StateKind.WallCrashStun => ColorPalette.Gold,
            Boss2StateKind.SkillRecovery => hpCfg.StateRecoveringColor,
            Boss2StateKind.PhaseTransition => hpCfg.StatePhaseTransitionColor,
            Boss2StateKind.Dead => ColorPalette.Gray,
            _ => hpCfg.StateDefaultColor
        };
    }

    private static void DrawBossHpBar(BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg, string bossTitle, string stateName, Color stateBadgeColor, Health health, int phase, float stateTimer, float stateDuration)
    {
        int barX1 = hpCfg.BarX1;
        int barY1 = hpCfg.BarY1;
        int barX2 = hpCfg.BarX2;
        int barY2 = hpCfg.BarY2;
        int totalWidth = barX2 - barX1;

        // 4.1 Boss 情報ヘッダー (HP バー上部)
        string hpText = $"HP: {health.Current:F0} / {health.Max:F0}";
        GameEngine.DrawString(barX1, barY1 - hpCfg.HeaderOffsetY, bossTitle, hpCfg.TitleColor);
        GameEngine.DrawString(barX2 - 170, barY1 - hpCfg.HeaderOffsetY, hpText, hpCfg.HpTextColor);

        GameEngine.DrawString(barX1 + hpCfg.StateBadgeOffsetX, barY1 - hpCfg.HeaderOffsetY, $"{stateName} ({stateTimer:F1}s/{stateDuration:F1}s)", stateBadgeColor);

        // 4.2 Boss HP バー背景および枠線
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BgColor, true);
        GameEngine.DrawBox(barX1 - hpCfg.BgPadding, barY1 - hpCfg.BgPadding, barX2 + hpCfg.BgPadding, barY2 + hpCfg.BgPadding, hpCfg.BorderColor, false);

        // 4.3 Boss HP ゲージ塗りつぶし
        Color hpFillColor = phase switch
        {
            1 => hpCfg.Phase1Color,
            2 => hpCfg.Phase2Color,
            3 => hpCfg.Phase3Color,
            _ => hpCfg.Phase1Color
        };

        float hpRatio = health.Ratio;
        int fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY2, hpFillColor, true);
            GameEngine.DrawBox(barX1, barY1, barX1 + fillWidth, barY1 + hpCfg.TopHighlightThickness, hpCfg.TopHighlightColor, true);
        }

        // 4.4 Phase 分割マーカー (1/3, 2/3)
        int p1Split = barX1 + totalWidth / 3;
        int p2Split = barX1 + (totalWidth * 2) / 3;
        GameEngine.DrawBox(p1Split - 1, barY1, p1Split + 1, barY2, hpCfg.BorderColor, true);
        GameEngine.DrawBox(p2Split - 1, barY1, p2Split + 1, barY2, hpCfg.BorderColor, true);

        // 4.5 勝利バナー
        if (health.Current <= 0.0f)
        {
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryBgColor, true);
            GameEngine.DrawBox(modalCfg.VictoryX1, modalCfg.VictoryY1, modalCfg.VictoryX2, modalCfg.VictoryY2, modalCfg.VictoryOuterBorderColor, false);
            GameEngine.DrawBox(modalCfg.VictoryX1 + 2, modalCfg.VictoryY1 + 2, modalCfg.VictoryX2 - 2, modalCfg.VictoryY2 - 2, modalCfg.VictoryInnerBorderColor, false);
            GameEngine.DrawString(modalCfg.VictoryTitleX, modalCfg.VictoryTitleY, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
            GameEngine.DrawString(modalCfg.VictoryRetryX, modalCfg.VictoryRetryY, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
        }
    }
}
