using AnimalBattle.Ai;
using AnimalBattle.Asset;
using AnimalBattle.Common;
using AnimalBattle.Core;
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
///     ゲームの全体ライフサイクル、共有ECSワールド、および進行状態遷移を統括するマネージャークラスです。
///     フレーム更新Progress内で全シミュレーションおよび描画パイプラインを順序立てて駆動します。
/// </summary>
public class GameManager : IDisposable {
    private World _ecsWorld;
    private readonly IConfigRegistry _configRegistry;
    private readonly AssetManager _assetManager;
    private bool _isDisposed;
    private bool _isUpdating;
    private Action? _pendingSceneTransition;

    /// <summary>
    ///     現在アクティブな GameManager インスタンスを取得します。
    /// </summary>
    public static GameManager Current { get; private set; } = null!;

    /// <summary>ゲーム全域で共有するECSワールドであり、Dispose呼び出し時まで破棄されません。</summary>
    public World EcsWorld => _ecsWorld;

    /// <summary>
    ///     設定レジストリインスタンスを取得します。
    /// </summary>
    public IConfigRegistry Configs => _configRegistry;

    /// <summary>
    ///     アセットマネージャーインスタンスを取得します。
    /// </summary>
    public AssetManager Assets => _assetManager;

    /// <summary>
    ///     <see cref="GameManager" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public GameManager()
    {
        _ecsWorld = World.Create();
        Current = this;

        _configRegistry = new ConfigRegistry();
        GameConfigRegistrar.RegisterAll(_configRegistry, _ecsWorld);

        _assetManager = new AssetManager();
        _assetManager.Initialize();
        _ecsWorld.Set<IAssetManager>(_assetManager);
        _ecsWorld.Set(_assetManager);

        // 共有パイプラインモジュールおよびプレハブテンプレートの決定論的登録
        _ecsWorld.Import<CommonModule>();
        _ecsWorld.Import<EffectModule>();
        _ecsWorld.Import<CombatFeedbackModule>();
        _ecsWorld.Import<AnimationModule>();
        _ecsWorld.Import<ArenaModule>();
        _ecsWorld.Import<CombatModule>();
        _ecsWorld.Import<PlayerModule>();
        _ecsWorld.Import<BossModule>();
        _ecsWorld.Import<RenderModule>(); // ネイティブ描画モジュール
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
    ///     ゲームエンジンを初期化し、キャラクター選択画面へ入場します。
    /// </summary>
    public void Initialize()
    {
        InputService.ClearAndSetContext(InputContext.Menu);
        StartSceneSpawner.Spawn(_ecsWorld);
    }

    /// <summary>
    ///     戦闘シーンへ遷移します。
    ///     システム実行中はECSステージの不整合を防ぐため、Progress終了後の同期ポイントへ処理を遅延させます。
    /// </summary>
    /// <param name="playerType">選択されたプレイヤーキャラクター</param>
    /// <param name="bossType">出撃対象のボス種別</param>
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

        InputService.ClearAndSetContext(InputContext.InGame);
        BattleSceneSpawner.Spawn(_ecsWorld, playerType, bossType);
    }

    /// <summary>
    ///     キャラクター選択画面へ復帰します。
    ///     システム実行中はECSステージの不整合を防ぐため、Progress終了後の同期ポイントへ処理を遅延させます。
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

        InputService.ClearAndSetContext(InputContext.Menu);
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
            // PreUpdateからOnStoreまでの各フェーズを順次駆動します
            _ecsWorld.Progress(deltaTime);
        }
        finally
        {
            _isUpdating = false;
        }

        // システム実行中の構造変更による不整合を防ぐため、Progress完了後に保留中のシーン遷移を実行します
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

        _assetManager.Dispose();

        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
