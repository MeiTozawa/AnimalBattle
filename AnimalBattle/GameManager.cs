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
    private readonly IConfigRegistry _configRegistry;
    private bool _isDisposed;
    private bool _isUpdating;
    private Action? _pendingSceneTransition;

    /// <summary>
    ///     現在アクティブな GameManager インスタンスを取得します。
    /// </summary>
    public static GameManager Current { get; private set; } = null!;

    /// <summary>
    ///     共有 Flecs.NET ECS ワールドを取得します。
    /// </summary>
    public World EcsWorld => _ecsWorld;

    /// <summary>
    ///     設定レジストリインスタンスを取得します。
    /// </summary>
    public IConfigRegistry Configs => _configRegistry;

    /// <summary>
    ///     <see cref="GameManager" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public GameManager()
    {
        _ecsWorld = World.Create();
        Current = this;

        _configRegistry = new ConfigRegistry();
        GameConfigRegistrar.RegisterAll(_configRegistry, _ecsWorld);
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
        _ecsWorld.Import<StartSceneModule>();
        _ecsWorld.Import<BattleSceneModule>();

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
    ///     システム実行中（ECS ステージ中）の場合は、Progress 終了後の安全な同期ポイントへ遅延実行されます。
    /// </summary>
    /// <param name="playerType">選択されたプレイヤーキャラクター</param>
    /// <param name="bossType">選択された首領</param>
    public void SwitchToBattle(CharacterType playerType, BossType bossType)
    {
        if (_isUpdating)
        {
            _pendingSceneTransition = () => ExecuteSwitchToBattle(playerType, bossType);
            return;
        }

        ExecuteSwitchToBattle(playerType, bossType);
    }

    private void ExecuteSwitchToBattle(CharacterType playerType, BossType bossType)
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
    ///     システム実行中（ECS ステージ中）の場合は、Progress 終了後の安全な同期ポイントへ遅延実行されます。
    /// </summary>
    public void SwitchToStart()
    {
        if (_isUpdating)
        {
            _pendingSceneTransition = ExecuteSwitchToStart;
            return;
        }

        ExecuteSwitchToStart();
    }

    private void ExecuteSwitchToStart()
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
        // フレーム開始時：保留中の設定ホットリロードをワールドへ安全に適用
        ConfigHotReloadDispatcher.Dispatch(_ecsWorld);

        _isUpdating = true;
        try
        {
            // PreUpdate -> OnUpdate -> OnValidate -> PostUpdate -> OnStore (RenderModule)
            _ecsWorld.Progress(deltaTime);
        }
        finally
        {
            _isUpdating = false;
        }

        // システム更新完了後：ステージ外（非同期安全）で保留中のシーン遷移を実行
        if (_pendingSceneTransition != null)
        {
            var transition = _pendingSceneTransition;
            _pendingSceneTransition = null;
            transition();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        StartSceneSpawner.Teardown(_ecsWorld);
        BattleSceneSpawner.Teardown(_ecsWorld);

        CombatPoolService.Current.Dispose();
        _configRegistry.Dispose();
        _ecsWorld.Dispose();

        AssetManager.Shutdown();

        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}