using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Characters;
using AnimalBattle.Configs;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes;

/// <summary>
///     Flecs.NET ワールドおよび多 Boss (Boss 1 / Boss 2) 戦パイプラインを統括するバトルシーンクラス。
///     TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, arena.toml）から初期パラメータをロードし、
///     プレイヤー、Boss、プラットフォームへ Bevy スタイルの Hitbox / Hurtbox を付与します。
/// </summary>
public class BattleScene : IDisposable {
    private readonly DebugRenderSystem _debugRenderer;
    private readonly WorldRenderSystem _worldRenderer;
    private World _ecsWorld;
    private Query _effectsQuery;
    private bool _isDisposed;
    private Query<Transform, CollisionBox> _platformQuery;
    private Query _playerBulletsQuery;
    private TileMap? _tileMap;

    /// <summary>
    ///     <see cref="BattleScene" /> の新しいインスタンスを初期化し、全 ECS パイプラインを登録します。
    /// </summary>
    public BattleScene()
    {
        _ecsWorld = World.Create();
        Current = this;

        ConfigManager.Initialize();
        AssetManager.Initialize();

        var playerHandleRes = AssetManager.GetCharacterSpriteHandle(CharacterType.Cat, "Idle");
        var playerHandle = !playerHandleRes.IsError ? playerHandleRes.Value : -1;

        _ecsWorld.Import<ComponentRequirementsModule>();
        _ecsWorld.Import<EffectModule>();
        _ecsWorld.Import<BulletModule>();

        var playerCfg = ConfigManager.Player;

        var playerEntity = _ecsWorld.Entity("Player1");
        Player = new Player(playerEntity, playerCfg, playerHandle);
        PlayerController = new PlayerController(Player);

        SetupArena();

        _platformQuery = _ecsWorld.QueryBuilder<Transform, CollisionBox>()
           .With<PlatformTag>()
           .Build();

        _worldRenderer = new WorldRenderSystem(_ecsWorld);
        _debugRenderer = new DebugRenderSystem(_ecsWorld);
        _playerBulletsQuery = _ecsWorld.QueryBuilder()
           .With<PlayerBulletTag>()
           .Build();
        _effectsQuery = _ecsWorld.QueryBuilder()
           .With<EffectTag>()
           .Build();

        // 3. アクティブな Boss (デフォルト: Boss 1) の生成
        SpawnBoss(CurrentBossType);
    }

    /// <summary>
    ///     現在アクティブな BattleScene インスタンスを取得します。
    /// </summary>
    public static BattleScene? Current { get; private set; }

    /// <summary>
    ///     現在アクティブなプレイヤー自機インスタンスを取得します。
    /// </summary>
    public Player Player { get; }

    /// <summary>
    ///     現在アクティブなプレイヤーコントローラーを取得します。
    /// </summary>
    public PlayerController PlayerController { get; }

    /// <summary>
    ///     現在アクティブな Boss インスタンスを取得します。
    /// </summary>
    public BossCharacter Boss { get; private set; } = null!;

    /// <summary>
    ///     現在アクティブな Boss の種別を取得または設定します。
    /// </summary>
    private BossType CurrentBossType { get; set; } = BossType.Boss1;

    /// <summary>
    ///     現在アクティブな Boss の識別名を取得します。
    /// </summary>
    private string CurrentBossId => CurrentBossType == BossType.Boss1 ? "Boss1" : "Boss2";

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        ConfigManager.Boss1Container.OnConfigReloaded -= OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded -= OnBoss2ConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded -= OnPlayerConfigReloaded;

        if (_ecsWorld.Has<CombatEntityPoolComponent>()) _ecsWorld.Get<CombatEntityPoolComponent>().Dispose();

        _worldRenderer.Dispose();
        _debugRenderer.Dispose();
        _playerBulletsQuery.Dispose();
        _effectsQuery.Dispose();
        _platformQuery.Dispose();
        _ecsWorld.Dispose();
        AssetManager.Shutdown();
        if (Current == this) Current = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     指定された種類の Boss エンティティを生成またはリセットします。
    /// </summary>
    /// <param name="bossType">生成する Boss の種別 (<see cref="BossType.Boss1" /> または <see cref="BossType.Boss2" />)</param>
    /// <returns>生成または再初期化された Flecs エンティティ</returns>
    private void SpawnBoss(BossType bossType)
    {
        var otherBossName = bossType == BossType.Boss1 ? "Boss2" : "Boss1";
        var otherBoss = _ecsWorld.Lookup(otherBossName);
        if (otherBoss.IsAlive()) otherBoss.Destruct();

        var entityName = bossType == BossType.Boss1 ? "Boss1" : "Boss2";
        var bossEntity = _ecsWorld.Entity(entityName);

        if (bossType == BossType.Boss1)
            SpawnBoss1(bossEntity);
        else if (bossType == BossType.Boss2) SpawnBoss2(bossEntity);
    }

