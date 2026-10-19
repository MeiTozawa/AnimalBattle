namespace AnimalBattle.Characters.Boss;

using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

/// <summary>
/// Boss 設定変更時に、Flecs.NET ワールドの単例、Prefab、および活体エンティティを無損同期・再水和する同期機構。
/// </summary>
public static class BossConfigSync
{
    /// <summary>
    /// Boss 1 設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の Boss 1 設定</param>
    public static void SyncBoss1(World world, Boss1Config newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "Boss1Config は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. Prefab 原型の再登録
        BossPrefabs.Register(world);

        // 3. 活体 Boss1 エンティティの再水和
        using var query = world.QueryBuilder().With<Boss1Tag>().Build();
        query.Each((Entity e) =>
        {
            ref var health = ref e.GetMut<Health>();
            var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;
            var newMax = newConfig.MaxHp;
            health.Max = newMax;
            health.Current = Math.Clamp(ratio * newMax, 0.0f, newMax);

            ref var col = ref e.GetMut<CollisionBox>();
            col.Size = newConfig.CollisionSize;

            ref var hurt = ref e.GetMut<Hurtbox>();
            hurt.Shape = newConfig.GetHurtboxShape();

            ref var hit = ref e.GetMut<Hitbox>();
            hit.Shape = newConfig.GetHitboxShape();
            hit.Damage = newConfig.BodyContactDamage;

            ref var flash = ref e.GetMut<HitFlashSettings>();
            flash.Duration = newConfig.HitFlashDuration;
        });
    }

    /// <summary>
    /// Boss 2 設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の Boss 2 設定</param>
    public static void SyncBoss2(World world, Boss2Config newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "Boss2Config は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. Prefab 原型の再登録
        BossPrefabs.Register(world);

        // 3. 活体 Boss2 エンティティの再水和
        using var query = world.QueryBuilder().With<Boss2Tag>().Build();
        query.Each((Entity e) =>
        {
            ref var health = ref e.GetMut<Health>();
            var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;
            var newMax = newConfig.MaxHp;
            health.Max = newMax;
            health.Current = Math.Clamp(ratio * newMax, 0.0f, newMax);

            ref var col = ref e.GetMut<CollisionBox>();
            col.Size = newConfig.CollisionSize;

            ref var hurt = ref e.GetMut<Hurtbox>();
            hurt.Shape = newConfig.GetHurtboxShape();

            ref var hit = ref e.GetMut<Hitbox>();
            hit.Shape = newConfig.GetHitboxShape();
            hit.Damage = newConfig.BodyContactDamage;

            ref var flash = ref e.GetMut<HitFlashSettings>();
            flash.Duration = newConfig.HitFlashDuration;
        });
    }
}
