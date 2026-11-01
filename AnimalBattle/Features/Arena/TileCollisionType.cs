namespace AnimalBattle.Maps;

/// <summary>
///     タイルごとの物理衝突セマンティクス分類。
/// </summary>
public enum TileCollisionType
{
    /// <summary>衝突なし：空気/空白</summary>
    None = 0,

    /// <summary>下方向からの跳躍すり抜けが可能な片道プラットフォーム足場</summary>
    OneWayPlatform = 2,

    /// <summary>全方向からの侵入を遮断する実心ブロック：壁・天井・不可侵地形</summary>
    Solid = 3
}
