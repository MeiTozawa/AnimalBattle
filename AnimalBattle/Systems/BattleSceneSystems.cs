using AnimalBattle.Ai;
using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Combat;
using AnimalBattle.CombatPool;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Scenes;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     戦闘シーンにおける入力受付、自機操作、Boss 思考およびカメラ追従を司る ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、各フレームの戦闘ロジックを推進します。
/// </summary>
public static class BattleSceneSystems {
    /// <summary>
    ///     戦闘シーンシステム群を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleSceneSystems(this World world)
    {
        var platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        var playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        var vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        var controllerQuery = world.QueryBuilder<PlayerController>().Build();
        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        // 1. 戦闘入力処理システム (PreUpdate)
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

        // 2. Boss AI 思考および動的カメラ追従システム (OnUpdate)
        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                PlayerPawn? player = null;
                playerQuery.Each((ref PlayerPawn p) => player = p);

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);

                if (player == null || boss == null) return;

                var dt = it.DeltaTime();

                boss.Update(
                    dt,
                    new Vec2f(player.X, player.Y),
                    new Vec2f(player.Vx, player.Vy),
                    w,
                    platformQuery);

                CameraService.Update(
                    dt,
                    player.X, player.Y,
                    player.Vx, player.Vy,
                    boss.X, boss.Y);
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
