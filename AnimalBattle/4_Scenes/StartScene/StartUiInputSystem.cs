using System;
using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ゲーム開始・選人画面の入力判定およびホバー・選択状態更新を司る ECS システム。
///     GameState.Start 状態時にのみアクティブとなり、マウスホバー動效・選人切り替え・戦闘開始を処理します。
/// </summary>
public static class StartUiInputSystem
{
    /// <summary>
    ///     開始選人画面の入力判定システムを Flecs.NET ワールドへ登録します。
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
    ///     開始選人画面の入力判定パイプライン（名前付き静的メソッド）。
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
        var boss1CardBounds = layout.Boss1Card.ToRectangle();
        var boss2CardBounds = layout.Boss2Card.ToRectangle();
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

            // --- 3. Boss 1 カード判定 (飛行浮遊動效) ---
            uiState.IsBoss1Hovered = boss1CardBounds.Contains(mousePos.X, mousePos.Y);
            if (uiState.IsBoss1Hovered)
            {
                uiState.Boss1FlyTimer += dt;
                if (isClick) flow.SelectedBoss = BossType.Boss1;
            }
            else
            {
                uiState.Boss1FlyTimer = Math.Max(0f, uiState.Boss1FlyTimer - dt * motion.Boss1FlyDecaySpeed);
            }

            // --- 4. Boss 2 カード判定 (騰空・重撃下砸動效) ---
            uiState.IsBoss2Hovered = boss2CardBounds.Contains(mousePos.X, mousePos.Y);
            if (uiState.IsBoss2Hovered)
            {
                uiState.Boss2SlamTimer += dt;
                if (uiState.Boss2SlamTimer > motion.Boss2SlamCycleDuration)
                {
                    uiState.Boss2SlamTimer -= motion.Boss2SlamCycleDuration;
                }
                if (isClick) flow.SelectedBoss = BossType.Boss2;
            }
            else
            {
                uiState.Boss2SlamTimer = 0f;
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
