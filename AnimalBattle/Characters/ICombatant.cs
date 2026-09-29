namespace AnimalBattle.Characters;

/// <summary>
///     戦闘空間に存在するすべてのキャラクター（自機・Bossなど）に共通する統一戦闘者契約インターフェース。
///     ECS エンティティのコンポーネントを唯一の真実の源としてカプセル化し、一貫したアクセスを提供します。
/// </summary>
public interface ICombatant {
    /// <summary>エンティティ ID を取得します。</summary>
    ulong EntityId { get; }

    /// <summary>ワールド X 座標を取得または設定します。</summary>
    float X { get; set; }

    /// <summary>ワールド Y 座標を取得または設定します。</summary>
    float Y { get; set; }

    /// <summary>X 軸移動速度を取得または設定します。</summary>
    float Vx { get; set; }

    /// <summary>Y 軸移動速度を取得または設定します。</summary>
    float Vy { get; set; }

    /// <summary>現在の HP を取得または設定します。</summary>
    float CurrentHp { get; set; }

    /// <summary>最大 HP を取得します。</summary>
    float MaxHp { get; }

    /// <summary>生存中（HP > 0 かつエンティティが有効）かどうかを取得します。</summary>
    bool IsAlive { get; }

    /// <summary>所属陣営（味方・敵性など）を取得します。</summary>
    Faction Faction { get; }

    /// <summary>無敵時間中かどうかを取得します。</summary>
    bool IsInvincible { get; }

    /// <summary>
    ///     ダメージを受けます。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    /// <param name="ignoreInvincible">無敵時間を無視するかどうか。</param>
    void TakeDamage(float damage, bool ignoreInvincible = false);
}