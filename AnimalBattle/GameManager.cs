using AnimalBattle.Ai;
using AnimalBattle.Asset;
using AnimalBattle.CombatPool;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Modules;
using AnimalBattle.Prefabs;
using AnimalBattle.Scenes;
using Flecs.NET.Core;

namespace AnimalBattle;

/// <summary>
///     ゲームの全体ライフサイクル、共有 ECS ワールド、および GameFlow 状態遷移を統括するマネージャークラス。
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
    ///     開始・選人シーンコントローラー。
    /// </summary>
    public StartScene StartScene { get; }

    /// <summary>
    ///     戦闘バトルシーンコントローラー。
    /// </summary>
    public BattleScene BattleScene { get; }

    /// <summary>
    ///     <see cref="GameManager" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public GameManager()
    {
        _ecsWorld = World.Create();
        Current = this;

        ConfigManager.Initialize();
        AssetManager.Initialize();

        // 共有 ECS パイプラインモジュールの登録
        _ecsWorld.Import<CombatFeedbackModule>();
        _ecsWorld.Import<AnimationModule>();
        _ecsWorld.Import<CombatModule>();
        _ecsWorld.Import<BulletModule>();
        _ecsWorld.Import<DamageResolutionModule>();
        _ecsWorld.Import<EffectModule>();

        PrefabRegistry.Initialize(_ecsWorld);
        CombatPoolService.Initialize(_ecsWorld);

        StartScene = new StartScene();
        BattleScene = new BattleScene();

        StartScene.OnConfirmStart = (player, boss) => SwitchToBattle(player, boss);
        BattleScene.OnReturnToStart = () => SwitchToStart();

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
        StartScene.Setup(_ecsWorld);
    }

    /// <summary>
    ///     戦闘シーンへ遷移します。
    /// </summary>
    /// <param name="playerType">選択されたプレイヤーキャラクター</param>
    /// <param name="bossType">選択された首領</param>
    public void SwitchToBattle(CharacterType playerType, BossType bossType)
    {
        StartScene.Teardown();

        ref var flow = ref _ecsWorld.GetMut<GameFlow>();
        flow.State = GameState.Battle;
        flow.SelectedPlayer = playerType;
        flow.SelectedBoss = bossType;

        BattleScene.Setup(_ecsWorld, playerType, bossType);
    }

    /// <summary>
    ///     開始選人シーンへ復帰します。
    /// </summary>
    public void SwitchToStart()
    {
        BattleScene.Teardown();

        ref var flow = ref _ecsWorld.GetMut<GameFlow>();
        flow.State = GameState.Start;

        StartScene.Setup(_ecsWorld);
    }

    /// <summary>
    ///     フレームごとの論理更新を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間秒数</param>
    public void Update(float deltaTime)
    {
        var flow = _ecsWorld.Get<GameFlow>();
        switch (flow.State)
        {
            case GameState.Start:
                StartScene.Update(deltaTime);
                // プレビュー自機アニメーション等（SpriteAnimationSystem）を進める
                _ecsWorld.Progress(deltaTime);
                break;

            case GameState.Battle:
                BattleScene.Update(deltaTime);
                // 物理・弾幕・衝突・ダメージ判定を進める
                _ecsWorld.Progress(deltaTime);
                break;
        }
    }

    /// <summary>
    ///     フレームごとの描画コマンドを発行します。
    /// </summary>
    public void Render()
    {
        var flow = _ecsWorld.Get<GameFlow>();
        switch (flow.State)
        {
            case GameState.Start:
                StartScene.Render();
                break;

            case GameState.Battle:
                BattleScene.Render();
                break;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;

        StartScene.Dispose();
        BattleScene.Dispose();

        CombatPoolService.Current.Dispose();
        _ecsWorld.Dispose();

        AssetManager.Shutdown();

        if (Current == this) Current = null!;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}