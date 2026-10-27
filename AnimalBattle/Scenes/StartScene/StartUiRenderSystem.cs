using System.Drawing;
using AnimalBattle.Ai;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の UI 描画：タイトルヘッダー、自機・ボスカード演出、確定ボタンを統括する純粋 ECS 描画システム。
///     GameState.Start 状態時にのみアクティブとなり、OnStore フェーズで画面へ UI を描画します。
/// </summary>
public static class StartUiRenderSystem
{
    /// <summary>
    ///     開始キャラクター選択画面の UI 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddStartUiRenderSystem(this World world)
    {
        world.System<StartUiState>("StartUiRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteStartUiRender);

        return world;
    }

    /// <summary>
    ///     開始キャラクター選択画面の UI 描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteStartUiRender(Iter it, Field<StartUiState> uiStates)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Start) return;

        var count = it.Count();
        if (count == 0) return;

        var cfg = w.Get<StartSceneConfig>();
        var layout = cfg.Layout;
        var colors = cfg.Colors;
        var texts = cfg.Texts;

        var catCardBounds = layout.CatCard.ToRectangle();
        var bunnyCardBounds = layout.BunnyCard.ToRectangle();
        var boss1CardBounds = layout.Boss1Card.ToRectangle();
        var boss2CardBounds = layout.Boss2Card.ToRectangle();
        var confirmBtnBounds = layout.ConfirmButton.ToRectangle();

        ref readonly var flow = ref w.Get<GameFlow>();

        for (var i = 0; i < count; i++)
        {
            ref var uiState = ref uiStates[i];

            // 1. タイトルヘッダー描画
            GameEngine.DrawString(layout.HeaderTitlePos, texts.HeaderTitle, colors.TextGold);
            GameEngine.DrawString(layout.PlayerHeaderPos, texts.PlayerHeader, colors.TextWhite);
            GameEngine.DrawString(layout.BossHeaderPos, texts.BossHeader, colors.TextWhite);

            // 2. Cat カード描画
            DrawPlayerCard(
                uiState.CatPreviewEntity,
                catCardBounds,
                texts.Cat,
                flow.SelectedPlayer == CharacterType.Cat,
                uiState.CatStepTimer,
                colors.PlayerSelectedColor,
                cfg);

            // 3. Bunny カード描画
            DrawPlayerCard(
                uiState.BunnyPreviewEntity,
                bunnyCardBounds,
                texts.Bunny,
                flow.SelectedPlayer == CharacterType.Bunny,
                uiState.BunnyStepTimer,
                colors.PlayerSelectedColor,
                cfg);

            // 4. Boss 1 カード描画 飛行浮遊アニメーション
            DrawBoss1Card(
                boss1CardBounds,
                texts.Boss1,
                flow.SelectedBoss == BossType.Boss1,
                uiState.Boss1FlyTimer,
                uiState.Boss1TextureHandle,
                cfg);

            // 5. Boss 2 カード描画 跳躍叩きつけアニメーション
            DrawBoss2Card(
                boss2CardBounds,
                texts.Boss2,
                flow.SelectedBoss == BossType.Boss2,
                uiState.Boss2SlamTimer,
                uiState.Boss2TextureHandle,
                cfg);

            // 6. 確定ボタン描画
            DrawConfirmButton(uiState.IsConfirmHovered, confirmBtnBounds, cfg);

            // 7. 下部ヒントテキスト
            GameEngine.DrawString(layout.HintTextPos, texts.HintText, colors.TextMuted);
        }
    }

