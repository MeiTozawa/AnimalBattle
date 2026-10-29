namespace AnimalBattle.Enemies.Sync;

using System;
using System.Diagnostics;
using AnimalBattle.Configs;
using AnimalBattle.Enemies;
using AnimalBattle.Enemies.Components;
using Flecs.NET.Core;

/// <summary>
/// 雑魚敵設定変更時に、Flecs.NET ワールドの単例、Prefab、および活体エンティティを無損同期・再水和する同期機構。
/// </summary>
public static class MinionConfigSync
{
    /// <summary>
    /// 雑魚敵設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の MinionConfig 設定</param>
    public static void Sync(World world, MinionConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "MinionConfig は null にできません。");

        // ワールド単例の更新
        world.Set(newConfig);

        // Prefab プレハブの再登録
        MinionPrefabs.Register(world);

        // 活体地面近接追撃怪エンティティの再水和
        var gCfg = newConfig.GroundMinion;
        using var groundQuery = world.QueryBuilder()
            .With<GroundMinionTag>()
            .Without<DeadTag>()
            .Build();

        groundQuery.Each((Entity e) =>
        {
            RehydrateMinionEntity(e, gCfg.MaxHp, gCfg.Scale, gCfg.CollisionSize, gCfg.ContactDamage, gCfg.HitFlashDuration, gCfg.HitSquashScale, gCfg.SpringFrequency, gCfg.SpringDampingRatio);
        });

        // 活体空中浮遊弾幕怪エンティティの再水和
        var fCfg = newConfig.FlyingMinion;
        var patternCfg = world.Has<BossSnakePatternConfig>() ? world.Get<BossSnakePatternConfig>() : null;
        using var flyingQuery = world.QueryBuilder<FlyingMinionComponent>()
            .With<FlyingMinionTag>()
            .Without<DeadTag>()
            .Build();

        flyingQuery.Each((Entity e, ref FlyingMinionComponent m) =>
        {
            RehydrateMinionEntity(e, fCfg.MaxHp, fCfg.Scale, fCfg.CollisionSize, fCfg.ContactDamage, fCfg.HitFlashDuration, fCfg.HitSquashScale, fCfg.SpringFrequency, fCfg.SpringDampingRatio);

            if (m.Asc != null && patternCfg != null)
            {
                MinionSpawner.AttachFlyingMinionAbility(world, e, m.Asc, fCfg, patternCfg);
            }
        });
    }

    private static void RehydrateMinionEntity(
        Entity e,
        float maxHp,
        Vec2f scale,
        Vec2f collisionSize,
        float contactDamage,
        float hitFlashDuration,
        Vec2f hitSquashScale,
        float springFrequency,
        float springDampingRatio)
    {
        // 生命値の比例更新
        ref var health = ref e.GetMut<Health>();
        var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;
        health.Max = maxHp;
        health.Current = Math.Clamp(ratio * maxHp, 0.0f, maxHp);

        // Transform スケール
        ref var trans = ref e.GetMut<Transform>();
        trans.Scale = scale;

        // CollisionBox サイズ
        ref var col = ref e.GetMut<CollisionBox>();
        col.Size = collisionSize;

        // Hurtbox 形状
        ref var hurt = ref e.GetMut<Hurtbox>();
        hurt.Shape = new CollisionShape.Box(collisionSize);

        // Hitbox 形状・ダメージ
        ref var hit = ref e.GetMut<Hitbox>();
        hit.Shape = new CollisionShape.Box(collisionSize);
        hit.Damage = contactDamage;

        // HitFlash 設定
        ref var flash = ref e.GetMut<HitFlashSettings>();
        flash.Duration = hitFlashDuration;

        // CombatProfile
        ref var profile = ref e.GetMut<CombatProfile>();
        profile.HitSquashScale = hitSquashScale;

        // SpringScale 設定
        ref var spring = ref e.GetMut<SpringScale>();
        spring.Frequency = springFrequency;
        spring.DampingRatio = springDampingRatio;
    }
}
