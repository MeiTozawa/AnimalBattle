using System;
using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Abilities;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Presentation.Animation;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     World 単位で独立して保持される HUD 描画システム用コンテキスト。
/// </summary>
internal sealed class HudRenderSystemContext : IDisposable
{
    public readonly record struct PlayerHudData(
        Health Health,
        CharacterType CharacterType,
        bool IsInvincible,
        bool IsAlive);

    public readonly record struct BossHudData(
        Health Health,
        BossCombatState CombatState,
        (float Phase2Threshold, float Phase3Threshold) PhaseThresholds);

    public required Query<Health, CharacterTypeComponent, GameplayTagState> PlayerQuery { get; init; }
    public required Query<Health, BossCombatState> BossQuery { get; init; }
    public PlayerHudData? CachedPlayer;
    public BossHudData? CachedBoss;

    public void Dispose()
    {
        PlayerQuery.Dispose();
        BossQuery.Dispose();
    }
}

/// <summary>
///     自機ステータス：HP、二段ジャンプ残数、Boss 大 HP バー、および操作ガイドを描画する ECS システム。
///     戦闘状態：GameState.Battle時にスクリーン空間へ直接描画します。
/// </summary>
public static class HudRenderSystem {
    /// <summary>
    ///     HUD 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddHudRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<Health, CharacterTypeComponent, GameplayTagState>()
            .With<PlayerTag>()
            .Build();
        var bossQuery = world.QueryBuilder<Health, BossCombatState>()
            .With<BossTag>()
            .Build();

        world.Set(new HudRenderSystemContext
        {
            PlayerQuery = playerQuery,
            BossQuery = bossQuery
        });

        world.System("HudRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteHudRender);

        return world;
    }

    /// <summary>
    ///     HUD 描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteHudRender(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var ctx = w.Get<HudRenderSystemContext>();
        ctx.CachedPlayer = null;
        ctx.CachedBoss = null;

        ctx.PlayerQuery.Iter(CollectPlayer);
        ctx.BossQuery.Iter(CollectBoss);

        var player = ctx.CachedPlayer;
        var boss = ctx.CachedBoss;
        ctx.CachedPlayer = null;
        ctx.CachedBoss = null;

        ref readonly var renderCfg = ref w.Get<RenderConfig>();

