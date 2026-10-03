using AnimalBattle.Characters;

namespace AnimalBattle.Components;

/// <summary>
///     敵対 Hitbox からのダメージを受け付ける被弾判定ボリュームを表すコンポーネント。
///     幾何形状、所属陣営、および受撃対象の <see cref="IDamageable" /> 参照をカプセル化します。
/// </summary>
/// <remarks>
///     <see cref="Hurtbox" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="shape">幾何形状</param>
/// <param name="faction">所属陣営（初期値: Friendly）</param>
/// <param name="owner">受撃対象の戦闘エンティティ（初期値: null）</param>
public struct Hurtbox(CollisionShape shape, Faction faction = Faction.Friendly, IDamageable? owner = null) {
    /// <summary>被弾判定の幾何形状</summary>
    public CollisionShape Shape = shape;

    /// <summary>被弾対象が所属する陣営</summary>
    public Faction Faction = faction;

    /// <summary>受撃対象の戦闘エンティティ</summary>
    public IDamageable? Owner = owner;
}