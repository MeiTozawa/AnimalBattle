using System.Drawing;
using AnimalBattle.Ai;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Prefabs;

/// <summary>
///     Flecs.NET の Prefab（原型エンティティ）の集中登録・管理およびインスタンス化を担うドメインサービス。
///     階層的な原型継承（IsA）とコンポーネント共有を構築し、Archetype の固定化とゼロアロケーションを提供します。
/// </summary>
public static class PrefabRegistry
{
    private static readonly Entity[] Prefabs = new Entity[Enum.GetValues<PrefabKey>().Length];
    private static World _world;
    private static bool _isInitialized;

    /// <summary>
    ///     全ての Prefab 原型を Flecs ワールドに初期登録します。
    ///     TOML 設定ファイルを読み込み、階層ツリーおよび共有コンポーネントを構築します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    public static void Initialize(World world)
    {
        _world = world;

        // PoolLeaseState に AutoOverride を付与し、インスタンス化時の Archetype 跨表搬遷を完全根絶
        world.Component<PoolLeaseState>().AutoOverride<PoolLeaseState>();

        RegisterBasePrefabs(world);
        RegisterCombatPrefabs(world);
        RegisterCharacterPrefabs(world);

        _isInitialized = true;
    }

    /// <summary>
    ///     指定された PrefabKey に対応する登録済み Prefab 原型エンティティを取得します。
    /// </summary>
    /// <param name="key">対象の PrefabKey。</param>
    /// <returns>Prefab 原型エンティティ句柄。</returns>
    public static Entity GetPrefab(PrefabKey key)
    {
        var idx = (int)key;
        return Prefabs[idx];
    }

    /// <summary>
    ///     指定された PrefabKey の原型から新しい実体エンティティをインスタンス化します。
    ///     内部的に world.Entity().IsA(prefab) を実行し、完全な Archetype を保持した状態で生成されます。
    /// </summary>
    /// <param name="world">エンティティを生成する Flecs ワールド。</param>
    /// <param name="key">対象の PrefabKey。</param>
    /// <returns>インスタンス化されたエンティティ。</returns>
    public static Entity Instantiate(World world, PrefabKey key)
    {
        var prefab = GetPrefab(key);
        return world.Entity().IsA(prefab);
    }

    /// <summary>
    ///     TOML 設定ホットリロード時に Prefab 上の共有コンポーネント数値を即座に再適用します。
    ///     Flecs の特性により、未オーバーライドの全アクティブ実体に即時反映されます。
    /// </summary>
    public static void ReloadPrefabs()
    {
        if (!_isInitialized) return;
        RegisterCombatPrefabs(_world);
        RegisterCharacterPrefabs(_world);
    }

    private static void RegisterBasePrefabs(World world)
    {
        // 抽象基底原型（外部から直接 Instantiate せず、派生 Prefab の共通親として機能）
        world.Prefab("BaseProjectilePrefab")
           .Set(new Transform(0, 0))
           .Set(new Velocity(0, 0));

        world.Prefab("BaseMeleeHitboxPrefab")
           .Set(new Transform(0, 0))
           .Set(new HitHistory());

        world.Prefab("BaseVfxPrefab")
           .Set(new Transform(0, 0))
           .Add<VfxTag>();

        world.Prefab("BaseCharacterPrefab")
           .Set(new Transform(0, 0))
           .Set(new Velocity(0, 0));
    }

