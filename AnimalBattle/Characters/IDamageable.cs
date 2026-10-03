namespace AnimalBattle.Characters;

/// <summary>
///     被弾・ダメージ処理を受け付ける戦闘エンティティの振る舞い契約インターフェース。
///     生命状態は <see cref="Health"/> へ委譲し、受撃倍率と無敵状態およびダメージ計算・適用を自己完結します。
/// </summary>
public interface IDamageable {
    /// <summary>エンティティのヘルス情報を取得します。</summary>
    Health Health { get; }

    /// <summary>被ダメージ倍率（易傷など、1.0f 超で被ダメージ増加）を取得します。デフォルトは等倍（1.0f）。</summary>
    float DamageMultiplier => 1.0f;

    /// <summary>戦闘状態による無敵（霸体・状態遷移中など）であるかを取得します。デフォルトは false。</summary>
    bool IsInvulnerable => false;

    /// <summary>
    ///     ダメージを適用します。無敵時間および無敵状態を判定し、倍率を適用してヘルスを減算します。
    /// </summary>
    /// <param name="damage">受ける基本ダメージ量。</param>
    /// <returns>ダメージが正常に適用された場合は true、無敵等で無効化された場合は false。</returns>
    bool TakeDamage(float damage)
    {
        if (!Health.IsAlive || damage <= 0.0f) return false;
        if (IsInvulnerable || Health.IsInvincible) return false;

        var finalDamage = damage * DamageMultiplier;
        Health.Current = Math.Max(0.0f, Health.Current - finalDamage);
        OnDamaged(finalDamage);
        return true;
    }

    /// <summary>
    ///     ダメージ適用成功後に呼び出されるフック処理です。
    /// </summary>
    /// <param name="actualDamage">実際に適用されたダメージ量。</param>
    void OnDamaged(float actualDamage) { }
}
