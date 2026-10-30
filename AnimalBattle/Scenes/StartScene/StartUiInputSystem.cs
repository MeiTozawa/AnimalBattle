using AnimalBattle.Ai;
using AnimalBattle.Features.Tutorial;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の入力判定およびホバー・選択状態更新を司る ECS システム。
///     GameState.Start 状態時にのみアクティブとなり、プレイヤー選択・チュートリアル開始・スキップを処理します。
/// </summary>
public static class StartUiInputSystem
{
    /// <summary>
    ///     開始キャラクター選択画面の入力判定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddStartUiInputSystem(this World world)
    {
        world.System<StartUiState>("StartUiInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteStartUiInput);

        return world;
    }

    /// <summary>
    ///     開始キャラクター選択画面の入力判定パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteStartUiInput(Iter it, Field<StartUiState> uiStates)
    {
        var w = it.World();
        if (w.Get<GameFlow>().State != GameState.Start) return;

        var count = it.Count();
        if (count == 0) return;

        ref readonly var cfg = ref w.Get<StartSceneConfig>();
        var layout = cfg.Layout;

        var catCardBounds = layout.CatCard.ToRectangle();
        var bunnyCardBounds = layout.BunnyCard.ToRectangle();
        var startTutorialBounds = layout.StartTutorialButton.Size.X > 0 
            ? layout.StartTutorialButton.ToRectangle() 
            : layout.ConfirmButton.ToRectangle();
        var skipTutorialBounds = layout.SkipTutorialButton.ToRectangle();

        ref var flow = ref w.GetMut<GameFlow>();
        var dt = it.DeltaTime();
        var mousePos = InputService.GetMousePosition();
        var isClick = InputService.IsPressed(InputAction.MouseClick);
        var isConfirmKey = InputService.IsPressed(InputAction.Confirm);
        var isCancelKey = InputService.IsPressed(InputAction.Cancel);

        for (var i = 0; i < count; i++)
        {
            ref var uiState = ref uiStates[i];

            // --- 1. Cat カード判定 ---
            var isCatHovered = catCardBounds.Contains(mousePos.X, mousePos.Y);
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
            var isBunnyHovered = bunnyCardBounds.Contains(mousePos.X, mousePos.Y);
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

            // --- 3. チュートリアル開始ボタン判定 ---
            uiState.IsStartTutorialHovered = startTutorialBounds.Contains(mousePos.X, mousePos.Y);
            uiState.IsConfirmHovered = uiState.IsStartTutorialHovered;

            // --- 4. チュートリアルスキップボタン判定 ---
            uiState.IsSkipTutorialHovered = skipTutorialBounds.Contains(mousePos.X, mousePos.Y);

            // --- 5. 画面遷移判定 ---
            if ((uiState.IsStartTutorialHovered && isClick) || isConfirmKey)
            {
                GameManager.Current.SwitchToTutorial(flow.SelectedPlayer, TutorialPhase.Movement);
                return;
            }

            if ((uiState.IsSkipTutorialHovered && isClick) || isCancelKey)
            {
                GameManager.Current.SwitchToTutorial(flow.SelectedPlayer, TutorialPhase.BossSelect);
                return;
            }
        }
    }
}