    private void SpawnBoss1(Entity bossEntity)
    {
        var boss1HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character031_3Png);
        var boss1Handle = !boss1HandleRes.IsError ? boss1HandleRes.Value : -1;
        Boss = new BossCharacter(bossEntity, BossType.Boss1, boss1Handle);
        Console.WriteLine(
            $"[BattleScene] Boss 1 (Ancient Cat Overlord) を生成・リセットしました (HP: {ConfigManager.Boss1.MaxHp}, Pos: {ConfigManager.Boss1.SpawnX}, {ConfigManager.Boss1.SpawnY})");
    }

    private void SpawnBoss2(Entity bossEntity)
    {
        var boss2HandleRes = AssetManager.GetTexture(ResourcePath.Characters.Character027_3Png);
        var boss2Handle = !boss2HandleRes.IsError ? boss2HandleRes.Value : -1;
        Boss = new BossCharacter(bossEntity, BossType.Boss2, boss2Handle, () => _tileMap?.NavGraph);
        Console.WriteLine(
            $"[BattleScene] Boss 2 (Ground Crusher) を生成・リセットしました (HP: {ConfigManager.Boss2.MaxHp}, Pos: {ConfigManager.Boss2.SpawnX}, {ConfigManager.Boss2.SpawnY})");
    }

    /// <summary>
    ///     シーンの追加初期化処理（設定ホットリロードフックの登録等）を行います。
    /// </summary>
    public void Initialize()
    {
        RegisterConfigReloadHooks();
    }

    /// <summary>
    ///     現在の Boss を初期状態にリセットして再生成します。
    /// </summary>
    private void ResetCurrentBoss()
    {
        Boss.Reset();
    }

    /// <summary>
    ///     対象の Boss へ切り替えます。
    /// </summary>
    /// <param name="bossId">切り替え先の Boss 識別名 ("Boss1" または "Boss2")</param>
    private void SwitchBoss(string bossId)
    {
        var targetType = bossId.Equals("Boss2", StringComparison.OrdinalIgnoreCase)
            ? BossType.Boss2
            : BossType.Boss1;
        CurrentBossType = targetType;
        SpawnBoss(targetType);
    }

    /// <summary>
    ///     TOML 設定ファイル変更時のホットリロードコールバックを登録し、Entity の衝突判定・スケールを即時再同期します。
    /// </summary>
    private void RegisterConfigReloadHooks()
    {
        ConfigManager.Boss1Container.OnConfigReloaded += OnBoss1ConfigReloaded;
        ConfigManager.Boss2Container.OnConfigReloaded += OnBoss2ConfigReloaded;
        ConfigManager.PlayerContainer.OnConfigReloaded += OnPlayerConfigReloaded;
    }

    private void OnBoss1ConfigReloaded()
    {
        if (CurrentBossId.Equals("Boss1", StringComparison.OrdinalIgnoreCase))
        {
            var bossEntity = _ecsWorld.Lookup("Boss1");
            if (bossEntity.IsAlive() && bossEntity.Has<Boss1Tag>())
            {
                var bCfg = ConfigManager.Boss1;
                bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
                bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), bCfg.BodyContactDamage, Faction.Hostile, false));

                if (bossEntity.Has<Transform>())
                {
                    ref var t = ref bossEntity.GetMut<Transform>();
                    t.ScaleX = bCfg.ScaleX;
                    t.ScaleY = bCfg.ScaleY;
                }

                Console.WriteLine("[BattleScene] Boss 1 の Hurtbox / Hitbox / Scale を TOML からリアルタイム同期しました");
            }
        }
    }

    private void OnBoss2ConfigReloaded()
    {
        if (CurrentBossId.Equals("Boss2", StringComparison.OrdinalIgnoreCase))
        {
            var bossEntity = _ecsWorld.Lookup("Boss2");
            if (bossEntity.IsAlive() && bossEntity.Has<Boss2Tag>())
            {
                var bCfg = ConfigManager.Boss2;
                bossEntity.Set(new Hurtbox(bCfg.GetHurtboxShape(), Faction.Hostile));
                bossEntity.Set(new Hitbox(bCfg.GetHitboxShape(), bCfg.BodyContactDamage, Faction.Hostile, false));
                bossEntity.Set(new CollisionBox(bCfg.CollisionWidth, bCfg.CollisionHeight));

                if (bossEntity.Has<Transform>())
                {
                    ref var t = ref bossEntity.GetMut<Transform>();
                    t.ScaleX = bCfg.ScaleX;
                    t.ScaleY = bCfg.ScaleY;
                }

                Console.WriteLine(
                    "[BattleScene] Boss 2 の Hurtbox / Hitbox / CollisionBox / Scale を TOML からリアルタイム同期しました");
            }
        }
    }

    private void OnPlayerConfigReloaded()
    {
        var playerEntity = _ecsWorld.Lookup("Player1");
        if (!playerEntity.IsAlive()) return;
        var pCfg = ConfigManager.Player;
        playerEntity.Set(new Hurtbox(pCfg.GetHurtboxShape()));
        playerEntity.Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight));

        if (playerEntity.Has<Transform>())
        {
            ref var t = ref playerEntity.GetMut<Transform>();
            t.ScaleX = pCfg.ScaleX;
            t.ScaleY = pCfg.ScaleY;
        }

        Console.WriteLine("[BattleScene] Player の Hurtbox / CollisionBox / Scale を TOML からリアルタイム同期しました");
    }

    /// <summary>
    ///     arena.toml の TileMap 設定および TileSet の衝突領域に基づいてプラットフォームコライダーを生成します。
    /// </summary>
    private void SetupArena()
    {
        var arenaCfg = ConfigManager.Arena;
        var tileMapCfg = arenaCfg.TileMap;

        var sheetHandleRes = AssetManager.LoadTexture(tileMapCfg.TileSheetPath);
        Debug.Assert(!sheetHandleRes.IsError, $"[BattleScene 警告] {tileMapCfg.TileSheetPath}の画像は読み取りできませんでした。");
        var sheetHandle = sheetHandleRes.Value;
        TileSet tileSet = new(sheetHandle, ConfigManager.TileSet);
        _tileMap = TileMap.FromConfig(tileMapCfg, tileSet);
        _ecsWorld.Set(_tileMap);

        _tileMap.BuildPlatformColliders(_ecsWorld);
        Console.WriteLine("[BattleScene] TileMap および TileSet プラットフォームコライダーが正常に初期化されました。");
    }

    /// <summary>
    ///     シーンのフレーム更新処理を行います。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
    public void Update(float deltaTime)
    {
        HandleInput();

        PlayerController.PollAndProcessInput();


       Player.Update(deltaTime, ConfigManager.Arena, _platformQuery, _ecsWorld);

       var playerPos = new Vec2f(Player.X, Player.Y);
       var playerVel = new Vec2f(Player.Vx, Player.Vy);
       Boss.Update(deltaTime, playerPos, playerVel, _ecsWorld, _platformQuery);
        

        _ecsWorld.Progress(deltaTime);
    }

    private void HandleInput()
    {
        if (InputService.IsPressed(InputAction.SelectBoss1))
        {
            SwitchBoss("Boss1");
            ResetCombat();
        }
        else if (InputService.IsPressed(InputAction.SelectBoss2))
        {
            SwitchBoss("Boss2");
            ResetCombat();
        }
        else if (InputService.IsPressed(InputAction.Restart))
        {
            ResetCurrentBoss();
            ResetCombat();
        }
    }

    private void ResetCombat()
    {
        CombatEffectHelper.ClearAllBossBullets(_ecsWorld);
        _playerBulletsQuery.Each(e => CombatEntityPoolHelper.Recycle(_ecsWorld, e));
        _effectsQuery.Each(e => CombatEntityPoolHelper.Recycle(_ecsWorld, e));
        Player.Reset(ConfigManager.Player);
    }

    /// <summary>
    ///     シーンの描画処理を行います。
    /// </summary>
    public void Render()
    {
        _worldRenderer.Render(_tileMap, _platformQuery);
        HudRenderSystem.Render(Player, Boss, Player.RemainingJumps);
        _debugRenderer.Render(_platformQuery, _tileMap);
    }
}