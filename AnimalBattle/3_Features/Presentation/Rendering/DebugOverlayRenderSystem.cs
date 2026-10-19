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
public static class DebugOverlayRenderSystem
{
    private static Query<PlayerPawn> _playerQuery;
    private static Query<BaseBossAgent> _bossQuery;

    private static PlayerPawn? _cachedPlayer;
    private static BaseBossAgent? _cachedBoss;

    /// <summary>
    ///     デバッグ論理オーバーレイ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugOverlayRenderSystem(this World world)
    {
        _playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        _bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("DebugOverlayRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteDebugOverlayRender);

        return world;
    }

    /// <summary>
    ///     デバッグ論理オーバーレイ描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteDebugOverlayRender(Iter it)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Battle) return;

        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;
        if (!dbgCfg.DebugMode || !dbgCfg.IsActive) return;

        _cachedPlayer = null;
        _cachedBoss = null;

        _playerQuery.Each(FindPlayer);
        _bossQuery.Each(FindBoss);

        var player = _cachedPlayer;
        var boss = _cachedBoss;
        _cachedPlayer = null;
        _cachedBoss = null;

        DrawDebugOverlayHud(player, boss, in renderCfg);
    }

    private static void FindPlayer(ref PlayerPawn p) => _cachedPlayer = p;
    private static void FindBoss(ref BaseBossAgent b) => _cachedBoss = b;

    private static void DrawDebugOverlayHud(PlayerPawn? player, BaseBossAgent? boss, in RenderConfig renderCfg)
    {
        var hudCfg = renderCfg.Hud;
        var hpCfg = renderCfg.BossHpBar;
        var dbgCfg = renderCfg.Debug;

        // 右上デバッグアクティブ表示（画面固定）
        if (!string.IsNullOrEmpty(dbgCfg.IndicatorText))
        {
            GameEngine.DrawString(dbgCfg.IndicatorPos, dbgCfg.IndicatorText, dbgCfg.IndicatorColor);
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
            GameEngine.DrawString(hudCfg.DoubleJumpPos, jumpText, jumpColor);
        }

        // デバッグ操作案内（Boss切替、キャラ切替、フェーズ切替）（画面固定）
        var guideY = hudCfg.PlayerHpPos.Y + dbgCfg.GuideOffsetY;
        var lineIndex = 0;
        if (!string.IsNullOrEmpty(hudCfg.BossSwitchGuideText))
        {
            GameEngine.DrawString(new Vec2i(hudCfg.TitlePos.X, guideY + hudCfg.LineSpacing * lineIndex++), hudCfg.BossSwitchGuideText, hudCfg.BossSwitchGuideColor);
        }
        if (!string.IsNullOrEmpty(hudCfg.CharacterSwitchGuideText))
        {
            GameEngine.DrawString(new Vec2i(hudCfg.TitlePos.X, guideY + hudCfg.LineSpacing * lineIndex++), hudCfg.CharacterSwitchGuideText, hudCfg.CharacterSwitchGuideColor);
        }
        if (!string.IsNullOrEmpty(dbgCfg.PhaseSwitchGuideText))
        {
            GameEngine.DrawString(new Vec2i(hudCfg.TitlePos.X, guideY + hudCfg.LineSpacing * lineIndex++), dbgCfg.PhaseSwitchGuideText, dbgCfg.PhaseSwitchGuideColor);
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

            var badgePos = hpCfg.BarPos1 + (hpCfg.StateBadgeOffset != Vec2i.Zero 
                ? hpCfg.StateBadgeOffset 
                : new Vec2i(0, -hpCfg.HeaderOffsetY));
            GameEngine.DrawString(badgePos, $"{stateName} ({timerText})", stateBadgeColor);
        }
    }
}
