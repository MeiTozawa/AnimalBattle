using AnimalBattle.Core;

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

    /// <summary>ノックバック初速度 (Vec2f)。</summary>
    public Vec2f Knockback;

    /// <summary>強打撃・易傷クリティカル判定フラグ。</summary>
    public bool IsHeavyImpact;
}
