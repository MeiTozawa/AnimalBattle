using AnimalBattle.Ai;

namespace AnimalBattle.Scenes.Start;

/// <summary>
///     ゲーム開始・キャラクター選択画面の入力判定およびホバー・選択状態更新を司る ECS システム。
///     GameState.Start 状態時にのみアクティブとなり、マウスホバー演出・キャラクター選択切り替え・戦闘開始を処理します。
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
        var motion = cfg.Motion;

        var catCardBounds = layout.CatCard.ToRectangle();
        var bunnyCardBounds = layout.BunnyCard.ToRectangle();
        var bossSnakeCardBounds = layout.BossSnakeCard.ToRectangle();
        var bossLionCardBounds = layout.BossLionCard.ToRectangle();
        var confirmButtonBounds = layout.ConfirmButton.ToRectangle();

        ref var flow = ref w.GetMut<GameFlow>();
        var dt = it.DeltaTime();
        var mousePos = InputService.GetMousePosition();
        var isClick = InputService.IsPressed(InputAction.MouseClick);
        var isConfirmKey = InputService.IsPressed(InputAction.Confirm);

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

            // --- 3. BossSnake カード判定 飛行浮遊アニメーション ---
            uiState.IsBossSnakeHovered = bossSnakeCardBounds.Contains(mousePos.X, mousePos.Y);
            if (uiState.IsBossSnakeHovered)
            {
                uiState.BossSnakeFlyTimer += dt;
                if (isClick) flow.SelectedBoss = BossType.BossSnake;
            }
            else
            {
                uiState.BossSnakeFlyTimer = Math.Max(0f, uiState.BossSnakeFlyTimer - dt * motion.BossSnakeFlyDecaySpeed);
            }

            // --- 4. BossLion カード判定 跳躍重撃アニメーション ---
            uiState.IsBossLionHovered = bossLionCardBounds.Contains(mousePos.X, mousePos.Y);
            if (uiState.IsBossLionHovered)
            {
                uiState.BossLionSlamTimer += dt;
                if (uiState.BossLionSlamTimer > motion.BossLionSlamCycleDuration)
                {
                    uiState.BossLionSlamTimer -= motion.BossLionSlamCycleDuration;
                }
                if (isClick) flow.SelectedBoss = BossType.BossLion;
            }
            else
            {
                uiState.BossLionSlamTimer = 0f;
            }

            // --- 5. 確定ボタン判定 ---
            uiState.IsConfirmHovered = confirmButtonBounds.Contains(mousePos.X, mousePos.Y);
            if ((uiState.IsConfirmHovered && isClick) || isConfirmKey)
            {
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, flow.SelectedBoss);
            }
        }
    }
}
