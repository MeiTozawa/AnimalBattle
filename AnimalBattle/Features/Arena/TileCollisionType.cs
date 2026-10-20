namespace AnimalBattle.Maps;

/// <summary>
///     タイルごとの物理衝突セマンティクス分類。
/// </summary>
public enum TileCollisionType
{
    /// <summary>衝突なし（空気/空白）</summary>
    None = 0,

    /// <summary>純粋な視覚演出・装飾タイル（草花、背景壁、装飾物など）</summary>
    DecoOnly = 1,

    /// <summary>下方向からの跳躍すり抜けが可能な片道プラットフォーム足場</summary>
    OneWayPlatform = 2,

    /// <summary>全方向からの侵入を遮断する実心ブロック（壁・天井・不可侵地形）</summary>
    Solid = 3,

    /// <summary>接触時にダメージや即死を発生させる危険領域（トゲ、マグマ、電流床など）</summary>
    Hazard = 4
}
