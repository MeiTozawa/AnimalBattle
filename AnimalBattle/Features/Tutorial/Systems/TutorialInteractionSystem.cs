using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアル目標物（コイン／リング）の接触回収判定、およびボス挑戦扉の近接判定と F キー押下インタラクションを司る ECS システム。
/// </summary>
public static class TutorialInteractionSystem
{
    /// <summary>
    ///     チュートリアルインタラクションシステムをワールドへ登録します。
    /// </summary>
    public static World AddTutorialInteractionSystem(this World world)
    {
        world.System("TutorialInteractionSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteTutorialInteraction);

        return world;
    }

    /// <summary>
    ///     チュートリアルインタラクション実行パイプライン。
    /// </summary>
    public static void ExecuteTutorialInteraction(Iter it)
    {
        var world = it.World();
        if (world.Get<GameFlow>().State != GameState.Tutorial) return;

        // 自機の現在位置を取得
        var hasPlayer = false;
        var playerPos = Vec2f.Zero;
        using var playerQuery = world.QueryBuilder<Transform>()
            .With<PlayerTag>()
            .Build();
        playerQuery.Each((_, ref tr) =>
        {
            playerPos = tr.Position;
            hasPlayer = true;
        });

        if (!hasPlayer) return;

        var dt = it.DeltaTime();
        var cfg = world.Get<TutorialConfig>();
        var isFKey = InputService.IsPressed(InputAction.Interact) || InputService.IsButtonPressed(InputButton.KeyF);

        // 1. ターゲットコイン／リングの回収判定
        using var targetQuery = world.QueryBuilder<TutorialTarget, Transform>().Build();
        targetQuery.Each((e, ref target, ref tr) =>
        {
            if (target.IsCollected) return;

            target.FloatPhase += dt * cfg.Timing.TargetFloatSpeed;

            // プレイヤーとの距離判定（プレイヤー接触半径マージン）
            var diff = playerPos - tr.Position;
            var reach = target.Radius + cfg.Timing.ReachMargin;
            if (diff.LengthSquared() <= reach * reach)
            {
                target.IsCollected = true;
                ref var state = ref world.GetMut<TutorialState>();
                state.TargetsRemaining = Math.Max(0, state.TargetsRemaining - 1);
                e.Destruct();
            }
        });

        // 2. ボス挑戦扉の接近判定および F キー入場処理
        using var doorQuery = world.QueryBuilder<TutorialDoor, Transform>().Build();
        doorQuery.Each((_, ref door, ref tr) =>
        {
            door.AnimTimer += dt;
            var diff = playerPos - tr.Position;
            var dist = diff.Length();

            door.IsPlayerInRange = dist <= door.InteractionRadius;

            if (door.IsPlayerInRange && isFKey)
            {
                var flow = world.Get<GameFlow>();
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, door.TargetBoss);
            }
        });
    }
}
