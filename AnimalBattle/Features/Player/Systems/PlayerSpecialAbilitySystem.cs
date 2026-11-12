namespace AnimalBattle.Player.Systems;

/// <summary>
///     特殊能力システムで使用する事前コンパイル済みクエリおよびコンテキストリソース。
/// </summary>
public sealed class PlayerSpecialAbilityContext : IDisposable
{
    /// <summary>敵弾幕消去判定用の敵弾クエリ</summary>
    public required Query<Transform, Hitbox, Bullet> EnemyBulletQuery { get; init; }

    /// <summary>リソースを解放します。</summary>
    public void Dispose()
    {
        EnemyBulletQuery.Dispose();
    }
}

/// <summary>
///     自機の特殊能力（必殺技）発動制御・クールダウン・固有挙動を実行する ECS システム群。
///     マウス右クリック等による <see cref="SpecialAbilityIntent"/> を検知し、
///     <see cref="EnergyGauge"/> を消費して各能力コンポーネント（<see cref="GiantOrbCapability"/> / <see cref="ScreenCleaveCapability"/>）に基づく必殺技を展開します。
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

        // 1. 誤爆防止インターバルタイマーの減衰システム
        world.System<EnergyGauge>("PlayerSpecialAbilityCooldownSystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(UpdateCooldown);

        // 2. 巨型エネルギー弾特殊能力発動システム
        world.System<Transform, Velocity, SpecialAbilityIntent, EnergyGauge, GiantOrbCapability>("GiantOrbSpecialAbilitySystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteGiantOrbSystem);

        // 3. 全画面斬撃特殊能力発動システム
        world.System<SpecialAbilityIntent, EnergyGauge, ScreenCleaveCapability, TimedEffectsState, GameplayTagState>("ScreenCleaveSpecialAbilitySystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteScreenCleaveSystem);

        return world;
    }

    private static void UpdateCooldown(Iter it, Field<EnergyGauge> gauges)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        for (var i = 0; i < count; i++)
        {
            ref var gauge = ref gauges[i];
            if (gauge.CooldownTimer > 0.0f)
            {
                gauge.CooldownTimer = MathF.Max(0.0f, gauge.CooldownTimer - dt);
            }
        }
    }

    private static void ExecuteGiantOrbSystem(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<SpecialAbilityIntent> intents,
        Field<EnergyGauge> gauges,
        Field<GiantOrbCapability> capabilities)
    {
        var count = it.Count();
        if (count == 0) return;

        var currentWorld = it.World();
        for (var i = 0; i < count; i++)
        {
            ref var intent = ref intents[i];
            ref var gauge = ref gauges[i];

            var triggered = intent.Triggered;
            intent.Triggered = false;

            if (!triggered || !gauge.CanUseAbility) continue;

            ref readonly var orb = ref capabilities[i];
            gauge.CurrentEnergy = MathF.Max(0.0f, gauge.CurrentEnergy - gauge.SegmentCapacity);
            gauge.CooldownTimer = orb.Cooldown;

            ref readonly var trans = ref transforms[i];
            ExecuteGiantOrb(currentWorld, in trans, in intent, in orb);
            EventBus.Publish(new SpecialAbilityExecutedEvent("GiantOrb"));
        }
    }

    private static void ExecuteScreenCleaveSystem(
        Iter it,
        Field<SpecialAbilityIntent> intents,
        Field<EnergyGauge> gauges,
        Field<ScreenCleaveCapability> capabilities,
        Field<TimedEffectsState> timedEffects,
        Field<GameplayTagState> tagStates)
    {
        var count = it.Count();
        if (count == 0) return;

        var currentWorld = it.World();
        var ctx = currentWorld.Get<PlayerSpecialAbilityContext>();

        for (var i = 0; i < count; i++)
        {
            ref var intent = ref intents[i];
            ref var gauge = ref gauges[i];

            var triggered = intent.Triggered;
            intent.Triggered = false;

            if (!triggered || !gauge.CanUseAbility) continue;

            ref readonly var cleave = ref capabilities[i];
            gauge.CurrentEnergy = MathF.Max(0.0f, gauge.CurrentEnergy - gauge.SegmentCapacity);
            gauge.CooldownTimer = cleave.Cooldown;

            var playerEntity = it.Entity(i);
            ref var effects = ref timedEffects[i];
            ref var tags = ref tagStates[i];

            ExecuteScreenCleave(currentWorld, playerEntity, ctx, in cleave, ref effects, ref tags);
            EventBus.Publish(new SpecialAbilityExecutedEvent("ScreenCleave"));
        }
    }

    /// <summary>
    ///     全画面斬撃特殊能力（敵弾消去・連続斬撃シーケンス生成・自機無敵化）を実行します。
    /// </summary>
    private static void ExecuteScreenCleave(
        World world,
        Entity playerEntity,
        PlayerSpecialAbilityContext ctx,
        in ScreenCleaveCapability cleave,
        ref TimedEffectsState effects,
        ref GameplayTagState tags)
    {
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
                TotalDuration = cleave.TotalDuration,
                NextSpawnTimer = 0.0f,
                SpawnedCount = 0,
                TargetCount = cleave.SlashCount,
                SlashLifetime = cleave.SlashLifetime,
                ScaleMin = cleave.ScaleMin,
                ScaleMax = cleave.ScaleMax,
                SlashSpeed = cleave.SlashSpeed,
                SlashColor = cleave.SlashColor,
                GraphHandle = hasTexture ? slashHandle : -1,
                CellSize = slashCfg.SpriteCellSize,
                ColIndex = slashCfg.SpriteColIndex,
                TotalDamage = cleave.TotalDamage,
                TickInterval = cleave.TickInterval,
                NextDamageTimer = 0.0f,
                DealtDamage = 0.0f,
                NextSfxTimer = 0.0f,
                SfxInterval = cleave.TickInterval
            });

        // 自機の無敵付与と演出効果
        TimedEffectService.ApplyEffect(ref effects, ref tags, GameplayTag.Untargetable, cleave.InvincibleDuration, playerEntity.Id);
        var blinkFreq = world.Get<PlayerVisualRenderConfig>().InvincibleBlinkFrequency;
        playerEntity.Set(new InvincibleBlinkEffect(cleave.InvincibleDuration, blinkFreq));

        CameraService.AddTrauma(cleave.CameraTrauma);
        EventBus.Publish(new ScreenCleaveExecutedEvent(bulletCount));
    }

    /// <summary>
    ///     巨型エネルギー弾特殊能力（低速貫通・接触継続判定生成）を実行します。
    /// </summary>
    private static void ExecuteGiantOrb(
        World world,
        in Transform trans,
        in SpecialAbilityIntent intent,
        in GiantOrbCapability orb)
    {
        var dir = intent.AimDirection;
        dir = dir.LengthSquared() < 0.0001f ? new Vec2f(1.0f, 0.0f) : dir.Normalized();

        var spawnPos = trans.Position + dir * 32.0f;
        var orbVel = dir * orb.Speed;
        var radius = orb.Radius;

        world.Entity()
            .IsA(CommonPrefabs.BaseProjectile)
            .Add<PlayerBulletTag>()
            .Add<PlayerSpecialAbilityTag>()
            .Set(new Transform(spawnPos))
            .Set(new Velocity(orbVel))
            .Set(new Hitbox(new CollisionShape.Circle(radius), 0.0f, destroyOnHit: false))
            .Set(new Bullet(radius, orb.FillColor, orb.OutlineColor))
            .Set(new ContinuousDamageDealer
            {
                DamagePerSecond = orb.DamagePerSecond,
                TickInterval = orb.TickInterval,
                NextTickTimer = 0.0f
            })
            .Set(new HitboxLifetime(orb.Lifetime));

        EventBus.Publish(new PlayerShotFiredEvent(spawnPos, dir));
        CameraService.AddTrauma(0.2f);
    }
}
