using AnimalBattle.Ai;
using AnimalBattle.Asset;
using AnimalBattle.Common;
using AnimalBattle.Player;
using AnimalBattle.Bosses;
using AnimalBattle.Combat;
using AnimalBattle.Presentation;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Scenes;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle;

/// <summary>
///     ゲームの全体ライフサイクル、共有 ECS ワールド、および GameFlow 状態遷移を統括するマネージャークラス。
///     純粋な ECS アーキテクチャに従い、フレーム更新（Progress）内で全シミュレーションおよび描画パイプラインを駆動します。
/// </summary>
public class GameManager : IDisposable {
    private World _ecsWorld;
    private bool _isDisposed;

    /// <summary>
    ///     現在アクティブな GameManager インスタンスを取得します。
    /// </summary>
    public static GameManager Current { get; private set; } = null!;

    /// <summary>
    ///     共有 Flecs.NET ECS ワールドを取得します。
    /// </summary>
    public World EcsWorld => _ecsWorld;

    /// <summary>
    ///     <see cref="GameManager" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public GameManager()
    {
        _ecsWorld = World.Create();
        Current = this;

        ConfigManager.Initialize();
        AssetManager.Initialize();

        // 共有 ECS パイプラインモジュールおよび Prefab 原型の登録（決定論的トポロジカル順序）
        _ecsWorld.Import<CommonModule>();
        _ecsWorld.Import<EffectModule>();
        _ecsWorld.Import<CombatFeedbackModule>();
        _ecsWorld.Import<AnimationModule>();
        _ecsWorld.Import<ArenaModule>();
        _ecsWorld.Import<CombatModule>();
        _ecsWorld.Import<PlayerModule>();
        _ecsWorld.Import<BossModule>();
        _ecsWorld.Import<RenderModule>(); // 原生描画モジュール (Ecs.OnStore)

        // 状態連動システム群の登録
        _ecsWorld.AddStartSceneSystems()
                 .AddBattleSceneSystems();

        CombatPoolService.Initialize(_ecsWorld);

        // 初期 GameFlow 状態の設定
        _ecsWorld.Set(new GameFlow {
            State = GameState.Start,
            SelectedPlayer = CharacterType.Cat,
            SelectedBoss = BossType.Boss1
        });
    }

    /// <summary>
    ///     ゲームを初期化し、開始選人シーンに入場します。
    /// </summary>
    public void Initialize()
    {
        StartSceneSpawner.Spawn(_ecsWorld);
    }

    /// <summary>
    ///     戦闘シーンへ遷移します。
    /// </summary>
    /// <param name="playerType">選択されたプレイヤーキャラクター</param>
    /// <param name="bossType">選択された首領</param>
    public void SwitchToBattle(CharacterType playerType, BossType bossType)
    {
        StartSceneSpawner.Teardown(_ecsWorld);

        ref var flow = ref _ecsWorld.GetMut<GameFlow>();
        flow.State = GameState.Battle;
        flow.SelectedPlayer = playerType;
        flow.SelectedBoss = bossType;

        BattleSceneSpawner.Spawn(_ecsWorld, playerType, bossType);
    }

    /// <summary>
    ///     開始選人シーンへ復帰します。
    /// </summary>
    public void SwitchToStart()
    {
        ref var flow = ref _ecsWorld.GetMut<GameFlow>();
        flow.State = GameState.Start;

        BattleSceneSpawner.Teardown(_ecsWorld);
        StartSceneSpawner.Spawn(_ecsWorld);
    }

    /// <summary>
    ///     フレームごとの論理更新および描画コマンド記録を ECS パイプライン内で一括実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間秒数</param>
    public void Update(float deltaTime)
    {
        // PreUpdate -> OnUpdate -> OnValidate -> PostUpdate -> OnStore (RenderModule)
        _ecsWorld.Progress(deltaTime);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        StartSceneSpawner.Teardown(_ecsWorld);
        BattleSceneSpawner.Teardown(_ecsWorld);

        CombatPoolService.Current.Dispose();
        _ecsWorld.Dispose();

        AssetManager.Shutdown();

        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}