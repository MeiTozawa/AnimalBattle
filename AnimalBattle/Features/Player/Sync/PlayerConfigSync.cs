namespace AnimalBattle.Player;

using System;
using System.Diagnostics;
using Configs;
using Flecs.NET.Core;

/// <summary>
///     プレイヤー設定変更時に、Flecs.NET ワールドのシングルトン、Prefab、およびアクティブエンティティをホットリロード・同期する機構。
/// </summary>
public static class PlayerConfigSync
{
    /// <summary>
    ///     Cat遠距離武器設定の変更をワールド、プレハブ、および生存エンティティへ反映します。
    /// </summary>
    public static void SyncCatWeapon(World world, CatWeaponConfig newConfig)
    {
        world.Set(newConfig);

        PlayerPrefabs.Register(world);

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Cat) return;

        using var q = world.QueryBuilder().With<PlayerTag>().With<RangedWeaponComponent>().Build();

        q.Each(e => {
            ref var weapon = ref e.GetMut<RangedWeaponComponent>();
            weapon = RangedWeaponComponent.FromConfig(newConfig, weapon.Timer);
        });
    }

    /// <summary>
    ///     Bunny近接武器設定の変更をワールド、プレハブ、および生存エンティティへ反映します。
    /// </summary>
    public static void SyncBunnyWeapon(World world, BunnyWeaponConfig newConfig)
    {
        world.Set(newConfig);

        PlayerPrefabs.Register(world);

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Bunny) return;

        using var q = world.QueryBuilder().With<PlayerTag>().With<MeleeWeaponComponent>().Build();

        q.Each(e => {
            ref var weapon = ref e.GetMut<MeleeWeaponComponent>();
            weapon = MeleeWeaponComponent.FromConfig(newConfig, weapon.Timer);
        });
    }

    public static void SyncCat(World world, CatConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        Debug.Assert(newConfig != null, "CatConfig は null にできません。");

        // 1. ワールドシングルトンの更新
        world.Set(newConfig);

        // 2. Prefab プレハブの再登録
        PlayerPrefabs.Register(world);

        // 3. アクティブエンティティの同期
        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Cat) return;

        using var query = world.QueryBuilder().With<PlayerTag>().Build();

        query.Each(e =>
        {
            RehydratePlayerEntity(e, newConfig);
        });
    }

    /// <summary>
    ///     Bunny設定の変更をワールド、プレハブ、および生存エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の Bunny 設定</param>
    public static void SyncBunny(World world, BunnyConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        Debug.Assert(newConfig != null, "BunnyConfig は null にできません。");

        // 1. ワールドシングルトンの更新
        world.Set(newConfig);

        // 2. Prefab プレハブの再登録
        PlayerPrefabs.Register(world);

        // 3. アクティブエンティティの同期
        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Bunny) return;

        using var query = world.QueryBuilder().With<PlayerTag>().Build();

        query.Each(e =>
        {
            RehydratePlayerEntity(e, newConfig);
        });
    }

    private static void RehydratePlayerEntity(Entity e, PlayerConfig newConfig)
    {
        // HP の比例更新：現在の割合を維持したまま最大値を更新
        ref var health = ref e.GetMut<Health>();

        var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;

        var newMax = newConfig.MaxHp;

        health.Max = newMax;

        health.Current = Math.Clamp(ratio * newMax, 0.0f, newMax);

        // スケール更新
        ref var trans = ref e.GetMut<Transform>();

        trans.Scale = newConfig.Scale;

        // 移動キネマティクス能力の更新
        ref var loco = ref e.GetMut<LocomotionCapability>();
        loco = LocomotionCapability.FromConfig(newConfig.Movement);

        // 跳躍能力の更新：現在の跳躍状態を保持しつつ上限・物理パラメータを同期
        ref var jump = ref e.GetMut<JumpCapability>();
        var updatedJump = JumpCapability.FromConfig(newConfig.Jump);
        updatedJump.RemainingJumps = Math.Min(jump.RemainingJumps, updatedJump.MaxJumps);
        updatedJump.CoyoteTimer = jump.CoyoteTimer;
        updatedJump.JumpBufferTimer = jump.JumpBufferTimer;
        jump = updatedJump;

        // 突進能力の更新
        if (newConfig.Dash != null)
        {
            e.Set(DashCapability.FromConfig(newConfig.Dash));
            e.Set(new DashState());
        }
        else
        {
            e.Remove<DashCapability>()
             .Remove<DashState>()
             .Remove<DashingTag>();
        }

        // 衝突判定・被弾判定の更新
        ref var col = ref e.GetMut<CollisionBox>();

        col.Size = newConfig.CollisionSize;

        ref var hurt = ref e.GetMut<Hurtbox>();

        hurt.Shape = newConfig.GetHurtboxShape();

        // スプリング拡縮設定の更新
        ref var spring = ref e.GetMut<SpringScale>();

        spring.Frequency = newConfig.SpringFrequency;

        spring.DampingRatio = newConfig.SpringDampingRatio;

        // 無敵時間および被弾フラッシュの更新
        ref var flash = ref e.GetMut<HitFlashSettings>();

        flash.Duration = newConfig.HitFlashDuration;

        // 被弾変形設定の更新
        ref var profile = ref e.GetMut<CombatProfile>();
        profile.HitSquashScale = newConfig.HitSquashScale;
        profile.InvincibleDuration = newConfig.InvincibleDuration;
    }
}
