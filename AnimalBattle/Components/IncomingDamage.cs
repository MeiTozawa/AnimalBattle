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
}
