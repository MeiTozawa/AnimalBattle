namespace AnimalBattle.Player;



using System;

using System.Diagnostics;

using AnimalBattle.Configs;

using Flecs.NET.Core;



/// <summary>

/// プレイヤー設定変更時に、Flecs.NET ワールドの単例、Prefab、および活体エンティティを無損同期・再水和する同期機構。

/// </summary>

public static class PlayerConfigSync

{

    /// <summary>

    /// Cat遠距離武器設定の変更をワールド、プレハブ、および生存エンティティへ反映します。

    /// </summary>

    /// <summary>

    /// Cat遠距離武器設定の変更をワールド、プレハブ、および生存エンティティへ反映します。

    /// </summary>

    public static void SyncCatWeapon(World world, CatWeaponConfig newConfig)

    {

        world.Set(newConfig);

        PlayerPrefabs.Register(world);

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Cat) return;



        using var q = world.QueryBuilder().With<PlayerTag>().With<RangedWeaponComponent>().Build();

        q.Each((Entity e) => {
            ref var weapon = ref e.GetMut<RangedWeaponComponent>();

            weapon = RangedWeaponComponent.FromConfig(newConfig, weapon.Timer);

        });

    }



    /// <summary>

    /// Bunny近接武器設定の変更をワールド、プレハブ、および生存エンティティへ反映します。

    /// </summary>

    public static void SyncBunnyWeapon(World world, BunnyWeaponConfig newConfig)

    {

        world.Set(newConfig);

        PlayerPrefabs.Register(world);

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Bunny) return;



        using var q = world.QueryBuilder().With<PlayerTag>().With<MeleeWeaponComponent>().Build();

        q.Each((Entity e) => {
            ref var weapon = ref e.GetMut<MeleeWeaponComponent>();

            weapon = MeleeWeaponComponent.FromConfig(newConfig, weapon.Timer);

        });

    }

    public static void SyncCat(World world, CatConfig newConfig)

    {

        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        Debug.Assert(newConfig != null, "CatConfig は null にできません。");



        // 1. ワールド単例の更新

        world.Set(newConfig);



        // 2. Prefab プレハブの再登録

        PlayerPrefabs.Register(world);



        // 3. 活体エンティティの無損再水和

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Cat) return;



        using var query = world.QueryBuilder().With<PlayerTag>().Build();

        query.Each((Entity e) =>

        {

            RehydratePlayerEntity(e, newConfig);

        });

    }



    /// <summary>

    /// Bunny設定の変更をワールド、プレハブ、および生存エンティティへ反映します。

    /// </summary>

    /// <param name="world">対象の Flecs.NET ワールド</param>

    /// <param name="newConfig">更新後の Bunny 設定</param>

    public static void SyncBunny(World world, BunnyConfig newConfig)

    {

        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");

        Debug.Assert(newConfig != null, "BunnyConfig は null にできません。");



        // 1. ワールド単例の更新

        world.Set(newConfig);



        // 2. Prefab プレハブの再登録

        PlayerPrefabs.Register(world);



        // 3. 活体エンティティの無損再水和

        ref readonly var flow = ref world.Get<GameFlow>();

        if (flow.SelectedPlayer != CharacterType.Bunny) return;



        using var query = world.QueryBuilder().With<PlayerTag>().Build();

        query.Each((Entity e) =>

        {

            RehydratePlayerEntity(e, newConfig);

        });

    }



    private static void RehydratePlayerEntity(Entity e, PlayerConfig newConfig)

    {

        // 生命値の比例更新：現在割合を維持したまま MaxHP を更新

        ref var health = ref e.GetMut<Health>();

        var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;

        var newMax = newConfig.MaxHp;

        health.Max = newMax;

        health.Current = Math.Clamp(ratio * newMax, 0.0f, newMax);



        // スケール更新

        ref var trans = ref e.GetMut<Transform>();

        trans.Scale = newConfig.Scale;



        // 跳躍能力の更新：現在の跳躍状態を保持しつつ上限・物理パラメータを同期

        ref var jump = ref e.GetMut<JumpCapability>();

        var updatedJump = JumpCapability.FromPlayerConfig(newConfig);

        updatedJump.RemainingJumps = Math.Min(jump.RemainingJumps, updatedJump.MaxJumps);

        updatedJump.CoyoteTimer = jump.CoyoteTimer;

        updatedJump.JumpBufferTimer = jump.JumpBufferTimer;

        jump = updatedJump;



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

        ref var inv = ref e.GetMut<InvincibleOnHit>();

        inv.Duration = newConfig.InvincibleDuration;



        ref var flash = ref e.GetMut<HitFlashSettings>();

        flash.Duration = newConfig.HitFlashDuration;

        // 被弾変形設定の更新
        ref var profile = ref e.GetMut<CombatProfile>();
        profile.HitSquashScale = newConfig.HitSquashScale;





        {




        }


        {




        }

    }

}

