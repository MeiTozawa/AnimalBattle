namespace AnimalBattle.Components;

/// <summary>
///     被弾判定ボリュームコンポーネント（マネージド参照を完全排除したアンマネージド構造体）。
/// </summary>
public struct Hurtbox(CollisionShape shape, Faction faction = Faction.Friendly) {
    /// <summary>被弾判定の幾何形状。</summary>
    public CollisionShape Shape = shape;

    /// <summary>被弾対象が所属する陣営。</summary>
    public Faction Faction = faction;
}