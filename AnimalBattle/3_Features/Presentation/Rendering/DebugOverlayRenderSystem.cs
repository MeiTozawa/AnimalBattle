using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Characters;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     スクリーン空間におけるデバッグ用 HUD（Boss ステートマシン、二段ジャンプ、操作案内、デバッグ表示）を描画する ECS システム。
///     CameraOffsetResetSystem の後に実行されるため、カメラ移動やシェイクの影響を受けずに画面固定で描画されます。
/// </summary>
public static class DebugOverlayRenderSystem
{
    private readonly record struct BossDebugData(BossCombatState State, IBossBrain Brain);

    private static Query<JumpCapability> _playerQuery;
    private static Query<BossCombatState, BossBrainComponent> _bossQuery;

    private static int? _cachedRemainingJumps;
    private static BossDebugData? _cachedBoss;

    /// <summary>
    ///     デバッグ論理オーバーレイ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDebugOverlayRenderSystem(this World world)
    {
        _playerQuery = world.QueryBuilder<JumpCapability>()
            .With<PlayerTag>()
            .Build();
        _bossQuery = world.QueryBuilder<BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Without<DeadTag>()
            .Build();

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

        _cachedRemainingJumps = null;
        _cachedBoss = null;

        _playerQuery.Each(FindPlayer);
        _bossQuery.Each(FindBoss);

        var remainingJumps = _cachedRemainingJumps;
        var boss = _cachedBoss;
        _cachedRemainingJumps = null;
        _cachedBoss = null;

        DrawDebugOverlayHud(remainingJumps, boss, in renderCfg);
    }

    private static void FindPlayer(ref JumpCapability jump) => _cachedRemainingJumps = jump.RemainingJumps;
    private static void FindBoss(ref BossCombatState state, ref BossBrainComponent brainComp) => _cachedBoss = new BossDebugData(state, brainComp.Brain);

    private static void DrawDebugOverlayHud(int? remainingJumps, in BossDebugData? boss, in RenderConfig renderCfg)
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
        if (remainingJumps.HasValue)
        {
            var jumps = remainingJumps.Value;
            var jumpDots = jumps switch {
                >= 2 => "[ ●  ● ]",
                1 => "[ ●  ○ ]",
                _ => "[ ○  ○ ]"
            };
            var jumpText = string.IsNullOrEmpty(hudCfg.DoubleJumpText)
                ? jumpDots
                : $"{hudCfg.DoubleJumpText} {jumpDots}";
            var jumpColor = jumps > 0 ? hudCfg.DoubleJumpActiveColor : hudCfg.DoubleJumpInactiveColor;
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

        // Boss ステートマシン詳細バッジとスキルタイマー（Boss HP バー上部・画面固定）
        if (boss != null)
        {
            var b = boss.Value;
            HudRenderSystem.GetBossStateInfo(b.State, b.Brain, hpCfg, out var stateName, out var stateBadgeColor);
            string timerText;
            if (b.Brain.AbilitySystem.IsCasting)
            {
                var pElapsed = b.Brain.AbilitySystem.Elapsed;
                var pDuration = b.Brain.AbilitySystem.Duration;
                timerText = pDuration > 0.0f
                    ? $"{pElapsed:F1}s/{pDuration:F1}s"
                    : $"{pElapsed:F1}s";
            }
            else
            {
                timerText = $"{b.State.StateTimer:F1}s";
            }

            var badgePos = hpCfg.BarPos1 + (hpCfg.StateBadgeOffset != Vec2i.Zero 
                ? hpCfg.StateBadgeOffset 
                : new Vec2i(0, -hpCfg.HeaderOffsetY));
            GameEngine.DrawString(badgePos, $"{stateName} ({timerText})", stateBadgeColor);
        }
    }
}
