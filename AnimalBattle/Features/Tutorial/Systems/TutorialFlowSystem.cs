using AnimalBattle.Combat.Components;
using AnimalBattle.Common.Components;
using AnimalBattle.Enemies;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Handlers;
using AnimalBattle.Features.Tutorial.Spawners;
using AnimalBattle.Player.Components;
using AnimalBattle.Scenes;

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
        var cfg = world.Get<TutorialConfig>() ?? new TutorialConfig();
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
        var flow = world.Get<GameFlow>();
        var playerCfg = world.GetPlayerConfig(flow.SelectedPlayer);
        using var playerQuery = world.QueryBuilder<Health, Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        var playerDied = false;
        playerQuery.Each((Entity _, ref Health hp, ref Transform tr, ref Velocity vel) =>
        {
            if (hp.Current <= 0)
            {
                // 即座に全回復し、初期リスポーン地点へ復帰
                hp.Current = playerCfg.MaxHp;
                tr.Position = playerCfg.Spawn;
                vel.Value = Vec2f.Zero;
                vel.Knockback = Vec2f.Zero;
                playerDied = true;
            }
        });

        if (playerDied)
        {
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
