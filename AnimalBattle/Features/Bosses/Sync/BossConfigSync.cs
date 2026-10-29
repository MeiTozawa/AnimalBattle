namespace AnimalBattle.Characters.Boss;

using System;
using System.Diagnostics;
using AnimalBattle.Ai.BossLion;
using AnimalBattle.Ai.BossSnake;
using AnimalBattle.Configs;
using AnimalBattle.Enemies;
using AnimalBattle.Enemies.Components;
using Flecs.NET.Core;

/// <summary>
/// Boss 設定変更時に、Flecs.NET ワールドの単例、Prefab、および活体エンティティを無損同期・再水和する同期機構。
/// </summary>
public static class BossConfigSync
{
    /// <summary>
    /// BossSnake 設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の BossSnake 設定</param>
    public static void SyncBossSnake(World world, BossSnakeConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "BossSnakeConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. Prefab プレハブの再登録
        BossPrefabs.Register(world);

        // 3. 活体 BossSnake エンティティの再水和
        using var query = world.QueryBuilder().With<BossSnakeTag>().Build();
        query.Each(e =>
        {
            RehydrateBossEntity(e, newConfig);
        });
    }

    /// <summary>
    /// BossLion 設定の変更をワールド、Prefab、および活体エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の BossLion 設定</param>
    public static void SyncBossLion(World world, BossLionConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "BossLionConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. Prefab プレハブの再登録
        BossPrefabs.Register(world);

        // 3. 活体 BossLion エンティティの再水和
        using var query = world.QueryBuilder().With<BossLionTag>().Build();
        query.Each(e =>
        {
            RehydrateBossEntity(e, newConfig);
        });
    }

    /// <summary>
    /// BossSnake 技能パターン設定の変更をワールド、Boss、および飛行小怪へ同期します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の BossSnakePatternConfig 設定</param>
    public static void SyncBossSnakePattern(World world, BossSnakePatternConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "BossSnakePatternConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 活体 BossSnake エンティティの技能再水和
        using var bossQuery = world.QueryBuilder<BossBrainComponent>().With<BossSnakeTag>().Build();
        bossQuery.Each((ref BossBrainComponent b) =>
        {
            if (b.Brain is BossSnakeBrain snakeBrain)
            {
                snakeBrain.ReloadSkills(newConfig);
            }
        });

        // 3. 活体 FlyingMinion エンティティの技能再水和
        if (world.Has<MinionConfig>())
        {
            var minionCfg = world.Get<MinionConfig>();
            using var minionQuery = world.QueryBuilder<FlyingMinionComponent>().With<FlyingMinionTag>().Build();
            minionQuery.Each((Entity e, ref FlyingMinionComponent m) =>
            {
                if (m.Asc != null)
                {
                    MinionSpawner.AttachFlyingMinionAbility(world, e, m.Asc, minionCfg.FlyingMinion, newConfig);
                }
            });
        }
    }

    /// <summary>
    /// BossLion 技能パターン設定の変更をワールドおよび Boss へ同期します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の BossLionPatternConfig 設定</param>
    public static void SyncBossLionPattern(World world, BossLionPatternConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "BossLionPatternConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 活体 BossLion エンティティの技能再水和
        using var bossQuery = world.QueryBuilder<BossBrainComponent>().With<BossLionTag>().Build();
        bossQuery.Each((ref BossBrainComponent b) =>
        {
            if (b.Brain is BossLionBrain lionBrain)
            {
                lionBrain.ReloadSkills(newConfig);
            }
        });
    }

    private static void RehydrateBossEntity(Entity e, BaseBossConfig newConfig)
    {
        // 生命値の比例更新
        ref var health = ref e.GetMut<Health>();
        var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;
        var newMax = newConfig.MaxHp;
        health.Max = newMax;
        health.Current = Math.Clamp(ratio * newMax, 0.0f, newMax);

        // スケール更新
        ref var trans = ref e.GetMut<Transform>();
        trans.Scale = newConfig.Scale;

        // 衝突判定・被弾判定・接触攻撃判定の更新
        ref var col = ref e.GetMut<CollisionBox>();
        col.Size = newConfig.CollisionSize;

        ref var hurt = ref e.GetMut<Hurtbox>();
        hurt.Shape = newConfig.GetHurtboxShape();

        ref var hit = ref e.GetMut<Hitbox>();
        hit.Shape = newConfig.GetHitboxShape();
        hit.Damage = newConfig.BodyContactDamage;

        // 被弾フラッシュの更新
        ref var flash = ref e.GetMut<HitFlashSettings>();
        flash.Duration = newConfig.HitFlashDuration;

        // ノックバック可否設定の同期
        ref var combat = ref e.GetMut<BossCombatState>();
        combat.CanBeKnockedBack = newConfig.CanBeKnockedBack;

        // スプリング拡縮および被弾変形設定の同期
        ref var spring = ref e.GetMut<SpringScale>();
        spring.Frequency = newConfig.SpringFrequency;
        spring.DampingRatio = newConfig.SpringDampingRatio;

        ref var profile = ref e.GetMut<CombatProfile>();
        profile.HitSquashScale = newConfig.HitSquashScale;
    }
}