    private static void RegisterCombatPrefabs(World world)
    {
        var cfg = ConfigManager.CombatPrefabs;
        var baseProj = world.Lookup("BaseProjectilePrefab");
        var baseMelee = world.Lookup("BaseMeleeHitboxPrefab");
        var baseVfx = world.Lookup("BaseVfxPrefab");

        // --- 1. 自機通常弾 ---
        var playerBulletCfg = cfg.PlayerBullet;
        var playerBulletPrefab = world.Prefab("PlayerBulletPrefab")
           .IsA(baseProj)
           .Add<PlayerBulletTag>()
           .Set(new CollisionBox(playerBulletCfg.Width, playerBulletCfg.Height))
           .Set(new Hitbox(new CollisionShape.Box(playerBulletCfg.Width, playerBulletCfg.Height), playerBulletCfg.Damage, Faction.Friendly));
        Prefabs[(int)PrefabKey.PlayerBullet] = playerBulletPrefab;

        // --- 2. Boss 弾幕基底 ---
        var bossNormalCfg = cfg.BossNormalBullet;
        var (normalFill, normalOutline) = BulletPalette.ResolveBulletColors(BulletType.Normal, ColorTranslator.FromHtml(bossNormalCfg.FillColor), ColorTranslator.FromHtml(bossNormalCfg.OutlineColor));
        var bossBulletBase = world.Prefab("BossBulletBasePrefab")
           .IsA(baseProj)
           .Add<BossBulletTag>()
           .Set(new Bullet(bossNormalCfg.Radius, normalFill, normalOutline))
           .Set(new Hitbox(new CollisionShape.Circle(bossNormalCfg.Radius), bossNormalCfg.Damage, Faction.Hostile));

        // --- 3. Boss 通常弾 ---
        var bossNormalBullet = world.Prefab("BossNormalBulletPrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(bossNormalCfg.Radius, normalFill, normalOutline))
           .Set(new Hitbox(new CollisionShape.Circle(bossNormalCfg.Radius), bossNormalCfg.Damage, Faction.Hostile));
        Prefabs[(int)PrefabKey.BossNormalBullet] = bossNormalBullet;

        // --- 4. Boss 品紅特殊弾 ---
        var magentaCfg = cfg.BossMagentaBullet;
        var (magentaFill, magentaOutline) = BulletPalette.ResolveBulletColors(BulletType.Magenta, ColorTranslator.FromHtml(magentaCfg.FillColor), ColorTranslator.FromHtml(magentaCfg.OutlineColor));
        var bossMagentaBullet = world.Prefab("BossMagentaBulletPrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(magentaCfg.Radius, magentaFill, magentaOutline))
           .Set(new Hitbox(new CollisionShape.Circle(magentaCfg.Radius), magentaCfg.Damage, Faction.Hostile))
           .Set(new BulletModifier(BulletType.Magenta, magentaCfg.Radius, 0.0f, 0.0f));
        Prefabs[(int)PrefabKey.BossMagentaBullet] = bossMagentaBullet;

        // --- 5. Boss 黄特殊弾 ---
        var yellowCfg = cfg.BossYellowBullet;
        var (yellowFill, yellowOutline) = BulletPalette.ResolveBulletColors(BulletType.Yellow, ColorTranslator.FromHtml(yellowCfg.FillColor), ColorTranslator.FromHtml(yellowCfg.OutlineColor));
        var bossYellowBullet = world.Prefab("BossYellowBulletPrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(yellowCfg.Radius, yellowFill, yellowOutline))
           .Set(new Hitbox(new CollisionShape.Circle(yellowCfg.Radius), yellowCfg.Damage, Faction.Hostile))
           .Set(new BulletModifier(BulletType.Yellow, yellowCfg.Radius, 0.0f, 0.0f));
        Prefabs[(int)PrefabKey.BossYellowBullet] = bossYellowBullet;

        // --- 6. Boss 黄特殊弾周回衛星 ---
        var satCfg = cfg.BossYellowSatellite;
        var satRadius = Math.Max(satCfg.MinRadius, yellowCfg.Radius * satCfg.RadiusMultiplier);
        var bossYellowSat = world.Prefab("BossYellowSatellitePrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(satRadius, yellowFill, yellowOutline))
           .Set(new Hitbox(new CollisionShape.Circle(satRadius), satCfg.Damage, Faction.Hostile))
           .Set(new BulletModifier(BulletType.Yellow, satRadius, 0.0f, 0.0f));
        Prefabs[(int)PrefabKey.BossYellowSatellite] = bossYellowSat;

        // --- 7. Boss 青特殊弾 ---
        var blueCfg = cfg.BossBlueBullet;
        var (blueFill, blueOutline) = BulletPalette.ResolveBulletColors(BulletType.Cyan, ColorTranslator.FromHtml(blueCfg.FillColor), ColorTranslator.FromHtml(blueCfg.OutlineColor));
        var bossBlueBullet = world.Prefab("BossBlueBulletPrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(blueCfg.Radius, blueFill, blueOutline))
           .Set(new Hitbox(new CollisionShape.Circle(blueCfg.Radius), blueCfg.Damage, Faction.Hostile))
           .Set(new BulletModifier(BulletType.Cyan, blueCfg.Radius, 0.0f, 0.0f));
        Prefabs[(int)PrefabKey.BossBlueBullet] = bossBlueBullet;

        // --- 8. Boss 黒特殊弾 ---
        var blackCfg = cfg.BossBlackBullet;
        var (blackFill, blackOutline) = BulletPalette.ResolveBulletColors(BulletType.Black, ColorTranslator.FromHtml(blackCfg.FillColor), ColorTranslator.FromHtml(blackCfg.OutlineColor));
        var bossBlackBullet = world.Prefab("BossBlackBulletPrefab")
           .IsA(bossBulletBase)
           .Set(new Bullet(blackCfg.Radius, blackFill, blackOutline))
           .Set(new Hitbox(new CollisionShape.Circle(blackCfg.Radius), blackCfg.Damage, Faction.Hostile))
           .Set(new BulletModifier(BulletType.Black, blackCfg.Radius, 0.0f, 0.0f));
        Prefabs[(int)PrefabKey.BossBlackBullet] = bossBlackBullet;

        // --- 9. 自機近接斬撃判定 ---
        var slashCfg = cfg.PlayerSlashHitbox;
        var playerSlashHitbox = world.Prefab("PlayerSlashHitboxPrefab")
           .IsA(baseMelee)
           .Add<PlayerMeleeTag>()
           .Set(new Hitbox(new CollisionShape.Box(slashCfg.Width, slashCfg.Height), slashCfg.Damage, Faction.Friendly));
        Prefabs[(int)PrefabKey.PlayerSlashHitbox] = playerSlashHitbox;

        // --- 10. 点粒子スパーク演出 ---
        var sparkCfg = cfg.SparkVfx;
        var sparkVfx = world.Prefab("SparkVfxPrefab")
           .IsA(baseVfx)
           .Set(new Velocity(0, 0))
           .Set(new Bullet(sparkCfg.DefaultRadius, Color.Empty, Color.Empty))
           .Set(new FadeEffect(sparkCfg.DefaultDuration));
        Prefabs[(int)PrefabKey.SparkVfx] = sparkVfx;

        // --- 11. リング拡散演出 ---
        var ringCfg = cfg.RingVfx;
        var ringVfx = world.Prefab("RingVfxPrefab")
           .IsA(baseVfx)
           .Set(new RingEffect(ringCfg.InitialRadius, ringCfg.MaxRadius, Color.Empty))
           .Set(new FadeEffect(ringCfg.DefaultDuration));
        Prefabs[(int)PrefabKey.RingVfx] = ringVfx;

        // --- 12. 判定付与エリア演出 ---
        var areaCfg = cfg.AreaVfx;
        var areaVfx = world.Prefab("AreaVfxPrefab")
           .IsA(baseVfx)
           .Set(new Velocity(0, 0))
           .Set(new Hitbox(new CollisionShape.Box(0, 0), areaCfg.Damage, Faction.Hostile, false))
           .Set(new Sprite(-1))
           .Set(new FadeEffect(areaCfg.DefaultDuration));
        Prefabs[(int)PrefabKey.AreaVfx] = areaVfx;
    }

    private static void RegisterCharacterPrefabs(World world)
    {
        var baseChar = world.Lookup("BaseCharacterPrefab");

        // プレイヤー共通基底
        var playerBase = world.Prefab("PlayerBasePrefab")
           .IsA(baseChar)
           .Add<PlayerTag>()
           .Set(new MovementIntent())
           .Set(new WeaponIntent())
           .Set(new DashState());

        // --- Cat プレイヤー ---
        var catCfg = ConfigManager.CatPrefab;
        var catPlayer = world.Prefab("CatPlayerPrefab")
           .IsA(playerBase)
           .Set(new Health(catCfg.MaxHp))
           .Set(new InvincibleOnHit(catCfg.InvincibleDuration))
           .Set(new HitFlashSettings(catCfg.HitFlashDuration))
           .Set(new CollisionBox(catCfg.CollisionWidth, catCfg.CollisionHeight))
           .Set(new Hurtbox(new CollisionShape.Circle(catCfg.HurtboxRadius, 0.0f, catCfg.HurtboxOffsetY), Faction.Friendly))
           .Set(new Transform(0.0f, 0.0f, catCfg.ScaleX, catCfg.ScaleY))
           .Set(new SpringScale(1.0f, 1.0f, catCfg.SpringFrequency, catCfg.SpringDampingRatio))
           .Set(JumpCapability.FromPlayerConfig(ConfigManager.Cat))
           .Set(CatWeaponState.FromConfig(ConfigManager.Cat));
        Prefabs[(int)PrefabKey.CatPlayer] = catPlayer;

        // --- Bunny プレイヤー ---
        var bunnyCfg = ConfigManager.BunnyPrefab;
        var bunnyPlayer = world.Prefab("BunnyPlayerPrefab")
           .IsA(playerBase)
           .Set(new Health(bunnyCfg.MaxHp))
           .Set(new InvincibleOnHit(bunnyCfg.InvincibleDuration))
           .Set(new HitFlashSettings(bunnyCfg.HitFlashDuration))
           .Set(new CollisionBox(bunnyCfg.CollisionWidth, bunnyCfg.CollisionHeight))
           .Set(new Hurtbox(new CollisionShape.Circle(bunnyCfg.HurtboxRadius, 0.0f, bunnyCfg.HurtboxOffsetY), Faction.Friendly))
           .Set(new Transform(0.0f, 0.0f, bunnyCfg.ScaleX, bunnyCfg.ScaleY))
           .Set(new SpringScale(1.0f, 1.0f, bunnyCfg.SpringFrequency, bunnyCfg.SpringDampingRatio))
           .Set(JumpCapability.FromPlayerConfig(ConfigManager.Bunny))
           .Set(BunnyWeaponState.FromConfig(ConfigManager.Bunny));
        Prefabs[(int)PrefabKey.BunnyPlayer] = bunnyPlayer;

        // Boss 共通基底
        var bossBase = world.Prefab("BossBasePrefab")
           .IsA(baseChar)
           .Add<BossTag>()
           .Set(new BossCombatState(Phase.Phase1));

        // --- Boss 1 ---
        var boss1Cfg = ConfigManager.Boss1Prefab;
        var boss1 = world.Prefab("Boss1Prefab")
           .IsA(bossBase)
           .Add<Boss1Tag>()
           .Set(new Health(boss1Cfg.MaxHp))
           .Set(new HitFlashSettings(boss1Cfg.HitFlashDuration))
           .Set(new Transform(0.0f, 0.0f, boss1Cfg.ScaleX, boss1Cfg.ScaleY))
           .Set(new Hurtbox(new CollisionShape.Circle(boss1Cfg.HurtboxRadius, 0.0f, boss1Cfg.HurtboxOffsetY), Faction.Hostile))
           .Set(new Hitbox(new CollisionShape.Circle(boss1Cfg.BodyHitboxRadius, 0.0f, boss1Cfg.BodyHitboxOffsetY), boss1Cfg.BodyContactDamage, Faction.Hostile, false));
        Prefabs[(int)PrefabKey.Boss1] = boss1;

        // --- Boss 2 ---
        var boss2Cfg = ConfigManager.Boss2Prefab;
        var boss2 = world.Prefab("Boss2Prefab")
           .IsA(bossBase)
           .Add<Boss2Tag>()
           .Set(new Health(boss2Cfg.MaxHp))
           .Set(new HitFlashSettings(boss2Cfg.HitFlashDuration))
           .Set(new Transform(0.0f, 0.0f, boss2Cfg.ScaleX, boss2Cfg.ScaleY))
           .Set(new CollisionBox(boss2Cfg.CollisionWidth, boss2Cfg.CollisionHeight))
           .Set(new Hurtbox(new CollisionShape.Box(boss2Cfg.HurtboxWidth, boss2Cfg.HurtboxHeight, 0.0f, boss2Cfg.HurtboxOffsetY), Faction.Hostile))
           .Set(new Hitbox(new CollisionShape.Box(boss2Cfg.BodyHitboxWidth, boss2Cfg.BodyHitboxHeight, 0.0f, boss2Cfg.BodyHitboxOffsetY), boss2Cfg.BodyContactDamage, Faction.Hostile, false));
        Prefabs[(int)PrefabKey.Boss2] = boss2;
    }
}
