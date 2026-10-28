using AnimalBattle.Combat.Components;
using AnimalBattle.Combat.Events;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     全戦闘ユニット共通の被弾ダメージ裁定および無敵時間・撃退・死亡処理を統括する単一責任 ECS システム。
///     <see cref="DamageInbox" /> 内の被弾イベントを集約・消費し、データ駆動の <see cref="CombatProfile" /> に従って
///     易傷倍率、反撃弾幕、被弾無敵、撃退、撃破演出およびカメラ衝撃を統一的に処理します。
/// </summary>
public static class DamageResolutionSystem
{
    /// <summary>
    ///     戦闘ダメージ裁定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddDamageResolutionSystem(this World world)
    {
        world.System<Health, HitFlashSettings, Transform, Velocity, CollisionBox, SpringScale, Sprite, GameplayTagState, TimedEffectsState, CombatProfile>("DamageResolutionSystem")
            .Kind(Ecs.OnValidate)
            .Iter(ExecuteDamageResolution);

        return world;
    }

    /// <summary>
    ///     戦闘ダメージ裁定パイプライン。
    /// </summary>
    public static void ExecuteDamageResolution(
        Iter it,
        Field<Health> hps,
        Field<HitFlashSettings> flashConfigs,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<SpringScale> springScales,
        Field<Sprite> sprites,
        Field<GameplayTagState> tags,
        Field<TimedEffectsState> timedEffects,
        Field<CombatProfile> profiles)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        ref var inbox = ref world.GetMut<DamageInbox>();
        var events = inbox.AsSpan();
        if (events.IsEmpty) return;

        ref readonly var cameraCfg = ref world.Get<CameraConfig>();
        var pCfg = world.GetActivePlayerConfig();
        var isGameOver = world.Has<GameOverTag>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref readonly var profile = ref profiles[i];

            // 被弾イベントを集約
            var totalDamage = 0.0f;
            var targetKnockback = Vec2f.Zero;
            var isHeavy = false;
            var hasHit = false;

            for (var j = 0; j < events.Length; j++)
            {
                ref readonly var evt = ref events[j];
                if (evt.Victim != entity) continue;

                hasHit = true;

                // 敵対判定から自機への同一フレーム多重被弾は最大値を採用して即死を防止
                if (profile.Kind == CombatEntityKind.Player && evt.SourceFaction == Faction.Hostile)
                {
                    totalDamage = MathF.Max(totalDamage, evt.Damage);
                }
                else
                {
                    totalDamage += evt.Damage;
                }

                isHeavy |= evt.IsHeavy;
                if (evt.IsHeavy || targetKnockback == Vec2f.Zero)
                {
                    targetKnockback = evt.Knockback;
                }
            }

            // 未受撃または無効なダメージの場合はスキップ
            if (!hasHit || totalDamage <= 0.0f) continue;

            ref var hp = ref hps[i];
            ref var tagState = ref tags[i];

