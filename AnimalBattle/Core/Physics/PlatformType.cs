namespace AnimalBattle.Core;

/// <summary>
/// 足場（プラットフォーム）の属性タイプを表す列挙型。
/// </summary>
public enum PlatformType : int
{
    /// <summary>
    /// 実体足場（全方向からの衝突・着地が可能）。
    /// </summary>
    Solid = 0,

    /// <summary>
    /// 半実体足場（すり抜け床：上方向からの着地のみ可能、下からのすり抜けおよび下降りが可能）。
    /// </summary>
    SemiSolid = 1
}
