namespace AnimalBattle.Player.Systems;

/// <summary>
///     特殊能力システムで使用する事前コンパイル済みクエリおよびコンテキストリソース。
/// </summary>
public sealed class PlayerSpecialAbilityContext : IDisposable
{
    public required Query<Transform, Hitbox, Bullet> EnemyBulletQuery { get; init; }

    public void Dispose()
    {
        EnemyBulletQuery.Dispose();
    }
}

/// <summary>
///     自機の特殊能力（必殺技）発動制御・クールダウン・固有挙動を実行する ECS システム。
///     マウス右クリック等による <see cref="SpecialAbilityIntent"/> を検知し、
///     <see cref="EnergyGauge"/> を消費して Bunny の全画面斬撃または Cat の巨型エネルギー弾を展開します。
/// </summary>
public static class PlayerSpecialAbilitySystem
{
    /// <summary>
    ///     プレイヤー特殊能力システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddPlayerSpecialAbilitySystem(this World world)
    {
        var bulletQuery = world.QueryBuilder<Transform, Hitbox, Bullet>()
            .With<EnemyBulletTag>()
            .Without<RecycleTag>()
            .Build();

        world.Set(new PlayerSpecialAbilityContext
        {
            EnemyBulletQuery = bulletQuery
        });

        world.System<Transform, Velocity, SpecialAbilityIntent, EnergyGauge, CharacterTypeComponent, TimedEffectsState, GameplayTagState>("PlayerSpecialAbilitySystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteSpecialAbility);

        return world;
    }

    private static void ExecuteSpecialAbility(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<SpecialAbilityIntent> intents,
        Field<EnergyGauge> gauges,
        Field<CharacterTypeComponent> characterTypes,
        Field<TimedEffectsState> timedEffects,
        Field<GameplayTagState> tagStates)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();
        var ctx = world.Get<PlayerSpecialAbilityContext>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref readonly var trans = ref transforms[i];
            ref var intent = ref intents[i];
            ref var gauge = ref gauges[i];
            ref readonly var charType = ref characterTypes[i];
            ref var effects = ref timedEffects[i];
            ref var tags = ref tagStates[i];

            // 1. 誤爆防止インターバルタイマーの減衰
            if (gauge.CooldownTimer > 0.0f)
            {
                gauge.CooldownTimer = MathF.Max(0.0f, gauge.CooldownTimer - dt);
            }

            var triggered = intent.Triggered;
            intent.Triggered = false;

            if (!triggered || !gauge.CanUseAbility) continue;

            // 2. 1スロット分のエネルギーを消費
            gauge.CurrentEnergy = MathF.Max(0.0f, gauge.CurrentEnergy - gauge.SegmentCapacity);

            EventBus.Publish(new SpecialAbilityExecutedEvent(charType.Value));

            // 3. キャラクター種別ごとの特殊能力実行
            if (charType.Value == CharacterType.Bunny)
            {
                ExecuteBunnyCleave(world, entity, ctx, ref gauge, ref effects, ref tags);
            }
            else if (charType.Value == CharacterType.Cat)
            {
                ExecuteCatGiantOrb(world, in trans, in intent, ref gauge);
            }
        }
    }

    /// <summary>
    ///     Bunny の特殊能力：全画面斬撃（弾幕消去・全敵確定ダメージ・無敵付与・画面振動）。
    /// </summary>
    private static void ExecuteBunnyCleave(
        World world,
        Entity playerEntity,
        PlayerSpecialAbilityContext ctx,
        ref EnergyGauge gauge,
        ref TimedEffectsState effects,
        ref GameplayTagState tags)
    {
        var bunnyWeaponCfg = world.Get<BunnyWeaponConfig>();
        gauge.CooldownTimer = bunnyWeaponCfg.SpecialAbilityCooldown;

        // 全画面敵弾幕の相殺消去
        var bulletCount = 0;
        ctx.EnemyBulletQuery.Each((bulletEntity, ref bt, ref _, ref b) =>
        {
            if (b.IsIndestructible) return;

            bulletEntity.Add<RecycleTag>();
            EventBus.Publish(new BulletCancelledEvent(bt.Position));
            bulletCount++;
        });

        // 全画面連続斬撃エフェクトシーケンスの起動
        var assets = world.Get<IAssetManager>();
        var hasTexture = assets.TryGetTexture(ResourcePath.Characters.SwordShieldEffectSpritesPng, out var slashHandle);
        var slashCfg = world.Get<SlashEffectRenderConfig>();

        world.Entity()
            .Add<VfxTag>()
            .Add<PlayerSpecialAbilityTag>()
            .Set(new ScreenCleaveSequence
            {
                ElapsedTimer = 0.0f,
                TotalDuration = bunnyWeaponCfg.SpecialCleaveDuration,
                NextSpawnTimer = 0.0f,
                SpawnedCount = 0,
                TargetCount = bunnyWeaponCfg.SpecialCleaveSlashCount,
                SlashLifetime = bunnyWeaponCfg.SpecialCleaveSlashLifetime,
                ScaleMin = bunnyWeaponCfg.SpecialCleaveSlashScaleMin,
                ScaleMax = bunnyWeaponCfg.SpecialCleaveSlashScaleMax,
                SlashSpeed = bunnyWeaponCfg.SpecialCleaveSlashSpeed,
                SlashColor = bunnyWeaponCfg.SpecialCleaveSlashColor,
                GraphHandle = hasTexture ? slashHandle : -1,
                CellSize = slashCfg.SpriteCellSize,
                ColIndex = slashCfg.SpriteColIndex,
                TotalDamage = bunnyWeaponCfg.SpecialCleaveDamage,
                TickInterval = bunnyWeaponCfg.SpecialCleaveTickInterval,
                NextDamageTimer = 0.0f,
                DealtDamage = 0.0f
            });

        // 自機の無敵付与と演出効果
        TimedEffectService.ApplyEffect(ref effects, ref tags, GameplayTag.Untargetable, bunnyWeaponCfg.SpecialCleaveInvincibleDuration, playerEntity.Id);
        var blinkFreq = world.Get<PlayerVisualRenderConfig>().InvincibleBlinkFrequency;
        playerEntity.Set(new InvincibleBlinkEffect(bunnyWeaponCfg.SpecialCleaveInvincibleDuration, blinkFreq));

        CameraService.AddTrauma(bunnyWeaponCfg.SpecialCleaveTrauma);
        EventBus.Publish(new ScreenCleaveExecutedEvent(bulletCount));
    }

    /// <summary>
    ///     Cat の特殊能力：巨型エネルギー弾（低速貫通・接触継続跳字ダメージ）。
    /// </summary>
    private static void ExecuteCatGiantOrb(
        World world,
        in Transform trans,
        in SpecialAbilityIntent intent,
        ref EnergyGauge gauge)
    {
        var catWeaponCfg = world.Get<CatWeaponConfig>();
        gauge.CooldownTimer = catWeaponCfg.SpecialAbilityCooldown;

        var dir = intent.AimDirection;
        dir = dir.LengthSquared() < 0.0001f ? new Vec2f(1.0f, 0.0f) : dir.Normalized();

        var spawnPos = trans.Position + dir * 32.0f;
        var orbVel = dir * catWeaponCfg.SpecialOrbSpeed;
        var radius = catWeaponCfg.SpecialOrbRadius;

        world.Entity()
            .IsA(CommonPrefabs.BaseProjectile)
            .Add<PlayerBulletTag>()
            .Add<PlayerSpecialAbilityTag>()
            .Set(new Transform(spawnPos))
            .Set(new Velocity(orbVel))
            .Set(new Hitbox(new CollisionShape.Circle(radius), 0.0f, destroyOnHit: false))
            .Set(new Bullet(radius, catWeaponCfg.SpecialOrbFillColor, catWeaponCfg.SpecialOrbOutlineColor))
            .Set(new ContinuousDamageDealer
            {
                DamagePerSecond = catWeaponCfg.SpecialOrbDamagePerSecond,
                TickInterval = catWeaponCfg.SpecialOrbTickInterval,
                NextTickTimer = 0.0f
            })
            .Set(new HitboxLifetime(catWeaponCfg.SpecialOrbLifetime));

        EventBus.Publish(new PlayerShotFiredEvent(spawnPos, dir));
        CameraService.AddTrauma(0.2f);
    }
}
