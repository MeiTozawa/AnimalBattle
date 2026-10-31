using System.Drawing;
using AnimalBattle.Ai;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の UI 描画：タイトルヘッダー、自機カード、およびチュートリアル分岐ボタンを統括する ECS 描画システム。
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
        var startGameBounds = layout.StartGameButton.Size.X > 0 
            ? layout.StartGameButton.ToRectangle() 
            : layout.ConfirmButton.ToRectangle();

        ref readonly var flow = ref w.Get<GameFlow>();

        for (var i = 0; i < count; i++)
        {
            ref var uiState = ref uiStates[i];

            // 1. タイトルヘッダー描画
            GameEngine.DrawLeftString(layout.HeaderTitlePos, texts.HeaderTitle, colors.TextGold);
            GameEngine.DrawLeftString(layout.PlayerHeaderPos, texts.PlayerHeader, colors.TextWhite);

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

            // 4. チュートリアル有効化チェックボックス描画
            DrawCheckbox(
                uiState.IsCheckboxHovered,
                uiState.IsTutorialEnabled,
                layout.TutorialCheckbox,
                texts.TutorialCheckbox,
                colors);

            // 5. 「ゲーム開始」ボタン描画
            var buttonText = !string.IsNullOrEmpty(texts.StartGameButton) ? texts.StartGameButton : texts.ConfirmButton;
            DrawActionButton(
                uiState.IsStartGameHovered,
                startGameBounds,
                buttonText,
                colors.ConfirmBtnDefaultColor,
                colors.ConfirmBtnHoverColor,
                colors.ConfirmBtnBorderDefaultColor,
                colors.ConfirmBtnBorderHoverColor,
                colors.TextWhite);

            // 6. 下部ヒントテキスト
            GameEngine.DrawLeftString(layout.HintTextPos, texts.HintText, colors.TextMuted);
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

        GameEngine.DrawLeftString(p1 + layout.CardTitleOffset, cardText.Title, isSelected ? selectColor : colors.TextWhite);
        GameEngine.DrawLeftString(p1 + layout.CardTagOffset, cardText.Tag, colors.TextGold);

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
        GameEngine.DrawLeftString(new Vec2i(bounds.Left + layout.CardBadgeLeftOffset, bounds.Bottom - layout.CardBadgeBottomOffset), badgeText, badgeColor);
        GameEngine.DrawLeftString(new Vec2i(bounds.Left + layout.CardDescLeftOffset, bounds.Bottom - layout.CardDescBottomOffset), cardText.Description, colors.TextMuted);
    }

    private static void DrawActionButton(
        bool isHovered,
        Rectangle bounds,
        string text,
        Color defaultBg,
        Color hoverBg,
        Color defaultBorder,
        Color hoverBorder,
        Color textColor)
    {
        var btnColor = isHovered ? hoverBg : defaultBg;
        var borderColor = isHovered ? hoverBorder : defaultBorder;

        var p1 = new Vec2i(bounds.Left, bounds.Top);
        var p2 = new Vec2i(bounds.Right, bounds.Bottom);

        GameEngine.DrawBox(p1, p2, btnColor, true);
        GameEngine.DrawBox(p1, p2, borderColor, false);

        if (isHovered)
        {
            GameEngine.DrawBox(p1 - Vec2i.One, p2 + Vec2i.One, borderColor, false);
        }

        GameEngine.DrawCenterString(
            new Vec2i(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2),
            text,
            textColor);
    }

    private static void DrawCheckbox(
        bool isHovered,
        bool isChecked,
        StartSceneCheckboxLayoutConfig config,
        string text,
        StartSceneColorsConfig colors)
    {
        var boxSize = config.BoxSize;
        var boxP1 = new Vec2i(config.Pos.X, config.Pos.Y + (config.Size.Y - boxSize) / 2);
        var boxP2 = new Vec2i(boxP1.X + boxSize, boxP1.Y + boxSize);

        var bg = colors.CardBgColor;
        var border = isHovered ? colors.ConfirmBtnBorderHoverColor : colors.BorderDefaultColor;
        var textColor = isHovered ? colors.TextWhite : colors.TextMuted;

        // 方形チェック枠の描画
        GameEngine.DrawBox(boxP1, boxP2, bg, true);
        GameEngine.DrawBox(boxP1, boxP2, border, false);

        if (isHovered)
        {
            GameEngine.DrawBox(boxP1 - Vec2i.One, boxP2 + Vec2i.One, border, false);
        }

        // チェック状態の描画（内部コア塗りつぶし）
        if (isChecked)
        {
            var innerMargin = 3;
            GameEngine.DrawBox(boxP1 + new Vec2i(innerMargin, innerMargin), boxP2 - new Vec2i(innerMargin, innerMargin), colors.PlayerSelectedColor, true);
        }

        // ラベルテキスト描画（ボックス右側）
        var textPos = new Vec2i(boxP2.X + 10, config.Pos.Y + 12);
        GameEngine.DrawLeftString(textPos, text, textColor);
    }
}
