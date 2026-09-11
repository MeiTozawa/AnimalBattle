using System;

namespace AnimalBattle.Maps.Navigation;

/// <summary>
/// プラットフォーム間遷移ナビゲーションアクション種別。
/// </summary>
public enum NavActionType
{
    /// <summary>同一プラットフォーム内での水平歩行追従</summary>
    Walk,

    /// <summary>他プラットフォームへの上方・斜め跳躍（放物線ジャンプ）</summary>
    Jump,

    /// <summary>プラットフォーム端からの直接落下・下落</summary>
    Drop
}

/// <summary>
/// 2D プラットフォームナビゲーショングラフにおける有向遷移リンク辺（あるプラットフォームから別のプラットフォームへの跳躍/下落）。
/// 離陸・起跳点座標、着地点座標、物理放物線に必要な初速および経路コストを保持します。
/// </summary>
/// <remarks>
/// <see cref="PlatformLink"/> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="fromNode">起点プラットフォーム</param>
/// <param name="toNode">終点プラットフォーム</param>
/// <param name="action">動作種別 (<see cref="NavActionType"/>)</param>
/// <param name="takeoffX">離脱・起跳 X 座標</param>
/// <param name="landingX">着地 X 座標</param>
/// <param name="requiredJumpVy">垂直初速度</param>
/// <param name="requiredJumpVx">水平移動速度</param>
/// <param name="cost">移動コスト</param>
public sealed class PlatformLink(
    PlatformNode fromNode,
    PlatformNode toNode,
    NavActionType action,
    float takeoffX,
    float landingX,
    float requiredJumpVy,
    float requiredJumpVx,
    float cost)
{
    /// <summary>起点平台节点</summary>
    public PlatformNode FromNode { get; } = fromNode;

    /// <summary>终点目标平台节点</summary>
    public PlatformNode ToNode { get; } = toNode;

    /// <summary>执行该转移所需的动作类型</summary>
    public NavActionType Action { get; } = action;

    /// <summary>在起点平台上的最佳起跳/下落 X 坐标</summary>
    public float TakeoffX { get; } = takeoffX;

    /// <summary>在目标平台上的预期着陆 X 坐标</summary>
    public float LandingX { get; } = landingX;

    /// <summary>起跳所需的垂直初速度（向上为负数，如 -600 px/s）</summary>
    public float RequiredJumpVy { get; } = requiredJumpVy;

    /// <summary>滞空飞行期间所需的水平速度</summary>
    public float RequiredJumpVx { get; } = requiredJumpVx;

    /// <summary>A* 启发式搜索所用的转移代价（基于位移距离与滞空耗时）</summary>
    public float Cost { get; } = cost;

    /// <summary>
    /// リンクの文字列表現を取得します。
    /// </summary>
    public override string ToString()
    {
        return $"Link #{FromNode.Id} -> #{ToNode.Id} [{Action}] Takeoff: {TakeoffX:F0} -> Landing: {LandingX:F0} (Cost: {Cost:F1})";
    }
}
