using System;
using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ゲーム開始・選人画面の UI 描画（タイトルヘッダー、自機・首領カード動效、確定ボタン）を統括する純粋 ECS 描画システム。
///     GameState.Start 状態時にのみアクティブとなり、OnStore フェーズで画面へ UI を描画します。
/// </summary>
public static class StartUiRenderSystem {
    /// <summary>
    ///     開始選人画面の UI 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddStartUiRenderSystem(this World world)
    {
        var uiStateQuery = world.QueryBuilder<StartUiState>().Build();

        world.System("StartUiRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Start) return;

                var cfg = w.Get<StartSceneConfig>();
                var layout = cfg.Layout;
                var colors = cfg.Colors;
                var texts = cfg.Texts;

                var catCardBounds = layout.CatCard.ToRectangle();
                var bunnyCardBounds = layout.BunnyCard.ToRectangle();
                var boss1CardBounds = layout.Boss1Card.ToRectangle();
                var boss2CardBounds = layout.Boss2Card.ToRectangle();
                var confirmBtnBounds = layout.ConfirmButton.ToRectangle();

                uiStateQuery.Each((ref StartUiState uiState) =>
                {
                    ref readonly var flow = ref w.Get<GameFlow>();

                    // 1. タイトルヘッダー描画
                    GameEngine.DrawString(layout.HeaderTitleX, layout.HeaderTitleY, texts.HeaderTitle, colors.TextGold);
                    GameEngine.DrawString(layout.PlayerHeaderX, layout.PlayerHeaderY, texts.PlayerHeader, colors.TextWhite);
                    GameEngine.DrawString(layout.BossHeaderX, layout.BossHeaderY, texts.BossHeader, colors.TextWhite);

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

                    // 4. Boss 1 カード描画 (飛行浮遊動效)
                    DrawBoss1Card(
                        boss1CardBounds,
                        texts.Boss1,
                        flow.SelectedBoss == BossType.Boss1,
                        uiState.Boss1FlyTimer,
                        uiState.Boss1TextureHandle,
                        cfg);

                    // 5. Boss 2 カード描画 (騰空下砸動效)
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
                    GameEngine.DrawString(layout.HintTextX, layout.HintTextY, texts.HintText, colors.TextMuted);
                });
            });

        return world;
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

        var borderColor = isSelected ? selectColor : colors.BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, colors.CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + layout.CardTitleOffsetX, bounds.Top + layout.CardTitleOffsetY, cardText.Title, isSelected ? selectColor : colors.TextWhite);
        GameEngine.DrawString(bounds.Left + layout.CardTagOffsetX, bounds.Top + layout.CardTagOffsetY, cardText.Tag, colors.TextGold);

        // プレビュー自機スプライト描画
        if (previewEntity.IsAlive())
        {
            ref readonly var sprite = ref previewEntity.Get<Sprite>();
            ref readonly var transform = ref previewEntity.Get<Transform>();

            // ホバー時の小幅左右ステップ振動
            var stepOffset = stepTimer > 0f ? MathF.Sin(stepTimer * motion.StepFrequency) * motion.StepAmplitude : 0f;

            var drawCenterX = (int)(transform.X + stepOffset);
            var drawCenterY = (int)transform.Y;

            GameEngine.DrawRectRotaGraph3(
                drawCenterX,
                drawCenterY,
                sprite.SrcRect.Left,
                sprite.SrcRect.Top,
                sprite.SrcRect.Width,
                sprite.SrcRect.Height,
                transform.ScaleX,
                transform.ScaleY,
                0.0,
                sprite.GraphHandle,
                true,
                false);
        }

        var badgeText = isSelected ? texts.SelectedBadge : texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : colors.TextMuted;
        GameEngine.DrawString(bounds.Left + layout.CardBadgeLeftOffset, bounds.Bottom - layout.CardBadgeBottomOffset, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + layout.CardDescLeftOffset, bounds.Bottom - layout.CardDescBottomOffset, cardText.Description, colors.TextMuted);
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

        var borderColor = isSelected ? selectColor : colors.BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, colors.CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + layout.CardTitleOffsetX, bounds.Top + layout.CardTitleOffsetY, cardText.Title, isSelected ? selectColor : colors.TextWhite);
        GameEngine.DrawString(bounds.Left + layout.CardTagOffsetX, bounds.Top + layout.CardTagOffsetY, cardText.Tag, colors.TextGold);

        var badgeText = isSelected ? texts.SelectedBadge : texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : colors.TextMuted;
        GameEngine.DrawString(bounds.Left + layout.CardBadgeLeftOffset, bounds.Bottom - layout.CardBadgeBottomOffset, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + layout.CardDescLeftOffset, bounds.Bottom - layout.CardDescBottomOffset, cardText.Description, colors.TextMuted);
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

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + layout.PreviewCenterOffsetY + flyOffsetY);

            // 192x192 のボス画像をカード枠に適合するスケールで独立描画
            GameEngine.DrawRotaGraph3(
                drawX,
                drawY,
                motion.Boss1Scale,
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
            var scaleX = baseScale;
            var scaleY = baseScale;

            if (slamTimer > 0f)
            {
                var riseEnd = motion.Boss2RiseDuration;
                var slamEnd = riseEnd + motion.Boss2SlamDuration;
                var impactEnd = slamEnd + motion.Boss2ImpactDuration;

                if (slamTimer <= riseEnd)
                {
                    var t = slamTimer / riseEnd;
                    offsetY = motion.Boss2RiseHeight * t;
                    scaleX = baseScale * motion.Boss2RiseScale;
                    scaleY = baseScale * (2.0 - motion.Boss2RiseScale);
                }
                else if (slamTimer <= slamEnd)
                {
                    var t = (slamTimer - riseEnd) / motion.Boss2SlamDuration;
                    offsetY = motion.Boss2RiseHeight * (1f - t);
                    scaleX = baseScale * motion.Boss2SlamScale;
                    scaleY = baseScale * (2.0 - motion.Boss2SlamScale);
                }
                else if (slamTimer <= impactEnd)
                {
                    var t = (slamTimer - slamEnd) / motion.Boss2ImpactDuration;
                    offsetY = MathF.Sin(t * MathF.PI * motion.Boss2ImpactShakeFreq) * motion.Boss2ImpactShakeAmplitude;
                    scaleX = baseScale * (1.15 - 0.15 * t);
                    scaleY = baseScale * (0.85 + 0.15 * t);
                }
            }

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + layout.PreviewCenterOffsetY + offsetY);

            // 192x192 のボス画像をカード枠に適合するスケールで独立拡縮描画
            GameEngine.DrawRotaGraph3(
                drawX,
                drawY,
                scaleX,
                scaleY,
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

        GameEngine.DrawBox(
            bounds.Left, bounds.Top,
            bounds.Right, bounds.Bottom,
            btnColor, true);

        GameEngine.DrawBox(
            bounds.Left, bounds.Top,
            bounds.Right, bounds.Bottom,
            borderColor, false);

        if (isHovered)
        {
            GameEngine.DrawBox(
                bounds.Left - 1, bounds.Top - 1,
                bounds.Right + 1, bounds.Bottom + 1,
                borderColor, false);
        }

        GameEngine.DrawString(
            bounds.Left + layout.ConfirmBtnTextOffsetX,
            bounds.Top + layout.ConfirmBtnTextOffsetY,
            texts.ConfirmButton,
            colors.TextWhite);
    }
}
