namespace AnimalBattle.Player;

/// <summary>
///     プレイヤー設定変更時に、Flecs.NET ワールドの Prefab およびアクティブエンティティをホットリロード・同期する機構。
/// </summary>
public static class PlayerConfigSync
{
    /// <summary>
    ///     Cat遠距離武器設定の変更をプレハブへ反映します。
    /// </summary>
    public static void SyncCatWeapon(World world, RangedWeaponConfig newConfig)
    {
        world.Set(newConfig);

        if (WeaponPrefabs.CatWeapon.IsAlive())
        {
            WeaponPrefabs.CatWeapon.Set(RangedWeaponComponent.FromConfig(newConfig));
        }
    }

    /// <summary>
    ///     Bunny近接武器設定の変更をプレハブへ反映します。
    /// </summary>
    public static void SyncBunnyWeapon(World world, MeleeWeaponConfig newConfig)
    {
        world.Set(newConfig);

        if (WeaponPrefabs.BunnyWeapon.IsAlive())
        {
            WeaponPrefabs.BunnyWeapon.Set(MeleeWeaponComponent.FromConfig(newConfig));
        }
    }

    /// <summary>
    ///     Cat特殊能力設定（巨型エネルギー弾）の変更をプレハブおよび生存エンティティへ反映します。
    /// </summary>
    public static void SyncGiantOrb(World world, GiantOrbAbilityConfig newConfig)
    {
        world.Set(newConfig);

        if (SpecialAbilityPrefabs.GiantOrb.IsAlive())
        {
            SpecialAbilityPrefabs.GiantOrb.Set(GiantOrbCapability.FromConfig(newConfig));
        }
    }

    /// <summary>
    ///     Bunny特殊能力設定（全画面斬撃）の変更をプレハブおよび生存エンティティへ反映します。
    /// </summary>
    public static void SyncScreenCleave(World world, ScreenCleaveAbilityConfig newConfig)
    {
        world.Set(newConfig);

        if (SpecialAbilityPrefabs.ScreenCleave.IsAlive())
        {
            SpecialAbilityPrefabs.ScreenCleave.Set(ScreenCleaveCapability.FromConfig(newConfig));
        }
    }

    /// <summary>
    ///     Cat設定の変更をプレハブおよび生存エンティティへ反映します。
    /// </summary>
    public static void SyncCat(World world, PlayerConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "PlayerConfig は null にできません。");

        // 1. Prefab プレハブの再設定
        if (PlayerPrefabs.CatPlayer.IsAlive())
        {
            PlayerPrefabs.ConfigureCat(newConfig, world);
        }

        // 2. アクティブエンティティの同期
        ref readonly var flow = ref world.Get<GameFlow>();
        if (flow.SelectedPlayer != CharacterType.Cat) return;

        using var query = world.QueryBuilder().With<PlayerTag>().Build();
        query.Each(e =>
        {
            RehydratePlayerEntity(world, e, newConfig);
        });
    }

    /// <summary>
    ///     Bunny設定の変更をプレハブおよび生存エンティティへ反映します。
    /// </summary>
    public static void SyncBunny(World world, PlayerConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "PlayerConfig は null にできません。");

        // 1. Prefab プレハブの再設定
        if (PlayerPrefabs.BunnyPlayer.IsAlive())
        {
            PlayerPrefabs.ConfigureBunny(newConfig, world);
        }

        // 2. アクティブエンティティの同期
        ref readonly var flow = ref world.Get<GameFlow>();
        if (flow.SelectedPlayer != CharacterType.Bunny) return;

        using var query = world.QueryBuilder().With<PlayerTag>().Build();
        query.Each(e =>
        {
            RehydratePlayerEntity(world, e, newConfig);
        });
    }

    private static void RehydratePlayerEntity(World world, Entity e, PlayerConfig newConfig)
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
            e.Set(DashCapability.FromConfig(newConfig.Dash)).Ensure<DashState>();
        }
        else
        {
            e.Remove<DashCapability>()
             .Remove<DashState>()
             .Remove<DashingTag>();
        }

        // エネルギーゲージの容量・チャージレート更新
        ref var gauge = ref e.GetMut<EnergyGauge>();
        gauge.MaxEnergy = newConfig.EnergyMaxSegments * newConfig.EnergySegmentCapacity;
        gauge.SegmentCapacity = newConfig.EnergySegmentCapacity;
        gauge.EnergyPerDamageDealt = newConfig.EnergyPerDamageDealt;
        gauge.EnergyPerDamageTaken = newConfig.EnergyPerDamageTaken;

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

        // 武器設定の更新
        if (!string.IsNullOrEmpty(newConfig.Weapon))
        {
            WeaponPrefabs.Attach(e, newConfig.Weapon);
        }

        // 特殊能力設定の更新
        if (!string.IsNullOrEmpty(newConfig.SpecialAbility))
        {
            SpecialAbilityPrefabs.Attach(e, newConfig.SpecialAbility);
        }
    }
}
