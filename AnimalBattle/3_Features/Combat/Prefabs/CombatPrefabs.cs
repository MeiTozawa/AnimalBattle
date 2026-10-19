using System.Drawing;
using AnimalBattle.Combat;
using AnimalBattle.Common;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Combat;

/// <summary>
///     戦闘ドメイン（自機弾・Boss統一弾幕・近接斬撃判定）の Prefab 登録およびインスタンス化。
/// </summary>
public static class CombatPrefabs
{
    public const string PlayerBulletName = "PlayerBulletPrefab";
    public const string BossBulletBaseName = "BossBulletBasePrefab";
    public const string PlayerSlashHitboxName = "PlayerSlashHitboxPrefab";

    public static Entity PlayerBullet { get; private set; }
    public static Entity BossBulletBase { get; private set; }
    public static Entity PlayerSlashHitbox { get; private set; }

    /// <summary>
    ///     戦闘関連の Prefab 原型を登録します。
    /// </summary>
    public static void Register(World world)
    {
        var cfg = world.Get<CombatPrefabConfig>();

        // --- 1. 自機通常弾 ---
        var playerBulletCfg = cfg.PlayerBullet;
        PlayerBullet = world.Prefab(PlayerBulletName)
            .IsA(CommonPrefabs.BaseProjectile)
            .Add<PlayerBulletTag>()
            .Set(new Hitbox(new CollisionShape.Box(playerBulletCfg.Width, playerBulletCfg.Height), playerBulletCfg.Damage, Faction.Friendly));

        // --- 2. Boss 統一弾幕基底（超集 Archetype：通常・品紅・黄・衛星・青・黒の全種別を同一 Archetype で共用） ---
        var bossNormalCfg = cfg.BossNormalBullet;
        var (normalFill, normalOutline) = BulletPalette.ResolveBulletColors(
            BulletType.Normal,
            ColorTranslator.FromHtml(bossNormalCfg.FillColor),
            ColorTranslator.FromHtml(bossNormalCfg.OutlineColor));

        BossBulletBase = world.Prefab(BossBulletBaseName)
            .IsA(CommonPrefabs.BaseProjectile) 
            .Add<BossBulletTag>() 
            .Set(new Bullet(bossNormalCfg.Radius, normalFill, normalOutline)) 
            .Set(new Hitbox(new CollisionShape.Circle(bossNormalCfg.Radius), bossNormalCfg.Damage, Faction.Hostile)) 
            .Set(new BulletModifier(BulletType.Normal, bossNormalCfg.Radius, 0.0f, 0.0f)) 
            .Set(new OrbitSatellite(false, Entity.Null(), 0.0f, 0.0f, 0));

        // --- 3. 自機近接斬撃判定 ---
        var slashCfg = cfg.PlayerSlashHitbox;
        PlayerSlashHitbox = world.Prefab(PlayerSlashHitboxName)
            .IsA(CommonPrefabs.BaseMeleeHitbox)
            .Add<PlayerMeleeTag>()
            .Set(new Hitbox(new CollisionShape.Box(slashCfg.Width, slashCfg.Height), slashCfg.Damage, Faction.Friendly));
    }
}
