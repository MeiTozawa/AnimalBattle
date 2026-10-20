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
public static class BattleInputSystem
{
    private static bool _prevF1;
    private static bool _prevF2;
    private static bool _prevF3;
    private static bool _prevF4;
    private static bool _prevKey1;
    private static bool _prevKey2;
    private static bool _prevKey3;
    private static bool _prevKey4;

    private static Query _playerBulletsQuery;
    private static Query _vfxQuery;
    private static Query<PlayerController> _controllerQuery;
    private static Query<PlayerPawn> _playerQuery;
    private static Query<BaseBossAgent> _bossQuery;
    private static Phase _targetDebugPhase;

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

        // デバッグ入力（F1 トグル、F2～F4 フェーズ切替）の処理
        var renderCfg = w.Get<RenderConfig>();
        var dbgCfg = renderCfg.Debug;
        if (dbgCfg.DebugMode)
        {
            ProcessDebugInput(w, dbgCfg, _bossQuery);
        }

        // ゲームオーバー時は自機の操作入力を停止
        if (w.Has<GameOverTag>()) return;

        _controllerQuery.Each(PollController);
    }

    private static void PollController(ref PlayerController controller)
    {
        controller.PollAndProcessInput();
    }

    private static void ResetCombat(World world)
    {
        if (world.Has<GameOverTag>())
        {
            world.Remove<GameOverTag>();
        }

        BulletSpawner.ClearAllBossBullets(world);
        _playerBulletsQuery.Each(RecycleEntity);
        _vfxQuery.Each(RecycleEntity);
        CameraService.Reset();
        world.Set(new AimReticleState());

        _bossQuery.Each(ResetBoss);
        _playerQuery.Iter(ResetPlayer);

        EventBus.Publish(new CombatResetEvent());
    }

    private static void RecycleEntity(Entity e) => e.Add<RecycleTag>();
    private static void ResetBoss(ref BaseBossAgent boss) => boss.Reset();

    private static void ResetPlayer(Iter it, Field<PlayerPawn> pawns)
    {
        var count = it.Count();
        var w = it.World();
        for (var i = 0; i < count; i++)
        {
            ref var player = ref pawns[i];
            var cfg = w.GetPlayerConfig(player.CharacterType);
            player.Reset(cfg);
        }
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

        // 1/2/3/4 キーによる Boss およびキャラクターの迅速切替
        var isKey1 = GameEngine.CheckHitKey((int)InputButton.Key1);
        var isKey2 = GameEngine.CheckHitKey((int)InputButton.Key2);
        var isKey3 = GameEngine.CheckHitKey((int)InputButton.Key3);
        var isKey4 = GameEngine.CheckHitKey((int)InputButton.Key4);

        var justKey1 = isKey1 && !_prevKey1;
        var justKey2 = isKey2 && !_prevKey2;
        var justKey3 = isKey3 && !_prevKey3;
        var justKey4 = isKey4 && !_prevKey4;

        _prevKey1 = isKey1;
        _prevKey2 = isKey2;
        _prevKey3 = isKey3;
        _prevKey4 = isKey4;

        if (justKey1 || justKey2 || justKey3 || justKey4)
        {
            ref readonly var flow = ref world.Get<GameFlow>();
            if (justKey1)
            {
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss1);
                return;
            }
            if (justKey2)
            {
                GameManager.Current.SwitchToBattle(flow.SelectedPlayer, BossType.Boss2);
                return;
            }
            if (justKey3)
            {
                GameManager.Current.SwitchToBattle(CharacterType.Cat, flow.SelectedBoss);
                return;
            }
            if (justKey4)
            {
                GameManager.Current.SwitchToBattle(CharacterType.Bunny, flow.SelectedBoss);
                return;
            }
        }

        // ゲームオーバー（Boss 撃破または自機死亡）時はフェーズ切替を無効化
        if (world.Has<GameOverTag>()) return;

        var isF2 = GameEngine.CheckHitKey((int)InputButton.KeyF2);
        var isF3 = GameEngine.CheckHitKey((int)InputButton.KeyF3);
        var isF4 = GameEngine.CheckHitKey((int)InputButton.KeyF4);

        if ((isF2 && !_prevF2) || (isF3 && !_prevF3) || (isF4 && !_prevF4))
        {
            _targetDebugPhase = (isF2 && !_prevF2) ? Phase.Phase1 : ((isF3 && !_prevF3) ? Phase.Phase2 : Phase.Phase3);
            bossQuery.Each(ApplyDebugPhase);
        }

        _prevF2 = isF2;
        _prevF3 = isF3;
        _prevF4 = isF4;
    }

    private static void ApplyDebugPhase(ref BaseBossAgent boss)
    {
        if (!boss.IsAlive || boss.IsDead || boss.Health.Current <= 0.0f || (boss.Entity.IsAlive() && boss.Entity.Has<DeadTag>())) return;
        boss.DebugSetPhase(_targetDebugPhase);
    }
}
