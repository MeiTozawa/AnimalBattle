using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Handlers;
using AnimalBattle.Features.Tutorial.Spawners;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアル各フェーズの目標達成判定、段階移行、およびプレイヤーの満血リトライ保護を司る ECS システム。
/// </summary>
public static class TutorialFlowSystem
{
    /// <summary>
    ///     チュートリアルフローシステムをワールドへ登録します。
    /// </summary>
    public static World AddTutorialFlowSystem(this World world)
    {
        world.System("TutorialFlowSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteTutorialFlow);

        return world;
    }

    /// <summary>
    ///     チュートリアルフロー実行パイプライン。
    /// </summary>
    public static void ExecuteTutorialFlow(Iter it)
    {
        var world = it.World();
        if (world.Get<GameFlow>().State != GameState.Tutorial) return;

        ref var state = ref world.GetMut<TutorialState>();
        var cfg = world.Get<TutorialConfig>();
        var dt = it.DeltaTime();

        // 1. リトライ演出タイマーの更新
        if (state.IsRetrying)
        {
            state.RetryTimer -= dt;
            if (state.RetryTimer <= 0f)
            {
                state.IsRetrying = false;
            }
        }

        // 2. プレイヤーのHP監視および挫折防止リトライ処理
        using var playerQuery = world.QueryBuilder<Health>()
            .With<PlayerTag>()
            .Build();

        var playerDied = false;
        playerQuery.Each((player, ref hp) =>
        {
            if (hp.Current <= 0 || player.Has<DeadTag>())
            {
                PlayerPrefabs.ResetPlayerEntity(player);
                playerDied = true;
            }
        });

        if (playerDied)
        {
            world.Remove<GameOverTag>();
            CameraService.Reset();

            using var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build();
            playerBullets.Each(CombatPoolService.Recycle);

            using var vfxQuery = world.QueryBuilder().With<VfxTag>().Build();
            vfxQuery.Each(CombatPoolService.Recycle);

            EventBus.Publish(new CombatResetEvent());

            state.IsRetrying = true;
            state.RetryTimer = cfg.Timing.RetryDuration;
            TutorialSceneSpawner.ResetCurrentPhase(world);
            return;
        }

        // 3. 現在のフェーズの取得と安全性チェック
        if ((uint)state.CurrentPhaseIndex >= (uint)cfg.Phases.Length) return;
        var currentPhaseCfg = cfg.Phases[state.CurrentPhaseIndex];

        var handler = TutorialPhaseDispatcher.GetHandler(currentPhaseCfg.Type);
        Debug.Assert(handler != null, $"未対応のチュートリアルフェーズ種別です: {currentPhaseCfg.Type}");
        if (handler == null) return;

        // 4. ハンドラーによる目標判定の更新
        var isFinished = handler.UpdateAndCheckCompletion(world, ref state, currentPhaseCfg, cfg, dt);

        // 5. 目標達成時の次フェーズ移行
        if (isFinished)
        {
            var nextIndex = state.CurrentPhaseIndex + 1;
            TutorialSceneSpawner.SetupPhase(world, nextIndex);
        }
    }
}
