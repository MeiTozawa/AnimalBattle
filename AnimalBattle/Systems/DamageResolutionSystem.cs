using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     Boss 敵対ユニット専用の被弾ダメージ裁定および無敵時間管理を行う ECS システム。
///     <see cref="IncomingDamage" /> 意図コンポーネントを消費し、易傷倍率、反撃弾幕、撃破演出およびカメラ衝撃を処理します。
/// </summary>
public static class DamageResolutionSystem
{
    /// <summary>
    ///     全ダメージ裁定関連の ECS システム（無敵タイマー減衰、自機被弾裁定、Boss 被弾裁定）を一括登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddDamageResolutionSystem(this World world)
    {
        return world.AddHealthInvincibilitySystem()
            .AddPlayerDamageResolutionSystem()
            .AddBossDamageResolutionSystem();
    }

    /// <summary>
    ///     無敵タイマー自然減衰システムを登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddHealthInvincibilitySystem(this World world)
    {
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
                        hp.InvincibleTimer = MathF.Max(0.0f, hp.InvincibleTimer - dt);
                    }
                }
            });

        return world;
    }

    /// <summary>
    ///     Boss 敵対ユニット被弾ダメージ裁定システムを登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBossDamageResolutionSystem(this World world)
    {
        world.System<Health, IncomingDamage, InvincibleOnHit, HitFlashConfig, Transform, BossCombatState, Sprite>("BossDamageResolutionSystem")
            .With<BossTag>()
            .Kind(Ecs.OnValidate)
            .Iter((Iter it,
                Field<Health> hps,
                Field<IncomingDamage> damages,
                Field<InvincibleOnHit> invConfigs,
                Field<HitFlashConfig> flashConfigs,
                Field<Transform> transforms,
                Field<BossCombatState> combats,
                Field<Sprite> sprites) =>
            {
                var count = it.Count();
                var pCfg = ConfigManager.Player;
                var cameraCfg = ConfigManager.Camera;

                for (var i = 0; i < count; i++)
                {
                    var entity = it.Entity(i);
                    ref var hp = ref hps[i];
                    ref readonly var dmg = ref damages[i];
                    ref readonly var invCfg = ref invConfigs[i];
                    ref readonly var flashCfg = ref flashConfigs[i];
                    ref readonly var trans = ref transforms[i];
                    ref readonly var combat = ref combats[i];
                    ref var sprite = ref sprites[i];

                    // 無敵時間中、死亡済み、またはダメージが 0 以下の場合は意図を破棄してスキップ
                    if (hp.InvincibleTimer > 0.0f || hp.Current <= 0.0f || dmg.Amount <= 0.0f)
                    {
                        entity.Remove<IncomingDamage>();
                        continue;
                    }

                    // 易傷倍率の評価
                    var isVulnerable = combat.IsVulnerable;
                    var multiplier = isVulnerable ? pCfg.VulnerableDamageMultiplier : 1.0f;
                    var finalDamage = dmg.Amount * multiplier;
                    hp.Current = MathF.Max(0.0f, hp.Current - finalDamage);

                    // 無敵時間および被弾白閃演出の注入
                    hp.InvincibleTimer = invCfg.Duration;
                    entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

                    // ヒットストップおよび受撃局所振動の適用
                    var isHeavy = isVulnerable || dmg.IsHeavyImpact;
                    var hitstopDuration = isHeavy ? pCfg.HitstopHeavyDuration : pCfg.HitstopLightDuration;
                    if (hitstopDuration > 0.0f)
                    {
                        entity.Set(new HitstopState(hitstopDuration));
                        entity.Set(new SpriteJitter(isHeavy ? 4.0f : 2.0f, 0.0f, 30.0f));
                    }

                    if (isVulnerable)
                    {
                        // 易傷特効クリティカル火花
                        CombatEffectHelper.SpawnHitSparkEffect(world, trans.X - 8.0f, trans.Y - 8.0f);
                        CombatEffectHelper.SpawnHitSparkEffect(world, trans.X + 8.0f, trans.Y + 8.0f);
                    }

                    // 死亡または生存時反撃・カメラ衝撃判定
                    if (hp.Current <= 0.0f)
                    {
                        entity.Add<DeadTag>();
                        sprite.IsVisible = false;
                        CombatEffectHelper.ClearAllBossBullets(world);
                        CombatEffectHelper.SpawnVictoryExplosions(world, trans.X, trans.Y);
                        CameraService.AddTrauma(cameraCfg.VictoryExplosionTrauma);
                    }
                    else
                    {
                        if (isVulnerable)
                        {
                            CameraService.AddTrauma(cameraCfg.BossVulnerableHitTrauma);
                        }

                        // Boss 被弾反撃：現在の Phase に応じた数量の黑色弾幕を発射
                        CombatEffectHelper.SpawnBossCounterAttack(world, trans.X, trans.Y, combat.Phase);
                    }

                    entity.Remove<IncomingDamage>();
                }
            });

        return world;
    }
}
