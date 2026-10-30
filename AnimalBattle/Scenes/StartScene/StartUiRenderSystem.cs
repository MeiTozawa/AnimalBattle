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
        var startTutorialBounds = layout.StartTutorialButton.Size.X > 0 
            ? layout.StartTutorialButton.ToRectangle() 
            : layout.ConfirmButton.ToRectangle();
        var skipTutorialBounds = layout.SkipTutorialButton.ToRectangle();

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

            // 4. 「チュートリアル開始」ボタン描画（推奨プライマリ色）
            DrawActionButton(
                uiState.IsStartTutorialHovered,
                startTutorialBounds,
                texts.StartTutorialButton,
                colors.ConfirmBtnDefaultColor,
                colors.ConfirmBtnHoverColor,
                colors.ConfirmBtnBorderDefaultColor,
                colors.ConfirmBtnBorderHoverColor,
                colors.TextWhite);

            // 5. 「チュートリアルをスキップ」ボタン描画（セカンダリ色）
            DrawActionButton(
                uiState.IsSkipTutorialHovered,
                skipTutorialBounds,
                texts.SkipTutorialButton,
                Color.FromArgb(50, 60, 80),
                Color.FromArgb(70, 85, 115),
                Color.FromArgb(90, 110, 140),
                Color.FromArgb(130, 160, 200),
                colors.TextMuted);

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
}
