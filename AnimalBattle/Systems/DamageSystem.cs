using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     エンティティの被弾・ダメージ処理、無敵保護判定、および HP 減算を統括する汎用ダメージシステム。
/// </summary>
public static class DamageSystem {
    /// <summary>
    ///     指定された Flecs エンティティに対してダメージを適用し、Health コンポーネントを更新します。
    /// </summary>
    /// <param name="entity">対象の Flecs エンティティ句柄。</param>
    /// <param name="damage">受けるダメージ量（0 以下の場合は無視されます）。</param>
    /// <param name="ignoreInvincible">無敵時間または無敵状態を無視するかどうか。</param>
    /// <returns>実際にダメージが適用された場合は true、無効化された場合は false。</returns>
    public static bool ApplyDamage(Entity entity, float damage, bool ignoreInvincible = false)
    {
        if (!entity.IsAlive() || !entity.Has<Health>() || damage <= 0.0f)
            return false;

        ref var health = ref entity.GetMut<Health>();
        if (!health.IsAlive)
            return false;

        if (!ignoreInvincible)
        {
            if (health.IsInvincible)
                return false;

            if (entity.Has<BossCombatState>())
            {
                ref readonly var combat = ref entity.Get<BossCombatState>();
                if (combat.IsInvulnerable)
                    return false;
            }
        }

        var finalDamage = damage;
        if (entity.Has<BossCombatState>())
        {
            ref readonly var combat = ref entity.Get<BossCombatState>();
            if (combat.IsVulnerable)
            {
                finalDamage *= 2.0f;
            }
        }

        health.Current = Math.Max(0.0f, health.Current - finalDamage);
        return true;
    }
}
