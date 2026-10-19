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
public static class BattleInputSystem
{
    private static Query _playerBulletsQuery;
    private static Query _vfxQuery;
    private static Query<PlayerController> _controllerQuery;
    private static Query<PlayerPawn> _playerQuery;
    private static Query<BaseBossAgent> _bossQuery;

    /// <summary>
    ///     戦闘入力処理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleInputSystem(this World world)
    {
        _playerBulletsQuery = world.QueryBuilder()
            .With<PlayerBulletTag>()
            .Build();

        _vfxQuery = world.QueryBuilder()
            .With<VfxTag>()
            .Build();

        _controllerQuery = world.QueryBuilder<PlayerController>().Build();
        _playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        _bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("BattleInputSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteBattleInput);

        return world;
    }

    /// <summary>
    ///     戦闘シーンの入力受付パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void ExecuteBattleInput(Iter it)
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
            ResetCombat(w);
        }

        _controllerQuery.Each(PollController);
    }

    private static void PollController(ref PlayerController controller)
    {
        controller.PollAndProcessInput();
    }

    private static void ResetCombat(World world)
    {
        BulletSpawner.ClearAllBossBullets(world);
        _playerBulletsQuery.Each(RecycleEntity);
        _vfxQuery.Each(RecycleEntity);
        CameraService.Reset();
        world.Set(new AimReticleState());

        _bossQuery.Each(ResetBoss);
        _playerQuery.Each(ResetPlayer);

        EventBus.Publish(new CombatResetEvent());
    }

    private static void RecycleEntity(Entity e) => e.Add<RecycleTag>();
    private static void ResetBoss(ref BaseBossAgent boss) => boss.Reset();
    private static void ResetPlayer(ref PlayerPawn player) => player.Reset(ConfigManager.GetPlayerConfig(player.CurrentCharacter));
}
