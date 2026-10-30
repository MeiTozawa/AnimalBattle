using AnimalBattle.Combat.Components;
using AnimalBattle.Common.Components;
using AnimalBattle.Enemies;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
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
        state.PhaseTimer += dt;

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
            // 当該フェーズの目標・小怪をクリーンアップして再生成
            TutorialSceneSpawner.SetupPhase(world, state.CurrentPhase);
            return;
        }

        // 3. ボス選択フェーズ（Phase 7）は扉のインタラクション待ちのためフロー側では自動移行しない
        if (state.CurrentPhase == TutorialPhase.BossSelect) return;

        // 4. 小怪フェーズ（Phase 5 飛行小怪、Phase 6 地上小怪）の撃破判定
        if (state.CurrentPhase is TutorialPhase.FlyingMinion or TutorialPhase.GroundMinion)
        {
            var aliveMinions = 0;
            using var minionQuery = world.QueryBuilder().With<MinionTag>().Build();
            minionQuery.Each((Entity _) => aliveMinions++);

            if (aliveMinions == 0 && !state.IsPhaseCompleted)
            {
                state.IsPhaseCompleted = true;
                state.CompletionTimer = cfg.Timing.MinionDefeatDelay;
            }
        }
        else
        {
            // 基礎動作フェーズ（Phase 1〜4）：全ターゲット回収判定
            if (state.TargetsRemaining <= 0 && !state.IsPhaseCompleted)
            {
                state.IsPhaseCompleted = true;
                state.CompletionTimer = cfg.Timing.TargetCollectDelay;
            }
        }

        // 5. 目標達成時の次フェーズ移行
        if (state.IsPhaseCompleted)
        {
            state.CompletionTimer -= dt;
            if (state.CompletionTimer <= 0f)
            {
                var nextPhase = (TutorialPhase)((int)state.CurrentPhase + 1);
                TutorialSceneSpawner.SetupPhase(world, nextPhase);
            }
        }
    }
}
