using System.Drawing;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     スクリーン空間における論理デバッグ HUD（Boss 状態機、二段ジャンプ、操作案内、デバッグ表示）を描画する ECS システム。
///     CameraOffsetResetSystem の後に実行されるため、カメラ移動やシェイクの影響を受けずに画面固定で描画されます。
/// </summary>
public static class DebugOverlayRenderSystem {
    /// <summary>
    ///     デバッグ論理オーバーレイ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugOverlayRenderSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("DebugOverlayRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                var renderCfg = w.Get<RenderConfig>();
                var dbgCfg = renderCfg.Debug;
                if (!dbgCfg.DebugMode || !dbgCfg.IsActive) return;

                PlayerPawn? player = null;
                playerQuery.Each((ref PlayerPawn p) => player = p);

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);

                DrawDebugOverlayHud(player, boss, in renderCfg);
            });

        return world;
    }

    private static void DrawDebugOverlayHud(PlayerPawn? player, BaseBossAgent? boss, in RenderConfig renderCfg)
    {
        var hudCfg = renderCfg.Hud;
        var hpCfg = renderCfg.BossHpBar;
        var dbgCfg = renderCfg.Debug;

        // 右上デバッグアクティブ表示（画面固定）
        if (!string.IsNullOrEmpty(dbgCfg.IndicatorText))
        {
            GameEngine.DrawString(dbgCfg.IndicatorX, dbgCfg.IndicatorY, dbgCfg.IndicatorText, dbgCfg.IndicatorColor);
        }

        // 二段ジャンプ残数インジケータ（画面固定）
        if (player != null)
        {
            var remainingJumps = player.RemainingJumps;
            var jumpDots = remainingJumps switch {
                >= 2 => "[ ●  ● ]",
                1 => "[ ●  ○ ]",
                _ => "[ ○  ○ ]"
            };
            var jumpText = string.IsNullOrEmpty(hudCfg.DoubleJumpText)
                ? jumpDots
                : $"{hudCfg.DoubleJumpText} {jumpDots}";
            var jumpColor = remainingJumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
            GameEngine.DrawString(hudCfg.DoubleJumpX, hudCfg.DoubleJumpY, jumpText, jumpColor);
        }

        // デバッグ操作案内（Boss切替、キャラ切替、フェーズ切替）（画面固定）
        var guideY = hudCfg.PlayerHpY + dbgCfg.GuideOffsetY;
        var lineIndex = 0;
        if (!string.IsNullOrEmpty(hudCfg.BossSwitchGuideText))
        {
            GameEngine.DrawString(hudCfg.TitleX, guideY + hudCfg.LineSpacing * lineIndex++, hudCfg.BossSwitchGuideText, hudCfg.BossSwitchGuideColor);
        }
        if (!string.IsNullOrEmpty(hudCfg.CharacterSwitchGuideText))
        {
            GameEngine.DrawString(hudCfg.TitleX, guideY + hudCfg.LineSpacing * lineIndex++, hudCfg.CharacterSwitchGuideText, hudCfg.CharacterSwitchGuideColor);
        }
        if (!string.IsNullOrEmpty(dbgCfg.PhaseSwitchGuideText))
        {
            GameEngine.DrawString(hudCfg.TitleX, guideY + hudCfg.LineSpacing * lineIndex++, dbgCfg.PhaseSwitchGuideText, dbgCfg.PhaseSwitchGuideColor);
        }

        // Boss 状態機詳細バッジとスキルタイマー（Boss HP バー上部・画面固定）
        if (boss != null)
        {
            HudRenderSystem.GetBossStateInfo(boss, hpCfg, out var stateName, out var stateBadgeColor);
            string timerText;
            if (boss.AbilitySystem.IsCasting)
            {
                var pElapsed = boss.AbilitySystem.PhaseElapsed;
                var pDuration = boss.AbilitySystem.PhaseDuration;
                timerText = pDuration > 0.0f
                    ? $"{pElapsed:F1}s/{pDuration:F1}s"
                    : $"{pElapsed:F1}s";
            }
            else
            {
                timerText = $"{boss.StateTimer:F1}s";
            }

            var badgeX = hpCfg.BarX1 + hpCfg.StateBadgeOffsetX;
            var badgeY = hpCfg.BarY1 - (hpCfg.StateBadgeOffsetY != 0 ? hpCfg.StateBadgeOffsetY : hpCfg.HeaderOffsetY);
            GameEngine.DrawString(badgeX, badgeY, $"{stateName} ({timerText})", stateBadgeColor);
        }
    }
}
