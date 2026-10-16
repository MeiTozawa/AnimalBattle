using System;
using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ゲーム開始・選人画面の入力判定および UI 描画を統括する ECS システム。
///     GameState.Start 状態時にのみアクティブとなり、マウスホバー動效・選人切り替え・戦闘開始を処理します。
/// </summary>
public static class StartSceneSystems {
    private static StartSceneLayoutConfig Layout => ConfigManager.StartScene.Layout;
    private static StartSceneColorsConfig Colors => ConfigManager.StartScene.Colors;
    private static StartSceneTextsConfig Texts => ConfigManager.StartScene.Texts;
    private static StartSceneMotionConfig Motion => ConfigManager.StartScene.Motion;

    private static Rectangle CatCardBounds => Layout.CatCard.ToRectangle();
    private static Rectangle BunnyCardBounds => Layout.BunnyCard.ToRectangle();
    private static Rectangle Boss1CardBounds => Layout.Boss1Card.ToRectangle();
    private static Rectangle Boss2CardBounds => Layout.Boss2Card.ToRectangle();
    private static Rectangle ConfirmButtonBounds => Layout.ConfirmButton.ToRectangle();

    /// <summary>
    ///     開始選人画面システム群を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddStartSceneSystems(this World world)
    {
        var uiStateQuery = world.QueryBuilder<StartUiState>().Build();

        // 1. 選人入力・ホバー状態更新システム (PreUpdate)
        world.System("StartUiInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Start) return;

                uiStateQuery.Each((ref StartUiState uiState) =>
                {
                    ref var flow = ref w.GetMut<GameFlow>();

                var dt = it.DeltaTime();
                var mousePos = InputService.GetMousePosition();
                var isClick = InputService.IsPressed(InputAction.MouseClick);
                var isConfirmKey = InputService.IsPressed(InputAction.Confirm);

                // --- 1. Cat カード判定 ---
                var isCatHovered = CatCardBounds.Contains(mousePos.X, mousePos.Y);
                if (uiState.CatPreviewEntity.IsAlive())
                {
                    ref var anim = ref uiState.CatPreviewEntity.GetMut<Animation>();
                    if (isCatHovered)
                    {
                        anim.Play("Run");
                        uiState.CatStepTimer += dt;
                    }
                    else
                    {
                        anim.Play("Idle");
                        uiState.CatStepTimer = 0f;
                    }
                }
                if (isCatHovered && isClick)
                {
                    flow.SelectedPlayer = CharacterType.Cat;
                }

                // --- 2. Bunny カード判定 ---
                var isBunnyHovered = BunnyCardBounds.Contains(mousePos.X, mousePos.Y);
                if (uiState.BunnyPreviewEntity.IsAlive())
                {
                    ref var anim = ref uiState.BunnyPreviewEntity.GetMut<Animation>();
                    if (isBunnyHovered)
                    {
                        anim.Play("Run");
                        uiState.BunnyStepTimer += dt;
                    }
                    else
                    {
                        anim.Play("Idle");
                        uiState.BunnyStepTimer = 0f;
                    }
                }
                if (isBunnyHovered && isClick)
                {
                    flow.SelectedPlayer = CharacterType.Bunny;
                }

                // --- 3. Boss 1 カード判定 (飛行浮遊動效) ---
                uiState.IsBoss1Hovered = Boss1CardBounds.Contains(mousePos.X, mousePos.Y);
                if (uiState.IsBoss1Hovered)
                {
                    uiState.Boss1FlyTimer += dt;
                    if (isClick) flow.SelectedBoss = BossType.Boss1;
                }
                else
                {
                    uiState.Boss1FlyTimer = Math.Max(0f, uiState.Boss1FlyTimer - dt * Motion.Boss1FlyDecaySpeed);
                }

                // --- 4. Boss 2 カード判定 (騰空・重撃下砸動效) ---
                uiState.IsBoss2Hovered = Boss2CardBounds.Contains(mousePos.X, mousePos.Y);
                if (uiState.IsBoss2Hovered)
                {
                    uiState.Boss2SlamTimer += dt;
                    if (uiState.Boss2SlamTimer > Motion.Boss2SlamCycleDuration)
                    {
                        uiState.Boss2SlamTimer -= Motion.Boss2SlamCycleDuration;
                    }
                    if (isClick) flow.SelectedBoss = BossType.Boss2;
                }
                else
                {
                    uiState.Boss2SlamTimer = 0f;
                }

                // --- 5. 確定ボタン判定 ---
                uiState.IsConfirmHovered = ConfirmButtonBounds.Contains(mousePos.X, mousePos.Y);
                if ((uiState.IsConfirmHovered && isClick) || isConfirmKey)
                {
                    GameManager.Current.SwitchToBattle(flow.SelectedPlayer, flow.SelectedBoss);
                }
                });
            });

