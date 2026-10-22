namespace AnimalBattle.Maps.Navigation;

/// <summary>
///     プラットフォーム間遷移ナビゲーションアクション種別。
/// </summary>
public enum NavActionType {
    /// <summary>同一プラットフォーム内での水平歩行追従</summary>
    Walk,

    /// <summary>他プラットフォームへの上方・斜め跳躍：放物線ジャンプ</summary>
    Jump,

    /// <summary>プラットフォーム端からの直接落下・下落</summary>
    Drop
}

/// <summary>
///     2D プラットフォームナビゲーショングラフにおける有向遷移リンク辺：あるプラットフォームから別のプラットフォームへの跳躍/落下。
///     離脱・踏切座標、着地点座標、物理放物線に必要な初速および経路コストを保持します。
/// </summary>
/// <remarks>
///     <see cref="PlatformLink" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="fromNode">起点プラットフォーム</param>
/// <param name="toNode">終点プラットフォーム</param>
/// <param name="action">動作種別 <see cref="NavActionType" /></param>
/// <param name="takeoffX">離脱・踏切 X 座標</param>
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
    float cost) {
    /// <summary>起点プラットフォームノード</summary>
    public PlatformNode FromNode { get; } = fromNode;

    /// <summary>終点プラットフォームノード</summary>
    public PlatformNode ToNode { get; } = toNode;

    /// <summary>遷移に必要なアクション種別</summary>
    public NavActionType Action { get; } = action;

    /// <summary>起点上の踏切/落下 X 座標</summary>
    public float TakeoffX { get; } = takeoffX;

    /// <summary>目標上の着地 X 座標</summary>
    public float LandingX { get; } = landingX;

    /// <summary>跳躍初速度 Y：上向き負値、例: -600 px/s</summary>
    public float RequiredJumpVy { get; } = requiredJumpVy;

    /// <summary>滞空水平移動速度 X</summary>
    public float RequiredJumpVx { get; } = requiredJumpVx;

    /// <summary>A* 探索コスト：移動距離と滞空時間に基づく</summary>
    public float Cost { get; } = cost;

    /// <summary>
    ///     リンクの文字列表現を取得します。
    /// </summary>
    public override string ToString()
    {
        return
            $"Link #{FromNode.Id} -> #{ToNode.Id} [{Action}] Takeoff: {TakeoffX:F0} -> Landing: {LandingX:F0} (Cost: {Cost:F1})";
    }
}