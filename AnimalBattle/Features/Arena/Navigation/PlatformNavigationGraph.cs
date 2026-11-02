using System.Diagnostics.CodeAnalysis;

namespace AnimalBattle.Maps.Navigation;

/// <summary>
///     2D プラットフォームナビゲーションのトポロジーグラフおよび A* 経路探索エンジン。
///     TileMap のプラットフォーム群から空間接続関係：跳躍/下落リンクを自動構築し、
///     Boss AI にリアルタイムのプラットフォーム間追従・跳躍ナビゲーションプランを提供します。
/// </summary>
public sealed class PlatformNavigationGraph(NavigationGraphConfig? config = null) {
    /// <summary>ナビゲーショングラフ探索・幾何パラメータ設定</summary>
    private NavigationGraphConfig Config { get; set; } = config ?? new NavigationGraphConfig();

    private readonly Dictionary<int, List<PlatformLink>> _adjacency = new();
    private readonly Dictionary<PlatformLink, PlatformLink?> _aStarCameFrom = new();

    // A* 経路探索用の一時バッファ：毎フレームの GC 堆割り当てを完全に防止
    private readonly Dictionary<PlatformLink, float> _aStarGScore = new();
    private readonly PriorityQueue<PlatformLink, float> _aStarOpenSet = new();
    private readonly List<PlatformLink> _links = new();
    private readonly List<PlatformNode> _nodes = new();
    private readonly List<PlatformLink> _reusablePath = new();



    /// <summary>すべての登録済みプラットフォームノード</summary>
    public IReadOnlyList<PlatformNode> Nodes => _nodes;

    /// <summary>すべての登録済みプラットフォーム間リンク</summary>
    public IReadOnlyList<PlatformLink> Links => _links;

    /// <summary>
    ///     プラットフォームノード群および物理パラメータからナビゲーショングラフを構築します。
    /// </summary>
    /// <param name="nodes">プラットフォームノード一覧</param>
    /// <param name="jumpForce">Boss の基準ジャンプ初速度 正の値で指定するジャンプ初速度</param>
    /// <param name="gravity">ワールド重力加速度 正の値で指定する重力加速度</param>
    /// <param name="moveSpeed">地上巡航移動速度 例として220.0f</param>
    /// <param name="config">オプションのナビゲーショングラフ設定</param>
    public void Build(
        IReadOnlyList<PlatformNode> nodes,
        float jumpForce,
        float gravity,
        float moveSpeed,
        NavigationGraphConfig? config = null)
    {
        if (config != null) Config = config;
        _nodes.Clear();
        _links.Clear();
        _adjacency.Clear();

        var safeGravity = MathF.Max(1.0f, gravity);
        var safeJumpForce = MathF.Max(0.0f, jumpForce);

        _nodes.AddRange(nodes);
        foreach (var node in _nodes) _adjacency[node.Id] = [];

        var maxJumpHeight = safeJumpForce * safeJumpForce / (2.0f * safeGravity);

        for (var i = 0; i < _nodes.Count; i++)
        {
            var nodeA = _nodes[i];
            for (var j = 0; j < _nodes.Count; j++)
            {
                if (i == j) continue;

                var nodeB = _nodes[j];
                EvaluateAndAddLinks(nodeA, nodeB, safeJumpForce, safeGravity, moveSpeed, maxJumpHeight);
            }
        }
    }
    /// <summary>
    ///     指定された足元座標 が位置するプラットフォームノードを検索します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="footY">足元 Y 座標</param>
    /// <param name="toleranceY">垂直許容誤差：省略時は設定値を採用</param>
    /// <param name="marginX">水平許容マージン：半身はみ出し許容用、省略時は設定値を採用</param>
    /// <returns>該当するプラットフォーム、存在しない場合は null</returns>
    public PlatformNode? FindNodeAt(float x, float footY, float? toleranceY = null, float? marginX = null)
    {
        var tolY = toleranceY ?? Config.EnemyNodeToleranceY;
        var marX = marginX ?? Config.EnemyNodeMarginX;
        PlatformNode? bestNode = null;
        var bestDistY = float.MaxValue;

        foreach (var node in _nodes)
            if (node.ContainsX(x, marX))
            {
                var dy = MathF.Abs(footY - node.TopY);
                if (dy <= tolY && dy < bestDistY)
                {
                    bestDistY = dy;
                    bestNode = node;
                }
            }

        return bestNode;
    }

