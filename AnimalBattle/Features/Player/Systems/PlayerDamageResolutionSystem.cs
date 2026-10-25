using System;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     プレイヤー自機専用の被弾ダメージ裁定 ECS システム。
///     <see cref="PlayerTag" /> を持つエンティティの <see cref="IncomingDamage" /> を消費し、
///     HP 減少、無敵時間、ヒットストップ、被弾スカッシュ、ノックバックおよびカメラ衝撃を処理します。
/// </summary>
public static class PlayerDamageResolutionSystem
{
    /// <summary>
    ///     プレイヤー被弾ダメージ裁定システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddPlayerDamageResolutionSystem(this World world)
    {
        world.System<Health, IncomingDamage, InvincibleOnHit, HitFlashSettings, Transform, Velocity, CollisionBox, SpringScale, Sprite, GameplayTagState, TimedEffectsState>("PlayerDamageResolutionSystem")
            .With<PlayerTag>()
            .Kind(Ecs.OnValidate)
            .Iter(ExecutePlayerDamageResolution);

        return world;
    }

    private static void ExecutePlayerDamageResolution(
        Iter it,
        Field<Health> hps,
        Field<IncomingDamage> damages,
        Field<InvincibleOnHit> invConfigs,
        Field<HitFlashSettings> flashConfigs,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<SpringScale> springScales,
        Field<Sprite> sprites,
        Field<GameplayTagState> tags,
        Field<TimedEffectsState> timedEffects)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        ref readonly var cameraCfg = ref world.Get<CameraConfig>();
        var pCfg = world.GetActivePlayerConfig();

        // GameOver 状態のループ不変式判定をループ外へ巻き上げ
        var isGameOver = world.Has<GameOverTag>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref var hp = ref hps[i];
            ref readonly var dmg = ref damages[i];
            ref readonly var invCfg = ref invConfigs[i];
            ref readonly var flashCfg = ref flashConfigs[i];
            ref readonly var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref var spring = ref springScales[i];
            ref var sprite = ref sprites[i];
            ref var tagState = ref tags[i];
            ref var effectState = ref timedEffects[i];

            // ゲームオーバー済み、無敵または不可命中状態：GAS Invincible / Untargetable 保持時、死亡済み、またはダメージが 0 以下の場合は意図を破棄してスキップ
            if (isGameOver || tagState.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable) || hp.Current <= 0.0f || dmg.Amount <= 0.0f)
            {
                entity.Remove<IncomingDamage>();
                continue;
            }

            // HP 減算および不可命中無敵時間・被弾白閃エフェクトの注入
            var prevHp = hp.Current;
            hp.Current = MathF.Max(0.0f, hp.Current - dmg.Amount);
            TimedEffectService.ApplyEffect(ref effectState, ref tagState, GameplayTag.Untargetable, invCfg.Duration, entity.Id);
            entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));
            EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hp.Current, hp.Max));

            // ヒットストップ時間および微小振動の付与
            var isHeavy = dmg.IsHeavyImpact;
            var hitstopDuration = isHeavy ? pCfg.HitstopHeavyDuration : pCfg.HitstopLightDuration;
            if (hitstopDuration > 0.0f)
            {
                entity.Set(new HitstopState(hitstopDuration));
                var jitterAmp = isHeavy ? pCfg.HitJitterHeavyAmplitude : pCfg.HitJitterLightAmplitude;
                entity.Set(new SpriteJitter(new Vec2f(jitterAmp, 0.0f), pCfg.HitJitterFrequency));
            }

            // スプリング被弾スカッシュ変形の直接適用
            spring.Current = pCfg.HitSquashScale;
            spring.Velocity = Vec2f.Zero;

            // ノックバック物理インパルスの付与
            vel.Knockback = dmg.Knockback;
            if (dmg.Knockback.Y < 0.0f)
            {
                col.IsGrounded = false;
            }

            // 死亡または生存時の演出・カメラ衝撃判定
            if (hp.Current <= 0.0f)
            {
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
            }
            else
            {
                var trauma = isHeavy ? cameraCfg.PlayerHeavyHitTrauma : cameraCfg.PlayerHitTrauma;
                CameraService.AddTrauma(trauma);
                CameraService.AddDirectionalImpulse(dmg.Knockback, cameraCfg.DirectionalImpulseRatio);
                EventBus.Publish(new PlayerDamagedEvent(dmg.Amount, hp.Current, isHeavy));
            }

            entity.Remove<IncomingDamage>();
        }
    }
}
