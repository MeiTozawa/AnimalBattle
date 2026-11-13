using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;
using AnimalBattle.Features.Tutorial.Spawners;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアル各ステップの目標達成判定、段階移行、およびプレイヤーの満血リトライ保護を司る ECS システム。
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

        var ctx = world.Get<TutorialSystemContext>();

        // 2. プレイヤーのHP監視および挫折防止リトライ処理
        ctx.HasPlayer = false;
        ctx.PlayerDied = false;
        ctx.DeadPlayerEntity = default;

        ctx.PlayerHealthQuery.Iter(static (Iter pIt, Field<Health> hps, Field<Transform> trans, Field<Velocity> vels) =>
        {
            var pCount = pIt.Count();
            if (pCount == 0) return;

            var pCtx = pIt.World().Get<TutorialSystemContext>();
            pCtx.HasPlayer = true;
            pCtx.CachedPlayerPos = trans[0].Position;
            pCtx.CachedPlayerVel = vels[0].Value;

            for (var i = 0; i < pCount; i++)
            {
                var pEntity = pIt.Entity(i);
                if (hps[i].Current <= 0 || pEntity.Has<DeadTag>())
                {
                    pCtx.DeadPlayerEntity = pEntity;
                    pCtx.PlayerDied = true;
                }
            }
        });

        if (!ctx.PlayerDied && ctx.DeadPlayerQuery.Count() > 0)
        {
            ctx.DeadPlayerQuery.Iter(static (Iter dIt) =>
            {
                if (!dIt.Any()) return;
                dIt.World().Get<TutorialSystemContext>().DeadPlayerEntity = dIt.Entity(0);
            });
            ctx.PlayerDied = true;
        }

        var playerDied = ctx.PlayerDied;

        if (playerDied)
        {
            // クエリ反復外かつ DeferSuspend 中にプレイヤーを即時リセットし、
            // 後続の ResetCurrentPhase（ChargeEnergyAction 等）が DeadTag 残存や遅延 Set に上書きされないようにする
            var isDeferred = world.IsDeferred();
            if (isDeferred) world.DeferSuspend();
            try
            {
                if (ctx.DeadPlayerEntity.IsAlive())
                {
                    PlayerPrefabs.ResetPlayerEntity(ctx.DeadPlayerEntity);
                }

                world.Remove<GameOverTag>();
                CameraService.Reset();

                CombatCleanupService.RecyclePlayerProjectilesAndVfx(world);

                EventBus.Publish(new CombatResetEvent());

                state.IsRetrying = true;
                state.RetryTimer = cfg.Timing.RetryDuration;
                TutorialSceneSpawner.ResetCurrentPhase(world);
            }
            finally
            {
                if (isDeferred) world.DeferResume();
            }

            return;
        }

        // 3. 現在のフェーズおよびステップの取得と境界チェック
        if ((uint)state.CurrentPhaseIndex >= (uint)cfg.Phases.Length) return;
        var currentPhaseCfg = cfg.Phases[state.CurrentPhaseIndex];

        if ((uint)state.CurrentStepIndex >= (uint)currentPhaseCfg.Steps.Length) return;
        var currentStepCfg = currentPhaseCfg.Steps[state.CurrentStepIndex];

        state.StepTimer += dt;

        // 4. ステップ達成条件のゼロアロケーション評価
        var condition = currentStepCfg.GetRuntimeCondition();
        Debug.Assert(condition != null, $"ステップ条件が初期化されていません: Phase={state.CurrentPhaseIndex}, Step={state.CurrentStepIndex}");
        if (condition == null) return;

        var isStepFinished = condition.IsSatisfied(world, ref state, dt);

        // 5. 条件達成時のステップ遷移またはフェーズ完了移行
        if (isStepFinished)
        {
            var nextStepIndex = state.CurrentStepIndex + 1;
            if (nextStepIndex < currentPhaseCfg.Steps.Length)
            {
                var root = world.Lookup(TutorialSceneSpawner.RootName);
                if (root.IsAlive())
                {
                    TutorialSceneSpawner.EnterStep(world, root, state.CurrentPhaseIndex, nextStepIndex);
                }
            }
            else
            {
                var nextPhaseIndex = state.CurrentPhaseIndex + 1;
                TutorialSceneSpawner.SetupPhase(world, nextPhaseIndex);
            }
        }
    }
}
