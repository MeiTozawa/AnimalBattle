namespace AnimalBattle.Combat.Sync;

using System.Diagnostics;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Presentation.Vfx;
using Flecs.NET.Core;

/// <summary>
/// 戦闘ドメイン設定変更時に、Flecs.NET ワールドの単例、Prefab、および活体近接判定エンティティを同期する同期機構。
/// </summary>
public static class CombatConfigSync
{
    /// <summary>
    /// 特殊弾幕設定の変更をワールドへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の SpecialBulletConfig 設定</param>
    public static void SyncSpecialBullets(World world, SpecialBulletConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "SpecialBulletConfig は null にできません。");

        // ワールド単例の更新
        world.Set(newConfig);
    }

    /// <summary>
    /// 戦闘 Prefab 設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の CombatPrefabConfig 設定</param>
    public static void SyncCombatPrefabs(World world, CombatPrefabConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "CombatPrefabConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. Prefab プレハブの再登録
        CombatPrefabs.Register(world);
        VfxPrefabs.Register(world);

        // 3. 活体自機近接斬撃判定（PlayerSlashHitbox）の再水和
        var slashCfg = newConfig.PlayerSlashHitbox;
        using var meleeQuery = world.QueryBuilder<Hitbox>().With<PlayerMeleeTag>().Build();
        meleeQuery.Each((ref Hitbox hit) =>
        {
            hit.Shape = new CollisionShape.Box(slashCfg.Size);
            hit.Damage = slashCfg.Damage;
            hit.Knockback = slashCfg.Knockback;
            hit.IsHeavy = slashCfg.IsHeavy;
        });

        // 4. 活体 BossLion 近接斬撃判定（BossSlashHitbox）の再水和
        var bossSlashCfg = newConfig.BossSlashHitbox;
        using var bossMeleeQuery = world.QueryBuilder<Hitbox, HitboxLifetime>().With<BossMeleeTag>().Build();
        bossMeleeQuery.Each((ref Hitbox hit, ref HitboxLifetime lifetime) =>
        {
            hit.Damage = bossSlashCfg.Damage;
            hit.Knockback = bossSlashCfg.Knockback;
            hit.IsHeavy = bossSlashCfg.IsHeavy;
            lifetime.Duration = bossSlashCfg.DefaultDuration;
        });
    }
}
