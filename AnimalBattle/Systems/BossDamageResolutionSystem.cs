using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Boss 敵対ユニット専用の被弾ダメージ裁定および無敵時間管理を行う単一責任 ECS システム。
///     <see cref="IncomingDamage" /> 意図コンポーネントを消費し、易傷倍率、反撃弾幕、撃破演出およびカメラ衝撃を処理します。
/// </summary>
public static class BossDamageResolutionSystem
{
    /// <summary>
    ///     Boss 敵対ユニット被弾ダメージ裁定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddBossDamageResolutionSystem(this World world)
    {
        world.System<Health, IncomingDamage, HitFlashSettings, Transform, BossCombatState, Sprite, AbilitySystemComponent>("BossDamageResolutionSystem")
            .With<BossTag>()
            .Kind(Ecs.OnValidate)
            .Iter((Iter it,
                Field<Health> hps,
                Field<IncomingDamage> damages,
                Field<HitFlashSettings> flashConfigs,
                Field<Transform> transforms,
                Field<BossCombatState> combats,
                Field<Sprite> sprites,
                Field<AbilitySystemComponent> ascs) =>
            {
                var count = it.Count();
                var pCfg = ConfigManager.Player;
                var cameraCfg = ConfigManager.Camera;

                for (var i = 0; i < count; i++)
                {
                    var entity = it.Entity(i);
                    ref var hp = ref hps[i];
                    ref readonly var dmg = ref damages[i];
                    ref readonly var flashCfg = ref flashConfigs[i];
                    ref readonly var trans = ref transforms[i];
                    ref readonly var combat = ref combats[i];
                    ref var sprite = ref sprites[i];
                    var asc = ascs[i];

                    // 完全無敵または不可命中状態（GAS Invincible / Untargetable 保持時）、死亡済み、またはダメージが 0 以下の場合は意図を破棄してスキップ
                    if (asc.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable) || hp.Current <= 0.0f || dmg.Amount <= 0.0f)
                    {
                        entity.Remove<IncomingDamage>();
                        continue;
                    }

                    // 易傷倍率の評価（GameplayTag.Vulnerable 駆動）
                    var isVulnerable = asc.HasTag(GameplayTag.Vulnerable);
                    var multiplier = isVulnerable ? pCfg.VulnerableDamageMultiplier : 1.0f;
                    var finalDamage = dmg.Amount * multiplier;
                    hp.Current = MathF.Max(0.0f, hp.Current - finalDamage);

                    // 被弾白閃演出の注入
                    entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

                    // ヒットストップおよび受撃局所振動の適用（SuperArmor 剛体タグ未保持時のみ適用）
                    var hasSuperArmor = asc.HasTag(GameplayTag.SuperArmor);
                    if (!hasSuperArmor)
                    {
                        var isHeavy = isVulnerable || dmg.IsHeavyImpact;
                        var hitstopDuration = isHeavy ? pCfg.HitstopHeavyDuration : pCfg.HitstopLightDuration;
                        if (hitstopDuration > 0.0f)
                        {
                            entity.Set(new HitstopState(hitstopDuration));
                            var jitterAmp = isHeavy ? pCfg.HitJitterHeavyAmplitude : pCfg.HitJitterLightAmplitude;
                            entity.Set(new SpriteJitter(jitterAmp, 0.0f, pCfg.HitJitterFrequency));
                        }
                    }

                    if (isVulnerable)
                    {
                        // 易傷特効クリティカル火花
                        EffectSpawner.SpawnHitSparkEffect(world, trans.X - 8.0f, trans.Y - 8.0f);
                        EffectSpawner.SpawnHitSparkEffect(world, trans.X + 8.0f, trans.Y + 8.0f);
                    }

                    // 死亡または生存時反撃・カメラ衝撃判定
                    if (hp.Current <= 0.0f)
                    {
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        BulletSpawner.ClearAllBossBullets(world);
                        EffectSpawner.SpawnVictoryExplosions(world, trans.X, trans.Y);
                        CameraService.AddTrauma(cameraCfg.VictoryExplosionTrauma);
                        EventBus.Publish(new BossDiedEvent());
                    }
                    else
                    {
                        if (isVulnerable)
                        {
                            CameraService.AddTrauma(cameraCfg.BossVulnerableHitTrauma);
                        }

                        EventBus.Publish(new BossDamagedEvent(finalDamage, hp.Current, isVulnerable));

                        // Boss 被弾反撃：現在の Phase に応じた数量の黑色弾幕を発射
                        BulletSpawner.SpawnBossCounterAttack(world, trans.X, trans.Y, combat.Phase);
                    }

                    entity.Remove<IncomingDamage>();
                }
            });

        return world;
    }
}
