using System;
using AnimalBattle.Abilities;
using AnimalBattle.Combat.Events;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Presentation.Rendering;
using AnimalBattle.Presentation.Vfx;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     プレイヤー自機専用の被弾ダメージ裁定 ECS システム。
///     <see cref="DamageInbox" /> 内のイベントを集約・消費し、
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
        world.System<Health, InvincibleOnHit, HitFlashSettings, Transform, Velocity, CollisionBox, SpringScale, Sprite, GameplayTagState, TimedEffectsState>("PlayerDamageResolutionSystem")
            .With<PlayerTag>()
            .Kind(Ecs.OnValidate)
            .Iter(ExecutePlayerDamageResolution);

        return world;
    }

    private static void ExecutePlayerDamageResolution(
        Iter it,
        Field<Health> hps,
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
        ref var inbox = ref world.GetMut<DamageInbox>();
        var events = inbox.AsSpan();
        if (events.IsEmpty) return;

        ref readonly var cameraCfg = ref world.Get<CameraConfig>();
        var pCfg = world.GetActivePlayerConfig();

        // GameOver 状態のループ不変式判定をループ外へ巻き上げ
        var isGameOver = world.Has<GameOverTag>();

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);

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
                // 敵対判定（Boss攻撃等）から自機への同一フレーム多重被弾は最大値を採用して多重被弾死を防止
                if (evt.SourceFaction == Faction.Hostile)
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

            // ゲームオーバー済み、無敵状態（GAS Invincible / Untargetable 保持時）、または死亡済みの場合はスキップ
            if (isGameOver || tagState.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable) || hp.Current <= 0.0f)
            {
                continue;
            }

            ref readonly var invCfg = ref invConfigs[i];
            ref readonly var flashCfg = ref flashConfigs[i];
            ref readonly var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref var spring = ref springScales[i];
            ref var sprite = ref sprites[i];
            ref var effectState = ref timedEffects[i];

            // HP 減算および不可命中無敵時間・被弾白閃・無敵点滅エフェクトの注入
            var prevHp = hp.Current;
            hp.Current = MathF.Max(0.0f, hp.Current - totalDamage);
            TimedEffectService.ApplyEffect(ref effectState, ref tagState, GameplayTag.Untargetable, invCfg.Duration, entity.Id);
            entity.Set(new HitFlashEffect(flashCfg.Duration, flashCfg.Color));
            var blinkFreq = world.Get<RenderConfig>().Player.InvincibleBlinkFrequency;
            entity.Set(new InvincibleBlinkEffect(invCfg.Duration, blinkFreq));
            EventBus.Publish(new PlayerHealthChangedEvent(prevHp, hp.Current, hp.Max));

            // ヒットストップ時間および微小振動の付与
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
            vel.Knockback = targetKnockback;
            if (targetKnockback.Y < 0.0f)
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
                CameraService.AddDirectionalImpulse(targetKnockback, cameraCfg.DirectionalImpulseRatio);
                EventBus.Publish(new PlayerDamagedEvent(totalDamage, hp.Current, isHeavy));
            }
        }
    }
}