        // 2. 選人画面 UI 描画システム (OnStore)
        world.System("StartUiRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Start) return;

                uiStateQuery.Each((ref StartUiState uiState) =>
                {
                    ref readonly var flow = ref w.Get<GameFlow>();

                // 1. タイトルヘッダー描画
                GameEngine.DrawString(Layout.HeaderTitleX, Layout.HeaderTitleY, Texts.HeaderTitle, Colors.TextGold);
                GameEngine.DrawString(Layout.PlayerHeaderX, Layout.PlayerHeaderY, Texts.PlayerHeader, Colors.TextWhite);
                GameEngine.DrawString(Layout.BossHeaderX, Layout.BossHeaderY, Texts.BossHeader, Colors.TextWhite);

                // 2. Cat カード描画
                DrawPlayerCard(
                    uiState.CatPreviewEntity,
                    CatCardBounds,
                    Texts.Cat,
                    flow.SelectedPlayer == CharacterType.Cat,
                    uiState.CatStepTimer,
                    Colors.PlayerSelectedColor);

                // 3. Bunny カード描画
                DrawPlayerCard(
                    uiState.BunnyPreviewEntity,
                    BunnyCardBounds,
                    Texts.Bunny,
                    flow.SelectedPlayer == CharacterType.Bunny,
                    uiState.BunnyStepTimer,
                    Colors.PlayerSelectedColor);

                // 4. Boss 1 カード描画 (飛行浮遊動效)
                DrawBoss1Card(
                    Boss1CardBounds,
                    Texts.Boss1,
                    flow.SelectedBoss == BossType.Boss1,
                    uiState.Boss1FlyTimer,
                    uiState.Boss1TextureHandle);

                // 5. Boss 2 カード描画 (騰空下砸動效)
                DrawBoss2Card(
                    Boss2CardBounds,
                    Texts.Boss2,
                    flow.SelectedBoss == BossType.Boss2,
                    uiState.Boss2SlamTimer,
                    uiState.Boss2TextureHandle);

                // 6. 確定ボタン描画
                DrawConfirmButton(uiState.IsConfirmHovered);

                // 7. 下部ヒントテキスト
                GameEngine.DrawString(Layout.HintTextX, Layout.HintTextY, Texts.HintText, Colors.TextMuted);
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
        Color selectColor)
    {
        var borderColor = isSelected ? selectColor : Colors.BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, Colors.CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + Layout.CardTitleOffsetX, bounds.Top + Layout.CardTitleOffsetY, cardText.Title, isSelected ? selectColor : Colors.TextWhite);
        GameEngine.DrawString(bounds.Left + Layout.CardTagOffsetX, bounds.Top + Layout.CardTagOffsetY, cardText.Tag, Colors.TextGold);

        // プレビュー自機スプライト描画
        if (previewEntity.IsAlive())
        {
            ref readonly var sprite = ref previewEntity.Get<Sprite>();
            ref readonly var transform = ref previewEntity.Get<Transform>();

            // ホバー時の小幅左右ステップ振動
            var stepOffset = stepTimer > 0f ? MathF.Sin(stepTimer * Motion.StepFrequency) * Motion.StepAmplitude : 0f;

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

        var badgeText = isSelected ? Texts.SelectedBadge : Texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : Colors.TextMuted;
        GameEngine.DrawString(bounds.Left + Layout.CardBadgeLeftOffset, bounds.Bottom - Layout.CardBadgeBottomOffset, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + Layout.CardDescLeftOffset, bounds.Bottom - Layout.CardDescBottomOffset, cardText.Description, Colors.TextMuted);
    }

    private static void DrawCardBox(
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        Color selectColor)
    {
        var borderColor = isSelected ? selectColor : Colors.BorderDefaultColor;
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, Colors.CardBgColor, true);
        GameEngine.DrawBox(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, borderColor, false);
        if (isSelected)
        {
            GameEngine.DrawBox(bounds.Left - 1, bounds.Top - 1, bounds.Right + 1, bounds.Bottom + 1, borderColor, false);
        }

        GameEngine.DrawString(bounds.Left + Layout.CardTitleOffsetX, bounds.Top + Layout.CardTitleOffsetY, cardText.Title, isSelected ? selectColor : Colors.TextWhite);
        GameEngine.DrawString(bounds.Left + Layout.CardTagOffsetX, bounds.Top + Layout.CardTagOffsetY, cardText.Tag, Colors.TextGold);

        var badgeText = isSelected ? Texts.SelectedBadge : Texts.UnselectedBadge;
        var badgeColor = isSelected ? selectColor : Colors.TextMuted;
        GameEngine.DrawString(bounds.Left + Layout.CardBadgeLeftOffset, bounds.Bottom - Layout.CardBadgeBottomOffset, badgeText, badgeColor);
        GameEngine.DrawString(bounds.Left + Layout.CardDescLeftOffset, bounds.Bottom - Layout.CardDescBottomOffset, cardText.Description, Colors.TextMuted);
    }

    private static void DrawBoss1Card(
        Rectangle bounds,
        StartSceneCardTextConfig cardText,
        bool isSelected,
        float flyTimer,
        int textureHandle)
    {
        DrawCardBox(bounds, cardText, isSelected, Colors.BossSelectedColor);

        if (textureHandle >= 0)
        {
            var flyOffsetY = flyTimer > 0f
                ? MathF.Sin(flyTimer * Motion.Boss1FlyFrequency) * Motion.Boss1FlyAmplitude + Motion.Boss1FlyOffsetY
                : 0f;
            var tiltAngle = flyTimer > 0f
                ? MathF.Sin(flyTimer * Motion.Boss1TiltFrequency) * Motion.Boss1TiltAmplitude
                : 0f;

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + Layout.PreviewCenterOffsetY + flyOffsetY);

            // 192x192 のボス画像をカード枠に適合するスケールで独立描画
            GameEngine.DrawRotaGraph3(
                drawX,
                drawY,
                Motion.Boss1Scale,
                Motion.Boss1Scale,
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
        int textureHandle)
    {
        DrawCardBox(bounds, cardText, isSelected, Colors.BossSelectedColor);

        if (textureHandle >= 0)
        {
            var offsetY = 0f;
            var baseScale = Motion.Boss2BaseScale;
            var scaleX = baseScale;
            var scaleY = baseScale;

            if (slamTimer > 0f)
            {
                var riseEnd = Motion.Boss2RiseDuration;
                var slamEnd = riseEnd + Motion.Boss2SlamDuration;
                var impactEnd = slamEnd + Motion.Boss2ImpactDuration;

                if (slamTimer <= riseEnd)
                {
                    var t = slamTimer / riseEnd;
                    offsetY = Motion.Boss2RiseHeight * t;
                    scaleX = baseScale * Motion.Boss2RiseScale;
                    scaleY = baseScale * (2.0 - Motion.Boss2RiseScale);
                }
                else if (slamTimer <= slamEnd)
                {
                    var t = (slamTimer - riseEnd) / Motion.Boss2SlamDuration;
                    offsetY = Motion.Boss2RiseHeight * (1f - t);
                    scaleX = baseScale * Motion.Boss2SlamScale;
                    scaleY = baseScale * (2.0 - Motion.Boss2SlamScale);
                }
                else if (slamTimer <= impactEnd)
                {
                    var t = (slamTimer - slamEnd) / Motion.Boss2ImpactDuration;
                    offsetY = MathF.Sin(t * MathF.PI * Motion.Boss2ImpactShakeFreq) * Motion.Boss2ImpactShakeAmplitude;
                    scaleX = baseScale * (1.15 - 0.15 * t);
                    scaleY = baseScale * (0.85 + 0.15 * t);
                }
            }

            var drawX = bounds.Left + bounds.Width / 2;
            var drawY = (int)(bounds.Top + Layout.PreviewCenterOffsetY + offsetY);

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

    private static void DrawConfirmButton(bool isHovered)
    {
        var btnColor = isHovered ? Colors.ConfirmBtnHoverColor : Colors.ConfirmBtnDefaultColor;
        var borderColor = isHovered ? Colors.ConfirmBtnBorderHoverColor : Colors.ConfirmBtnBorderDefaultColor;

        GameEngine.DrawBox(
            ConfirmButtonBounds.Left, ConfirmButtonBounds.Top,
            ConfirmButtonBounds.Right, ConfirmButtonBounds.Bottom,
            btnColor, true);

        GameEngine.DrawBox(
            ConfirmButtonBounds.Left, ConfirmButtonBounds.Top,
            ConfirmButtonBounds.Right, ConfirmButtonBounds.Bottom,
            borderColor, false);

        if (isHovered)
        {
            GameEngine.DrawBox(
                ConfirmButtonBounds.Left - 1, ConfirmButtonBounds.Top - 1,
                ConfirmButtonBounds.Right + 1, ConfirmButtonBounds.Bottom + 1,
                borderColor, false);
        }

        GameEngine.DrawString(
            ConfirmButtonBounds.Left + Layout.ConfirmBtnTextOffsetX,
            ConfirmButtonBounds.Top + Layout.ConfirmBtnTextOffsetY,
            Texts.ConfirmButton,
            Colors.TextWhite);
    }
}
