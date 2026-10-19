using AnimalBattle.Ai;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     戦闘シーンにおける入力受付、自機操作、および戦闘リセット・シーン復帰を司る ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、PreUpdate フェーズでプレイヤーの入力を処理します。
/// </summary>
public static class BattleInputSystem {
    private static bool _prevF1;
    private static bool _prevF2;
    private static bool _prevF3;
    private static bool _prevF4;

    /// <summary>
    ///     戦闘入力処理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleInputSystem(this World world)
    {
        var playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        var vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        var controllerQuery = world.QueryBuilder<PlayerController>().Build();
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("BattleInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                // ESC キーでタイトル／選人画面へ復帰
                if (InputService.IsPressed(InputAction.Cancel))
                {
                    GameManager.Current.SwitchToStart();
                    return;
                }

                // R キーで戦闘リセット
                if (InputService.IsPressed(InputAction.Restart))
                {
                    ResetCombat(w, playerBulletsQuery, vfxQuery, playerQuery, bossQuery);
                }

                // デバッグ入力（F1 トグル、F2～F4 フェーズ切替）の処理
                var renderCfg = w.Get<RenderConfig>();
                var dbgCfg = renderCfg.Debug;
                if (dbgCfg.DebugMode)
                {
                    ProcessDebugInput(w, dbgCfg, bossQuery);
                }

                // ゲームオーバー時は自機の操作入力を停止
                if (w.Has<GameOverTag>()) return;

                controllerQuery.Each((ref PlayerController controller) =>
                {
                    controller.PollAndProcessInput();
                });
            });

        return world;
    }

    private static void ResetCombat(
        World world,
        Query playerBulletsQuery,
        Query vfxQuery,
        Query<PlayerPawn> playerQuery,
        Query<BaseBossAgent> bossQuery)
    {
        if (world.Has<GameOverTag>())
        {
            world.Remove<GameOverTag>();
        }

        BulletSpawner.ClearAllBossBullets(world);
        playerBulletsQuery.Each(e => e.Add<RecycleTag>());
        vfxQuery.Each(e => e.Add<RecycleTag>());
        CameraService.Reset();
        world.Set(new AimReticleState());

        bossQuery.Each((ref BaseBossAgent boss) => boss.Reset());
        playerQuery.Each((ref PlayerPawn player) =>
        {
            var cfg = world.GetPlayerConfig(player.CharacterType);
            player.Reset(cfg);
        });

        EventBus.Publish(new CombatResetEvent());
    }

    private static void ProcessDebugInput(World world, DebugRenderConfig dbgCfg, Query<BaseBossAgent> bossQuery)
    {
        var isF1 = GameEngine.CheckHitKey((int)InputButton.KeyF1) || GameEngine.CheckHitKey((int)InputButton.KeyH);
        if (isF1 && !_prevF1)
        {
            dbgCfg.IsActive = !dbgCfg.IsActive;
            dbgCfg.Enabled = dbgCfg.IsActive;
        }
        _prevF1 = isF1;

        if (!dbgCfg.IsActive) return;

        // ゲームオーバー（Boss 撃破または自機死亡）時はフェーズ切替を無効化
        if (world.Has<GameOverTag>()) return;

        var isF2 = GameEngine.CheckHitKey((int)InputButton.KeyF2);
        var isF3 = GameEngine.CheckHitKey((int)InputButton.KeyF3);
        var isF4 = GameEngine.CheckHitKey((int)InputButton.KeyF4);

        if ((isF2 && !_prevF2) || (isF3 && !_prevF3) || (isF4 && !_prevF4))
        {
            bossQuery.Each((ref BaseBossAgent boss) =>
            {
                if (!boss.IsAlive || boss.IsDead || boss.Health.Current <= 0.0f || (boss.Entity.IsAlive() && boss.Entity.Has<DeadTag>())) return;

                if (isF2 && !_prevF2) boss.DebugSetPhase(Phase.Phase1);
                else if (isF3 && !_prevF3) boss.DebugSetPhase(Phase.Phase2);
                else if (isF4 && !_prevF4) boss.DebugSetPhase(Phase.Phase3);
            });
        }

        _prevF2 = isF2;
        _prevF3 = isF3;
        _prevF4 = isF4;
    }
}