        Render(player, boss, in renderCfg);
    }

    /// <summary>
    ///     自機 HUD データを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void CollectPlayer(
        Iter it,
        Field<Health> hps,
        Field<CharacterTypeComponent> charTypes,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        if (count == 0) return;

        var ctx = it.World().Get<HudRenderSystemContext>();
        var idx = count - 1;
        ref readonly var hp = ref hps[idx];
        ctx.CachedPlayer = new HudRenderSystemContext.PlayerHudData(
            hp,
            charTypes[idx].Value,
            tags[idx].HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable),
            hp.Current > 0.0f);
    }

    /// <summary>
    ///     Boss HUD データを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void CollectBoss(Iter it, Field<Health> hps, Field<BossCombatState> states)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        var ctx = world.Get<HudRenderSystemContext>();
        var idx = count - 1;
        ref readonly var state = ref states[idx];
        var (p2, p3) = state.BossType switch
        {
            BossType.Boss1 => (world.Get<Boss1Config>().Phase2.HpThreshold, world.Get<Boss1Config>().Phase3.HpThreshold),
            BossType.Boss2 => (world.Get<Boss2Config>().Phase2.HpThreshold, world.Get<Boss2Config>().Phase3.HpThreshold),
            _ => (0.7f, 0.3f)
        };
        ctx.CachedBoss = new HudRenderSystemContext.BossHudData(hps[idx], state, (p2, p3));
    }

    /// <summary>
    ///     HUD および各種ステータス UI をスクリーン空間に描画します。
    /// </summary>
    /// <param name="player">現在アクティブな自機データ。</param>
    /// <param name="boss">現在アクティブな Boss データ。</param>
    /// <param name="renderCfg">描画設定。</param>
    private static void Render(
        in HudRenderSystemContext.PlayerHudData? player,
        in HudRenderSystemContext.BossHudData? boss,
        in RenderConfig renderCfg)
    {
        Debug.Assert(GameEngine.CameraOffset == Vec2i.Zero, "[HudRenderSystem] HUDはスクリーン空間固定で描画される必要があり、CameraOffsetはゼロでなければなりません。");
        var hudCfg = renderCfg.Hud;
        var hpCfg = renderCfg.BossHpBar;
        var modalCfg = renderCfg.Modals;

        DrawControlHelp(hudCfg);
        DrawPlayerStatus(player, hudCfg);
        DrawBossStatus(boss, hpCfg, modalCfg);
        DrawGameOverModal(player, modalCfg);
    }

    private static void DrawControlHelp(HudRenderConfig hudCfg)
    {
        GameEngine.DrawString(hudCfg.TitlePos, hudCfg.TitleText, hudCfg.TitleColor);

        var lineIndex = 1;
        foreach (var item in hudCfg.Text)
        {
            var drawPos = new Vec2i(hudCfg.TitlePos.X, hudCfg.TitlePos.Y + hudCfg.LineSpacing * lineIndex);
            GameEngine.DrawString(drawPos, item.Text, item.Color);
            lineIndex++;
        }
    }

    private static void DrawPlayerStatus(in HudRenderSystemContext.PlayerHudData? player, HudRenderConfig hudCfg)
    {
        if (player == null) return;
        var p = player.Value;

        var maxHp = (int)Math.Max(1.0f, p.Health.Max);
        var currentHp = Math.Clamp((int)p.Health.Current, 0, maxHp);

        Span<char> symbolsBuf = stackalloc char[128];
        var symLen = 0;
        symbolsBuf[symLen++] = '[';
        symbolsBuf[symLen++] = ' ';
        for (var i = 0; i < maxHp && symLen + 3 < symbolsBuf.Length - 1; i++)
        {
            symbolsBuf[symLen++] = i < currentHp ? '●' : '○';
            symbolsBuf[symLen++] = ' ';
            symbolsBuf[symLen++] = ' ';
        }
        symbolsBuf[symLen++] = ']';
        var symbols = symbolsBuf[..symLen];

        var hpRatio = (float)currentHp / maxHp;
        var hpColor = hpRatio switch {
            var r when r >= hudCfg.PlayerHpHighThreshold => hudCfg.PlayerHpHighColor,
            var r when r >= hudCfg.PlayerHpMidThreshold => hudCfg.PlayerHpMidColor,
            > 0.0f => hudCfg.PlayerHpLowColor,
            _ => hudCfg.PlayerHpDeadColor
        };

        var charName = EnumStringLut.Get(p.CharacterType);
        Span<char> textBuf = stackalloc char[160];
        int charsWritten;
        if (p.IsInvincible)
        {
            textBuf.TryWrite($"PLAYER [{charName}] HP: {symbols} ({currentHp}/{maxHp}) [INVINCIBLE]", out charsWritten);
        }
        else
        {
            textBuf.TryWrite($"PLAYER [{charName}] HP: {symbols} ({currentHp}/{maxHp})", out charsWritten);
        }
        GameEngine.DrawString(hudCfg.PlayerHpPos, textBuf[..charsWritten], hpColor);
    }

    private static void DrawBossStatus(in HudRenderSystemContext.BossHudData? boss, BossHpBarRenderConfig hpCfg, ModalRenderConfig modalCfg)
    {
        if (boss == null) return;
        var b = boss.Value;

        var phase = ToLegacyPhase(b.CombatState.Phase);
        var bossTitle = GetBossTitle(b.CombatState.BossType, phase);
        var bossHp = b.Health;
        DrawBossHpBar(hpCfg, modalCfg, bossTitle, bossHp.Current, bossHp.Max, phase, b.PhaseThresholds);
    }

    /// <summary>
    ///     Boss の称号およびフェーズ表記を取得します：パターンマッチにより固定文字列を返し 0-GC で描画。
    /// </summary>
    private static string GetBossTitle(BossType bossType, int phase)
    {
        var p = Math.Clamp(phase, 1, 3);
        return (bossType, p) switch {
            (BossType.Boss1, 1) => "BOSS: Boss1 [PHASE 1/3]",
            (BossType.Boss1, 2) => "BOSS: Boss1 [PHASE 2/3]",
            (BossType.Boss1, 3) => "BOSS: Boss1 [PHASE 3/3]",
            (BossType.Boss2, 1) => "BOSS: Boss2 [PHASE 1/3]",
            (BossType.Boss2, 2) => "BOSS: Boss2 [PHASE 2/3]",
            (BossType.Boss2, 3) => "BOSS: Boss2 [PHASE 3/3]",
            _ => $"BOSS: {bossType} [PHASE {p}/3]"
        };
    }

    private static void DrawGameOverModal(in HudRenderSystemContext.PlayerHudData? player, ModalRenderConfig modalCfg)
    {
        if (player == null || player.Value.IsAlive) return;
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverBgColor, true);
        GameEngine.DrawBox(modalCfg.GameOverPos1, modalCfg.GameOverPos2, modalCfg.GameOverOuterBorderColor, false);
        var pad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.GameOverPos1 + pad, modalCfg.GameOverPos2 - pad, modalCfg.GameOverInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.GameOverTitlePos, modalCfg.GameOverTitleText, modalCfg.GameOverTitleColor);
        GameEngine.DrawString(modalCfg.GameOverRetryPos, modalCfg.GameOverRetryText, modalCfg.GameOverRetryColor);
    }

    private static int ToLegacyPhase(Phase phase) => phase switch {
        Phase.Phase1 => 1,
        Phase.Phase2 => 2,
        Phase.Phase3 => 3,
        _ => AssertHelper.FailWithDefault("[HUD 警告] 未知のフェーズ", 1)
    };

    internal static void FormatBossStateInfo(
        Span<char> destination,
        out int charsWritten,
        BossCombatState combatState,
        IBossBrain? brain,
        BossHpBarRenderConfig hpCfg,
        out Color badgeColor)
    {
        var currentAbility = brain?.AbilitySystem.CurrentAbility;
        var abilityName = currentAbility?.DisplayName;
        if (string.IsNullOrEmpty(abilityName))
        {
            abilityName = !combatState.CurrentSkillTag.IsEmpty ? combatState.CurrentSkillTag.ToString() : "None";
        }

        var tags = brain?.AbilitySystem.Tags ?? default;
        var isDead = brain?.IsDead ?? false;

        if (isDead)
        {
            badgeColor = ColorPalette.Gray;
            destination.TryWrite($"[STATE: DEFEATED]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Phasing))
        {
            badgeColor = hpCfg.StatePhaseTransitionColor;
            destination.TryWrite($"[STATE: PHASE TRANSITION (INVINCIBLE)]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Recovery))
        {
            badgeColor = hpCfg.StateRecoveringColor;
            destination.TryWrite($"[STATE: RECOVERING]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Stunned))
        {
            badgeColor = hpCfg.StateStunnedColor;
            destination.TryWrite($"[STATE: STUNNED (VULNERABLE)]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Telegraph))
        {
            badgeColor = hpCfg.StateTelegraphingColor;
            destination.TryWrite($"[STATE: TELEGRAPHING (WIND-UP: {abilityName})]", out charsWritten);
            return;
        }
        if (tags.HasTag(GameplayTag.Active))
        {
            badgeColor = hpCfg.StateCastingColor;
            destination.TryWrite($"[STATE: EXECUTING ({abilityName})]", out charsWritten);
            return;
        }

        badgeColor = hpCfg.StateDefaultColor;
        switch (brain)
        {
            case Boss1Brain b1:
                switch (b1.State)
                {
                    case Boss1State.Hover:
                        destination.TryWrite($"[STATE: HOVERING (IDLE)]", out charsWritten);
                        return;
                    case Boss1State.AttackSelection:
                        destination.TryWrite($"[STATE: ATTACK SELECTION]", out charsWritten);
                        return;
                    default:
                        destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                        return;
                }
            case Boss2Brain b2:
                switch (b2.State)
                {
                    case Boss2State.Idle:
                        destination.TryWrite($"[STATE: IDLE / ALERT]", out charsWritten);
                        return;
                    case Boss2State.PatrolChase:
                        destination.TryWrite($"[STATE: CHASE (NEXT: {abilityName})]", out charsWritten);
                        return;
                    case Boss2State.MoveToTakeoff:
                        destination.TryWrite($"[STATE: NAVIGATING TO TAKEOFF]", out charsWritten);
                        return;
                    case Boss2State.PlatformTraverse:
                        destination.TryWrite($"[STATE: PLATFORM TRAVERSE (AIRBORNE)]", out charsWritten);
                        return;
                    default:
                        destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                        return;
                }
            default:
                destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                return;
        }
    }

    private static void DrawBossHpBar(
        BossHpBarRenderConfig hpCfg,
        ModalRenderConfig modalCfg,
        string bossTitle,
        float currentHp,
        float maxHp,
        int phase,
        (float Phase2Threshold, float Phase3Threshold) phaseThresholds)
    {
        var barP1 = hpCfg.BarPos1;
        var barP2 = hpCfg.BarPos2;
        var totalWidth = barP2.X - barP1.X;

        // ボス情報ヘッダー：HPバー上部
        Span<char> hpBuf = stackalloc char[64];
        hpBuf.TryWrite($"HP: {currentHp:F0} / {maxHp:F0}", out var hpCharsWritten);

        var titlePos = barP1 + (hpCfg.TitleOffset != Vec2i.Zero
            ? hpCfg.TitleOffset
            : new Vec2i(0, -hpCfg.HeaderOffsetY));
        GameEngine.DrawString(titlePos, bossTitle, hpCfg.TitleColor);

        var hpX = barP2.X - (hpCfg.HpTextOffsetRight != 0 ? hpCfg.HpTextOffsetRight : 170);
        var hpY = barP1.Y - (hpCfg.HpTextOffsetY != 0 ? hpCfg.HpTextOffsetY : hpCfg.HeaderOffsetY);
        GameEngine.DrawString(new Vec2i(hpX, hpY), hpBuf[..hpCharsWritten], hpCfg.HpTextColor);

        // Boss HP バー背景および枠線
        var bgPad = new Vec2i(hpCfg.BgPadding, hpCfg.BgPadding);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BgColor, true);
        GameEngine.DrawBox(barP1 - bgPad, barP2 + bgPad, hpCfg.BorderColor, false);

        // Boss HP ゲージ塗りつぶし
        Color hpFillColor;
        switch (phase)
        {
            case 1:
                hpFillColor = hpCfg.Phase1Color;
            break;
            case 2:
                hpFillColor = hpCfg.Phase2Color;
            break;
            case 3:
                hpFillColor = hpCfg.Phase3Color;
            break;
            default:
                Debug.Fail("[HUD 警告] 未知のフェーズ");
                hpFillColor = hpCfg.Phase1Color;
            break;
        }

        var hpRatio = maxHp > 0.0f ? Math.Clamp(currentHp / maxHp, 0.0f, 1.0f) : 0.0f;
        var fillWidth = (int)(totalWidth * hpRatio);
        if (fillWidth > 0)
        {
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP2.Y), hpFillColor, true);
            GameEngine.DrawBox(barP1, new Vec2i(barP1.X + fillWidth, barP1.Y + hpCfg.TopHighlightThickness),
                hpCfg.TopHighlightColor, true);
        }

        // Phase 分割マーカー：設定ファイル駆動の HpThreshold に基づく動的描画
        var p2Ratio = Math.Clamp(phaseThresholds.Phase2Threshold, 0.0f, 1.0f);
        var p3Ratio = Math.Clamp(phaseThresholds.Phase3Threshold, 0.0f, 1.0f);

        var phase2MarkerX = barP1.X + (int)(totalWidth * p2Ratio);
        var phase3MarkerX = barP1.X + (int)(totalWidth * p3Ratio);

        GameEngine.DrawBox(new Vec2i(phase2MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase2MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);
        GameEngine.DrawBox(new Vec2i(phase3MarkerX - hpCfg.PhaseSplitWidth, barP1.Y), new Vec2i(phase3MarkerX + hpCfg.PhaseSplitWidth, barP2.Y), hpCfg.BorderColor, true);

        // 勝利バナー
        if (currentHp > 0.0f) return;
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryBgColor, true);
        GameEngine.DrawBox(modalCfg.VictoryPos1, modalCfg.VictoryPos2, modalCfg.VictoryOuterBorderColor, false);
        var vicPad = new Vec2i(modalCfg.InnerBorderPadding, modalCfg.InnerBorderPadding);
        GameEngine.DrawBox(modalCfg.VictoryPos1 + vicPad, modalCfg.VictoryPos2 - vicPad, modalCfg.VictoryInnerBorderColor, false);
        GameEngine.DrawString(modalCfg.VictoryTitlePos, modalCfg.VictoryTitleText, modalCfg.VictoryTitleColor);
        GameEngine.DrawString(modalCfg.VictoryRetryPos, modalCfg.VictoryRetryText, modalCfg.VictoryRetryColor);
    }
}