            // ゲームオーバー済み、無敵状態、または死亡済みの場合はスキップ
            if (isGameOver || tagState.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable) || hp.Current <= 0.0f)
            {
                continue;
            }

            ref readonly var flashCfg = ref flashConfigs[i];
            ref readonly var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref var spring = ref springScales[i];
            ref var sprite = ref sprites[i];
            ref var effectState = ref timedEffects[i];

            // 敵対系ユニットの易傷倍率評価
            var isVulnerable = tagState.HasTag(GameplayTag.Vulnerable);
            var multiplier = (profile.Kind != CombatEntityKind.Player && isVulnerable) ? pCfg.VulnerableDamageMultiplier : 1.0f;
            var finalDamage = totalDamage * multiplier;

            var prevHp = hp.Current;
            hp.Current = MathF.Max(0.0f, hp.Current - finalDamage);

            // 被弾白閃演出
            entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

            // ヒットストップ時間・微小振動・ノックバックの付与
            var hasSuperArmor = tagState.HasTag(GameplayTag.SuperArmor);
            if (!hasSuperArmor || isVulnerable || profile.Kind == CombatEntityKind.Player)
            {
                var heavy = (profile.Kind != CombatEntityKind.Player && isVulnerable) || isHeavy;
                var hitstopDuration = heavy ? pCfg.HitstopHeavyDuration : pCfg.HitstopLightDuration;
                if (hitstopDuration > 0.0f)
                {
                    entity.Set(new HitstopState(hitstopDuration));
                    var jitterAmp = heavy ? pCfg.HitJitterHeavyAmplitude : pCfg.HitJitterLightAmplitude;
                    entity.Set(new SpriteJitter(new Vec2f(jitterAmp, 0.0f), pCfg.HitJitterFrequency));
                }

                if (profile.CanBeKnockedBack)
                {
                    vel.Knockback = targetKnockback;
                    if (targetKnockback.Y < 0.0f)
                    {
                        col.IsGrounded = false;
                    }
                }
            }

            // 被弾スカッシュ変形の適用
            spring.Current = pCfg.HitSquashScale;
            spring.Velocity = Vec2f.Zero;

            // ロール固有の被弾リアクション
            if (profile.Kind == CombatEntityKind.Player)
            {
                if (profile.InvincibleDuration > 0.0f)
                {
                    TimedEffectService.ApplyEffect(ref effectState, ref tagState, GameplayTag.Untargetable, profile.InvincibleDuration, entity.Id);
                    var blinkFreq = world.Get<RenderConfig>().Player.InvincibleBlinkFrequency;
                    entity.Set(new InvincibleBlinkEffect(profile.InvincibleDuration, blinkFreq));
                }
                EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hp.Current, hp.Max));
            }
            else if (isVulnerable)
            {
                // 敵対ユニット易傷クリティカル火花
                EffectSpawner.SpawnHitSparkEffect(world, trans.Position - new Vec2f(8.0f, 8.0f));
                EffectSpawner.SpawnHitSparkEffect(world, trans.Position + new Vec2f(8.0f, 8.0f));
            }

            // 死亡または生存時の挙動分岐
            if (hp.Current <= 0.0f)
            {
                switch (profile.Kind)
                {
                    case CombatEntityKind.Player:
                        world.Add<GameOverTag>();
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        var melee = entity.Target<EquippedWeapon>();
                        if (melee.IsAlive())
                        {
                            melee.Disable();
                            melee.GetMut<Sprite>().IsVisible = false;
                        }

                        EffectSpawner.SpawnPlayerDeathParticles(world, trans.Position);
                        CameraService.AddTrauma(cameraCfg.PlayerHeavyHitTrauma);
                        EventBus.Publish(new PlayerDiedEvent());
                        break;

                    case CombatEntityKind.Boss:
                        world.Add<GameOverTag>();
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        entity.GetMut<Hitbox>().IsActive = false;
                        BulletSpawner.ClearAllBossBullets(world);
                        MeleeSpawner.ClearAllBossMelee(world);
                        EffectSpawner.SpawnVictoryExplosions(world, trans.Position);
                        CameraService.AddTrauma(cameraCfg.VictoryExplosionTrauma);
                        EventBus.Publish(new BossDiedEvent());
                        break;

                    case CombatEntityKind.Enemy:
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        entity.GetMut<Hitbox>().IsActive = false;
                        EffectSpawner.SpawnHitSparkEffect(world, trans.Position);
                        entity.Destruct();
                        break;

                    case CombatEntityKind.Dummy:
                        // 木桩：HPを即時満タンにリセットして継続テスト可能を維持
                        hp.Current = hp.Max;
                        break;
                }
            }
            else
            {
                switch (profile.Kind)
                {
                    case CombatEntityKind.Player:
                        var trauma = isHeavy ? cameraCfg.PlayerHeavyHitTrauma : cameraCfg.PlayerHitTrauma;
                        CameraService.AddTrauma(trauma);
                        CameraService.AddDirectionalImpulse(targetKnockback, cameraCfg.DirectionalImpulseRatio);
                        EventBus.Publish(new PlayerDamagedEvent(totalDamage, hp.Current, isHeavy));
                        break;

                    case CombatEntityKind.Boss:
                        if (isVulnerable)
                        {
                            CameraService.AddTrauma(cameraCfg.BossVulnerableHitTrauma);
                        }

                        EventBus.Publish(new BossDamagedEvent(finalDamage, hp.Current, isVulnerable));

                        // 被弾反撃弾幕の生成
                        if (profile.HasCounterAttack)
                        {
                            BulletSpawner.SpawnCounterAttack(world, trans.Position, profile.Phase);
                        }
                    break;
                }
            }
        }
    }
}
