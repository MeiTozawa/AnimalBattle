using System.Drawing;
using AnimalBattle.Combat;
using AnimalBattle.Combat.Events;
using AnimalBattle.Common;
using AnimalBattle.Combat.Components;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     戦闘ドメイン：自機弾・Boss統一弾幕・近接斬撃判定の Prefab 登録およびインスタンス化。
/// </summary>
public static class CombatPrefabs
{
    public const string PlayerBulletName = "PlayerBulletPrefab";
    public const string BossBulletBaseName = "BossBulletBasePrefab";
    public const string PlayerSlashHitboxName = "PlayerSlashHitboxPrefab";
    public const string BossSlashHitboxName = "BossSlashHitboxPrefab";

    public static Entity PlayerBullet { get; private set; }
    public static Entity BossBulletBase { get; private set; }
    public static Entity PlayerSlashHitbox { get; private set; }
    public static Entity BossSlashHitbox { get; private set; }

    /// <summary>
    ///     戦闘関連の Prefab プレハブを登録します。
    /// </summary>
    public static void Register(World world)
    {
        var cfg = world.Get<CombatPrefabConfig>();

        // ワールド常駐型被弾イベントバッファの初期化
        world.Set(new DamageInbox());

        // 実行時インスタンスコンポーネントに AutoOverride を付与し、Archetype 安定化およびインスタンス固有メモリを保証
        world.Component<Bullet>().AutoOverride<Bullet>();
        world.Component<Hitbox>().AutoOverride<Hitbox>();
        world.Component<BulletModifier>().AutoOverride<BulletModifier>();
        world.Component<OrbitSatellite>().AutoOverride<OrbitSatellite>();
        world.Component<HitboxLifetime>().AutoOverride<HitboxLifetime>();

        // --- 1. 自機通常弾 ---
        var playerBulletCfg = cfg.PlayerBullet;
        PlayerBullet = world.Prefab(PlayerBulletName)
            .IsA(CommonPrefabs.BaseProjectile)
            .Add<PlayerBulletTag>()
            .Set(new Hitbox(new CollisionShape.Box(playerBulletCfg.Size), playerBulletCfg.Damage, Faction.Friendly, knockback: playerBulletCfg.Knockback, isHeavy: playerBulletCfg.IsHeavy));

        // --- 2. Boss 統一弾幕基底：超集 Archetype：通常・品紅・黄・衛星・青・黒の全種別を同一 Archetype で共用 ---
        var bossNormalCfg = cfg.BossNormalBullet;
        var (normalFill, normalOutline) = BulletPalette.ResolveBulletColors(BulletType.Normal);

        BossBulletBase = world.Prefab(BossBulletBaseName)
            .IsA(CommonPrefabs.BaseProjectile) 
            .Add<BossBulletTag>() 
            .Set(new Bullet(bossNormalCfg.Radius, normalFill, normalOutline)) 
            .Set(new Hitbox(new CollisionShape.Circle(bossNormalCfg.Radius), bossNormalCfg.Damage, Faction.Hostile)) 
            .Set(new BulletModifier(BulletType.Normal, bossNormalCfg.Radius, Vec2f.Zero)) 
            .Set(new OrbitSatellite(0.0f, 0.0f));

        // --- 3. 自機近接斬撃判定：常駐エンティティであり持続時間は BunnyWeaponSystem が有効無効を制御するため HitboxLifetime は付与しない ---
        var slashCfg = cfg.PlayerSlashHitbox;
        PlayerSlashHitbox = world.Prefab(PlayerSlashHitboxName)
            .IsA(CommonPrefabs.BaseMeleeHitbox)
            .Add<PlayerMeleeTag>()
            .Set(new Hitbox(new CollisionShape.Box(slashCfg.Size), slashCfg.Damage, Faction.Friendly, destroyOnHit: false, knockback: slashCfg.Knockback, isHeavy: slashCfg.IsHeavy));

        // --- 4. Boss 2 近接斬撃攻撃判定実体 ---
        var bossSlashCfg = cfg.BossSlashHitbox;
        BossSlashHitbox = world.Prefab(BossSlashHitboxName)
            .IsA(CommonPrefabs.BaseMeleeHitbox)
            .Add<BossMeleeTag>()
            .Set(new Velocity(Vec2f.Zero))
            .Set(new Hitbox(new CollisionShape.Box(Vec2f.Zero), bossSlashCfg.Damage, Faction.Hostile, false, knockback: bossSlashCfg.Knockback, isHeavy: bossSlashCfg.IsHeavy))
            .Set(new Sprite(-1))
            .Set(new HitboxLifetime(bossSlashCfg.DefaultDuration));
    }
}