    /// <summary>
    ///     指定された X 座標直下に存在する最も近いプラットフォームノードを検索します：空中落下フォールバック用。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="footY">足元 Y 座標</param>
    /// <param name="marginX">水平許容マージン：省略時は設定値を採用</param>
    /// <returns>直下のプラットフォーム、存在しない場合は null</returns>
    public PlatformNode? FindPlatformBelow(float x, float footY, float? marginX = null)
    {
        var marX = marginX ?? Config.EnemyNodeMarginX;
        PlatformNode? bestNode = null;
        var bestDistY = float.MaxValue;

        foreach (var node in _nodes)
        {
            if (node.ContainsX(x, marX))
            {
                var dy = node.TopY - footY;
                if (dy >= -Config.EnemyNodeToleranceY && dy < bestDistY)
                {
                    bestDistY = dy;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    ///     現在位置からプレイヤー目標位置への次フレームナビゲーションプランを計算します。
    /// </summary>
    /// <param name="currentX">Boss の中心 X 座標</param>
    /// <param name="currentFootY">Boss の足元 Y 座標</param>
    /// <param name="targetX">プレイヤーの中心 X 座標</param>
    /// <param name="targetFootY">プレイヤーの足元 Y 座標</param>
    /// <param name="isGrounded">Boss が接地中かどうか</param>
    /// <param name="moveSpeed">巡航移動速度</param>
    /// <param name="activeLink">外部指定のアクティブリンク：省略時は内部保持リンクを採用</param>
    /// <returns>計算されたナビゲーションプラン <see cref="NavPlan" /></returns>
    public NavPlan FindPath(
        float currentX,
        float currentFootY,
        float targetX,
        float targetFootY,
        bool isGrounded,
        float moveSpeed,
        PlatformLink? activeLink = null)
    {
        var currentActiveLink = activeLink;

        // 1. 空中滞空中の場合：着地目標地点に向かって水平推力を維持：接地判定・ノード特定を先行させない
        if (!isGrounded)
        {
            var targetNodeInAir = FindNodeAt(targetX, targetFootY, Config.TargetToleranceY, Config.TargetMarginX);

            if (currentActiveLink != null)
            {
                // 起点・終点プラットフォームのいずれよりも下方に転落した場合はリンク破棄
                var lowestY = MathF.Max(currentActiveLink.FromNode.TopY, currentActiveLink.ToNode.TopY);
                if (currentFootY > lowestY + Config.EnemyNodeToleranceY)
                {
                            }
                else
                {
                    return new NavPlan(
                        NavStatus.InAir,
                        currentActiveLink.RequiredJumpVx,
                        0.0f,
                        currentActiveLink);
                }
            }

            // アクティブリンクなし、またはリンク破棄後の空中フォールバック
            var fallbackNode = FindPlatformBelow(currentX, currentFootY);
            var dxInAir = targetX - currentX;
            var vxInAir = MathF.Abs(dxInAir) > Config.TakeoffTriggerDistance
                ? MathF.Sign(dxInAir) * moveSpeed
                : 0.0f;

            return new NavPlan(
                NavStatus.InAir,
                vxInAir);
        }

        // 接地状態復帰時はアクティブリンクをリセット

        var currentNode = FindNodeAt(currentX, currentFootY, Config.EnemyNodeToleranceY, Config.EnemyNodeMarginX);
        var targetNode = FindNodeAt(targetX, targetFootY, Config.TargetToleranceY, Config.TargetMarginX);

        // 2. 同一プラットフォーム上にいる場合は直接歩行追従
        if (currentNode != null && targetNode != null && currentNode.Id == targetNode.Id)
            return NavPlan.CreateDirectWalk(currentX, targetX, moveSpeed, currentNode, targetNode, Config.TakeoffTriggerDistance);

        // 3. プラットフォームノードが特定できない場合のフォールバック
        if (currentNode == null || targetNode == null)
            return NavPlan.CreateNoPath(currentX, targetX, moveSpeed, currentNode, targetNode);

        // 4. A* 経路探索
        if (!TrySearchAStar(currentNode, targetNode, currentX, targetX, out var path) || path.Count == 0)
            return NavPlan.CreateNoPath(currentX, targetX, moveSpeed, currentNode, targetNode);

        var firstLink = path[0];

        // 5. 接地中：動作種別：Jump / Dropに応じた到達・単方向推進判定
        if (firstLink.Action == NavActionType.Jump)
        {
            var distToTakeoff = MathF.Abs(currentX - firstLink.TakeoffX);
            if (distToTakeoff <= Config.TakeoffTriggerDistance)
            {
                currentActiveLink = firstLink;
                return new NavPlan(
                    NavStatus.ExecuteJump,
                    firstLink.RequiredJumpVx,
                    firstLink.RequiredJumpVy,
                    firstLink);
            }

            float walkDir = MathF.Sign(firstLink.TakeoffX - currentX);
            return new NavPlan(
                NavStatus.MoveToTakeoff,
                walkDir * moveSpeed,
                0.0f,
                firstLink);
        }

        // Drop
        var isDroppingLeft = firstLink.LandingX < firstLink.TakeoffX;
        if (isDroppingLeft)
        {
            // 左端から下落：TakeoffX より右側にいるときは左へ走る、TakeoffX 以左に到達したら下落完了まで左へ走り抜ける
            if (currentX > firstLink.TakeoffX + Config.DropTriggerDistance)
                return new NavPlan(
                    NavStatus.MoveToTakeoff,
                    -moveSpeed,
                    0.0f,
                    firstLink);

            currentActiveLink = firstLink;
            return new NavPlan(
                NavStatus.ExecuteDrop,
                -moveSpeed,
                0.0f,
                firstLink);
        }

        // isDroppingRight
        // 右端から下落：TakeoffX より左側にいるときは右へ走る、TakeoffX 以右に到達したら下落完了まで右へ走り抜ける
        if (currentX < firstLink.TakeoffX - Config.DropTriggerDistance)
            return new NavPlan(
                NavStatus.MoveToTakeoff,
                moveSpeed,
                0.0f,
                firstLink);

        currentActiveLink = firstLink;
        return new NavPlan(
            NavStatus.ExecuteDrop,
            moveSpeed,
            0.0f,
            firstLink);
    }

    /// <summary>
    ///     A* 最短経路探索を実行し、最適な遷移リンク列の計算を試行します。
    /// </summary>
    private bool TrySearchAStar(
        PlatformNode start,
        PlatformNode goal,
        float startX,
        float targetX,
        out List<PlatformLink> path)
    {
        _reusablePath.Clear();
        if (!_adjacency.TryGetValue(start.Id, out var startLinks) || startLinks.Count == 0)
        {
            path = _reusablePath;
            return false;
        }

        // 各リンクごとの最小到達累積コスト G
        _aStarGScore.Clear();
        _aStarCameFrom.Clear();
        _aStarOpenSet.Clear();

        // 終端目標地点：goal, targetXへの最良到達解
        PlatformLink? bestGoalLink = null;
        var bestGoalCost = float.MaxValue;

        // 1. 開始ノードから直接出発するすべてのリンクを初期化
        foreach (var link in startLinks)
        {
            var walkToTakeoff = MathF.Abs(startX - link.TakeoffX);
            var g = walkToTakeoff + link.Cost;
            _aStarGScore[link] = g;
            _aStarCameFrom[link] = null;

            if (link.ToNode.Id == goal.Id)
            {
                var totalCostToGoal = g + MathF.Abs(link.LandingX - targetX);
                if (totalCostToGoal < bestGoalCost)
                {
                    bestGoalCost = totalCostToGoal;
                    bestGoalLink = link;
                }
            }

            var h = Heuristic(link.LandingX, link.ToNode.TopY, targetX, goal.TopY);
            _aStarOpenSet.Enqueue(link, g + h);
        }

        // 2. A* / 最短経路探索ループ
        while (_aStarOpenSet.TryDequeue(out var currentLink, out var priority))
        {
            var currentG = _aStarGScore[currentLink];
            var currentH = Heuristic(currentLink.LandingX, currentLink.ToNode.TopY, targetX, goal.TopY);

            // 既に目標への確定コストを下回ることが不可能な場合、またはキュー内の古い項目はスキップ
            if (currentG + currentH >= bestGoalCost || priority > currentG + currentH + 0.001f) continue;

            // 次のプラットフォームからの遷移リンクを展開
            if (!_adjacency.TryGetValue(currentLink.ToNode.Id, out var nextLinks)) continue;

            foreach (var nextLink in nextLinks)
            {
                // 直前の出発プラットフォームへの逆走ループを抑制
                if (nextLink.ToNode.Id == currentLink.FromNode.Id) continue;

                var intermediateWalk = MathF.Abs(currentLink.LandingX - nextLink.TakeoffX);
                var tentativeG = currentG + intermediateWalk + nextLink.Cost;

                if (_aStarGScore.TryGetValue(nextLink, out var existingG) && !(tentativeG < existingG)) continue;
                _aStarGScore[nextLink] = tentativeG;
                _aStarCameFrom[nextLink] = currentLink;

                if (nextLink.ToNode.Id == goal.Id)
                {
                    var totalCostToGoal = tentativeG + MathF.Abs(nextLink.LandingX - targetX);
                    if (totalCostToGoal < bestGoalCost)
                    {
                        bestGoalCost = totalCostToGoal;
                        bestGoalLink = nextLink;
                    }
                }

                var nextH = Heuristic(nextLink.LandingX, nextLink.ToNode.TopY, targetX, goal.TopY);
                if (tentativeG + nextH < bestGoalCost) _aStarOpenSet.Enqueue(nextLink, tentativeG + nextH);
            }
        }

        if (bestGoalLink == null)
        {
            path = _reusablePath;
            return false;
        }

        // 3. 最良終端リンクから始点リンクまでバックトラックして経路を復元
        var curr = bestGoalLink;
        while (curr != null)
        {
            _reusablePath.Add(curr);
            curr = _aStarCameFrom[curr];
        }

        _reusablePath.Reverse();
        path = _reusablePath;
        return true;
    }

    /// <summary>
    ///     着地予測点から目標点までのユークリッド距離ヒューリスティック関数。
    /// </summary>
    private static float Heuristic(float fromX, float fromY, float targetX, float targetY)
    {
        var dx = fromX - targetX;
        var dy = fromY - targetY;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    ///     2 つのプラットフォーム間の物理跳躍・下落リンク可能性を評価し、グラフに追加します。
    /// </summary>
    private void EvaluateAndAddLinks(
        PlatformNode a,
        PlatformNode b,
        float jumpForce,
        float gravity,
        float moveSpeed,
        float maxJumpHeight)
    {
        var safeGravity = MathF.Max(1.0f, gravity);
        var safeJumpForce = MathF.Max(0.0f, jumpForce);
        var deltaY = a.TopY - b.TopY;

        // 1. 上方跳躍：nodeBがnodeAより高い位置にある場合
        if (deltaY > Config.MinJumpDeltaY && deltaY <= maxJumpHeight)
        {
            // 鉛直運動方程式: y = v0*t - 0.5*g*t^2 = deltaY
            // 放物線方程式から滞空時間を算出します
            var discriminant = safeJumpForce * safeJumpForce - 2.0f * safeGravity * deltaY;
            if (!(discriminant >= 0.0f)) return;

            // 着地目標高度 deltaY に上方から下降着地する物理滞空時間 tLand
            var tLand = (safeJumpForce + MathF.Sqrt(discriminant)) / safeGravity;
            var maxReachDist = moveSpeed * Config.MaxJumpVxRatio * tLand;

            // 水平重なり判定
            var overlapLeft = MathF.Max(a.LeftX, b.LeftX);
            var overlapRight = MathF.Min(a.RightX, b.RightX);

            if (overlapRight >= overlapLeft + Config.PlatformOverlapThreshold)
            {
                // 左側アプローチ斜め跳躍
                var leftTakeoffX = Math.Clamp(overlapLeft - Config.OverlapTakeoffOffsetX, a.LeftX + Config.PlatformEdgeMargin, a.RightX - Config.PlatformEdgeMargin);
                var leftLandingX = Math.Clamp(overlapLeft + Config.OverlapLandingOffsetX, b.LeftX + Config.PlatformEdgeMargin, b.RightX - Config.PlatformEdgeMargin);
                var leftFlightDist = leftLandingX - leftTakeoffX;
                if (leftFlightDist > 0.0f)
                {
                    var leftVx = leftFlightDist / MathF.Max(0.001f, tLand);
                    if (leftVx <= moveSpeed * Config.MaxJumpVxRatio)
                    {
                        leftVx = Math.Clamp(leftVx, moveSpeed * Config.MinJumpVxRatio, moveSpeed * Config.MaxJumpVxRatio);
                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            leftTakeoffX,
                            leftLandingX,
                            -safeJumpForce,
                            leftVx,
                            deltaY + Config.JumpCostBase));
                    }
                }

                // 右側アプローチ斜め跳躍
                var rightTakeoffX = Math.Clamp(overlapRight + Config.OverlapTakeoffOffsetX, a.LeftX + Config.PlatformEdgeMargin, a.RightX - Config.PlatformEdgeMargin);
                var rightLandingX = Math.Clamp(overlapRight - Config.OverlapLandingOffsetX, b.LeftX + Config.PlatformEdgeMargin, b.RightX - Config.PlatformEdgeMargin);
                var rightFlightDist = rightTakeoffX - rightLandingX;
                if (rightFlightDist > 0.0f)
                {
                    var rightVx = rightFlightDist / MathF.Max(0.001f, tLand);
                    if (rightVx <= moveSpeed * Config.MaxJumpVxRatio)
                    {
                        rightVx = Math.Clamp(rightVx, moveSpeed * Config.MinJumpVxRatio, moveSpeed * Config.MaxJumpVxRatio);
                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            rightTakeoffX,
                            rightLandingX,
                            -safeJumpForce,
                            -rightVx,
                            deltaY + Config.JumpCostBase));
                    }
                }
            }
            else if (b.LeftX > a.RightX) // B は A の右側
            {
                var gap = b.LeftX - a.RightX;
                if (gap <= maxReachDist)
                {
                    var takeoffX = Math.Clamp(a.RightX - Config.PlatformEdgeMargin, a.LeftX, a.RightX);
                    var landingX = Math.Clamp(b.LeftX + 2.0f * Config.PlatformEdgeMargin, b.LeftX, b.RightX);
                    var flightDist = landingX - takeoffX;
                    var reqVx = flightDist / MathF.Max(0.001f, tLand);

                    if (reqVx <= moveSpeed * Config.MaxJumpVxRatio)
                    {
                        reqVx = Math.Clamp(reqVx, moveSpeed * Config.MinJumpVxRatio, moveSpeed * Config.MaxJumpVxRatio);
                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX,
                            landingX,
                            -safeJumpForce,
                            reqVx,
                            deltaY + gap + Config.CrossGapJumpCostBase));
                    }
                }
            }
            else if (b.RightX < a.LeftX) // B は A の左側
            {
                var gap = a.LeftX - b.RightX;
                if (gap <= maxReachDist)
                {
                    var takeoffX = Math.Clamp(a.LeftX + Config.PlatformEdgeMargin, a.LeftX, a.RightX);
                    var landingX = Math.Clamp(b.RightX - 2.0f * Config.PlatformEdgeMargin, b.LeftX, b.RightX);
                    var flightDist = takeoffX - landingX;
                    var reqVx = flightDist / MathF.Max(0.001f, tLand);

                    if (reqVx <= moveSpeed * Config.MaxJumpVxRatio)
                    {
                        reqVx = Math.Clamp(reqVx, moveSpeed * Config.MinJumpVxRatio, moveSpeed * Config.MaxJumpVxRatio);
                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX,
                            landingX,
                            -safeJumpForce,
                            -reqVx,
                            deltaY + gap + Config.CrossGapJumpCostBase));
                    }
                }
            }
        }
        // 2. 下方落下：nodeBがnodeAより低い位置にある場合
        else if (deltaY < Config.MinDropDeltaY)
        {
            var dropHeight = -deltaY;
            var tFall = MathF.Sqrt(2.0f * dropHeight / safeGravity);
            var maxDropSpan = moveSpeed * tFall;

            // A の左端から下落
            if (b.ContainsX(a.LeftX - Config.PlatformEdgeMargin, maxDropSpan))
            {
                var takeoffX = Math.Clamp(a.LeftX + Config.PlatformEdgeMargin, a.LeftX, a.RightX);
                var landingX = Math.Clamp(a.LeftX - Config.DropLandingOffsetX, b.LeftX + Config.PlatformEdgeMargin, b.RightX - Config.PlatformEdgeMargin);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX,
                    landingX,
                    0.0f,
                    -moveSpeed,
                    dropHeight * Config.DropCostHeightMultiplier + Config.DropCostBase));
            }

            // A の右端から下落
            if (b.ContainsX(a.RightX + Config.PlatformEdgeMargin, maxDropSpan))
            {
                var takeoffX = Math.Clamp(a.RightX - Config.PlatformEdgeMargin, a.LeftX, a.RightX);
                var landingX = Math.Clamp(a.RightX + Config.DropLandingOffsetX, b.LeftX + Config.PlatformEdgeMargin, b.RightX - Config.PlatformEdgeMargin);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX,
                    landingX,
                    0.0f,
                    moveSpeed,
                    dropHeight * Config.DropCostHeightMultiplier + Config.DropCostBase));
            }
        }
    }

    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    private void AddLink(PlatformLink link)
    {
        var clampedTakeoffX = Math.Clamp(link.TakeoffX, link.FromNode.LeftX, link.FromNode.RightX);
        var clampedLandingX = Math.Clamp(link.LandingX, link.ToNode.LeftX, link.ToNode.RightX);

        var clampedLink = clampedTakeoffX == link.TakeoffX && clampedLandingX == link.LandingX
            ? link
            : new PlatformLink(
                link.FromNode,
                link.ToNode,
                link.Action,
                clampedTakeoffX,
                clampedLandingX,
                link.RequiredJumpVy,
                link.RequiredJumpVx,
                link.Cost);

        _links.Add(clampedLink);
        if (_adjacency.TryGetValue(clampedLink.FromNode.Id, out var list)) list.Add(clampedLink);
    }
}
