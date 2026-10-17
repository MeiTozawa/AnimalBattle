namespace AnimalBattle.Components;

/// <summary>
///     エンティティが本フレームで受信した未処理のダメージ意図コンポーネント。
/// </summary>
public struct IncomingDamage
{
    /// <summary>受ける基本ダメージ量。</summary>
    public float Amount;

    /// <summary>攻撃元の所属陣営。</summary>
    public Faction SourceFaction;

    /// <summary>ノックバック水平初速度（px/s）。</summary>
    public float KnockbackX;

    /// <summary>ノックバック垂直初速度（px/s）。</summary>
    public float KnockbackY;

    /// <summary>強打撃・易傷クリティカル判定フラグ。</summary>
    public bool IsHeavyImpact;
}
