using AnimalBattle.Combat.Events;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     ダメージ裁定システムで使用する事前コンパイル済みクエリおよびコンテキストリソース。
/// </summary>
internal sealed class DamageResolutionContext : IDisposable
{
    /// <summary>自機近接判定エンティティの制御クエリ。</summary>
    public required Query PlayerMeleeQuery { get; init; }

    /// <summary>Boss 制御コンポーネント抽出クエリ。</summary>
    public required Query<BossBrainComponent> BossBrainQuery { get; init; }

    public void Dispose()
    {
        PlayerMeleeQuery.Dispose();
        BossBrainQuery.Dispose();
    }
}

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
        var playerMeleeQuery = world.QueryBuilder()
            .With<PlayerMeleeTag>()
            .Build();

        var bossBrainQuery = world.QueryBuilder<BossBrainComponent>()
            .With<BossTag>()
            .Build();

        world.SetDisposable(new DamageResolutionContext
        {
            PlayerMeleeQuery = playerMeleeQuery,
            BossBrainQuery = bossBrainQuery
        });

        // 出現バッファ中のエンティティはダメージ裁定対象から除外（無敵保証）
        world.System<Health, HitFlashSettings, Transform, Velocity, CollisionBox, SpringScale, Sprite, GameplayTagState, TimedEffectsState, CombatProfile>("DamageResolutionSystem")
            .Without<SpawnBufferState>()
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

        var combatCfg = world.Get<CombatPrefabConfig>();
        var physicsCfg = combatCfg.Physics;
        var isGameOver = world.Has<GameOverTag>();
        var ctx = world.Get<DamageResolutionContext>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref readonly var profile = ref profiles[i];

            // 被弾イベントを集約
            var totalDamage = 0.0f;
            var playerDamageDealt = 0.0f;
            var targetKnockback = Vec2f.Zero;
            var isHeavy = false;
            var hasHit = false;

            foreach (var t in events)
            {
                ref readonly var evt = ref t;
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

                if (evt.SourceFaction == Faction.Friendly)
                {
                    playerDamageDealt += evt.Damage;
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
            var multiplier = (profile.Kind != CombatEntityKind.Player && isVulnerable) ? physicsCfg.VulnerableDamageMultiplier : 1.0f;
            var finalDamage = totalDamage * multiplier;

            // プレイヤーが敵に与えたダメージに応じて自機エネルギーゲージを加算（イベント通知）
            var finalPlayerDamage = playerDamageDealt * multiplier;
            if (profile.Kind != CombatEntityKind.Player && finalPlayerDamage > 0.0f)
            {
                EventBus.Publish(new PlayerDealtDamageEvent(finalPlayerDamage));
            }

            var prevHp = hp.Current;
            hp.Current = MathF.Max(0.0f, hp.Current - finalDamage);

            // 被弾白閃演出
            entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

            // ヒットストップ時間・微小振動・ノックバックの付与
            var hasSuperArmor = tagState.HasTag(GameplayTag.SuperArmor);
            if (!hasSuperArmor || isVulnerable || profile.Kind == CombatEntityKind.Player)
            {
                var heavy = (profile.Kind != CombatEntityKind.Player && isVulnerable) || isHeavy;
                var hitstopDuration = heavy ? physicsCfg.HitstopHeavyDuration : physicsCfg.HitstopLightDuration;
                if (hitstopDuration > 0.0f)
                {
                    entity.Set(new HitstopState(hitstopDuration));
                    var jitterAmp = heavy ? physicsCfg.HitJitterHeavyAmplitude : physicsCfg.HitJitterLightAmplitude;
                    entity.Set(new SpriteJitter(new Vec2f(jitterAmp, 0.0f), physicsCfg.HitJitterFrequency));
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
            var hitSquash = profile.HitSquashScale != Vec2f.Zero ? profile.HitSquashScale : physicsCfg.DefaultHitSquashScale;
            spring.Current = hitSquash;
            spring.Velocity = Vec2f.Zero;

            // ロール固有の被弾リアクション
            if (profile.Kind == CombatEntityKind.Player)
            {
                if (profile.InvincibleDuration > 0.0f)
                {
                    TimedEffectService.ApplyEffect(ref effectState, ref tagState, GameplayTag.Untargetable, profile.InvincibleDuration, entity.Id);
                    var blinkFreq = world.Get<PlayerVisualRenderConfig>().InvincibleBlinkFrequency;
                    entity.Set(new InvincibleBlinkEffect(profile.InvincibleDuration, blinkFreq));
                }
                EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hp.Current));
            }
            else if (isVulnerable)
            {
                // 敵対ユニット易傷クリティカル火花
                EventBus.Publish(new CombatHitLandedEvent(trans.Position, IsHeavy: true));
            }
            else if (hasSuperArmor && profile.Kind == CombatEntityKind.Boss)
            {
                // スーパーアーマー（覇体）被弾時の装甲火花・偏向エフェクト
                EventBus.Publish(new SuperArmorDeflectedEvent(trans.Position));
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
                        ctx.PlayerMeleeQuery.Each(static (Entity e) => e.Disable());

                        EventBus.Publish(new PlayerDiedEvent(trans.Position));
                        break;

                    case CombatEntityKind.Boss:
                        world.Add<GameOverTag>();
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;

                        ctx.BossBrainQuery.Each(static (ref BossBrainComponent b) =>
                        {
                            if (!b.Brain.IsDead)
                            {
                                b.Brain.Kill();
                            }
                        });

                        BulletSpawner.ClearAllEnemyBullets(world);
                        MeleeSpawner.ClearAllEnemyMelee(world);
                        MinionSpawner.ClearAllMinionsImmediate(world);
                        EventBus.Publish(new BossDiedEvent(trans.Position));
                        break;

                    case CombatEntityKind.Minion:
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        EventBus.Publish(new CombatHitLandedEvent(trans.Position));
                        // 雑魚敵撃破時の回復アイテムドロップ判定（イベント通知）
                        EventBus.Publish(new EnemyDamagedEvent(trans.Position, hp.Current, hp.Max, finalDamage));
                        entity.Destruct();
                        break;

                    case CombatEntityKind.Dummy:
                        // トレーニングダミー：HPを即座に全回復して継続検証を維持
                        hp.Current = hp.Max;
                        break;
                }
            }
            else
            {
                switch (profile.Kind)
                {
                    case CombatEntityKind.Player:
                        EventBus.Publish(new PlayerDamagedEvent(finalDamage, targetKnockback, isHeavy));
                        break;

                    case CombatEntityKind.Boss:
                        EventBus.Publish(new BossDamagedEvent(isVulnerable));

                        // 被弾反撃弾幕の生成
                        if (profile.HasCounterAttack)
                        {
                            BulletSpawner.SpawnCounterAttack(world, trans.Position, profile.Phase);
                        }

                        // ボス被弾時の回復アイテムドロップ判定（イベント通知）
                        EventBus.Publish(new EnemyDamagedEvent(trans.Position, hp.Current, hp.Max, finalDamage));
                        break;

                    case CombatEntityKind.Minion:
                    case CombatEntityKind.Dummy:
                        // 雑魚敵およびダミー被弾時の回復アイテムドロップ判定（イベント通知）
                        EventBus.Publish(new EnemyDamagedEvent(trans.Position, hp.Current, hp.Max, finalDamage));
                        break;
                }
            }
        }
    }
}
