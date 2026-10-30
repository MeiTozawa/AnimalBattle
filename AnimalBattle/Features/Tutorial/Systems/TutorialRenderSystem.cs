using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Scenes;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアルのフェーズ別背景操作ガイド、目標物、全操作看板、およびボス扉の描画を統括する ECS 描画システム。
/// </summary>
public static class TutorialRenderSystem
{
    /// <summary>
    ///     チュートリアル描画システムをワールドへ登録します。
    /// </summary>
    public static World AddTutorialRenderSystem(this World world)
    {
        world.System("TutorialRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteTutorialRender);

        return world;
    }

    /// <summary>
    ///     チュートリアル描画パイプライン。
    /// </summary>
    public static void ExecuteTutorialRender(Iter it)
    {
        var world = it.World();
        if (world.Get<GameFlow>().State != GameState.Tutorial) return;

        ref readonly var state = ref world.Get<TutorialState>();
        var cfg = world.Get<TutorialConfig>() ?? new TutorialConfig();
        var colors = cfg.Colors;
        var assets = world.Get<IAssetManager>();

        // 1. 目標物（コイン／リング）の描画
        using var targetQuery = world.QueryBuilder<TutorialTarget, Transform>().Build();
        targetQuery.Each((Entity _, ref TutorialTarget target, ref Transform tr) =>
        {
            var bob = (int)(MathF.Sin(target.FloatPhase) * cfg.Timing.TargetFloatAmplitude);
            var center = new Vec2i((int)tr.Position.X, (int)tr.Position.Y + bob);
            var r = (int)target.Radius;

            // 外枠発光リングと内側コア
            GameEngine.DrawCircle(center, r, colors.Gold, false);
            GameEngine.DrawCircle(center, Math.Max(2, (int)(r * 0.5f)), colors.Gold, true);
            GameEngine.DrawCenterString(center, "★", colors.White);
        });

        // 2. ボス挑戦扉（Phase 7）の描画
        using var doorQuery = world.QueryBuilder<TutorialDoor, Transform>().Build();
        doorQuery.Each((Entity _, ref TutorialDoor door, ref Transform tr) =>
        {
            var doorCenter = new Vec2i((int)tr.Position.X, (int)tr.Position.Y);
            var halfW = cfg.Doors.Size.X / 2;
            var halfH = cfg.Doors.Size.Y / 2;

            var p1 = new Vec2i(doorCenter.X - halfW, doorCenter.Y - halfH);
            var p2 = new Vec2i(doorCenter.X + halfW, doorCenter.Y + halfH);

            // 扉の外枠と背景
            GameEngine.DrawBox(p1, p2, colors.DoorBg, true);
            var borderColor = door.IsPlayerInRange ? colors.Cyan : colors.PanelBorder;
            GameEngine.DrawBox(p1, p2, borderColor, false);

            if (door.IsPlayerInRange)
            {
                GameEngine.DrawBox(p1 - Vec2i.One, p2 + Vec2i.One, colors.Cyan, false);
                // 足元ハイライトサークル
                GameEngine.DrawCircle(new Vec2i(doorCenter.X, p2.Y - 5), cfg.Doors.HighlightCircleRadius, colors.Cyan, false);
            }

            // ボス名プレートおよびタグ
            var isSnake = door.TargetBoss == BossType.BossSnake;
            var doorItem = isSnake ? cfg.Doors.Snake : cfg.Doors.Lion;
            var tagColor = isSnake ? colors.Cyan : colors.Gold;

            GameEngine.DrawCenterString(new Vec2i(doorCenter.X, p1.Y + 20), doorItem.Title, colors.White);
            GameEngine.DrawCenterString(new Vec2i(doorCenter.X, p1.Y + 40), doorItem.Tag, tagColor);

            // 扉中央の Boss プレビュー画像
            var texPath = doorItem.TexturePath;
            if (assets.TryGetTexture(texPath, out var texHandle))
            {
                var bobY = isSnake ? MathF.Sin(door.AnimTimer * cfg.Doors.PreviewBobFrequency) * cfg.Doors.PreviewBobAmplitude : 0f;
                var previewPos = new Vec2i(doorCenter.X, (int)(doorCenter.Y + 15 + bobY));
                GameEngine.DrawRotaGraph3(
                    previewPos,
                    cfg.Doors.PreviewScale,
                    0.0,
                    texHandle,
                    true);
            }

            // 接近時の挑戦ツールチップ
            if (door.IsPlayerInRange)
            {
                var promptPos = new Vec2i(doorCenter.X, p1.Y + cfg.Doors.TooltipOffsetY);
                var pulse = (int)(MathF.Sin(door.AnimTimer * 6f) * 2f);
                var halfTipW = cfg.Doors.TooltipSize.X / 2;
                var halfTipH = cfg.Doors.TooltipSize.Y / 2;
                var bubbleP1 = new Vec2i(promptPos.X - halfTipW, promptPos.Y - halfTipH + pulse);
                var bubbleP2 = new Vec2i(promptPos.X + halfTipW, promptPos.Y + halfTipH + pulse);

                GameEngine.DrawBox(bubbleP1, bubbleP2, colors.TooltipBg, true);
                GameEngine.DrawBox(bubbleP1, bubbleP2, colors.Cyan, false);
                GameEngine.DrawCenterString(promptPos + new Vec2i(0, pulse), cfg.Texts.DoorInteractPrompt, colors.Gold);
            }
        });

        // 3. 画面上部・中央のフェーズ別背景操作ガイド文字
        DrawPhaseGuidance(state, cfg);

        // 4. 戦闘フェーズ（Phase 5 飛行小怪、Phase 6 地上小怪）における全操作ガイド看板
        if (state.CurrentPhase is TutorialPhase.FlyingMinion or TutorialPhase.GroundMinion)
        {
            DrawControlGuidanceBoard(cfg);
        }

        // 5. リトライ表示
        if (state.IsRetrying)
        {
            GameEngine.DrawCenterString(cfg.Layout.RetryPos, cfg.Texts.RetryPrompt, colors.Red);
        }
    }

    private static void DrawPhaseGuidance(in TutorialState state, TutorialConfig cfg)
    {
        var centerPos = cfg.Layout.GuidanceTitlePos;
        var subPos = cfg.Layout.GuidanceSubPos;
        var colors = cfg.Colors;
        var texts = cfg.Texts;

        var phaseText = state.CurrentPhase switch
        {
            TutorialPhase.Movement => texts.Movement,
            TutorialPhase.Jump => texts.Jump,
            TutorialPhase.DoubleJump => texts.DoubleJump,
            TutorialPhase.Dash => texts.Dash,
            TutorialPhase.FlyingMinion => texts.FlyingMinion,
            TutorialPhase.GroundMinion => texts.GroundMinion,
            TutorialPhase.BossSelect => texts.BossSelect,
            _ => null
        };

        if (phaseText != null)
        {
            GameEngine.DrawCenterString(centerPos, phaseText.Title, colors.Gold);
            GameEngine.DrawCenterString(subPos, phaseText.Subtitle, colors.White);
        }
    }

    private static void DrawControlGuidanceBoard(TutorialConfig cfg)
    {
        var p1 = cfg.Layout.ControlBoardP1;
        var p2 = cfg.Layout.ControlBoardP2;
        var colors = cfg.Colors;
        var texts = cfg.Texts;

        GameEngine.DrawBox(p1, p2, colors.PanelBg, true);
        GameEngine.DrawBox(p1, p2, colors.PanelBorder, false);

        var startX = p1.X + 15;
        var y = p1.Y + 15;

        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardTitle, colors.Cyan);
        y += 24;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardMove, colors.White);
        y += 22;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardJump, colors.White);
        y += 22;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardDash, colors.White);
        y += 24;
        GameEngine.DrawLeftString(new Vec2i(startX, y), texts.ControlBoardAttack, colors.Gold);
    }
}
