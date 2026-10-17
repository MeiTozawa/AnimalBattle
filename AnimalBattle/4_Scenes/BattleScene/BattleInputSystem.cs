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
        BulletSpawner.ClearAllBossBullets(world);
        playerBulletsQuery.Each(e => e.Add<RecycleTag>());
        vfxQuery.Each(e => e.Add<RecycleTag>());
        CameraService.Reset();
        world.Set(new AimReticleState());

        bossQuery.Each((ref BaseBossAgent boss) => boss.Reset());
        playerQuery.Each((ref PlayerPawn player) => player.Reset(ConfigManager.GetPlayerConfig(player.CurrentCharacter)));

        EventBus.Publish(new CombatResetEvent());
    }
}