    private static void DrawPlayerCard(
        Entity previewEntity,
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        float stepTimer,
        Color selectColor,
        StartSceneConfig cfg)
    {
        var colors = cfg.Colors;
        var layout = cfg.Layout;
        var motion = cfg.Motion;
        var texts = cfg.Texts;

        var p1 = new Vec2i(bounds.Left, bounds.Top);
        var p2 = new Vec2i(bounds.Right, bounds.Bottom);
        var borderColor = isSelected ? selectColor : colors.BorderDefaultColor;
        GameEngine.DrawBox(p1, p2, colors.CardBgColor, true);
        GameEngine.DrawBox(p1, p2, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(p1 - Vec2i.One, p2 + Vec2i.One, borderColor, false);
        }

        GameEngine.DrawString(p1 + layout.CardTitleOffset, cardText.Title, isSelected ? selectColor : colors.TextWhite);
        GameEngine.DrawString(p1 + layout.CardTagOffset, cardText.Tag, colors.TextGold);

        // プレビュー自機スプライト描画
        if (previewEntity.IsAlive())
        {
            ref readonly var sprite = ref previewEntity.Get<Sprite>();
            ref readonly var transform = ref previewEntity.Get<Transform>();

            // ホバー時の小幅左右ステップ振動
            var stepOffset = stepTimer > 0f ? MathF.Sin(stepTimer * motion.StepFrequency) * motion.StepAmplitude : 0f;

            var drawCenter = new Vec2i((int)(transform.Position.X + stepOffset), (int)transform.Position.Y);

            GameEngine.DrawRectRotaGraph(
                drawCenter,
                new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top),
                new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height),
                transform.Scale,
                0.0,
                sprite.GraphHandle,
                true,
                false);
        }

        var badgeText = isSelected ? texts.SelectedBadge : texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : colors.TextMuted;
        GameEngine.DrawString(new Vec2i(bounds.Left + layout.CardBadgeLeftOffset, bounds.Bottom - layout.CardBadgeBottomOffset), badgeText, badgeColor);
        GameEngine.DrawString(new Vec2i(bounds.Left + layout.CardDescLeftOffset, bounds.Bottom - layout.CardDescBottomOffset), cardText.Description, colors.TextMuted);
    }

    private static void DrawCardBox(
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        Color selectColor,
        StartSceneConfig cfg)
    {
        var colors = cfg.Colors;
        var layout = cfg.Layout;
        var texts = cfg.Texts;

        var p1 = new Vec2i(bounds.Left, bounds.Top);
        var p2 = new Vec2i(bounds.Right, bounds.Bottom);
        var borderColor = isSelected ? selectColor : colors.BorderDefaultColor;
        GameEngine.DrawBox(p1, p2, colors.CardBgColor, true);
        GameEngine.DrawBox(p1, p2, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(p1 - Vec2i.One, p2 + Vec2i.One, borderColor, false);
        }

        GameEngine.DrawString(p1 + layout.CardTitleOffset, cardText.Title, isSelected ? selectColor : colors.TextWhite);
        GameEngine.DrawString(p1 + layout.CardTagOffset, cardText.Tag, colors.TextGold);

        var badgeText = isSelected ? texts.SelectedBadge : texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : colors.TextMuted;
        GameEngine.DrawString(new Vec2i(bounds.Left + layout.CardBadgeLeftOffset, bounds.Bottom - layout.CardBadgeBottomOffset), badgeText, badgeColor);
        GameEngine.DrawString(new Vec2i(bounds.Left + layout.CardDescLeftOffset, bounds.Bottom - layout.CardDescBottomOffset), cardText.Description, colors.TextMuted);
    }

    private static void DrawBoss1Card(
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        float flyTimer,
        int textureHandle,
        StartSceneConfig cfg)
    {
        DrawCardBox(bounds, cardText, isSelected, cfg.Colors.BossSelectedColor, cfg);

        if (textureHandle >= 0)
        {
            var motion = cfg.Motion;
            var layout = cfg.Layout;

            var flyOffsetY = flyTimer > 0f
                ? MathF.Sin(flyTimer * motion.Boss1FlyFrequency) * motion.Boss1FlyAmplitude + motion.Boss1FlyOffsetY
                : 0f;
            var tiltAngle = flyTimer > 0f
                ? MathF.Sin(flyTimer * motion.Boss1TiltFrequency) * motion.Boss1TiltAmplitude
                : 0f;

            var drawCenter = new Vec2i(bounds.Left + bounds.Width / 2, (int)(bounds.Top + layout.PreviewCenterOffsetY + flyOffsetY));

            // 192x192 のボス画像をカード枠に適合するスケールで独立描画
            GameEngine.DrawRotaGraph(
                drawCenter,
                motion.Boss1Scale,
                tiltAngle,
                textureHandle,
                true);
        }
    }

    private static void DrawBoss2Card(
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        float slamTimer,
        int textureHandle,
        StartSceneConfig cfg)
    {
        DrawCardBox(bounds, cardText, isSelected, cfg.Colors.BossSelectedColor, cfg);

        if (textureHandle >= 0)
        {
            var motion = cfg.Motion;
            var layout = cfg.Layout;

            var offsetY = 0f;
            var baseScale = motion.Boss2BaseScale;
            var scale = new Vec2f((float)baseScale, (float)baseScale);

            if (slamTimer > 0f)
            {
                var riseEnd = motion.Boss2RiseDuration;
                var slamEnd = riseEnd + motion.Boss2SlamDuration;
                var impactEnd = slamEnd + motion.Boss2ImpactDuration;

                if (slamTimer <= riseEnd)
                {
                    var t = slamTimer / riseEnd;
                    offsetY = motion.Boss2RiseHeight * t;
                    scale = new Vec2f((float)(baseScale * motion.Boss2RiseScale), (float)(baseScale * (2.0 - motion.Boss2RiseScale)));
                }
                else if (slamTimer <= slamEnd)
                {
                    var t = (slamTimer - riseEnd) / motion.Boss2SlamDuration;
                    offsetY = motion.Boss2RiseHeight * (1f - t);
                    scale = new Vec2f((float)(baseScale * motion.Boss2SlamScale), (float)(baseScale * (2.0 - motion.Boss2SlamScale)));
                }
                else if (slamTimer <= impactEnd)
                {
                    var t = (slamTimer - slamEnd) / motion.Boss2ImpactDuration;
                    offsetY = MathF.Sin(t * MathF.PI * motion.Boss2ImpactShakeFreq) * motion.Boss2ImpactShakeAmplitude;
                    scale = new Vec2f((float)(baseScale * (1.15 - 0.15 * t)), (float)(baseScale * (0.85 + 0.15 * t)));
                }
            }

            var drawCenter = new Vec2i(bounds.Left + bounds.Width / 2, (int)(bounds.Top + layout.PreviewCenterOffsetY + offsetY));

            // 192x192 のボス画像をカード枠に適合するスケールで独立拡縮描画
            GameEngine.DrawRotaGraph3(
                drawCenter,
                scale,
                0.0,
                textureHandle,
                true);
        }
    }

    private static void DrawConfirmButton(bool isHovered, Rectangle bounds, StartSceneConfig cfg)
    {
        var colors = cfg.Colors;
        var layout = cfg.Layout;
        var texts = cfg.Texts;

        var btnColor = isHovered ? colors.ConfirmBtnHoverColor : colors.ConfirmBtnDefaultColor;
        var borderColor = isHovered ? colors.ConfirmBtnBorderHoverColor : colors.ConfirmBtnBorderDefaultColor;

        var p1 = new Vec2i(bounds.Left, bounds.Top);
        var p2 = new Vec2i(bounds.Right, bounds.Bottom);

        GameEngine.DrawBox(p1, p2, btnColor, true);
        GameEngine.DrawBox(p1, p2, borderColor, false);

        if (isHovered)
        {
            GameEngine.DrawBox(p1 - Vec2i.One, p2 + Vec2i.One, borderColor, false);
        }

        GameEngine.DrawString(
            p1 + layout.ConfirmBtnTextOffset,
            texts.ConfirmButton,
            colors.TextWhite);
    }
}
