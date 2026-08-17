namespace AnimalBattle.Components;

/// <summary>
/// 敵対 Hitbox からのダメージを受け付ける被弾判定ボリュームを表すコンポーネント。
/// Bevy の Hurtbox パターンに基づき、幾何形状と所属陣営をカプセル化します。
/// </summary>
/// <remarks>
/// <see cref="Hurtbox"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="shape">幾何形状</param>
/// <param name="faction">所属陣営（初期値: Friendly）</param>
public struct Hurtbox(CollisionShape shape, Faction faction = Faction.Friendly)
{
    /// <summary>被弾判定の幾何形状</summary>
    public CollisionShape Shape = shape;

    /// <summary>被弾対象が所属する陣営</summary>
    public Faction Faction = faction;
}
