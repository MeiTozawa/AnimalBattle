using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;
/// <summary>
/// 4 人対戦における攻撃判定（Hitbox/Hurtbox 相互判定）、被弾時ヒットストップ・ダメージ蓄積・ノックバック付与を処理する Flecs.NET ECS システム。
/// </summary>
public static class CombatSystems
{
    /// <summary>
    /// 攻撃モーション中の全プレイヤーと被弾可能状態の他プレイヤーとの衝突判定を行い、被弾演出およびノックバックを適用します。
    /// </summary>
    public static readonly Action<World> CombatSystem = (world) =>
    {
        Query<PlayerId, Transform, Velocity, CollisionBox, Animation, Sprite, CharacterStats> playerQuery =
            world.QueryBuilder<PlayerId, Transform, Velocity, CollisionBox, Animation, Sprite, CharacterStats>()
                 .With<PlayerTag>()
                 .Build();

        playerQuery.Each((ref PlayerId attackerPid, ref Transform attackerTrans, ref Velocity attackerVel, ref CollisionBox attackerCol, ref Animation attackerAnim, ref Sprite attackerSprite, ref CharacterStats attackerStats) =>
        {
            if (!string.Equals(attackerAnim.CurrentClip, "Attack", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (attackerAnim.Timer < 0.04f || attackerAnim.Timer > 0.22f)
            {
                return;
            }

            int aId = attackerPid.Id;
            bool attackerFlipX = attackerSprite.FlipX;
            float atkX = attackerTrans.X + (attackerFlipX ? -35.0f : 35.0f);
            float atkY = attackerTrans.Y;
            float atkWidth = 45.0f;
            float atkHeight = 40.0f;

            Rect<float> attackHitbox = new Rect<float>(
                atkX - atkWidth / 2.0f,
                atkY - atkHeight / 2.0f,
                atkX + atkWidth / 2.0f,
                atkY + atkHeight / 2.0f);

            playerQuery.Each((ref PlayerId defenderPid, ref Transform defenderTrans, ref Velocity defenderVel, ref CollisionBox defenderCol, ref Animation defenderAnim, ref Sprite defenderSprite, ref CharacterStats defenderStats) =>
            {
                if (aId == defenderPid.Id)
                {
                    return;
                }

                Rect<float> defenderHurtbox = new Rect<float>(
                    defenderTrans.X - defenderCol.Width / 2.0f,
                    defenderTrans.Y - defenderCol.Height / 2.0f,
                    defenderTrans.X + defenderCol.Width / 2.0f,
                    defenderTrans.Y + defenderCol.Height / 2.0f);

                if (attackHitbox.Intersects(defenderHurtbox))
                {
                    if (defenderStats.HitstunTimer <= 0.0f)
                    {
                        defenderStats.DamagePercent += 12.0f;
                        defenderStats.HitstunTimer = 0.25f;

                        float knockbackScaling = 1.0f + (defenderStats.DamagePercent / 80.0f);
                        float knockbackDirX = attackerFlipX ? -1.0f : 1.0f;
                        defenderVel.KnockbackX = knockbackDirX * 12.0f * knockbackScaling;
                        defenderVel.KnockbackY = -9.0f * knockbackScaling;

                        SpawnHitEffect(world, (atkX + defenderTrans.X) / 2.0f, (atkY + defenderTrans.Y) / 2.0f);
                    }
                }
            });
        });
    };

    private static void SpawnHitEffect(World world, float x, float y)
    {
        world.Entity()
            .Set(new Transform(x, y, scaleX: 2.0f, scaleY: 2.0f))
            .Set(new Sprite(isVisible: true))
            .Set(new Animation("SlashEffect", isPlaying: true))
            .Set(FadeEffect.FadeOut(0.20f))
            .Set(new HitFlashEffect(0.12f, 0xFFFFFF00))
            .Add<EffectTag>();
    }
}
