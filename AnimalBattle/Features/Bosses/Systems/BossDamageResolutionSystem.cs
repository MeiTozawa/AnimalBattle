using System;
using AnimalBattle.Abilities;
using AnimalBattle.Combat;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Bosses.Systems;

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
        world.System<Health, IncomingDamage, HitFlashSettings, Transform, BossCombatState, Sprite, GameplayTagState, Velocity>("BossDamageResolutionSystem")
            .With<BossTag>()
            .Kind(Ecs.OnValidate)
            .Iter(ExecuteBossDamageResolution);

        return world;
    }

    /// <summary>
    ///     Boss 被弾ダメージ裁定パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void ExecuteBossDamageResolution(
        Iter it,
        Field<Health> hps,
        Field<IncomingDamage> damages,
        Field<HitFlashSettings> flashConfigs,
        Field<Transform> transforms,
        Field<BossCombatState> combats,
        Field<Sprite> sprites,
        Field<GameplayTagState> tags,
        Field<Velocity> velocities)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        var pCfg = world.GetActivePlayerConfig();
        ref readonly var cameraCfg = ref world.Get<CameraConfig>();

        // GameOver 状態のループ不変式判定をループ外へ巻き上げ
        var isGameOver = world.Has<GameOverTag>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref var hp = ref hps[i];
            ref readonly var dmg = ref damages[i];
            ref readonly var flashCfg = ref flashConfigs[i];
            ref readonly var trans = ref transforms[i];
            ref readonly var combat = ref combats[i];
            ref var sprite = ref sprites[i];
            ref readonly var tagState = ref tags[i];
            ref var vel = ref velocities[i];

            // ゲームオーバー済み、完全無敵または不可命中状態：GAS Invincible / Untargetable 保持時、死亡済み、またはダメージが 0 以下の場合は意図を破棄してスキップ
            if (isGameOver || tagState.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable) || hp.Current <= 0.0f || dmg.Amount <= 0.0f)
            {
                entity.Remove<IncomingDamage>();
                continue;
            }

            // 易傷倍率の評価：GameplayTag.Vulnerable 駆動
            var isVulnerable = tagState.HasTag(GameplayTag.Vulnerable);
            var multiplier = isVulnerable ? pCfg.VulnerableDamageMultiplier : 1.0f;
            var finalDamage = dmg.Amount * multiplier;
            hp.Current = MathF.Max(0.0f, hp.Current - finalDamage);

            // 被弾白閃演出の注入
            entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));

            // ヒットストップ、受撃局所振動およびノックバックの適用：SuperArmor 剛体タグ未保持時または易傷時に適用
            var hasSuperArmor = tagState.HasTag(GameplayTag.SuperArmor);
            if (!hasSuperArmor || isVulnerable)
            {
                var isHeavy = isVulnerable || dmg.IsHeavyImpact;
                var hitstopDuration = isHeavy ? pCfg.HitstopHeavyDuration : pCfg.HitstopLightDuration;
                if (hitstopDuration > 0.0f)
                {
                    entity.Set(new HitstopState(hitstopDuration));
                    var jitterAmp = isHeavy ? pCfg.HitJitterHeavyAmplitude : pCfg.HitJitterLightAmplitude;
                    entity.Set(new SpriteJitter(new Vec2f(jitterAmp, 0.0f), pCfg.HitJitterFrequency));
                }

                // ノックバック外力速度の設定：設定でノックバックが無効化されている場合は適用しません
                vel.Knockback = combat.CanBeKnockedBack ? dmg.Knockback : Vec2f.Zero;
            }

            if (isVulnerable)
            {
                // 易傷特効クリティカル火花
                EffectSpawner.SpawnHitSparkEffect(world, trans.Position - new Vec2f(8.0f, 8.0f));
                EffectSpawner.SpawnHitSparkEffect(world, trans.Position + new Vec2f(8.0f, 8.0f));
            }

            // 死亡または生存時反撃・カメラ衝撃判定
            if (hp.Current <= 0.0f)
            {
                world.Add<GameOverTag>();
                // 自機エンティティに GameOverTag を付与し、入力受付停止および意図ゼロクリアへ遷移
                using var playerQuery = world.QueryBuilder().With<PlayerTag>().Build();
                playerQuery.Each((Entity p) => p.Add<GameOverTag>());
                entity.Add<DeadTag>();
                sprite.IsVisible = false;
                BulletSpawner.ClearAllBossBullets(world);
                EffectSpawner.SpawnVictoryExplosions(world, trans.Position);
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
                BulletSpawner.SpawnBossCounterAttack(world, trans.Position, combat.Phase);
            }

            entity.Remove<IncomingDamage>();
        }
    }
}
