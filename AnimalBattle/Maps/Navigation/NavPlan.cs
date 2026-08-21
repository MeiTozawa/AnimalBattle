namespace AnimalBattle.Maps.Navigation;

/// <summary>
/// ナビゲーション実行状態の分類。
/// </summary>
public enum NavStatus
{
    /// <summary>目標と自身が同一プラットフォーム上に存在し、直接地面を水平歩行追従する状態</summary>
    DirectWalk,

    /// <summary>目標が別プラットフォームに存在し、起跳/下落地点に向かって移動中の状態</summary>
    MoveToTakeoff,

    /// <summary>起跳地点に到達し、上方・斜め跳躍を実行する状態</summary>
    ExecuteJump,

    /// <summary>プラットフォーム端に到達し、下層プラットフォームへ落下・下落する状態</summary>
    ExecuteDrop,

    /// <summary>空中滞空・落下中であり、着地点へ向けて水平慣性を維持する状態</summary>
    InAir,

    /// <summary>有効な経路が見つからない場合のフォールバック状態</summary>
    NoPath
}

/// <summary>
/// プラットフォームナビゲーショングラフが出力する単一フレームの意思決定プラン。
/// AI エンティティが現在実行すべき移動目標、跳躍指示、および運動学的速度を伝達します。
/// </summary>
/// <remarks>
/// <see cref="NavPlan"/> の新しいインスタンスを初期化します。
/// </remarks>
public sealed class NavPlan(
    NavStatus status,
    float targetX,
    float takeoffX,
    float landingX,
    float requiredVx,
    float requiredVy,
    PlatformNode? currentNode = null,
    PlatformNode? targetNode = null,
    PlatformLink? nextLink = null)
{
    /// <summary>現在のナビゲーション状態</summary>
    public NavStatus Status { get; } = status;

    /// <summary>現在ステップの水平移動目標 X 座標</summary>
    public float TargetX { get; } = targetX;

    /// <summary>次アクションの離陸・起跳 X 座標</summary>
    public float TakeoffX { get; } = takeoffX;

    /// <summary>次アクションの目標着地 X 座標</summary>
    public float LandingX { get; } = landingX;

    /// <summary>推奨される水平移動・飛行速度</summary>
    public float RequiredVx { get; } = requiredVx;

    /// <summary>推奨される垂直ジャンプ初速度（上向きは負値）</summary>
    public float RequiredVy { get; } = requiredVy;

    /// <summary>現在フレームで直ちにジャンプをトリガーすべきかどうか</summary>
    public bool ShouldJump => Status == NavStatus.ExecuteJump;

    /// <summary>自身が現在位置しているプラットフォームノード（どのプラットフォームにも属さない場合は null）</summary>
    public PlatformNode? CurrentNode { get; } = currentNode;

    /// <summary>目標プレイヤーが現在位置しているプラットフォームノード（どのプラットフォームにも属さない場合は null）</summary>
    public PlatformNode? TargetNode { get; } = targetNode;

    /// <summary>現在実行中または実行準備中のプラットフォーム間遷移リンク</summary>
    public PlatformLink? NextLink { get; } = nextLink;

    /// <summary>
    /// 同一プラットフォーム内での直接歩行追従プランを生成します。
    /// </summary>
    public static NavPlan CreateDirectWalk(float targetX, float moveSpeed, PlatformNode? currentNode, PlatformNode? targetNode)
    {
        return new NavPlan(
            NavStatus.DirectWalk,
            targetX: targetX,
            takeoffX: targetX,
            landingX: targetX,
            requiredVx: moveSpeed,
            requiredVy: 0.0f,
            currentNode: currentNode,
            targetNode: targetNode);
    }

    /// <summary>
    /// 経路が存在しない場合のフォールバックプランを生成します。
    /// </summary>
    public static NavPlan CreateNoPath(float targetX, float moveSpeed, PlatformNode? currentNode, PlatformNode? targetNode)
    {
        return new NavPlan(
            NavStatus.NoPath,
            targetX: targetX,
            takeoffX: targetX,
            landingX: targetX,
            requiredVx: moveSpeed,
            requiredVy: 0.0f,
            currentNode: currentNode,
            targetNode: targetNode);
    }
}
