using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Plugins;

namespace AnimalBattle.Scenes;

/// <summary>
/// Flecs.NET ワールドおよび Boss 戦パイプラインを統括するバトルシーンクラス。
/// TOML 設定ファイル（player.toml, boss.toml, arena.toml）から初期パラメータをロードします。
/// </summary>
public partial class BattleScene : IDisposable
{
    private readonly World _ecsWorld;
    private bool _isDisposed;

    /// <summary>
    /// <see cref="BattleScene"/> の新しいインスタンスを初期化します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
    }

    /// <summary>
    /// 各種 TOML 設定ファイル、システムおよびエンティティ（プレイヤー、Boss、プラットフォーム）の初期化を行います。
    /// </summary>
    public void Initialize()
    {
        ConfigManager.Initialize();
        AnimationManager.Initialize();
        int catHandle = AnimationManager.GetCharacterSpriteHandle();

        App app = new App(_ecsWorld)
            .AddPluginGroup(new DefaultPlugins());
        app.FinishSetup();

        PlayerConfig playerCfg = ConfigManager.Player;
        BossConfig bossCfg = ConfigManager.Boss;

        // 1. プレイヤーの生成
        _ecsWorld.Entity("Player1")
            .Set(new Transform(playerCfg.SpawnX, playerCfg.SpawnY, scaleX: playerCfg.ScaleX, scaleY: playerCfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(playerCfg.HitboxWidth, playerCfg.HitboxHeight))
            .Set(new Health(playerCfg.MaxHp))
            .Set(new DoubleJump(playerCfg.MaxJumps))
            .Set(new PlayerState())
            .Set(new Sprite(catHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 32, 32)))
            .Set(new Animation("Idle", isPlaying: true))
            .Add<PlayerTag>();

        // 2. Boss の生成
        _ecsWorld.Entity("Boss")
            .Set(new Transform(bossCfg.BaseX, bossCfg.BaseY, scaleX: bossCfg.ScaleX, scaleY: bossCfg.ScaleY))
            .Set(new Velocity())
            .Set(new CollisionBox(bossCfg.HitboxWidth, bossCfg.HitboxHeight))
            .Set(new Health(bossCfg.MaxHp))
            .Set(new BossStats(baseX: bossCfg.BaseX, baseY: bossCfg.BaseY))
            .Set(new BossStateMachine())
            .Set(new Sprite(catHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 32, 32)))
            .Add<BossTag>();

        // 3. アリーナ足場
        SetupArena();

        Console.WriteLine("[初期化完了] Boss バトルシーンが正常に初期化されました！");
    }

    /// <summary>
    /// arena.toml に基づいて戦闘用の固定アリーナプラットフォームを生成します。
    /// </summary>
    private void SetupArena()
    {
        ArenaConfig arenaCfg = ConfigManager.Arena;

        // メイン底面フロア
        _ecsWorld.Entity("Platform_Floor")
            .Set(new Transform(arenaCfg.Floor.X, arenaCfg.Floor.Y))
            .Set(new CollisionBox(arenaCfg.Floor.Width, arenaCfg.Floor.Height))
            .Add<PlatformTag>();

        // 左側浮遊足場
        _ecsWorld.Entity("Platform_Left")
            .Set(new Transform(arenaCfg.PlatformLeft.X, arenaCfg.PlatformLeft.Y))
            .Set(new CollisionBox(arenaCfg.PlatformLeft.Width, arenaCfg.PlatformLeft.Height))
            .Add<PlatformTag>();

        // 右側浮遊足場
        _ecsWorld.Entity("Platform_Right")
            .Set(new Transform(arenaCfg.PlatformRight.X, arenaCfg.PlatformRight.Y))
            .Set(new CollisionBox(arenaCfg.PlatformRight.Width, arenaCfg.PlatformRight.Height))
            .Add<PlatformTag>();

        // 中央高台足場
        _ecsWorld.Entity("Platform_Center")
            .Set(new Transform(arenaCfg.PlatformCenter.X, arenaCfg.PlatformCenter.Y))
            .Set(new CollisionBox(arenaCfg.PlatformCenter.Width, arenaCfg.PlatformCenter.Height))
            .Add<PlatformTag>();
    }

    /// <summary>
    /// シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    /// <returns>正常続行時は true</returns>
    public bool Update(float deltaTime)
    {
        _ecsWorld.Progress(deltaTime);
        return true;
    }

    /// <summary>
    /// シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _ecsWorld.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
