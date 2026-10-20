namespace AnimalBattle.Maps.Navigation;

/// <summary>
///     ナビゲーション実行状態の分類。
/// </summary>
public enum NavStatus {
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
///     プラットフォームナビゲーショングラフが出力する単一フレームの意思決定プラン。
///     AI エンティティが現在実行すべき移動目標、跳躍指示、および運動学的速度を伝達します。
/// </summary>
public readonly record struct NavPlan(
    NavStatus Status,
    float TargetX,
    float TakeoffX,
    float LandingX,
    float RequiredVx,
    float RequiredVy,
    PlatformNode? CurrentNode = null,
    PlatformNode? TargetNode = null,
    PlatformLink? NextLink = null) {
    /// <summary>現在フレームで直ちにジャンプをトリガーすべきかどうか</summary>
    public bool ShouldJump => Status == NavStatus.ExecuteJump;

    /// <summary>
    ///     同一プラットフォーム内での直接歩行追従プランを生成します。
    /// </summary>
    public static NavPlan CreateDirectWalk(
        float currentX,
        float targetX,
        float moveSpeed,
        PlatformNode? currentNode,
        PlatformNode? targetNode,
        float moveThreshold = 15.0f)
    {
        var dx = targetX - currentX;
        var dir = MathF.Abs(dx) > moveThreshold ? MathF.Sign(dx) : 0.0f;
        return new NavPlan(
            NavStatus.DirectWalk,
            targetX,
            targetX,
            targetX,
            dir * moveSpeed,
            0.0f,
            currentNode,
            targetNode);
    }

    /// <summary>
    ///     経路が存在しない場合のフォールバックプランを生成します。
    /// </summary>
    public static NavPlan CreateNoPath(
        float currentX,
        float targetX,
        float moveSpeed,
        PlatformNode? currentNode,
        PlatformNode? targetNode,
        float moveThreshold = 15.0f)
    {
        var dx = targetX - currentX;
        var dir = MathF.Abs(dx) > moveThreshold ? MathF.Sign(dx) : 0.0f;
        return new NavPlan(
            NavStatus.NoPath,
            targetX,
            targetX,
            targetX,
            dir * moveSpeed,
            0.0f,
            currentNode,
            targetNode);
    }
}
