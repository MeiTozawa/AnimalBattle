namespace AnimalBattle.Combat.Components;

/// <summary>
///     他エンティティにダメージや衝撃を与える攻撃判定ボリュームを表すコンポーネント。
///     Bevy の Hitbox パターンに基づき、幾何形状・与ダメージ・陣営・命中時消滅フラグ・および動的有効化状態 をカプセル化します。
/// </summary>
/// <remarks>
///     <see cref="Hitbox" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="shape">幾何形状</param>
/// <param name="damage">ダメージ量：初期値: 1.0f</param>
/// <param name="faction">所属陣営：初期値: Friendly</param>
/// <param name="destroyOnHit">命中時自動破棄フラグ：初期値: true</param>
/// <param name="isActive">有効化フラグ：初期値: true</param>
public struct Hitbox(
    CollisionShape shape,
    float damage = 1.0f,
    Faction faction = Faction.Friendly,
    bool destroyOnHit = true,
    bool isActive = true) {
    /// <summary>攻撃判定の幾何形状</summary>
    public CollisionShape Shape = shape;

    /// <summary>命中時に与えるダメージ量</summary>
    public float Damage = damage;

    /// <summary>攻撃判定を保有する陣営</summary>
    public Faction Faction = faction;

    /// <summary>命中時にエンティティ自体を破棄するか：銃弾は true、Boss 本体突進は false</summary>
    public bool DestroyOnHit = destroyOnHit;

    /// <summary>現在この攻撃判定が有効：ダメージ発生可能かどうかのフラグ</summary>
    public bool IsActive = isActive;
}