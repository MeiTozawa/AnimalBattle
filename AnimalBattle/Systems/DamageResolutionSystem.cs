using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     純粋な ECS ダメージ裁定システム。
///     衝突検出（幾何交差）からダメージ適用・無敵時間・演出付与を完全分離し、
///     <see cref="IncomingDamage" /> 意図コンポーネントを消費して一元処理します。
/// </summary>
public static class DamageResolutionSystem
{
    /// <summary>
    ///     ダメージ裁定関連の ECS システム（無敵タイマー減衰およびダメージ意図消費）を登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddDamageResolutionSystem(this World world)
    {
        // 1. 無敵タイマーの毎フレーム自然減衰（全 Health コンポーネントを一括走査）
        world.System<Health>("HealthInvincibilitySystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<Health> hps) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ref var hp = ref hps[i];
                    if (hp.InvincibleTimer > 0.0f)
                    {
                        hp.InvincibleTimer = Math.Max(0.0f, hp.InvincibleTimer - dt);
                    }
                }
            });

        // 2. ダメージ意図の集中裁定（IncomingDamage を持つエンティティのみを処理）
        world.System<Health, IncomingDamage, InvincibleOnHit, HitFlashConfig, Transform>("DamageResolutionSystem")
            .Kind(Ecs.OnValidate)
            .Iter((Iter it, Field<Health> hps, Field<IncomingDamage> damages, Field<InvincibleOnHit> invConfigs, Field<HitFlashConfig> flashConfigs, Field<Transform> transforms) =>
            {
                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    var entity = it.Entity(i);
                    ref var hp = ref hps[i];
                    ref readonly var dmg = ref damages[i];
                    ref readonly var invCfg = ref invConfigs[i];
                    ref readonly var flashCfg = ref flashConfigs[i];
                    ref readonly var trans = ref transforms[i];

                    // 無敵判定は Health.InvincibleTimer のみを真実の源として評価
                    if (hp.InvincibleTimer > 0.0f || hp.Current <= 0.0f || dmg.Amount <= 0.0f)
                    {
                        entity.Remove<IncomingDamage>();
                        continue;
                    }

                    // 易傷倍率の取得（BossCombatState が存在する場合に評価）
                    var multiplier = 1.0f;
                    var isVulnerable = false;
                    if (entity.Has<BossCombatState>())
                    {
                        ref readonly var combat = ref entity.Get<BossCombatState>();
                        if (combat.IsVulnerable)
                        {
                            multiplier = ConfigManager.Player.VulnerableDamageMultiplier;
                            isVulnerable = true;
                        }
                    }

                    var finalDamage = dmg.Amount * multiplier;
                    hp.Current = Math.Max(0.0f, hp.Current - finalDamage);

                    // クエリから直接取得した設定値で無敵時間と被弾白閃演出を注入
                    hp.InvincibleTimer = invCfg.Duration;
                    entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

                    // ヒットストップおよび受撃局所振動の適用
                    var isHeavy = isVulnerable || dmg.IsHeavyImpact;
                    var hitstopDuration = isHeavy
                        ? ConfigManager.Player.HitstopHeavyDuration
                        : ConfigManager.Player.HitstopLightDuration;

                    if (hitstopDuration > 0.0f)
                    {
                        entity.Set(new HitstopState(hitstopDuration));
                        var jitterAmp = isHeavy ? 4.0f : 2.0f;
                        entity.Set(new SpriteJitter(jitterAmp, 0.0f, 30.0f));
                    }

                    // ノックバック物理インパルスの適用
                    if (entity.Has<PlayerTag>() && entity.Has<Velocity>())
                    {
                        ref var vel = ref entity.GetMut<Velocity>();
                        vel.Vx = dmg.KnockbackX;
                        if (dmg.KnockbackY < 0.0f)
                        {
                            vel.Vy = dmg.KnockbackY;
                            if (entity.Has<CollisionBox>())
                            {
                                entity.GetMut<CollisionBox>().IsGrounded = false;
                            }
                        }
                    }

                    if (isVulnerable)
                    {
                        // 易傷特効クリティカル火花
                        CombatEffectHelper.SpawnHitSparkEffect(world, trans.X - 8.0f, trans.Y - 8.0f);
                        CombatEffectHelper.SpawnHitSparkEffect(world, trans.X + 8.0f, trans.Y + 8.0f);
                    }

                    // 死亡または反撃判定
                    var cameraCfg = ConfigManager.Camera;
                    if (hp.Current <= 0.0f)
                    {
                        entity.Add<DeadTag>();
                        if (entity.Has<Sprite>())
                        {
                            entity.GetMut<Sprite>().IsVisible = false;
                        }

                        if (entity.Has<PlayerTag>())
                        {
                            CombatEffectHelper.SpawnPlayerDeathParticles(world, trans.X, trans.Y);
                            CameraService.AddTrauma(cameraCfg.PlayerHeavyHitTrauma);
                        }
                        else if (entity.Has<BossTag>())
                        {
                            CombatEffectHelper.ClearAllBossBullets(world);
                            CombatEffectHelper.SpawnVictoryExplosions(world, trans.X, trans.Y);
                            CameraService.AddTrauma(cameraCfg.VictoryExplosionTrauma);
                        }
                    }
                    else
                    {
                        if (entity.Has<PlayerTag>())
                        {
                            var trauma = isHeavy ? cameraCfg.PlayerHeavyHitTrauma : cameraCfg.PlayerHitTrauma;
                            CameraService.AddTrauma(trauma);
                            CameraService.AddDirectionalImpulse(dmg.KnockbackX, dmg.KnockbackY, cameraCfg.DirectionalImpulseRatio);
                        }
                        else if (isVulnerable)
                        {
                            CameraService.AddTrauma(cameraCfg.BossVulnerableHitTrauma);
                        }

                        if (entity.Has<BossCombatState>())
                        {
                            ref readonly var combat = ref entity.Get<BossCombatState>();
                            // Boss 被弾反撃：現在の Phase に応じた数量の黑色弾幕を発射
                            CombatEffectHelper.SpawnBossCounterAttack(world, trans.X, trans.Y, combat.Phase);
                        }
                    }

                    entity.Remove<IncomingDamage>();
                }
            });

        return world;
    }
}
