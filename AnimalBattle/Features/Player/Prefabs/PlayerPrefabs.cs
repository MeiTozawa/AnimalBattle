namespace AnimalBattle.Player;

/// <summary>
///     プレイヤー自機：Cat & Bunnyの Prefab プレハブ登録およびインスタンス化。
///     設定ファイルから生成されたコンポーネント群を保持し、純粋な ECS プレハブとして機能します。
/// </summary>
public static class PlayerPrefabs
{
    public const string PlayerBaseName = "PlayerBasePrefab";
    public const string CatPlayerName = "CatPlayerPrefab";
    public const string BunnyPlayerName = "BunnyPlayerPrefab";

    public static Entity PlayerBase { get; private set; }
    public static Entity CatPlayer { get; private set; }
    public static Entity BunnyPlayer { get; private set; }

    private static bool _catHasDash;
    private static bool _bunnyHasDash;
    private static string _catWeapon = string.Empty;
    private static string _bunnyWeapon = string.Empty;

    /// <summary>
    ///     プレイヤー共通基底および各キャラクターの Prefab プレハブを設定から登録・構築します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    /// <param name="catCfg">Cat のプレイヤー設定。</param>
    /// <param name="bunnyCfg">Bunny のプレイヤー設定。</param>
    public static void Register(World world, PlayerConfig catCfg, PlayerConfig bunnyCfg)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(catCfg != null, "Cat の設定は null にできません。");
        Debug.Assert(bunnyCfg != null, "Bunny の設定は null にできません。");

        // プレイヤー共通基底
        PlayerBase = world.Prefab(PlayerBaseName)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<PlayerTag>()
            .Set(new MovementIntent())
            .Set(new WeaponIntent())
            .Set(new SpecialAbilityIntent());

        // --- Cat プレイヤー ---
        CatPlayer = world.Prefab(CatPlayerName).IsA(PlayerBase);
        ConfigureCat(catCfg, world);

        // --- Bunny プレイヤー ---
        BunnyPlayer = world.Prefab(BunnyPlayerName).IsA(PlayerBase);
        ConfigureBunny(bunnyCfg, world);
    }

    /// <summary>
    ///     Cat プレハブに設定パラメータを適用・更新します。
    /// </summary>
    public static void ConfigureCat(PlayerConfig cfg, World world)
    {
        _catHasDash = cfg.Dash != null;
        _catWeapon = cfg.Weapon;
        ConfigurePlayerPrefab(CatPlayer, cfg, world);
    }

    /// <summary>
    ///     Bunny プレハブに設定パラメータを適用・更新します。
    /// </summary>
    public static void ConfigureBunny(PlayerConfig cfg, World world)
    {
        _bunnyHasDash = cfg.Dash != null;
        _bunnyWeapon = cfg.Weapon;
        ConfigurePlayerPrefab(BunnyPlayer, cfg, world);
    }

    /// <summary>
    ///     指定されたプレイヤー Prefab に設定パラメータをコンポーネントとして適用・更新します。
    /// </summary>
    private static void ConfigurePlayerPrefab(Entity prefab, PlayerConfig cfg, World world)
    {
        prefab.Set(new Health(cfg.MaxHp))
            .Set(CombatProfile.ForPlayer(cfg.InvincibleDuration, cfg.HitSquashScale))
            .Set(new HitFlashSettings(cfg.HitFlashDuration))
            .Set(new CollisionBox(cfg.CollisionSize))
            .Set(new Hurtbox(cfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(cfg.Spawn, cfg.Scale))
            .Set(new SpringScale(Vec2f.One, cfg.SpringFrequency, cfg.SpringDampingRatio))
            .Set(LocomotionCapability.FromConfig(cfg.Movement))
            .Set(JumpCapability.FromConfig(cfg.Jump))
            .Set(new EnergyGauge
            {
                CurrentEnergy = 0.0f,
                MaxEnergy = cfg.EnergyMaxSegments * cfg.EnergySegmentCapacity,
                SegmentCapacity = cfg.EnergySegmentCapacity,
                CooldownTimer = 0.0f,
                EnergyPerDamageDealt = cfg.EnergyPerDamageDealt,
                EnergyPerDamageTaken = cfg.EnergyPerDamageTaken
            });

        if (cfg.Dash != null)
        {
            prefab.Set(DashCapability.FromConfig(cfg.Dash)).Set(new DashState());
        }
        else
        {
            prefab.Remove<DashCapability>().Remove<DashState>();
        }

        WeaponPrefabs.Attach(prefab, cfg.Weapon);
        SpecialAbilityPrefabs.Attach(prefab, cfg.SpecialAbility);
    }

    /// <summary>
    ///     指定された CharacterType の Prefab プレハブを取得します。
    /// </summary>
    public static Entity GetPrefab(CharacterType type)
    {
        return type switch {
            CharacterType.Cat => CatPlayer,
            CharacterType.Bunny => BunnyPlayer,
            _ => AssertHelper.FailWithDefault("", CatPlayer)
        };
    }

    /// <summary>
    ///     指定されたプレイヤー種別のインスタンスエンティティを生成・初期化し、対応する武器装備をセットアップします。
    /// </summary>
    public static Entity SpawnPlayer(World world, CharacterType playerType, Entity root, string name, int playerHandle)
    {
        var prefab = GetPrefab(playerType);
        ref readonly var initialTrans = ref prefab.Get<Transform>();
        var playerEntity = world.Entity(name)
            .ChildOf(root)
            .IsA(prefab)
            .Set(new Transform(initialTrans.Position, initialTrans.Scale))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new Animation(playerType, "Idle"))
            .Set(new Sprite(playerHandle, false, true, new Rect(0, 0, 32, 32)))
            .Set(new CharacterTypeComponent(playerType));

        var weaponName = playerType == CharacterType.Cat ? _catWeapon : _bunnyWeapon;
        WeaponPrefabs.SetupInstance(world, playerEntity, weaponName);

        return playerEntity;
    }

    /// <summary>
    ///     プレイヤーエンティティの状態（位置、体力、入力意図、能力、武器、タグ、スプライト表示）を Prefab から初期状態へ安全に完全復帰させます。
    /// </summary>
    public static void ResetPlayerEntity(Entity player)
    {
        if (!player.IsAlive()) return;

        var type = player.Get<CharacterTypeComponent>().Value;
        switch (type)
        {
            case CharacterType.Cat:
                ResetCat(player);
                break;
            case CharacterType.Bunny:
                ResetBunny(player);
                break;
        }
    }

    private static void ResetCommon(Entity player, Entity prefab, CharacterType type)
    {
        ref readonly var initialTrans = ref prefab.Get<Transform>();
        player.Set(new Transform(initialTrans.Position, initialTrans.Scale));
        player.Set(new Velocity(Vec2f.Zero));
        player.Set(new Health(prefab.Get<Health>().Max));
        player.Set(new MovementIntent());
        player.Set(new WeaponIntent());
        player.Set(new SpecialAbilityIntent());
        player.Set(prefab.Get<EnergyGauge>());
        player.Set(prefab.Get<LocomotionCapability>());
        player.Set(prefab.Get<JumpCapability>());
        player.Set(new GameplayTagState());
        player.Set(new TimedEffectsState());
        player.Set(prefab.Get<SpringScale>());
        player.Set(new Animation(type, "Idle"));

        player.Remove<HitstopState>()
            .Remove<SpriteJitter>()
            .Remove<DeadTag>()
            .Remove<HitFlashEffect>()
            .Remove<InvincibleBlinkEffect>()
            .Remove<DashingTag>();

        ref var sprite = ref player.GetMut<Sprite>();
        sprite.IsVisible = true;
        sprite.JitterOffset = Vec2f.Zero;
        sprite.Scale = Vec2f.One;
        sprite.SpringOffsetY = 0.0f;
    }

    private static void ResetCat(Entity player)
    {
        ResetCommon(player, CatPlayer, CharacterType.Cat);
        if (_catHasDash)
        {
            player.Set(CatPlayer.Get<DashCapability>()).Set(new DashState());
        }
        else
        {
            player.Remove<DashCapability>().Remove<DashState>();
        }
        player.Set(CatPlayer.Get<RangedWeaponComponent>());
    }

    private static void ResetBunny(Entity player)
    {
        ResetCommon(player, BunnyPlayer, CharacterType.Bunny);
        if (_bunnyHasDash)
        {
            player.Set(BunnyPlayer.Get<DashCapability>()).Set(new DashState());
        }
        else
        {
            player.Remove<DashCapability>().Remove<DashState>();
        }
        player.Set(BunnyPlayer.Get<MeleeWeaponComponent>());
        var melee = player.Target<EquippedWeapon>();
        if (melee.IsAlive())
        {
            melee.Disable();
            melee.Set(new HitHistory());
            ref var meleeSprite = ref melee.GetMut<Sprite>();
            meleeSprite.IsVisible = true;
        }
    }
}
