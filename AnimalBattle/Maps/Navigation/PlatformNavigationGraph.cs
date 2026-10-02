using System.Diagnostics.CodeAnalysis;
using ErrorOr;

namespace AnimalBattle.Maps.Navigation;

/// <summary>
///     2D プラットフォームナビゲーションのトポロジーグラフおよび A* 経路探索エンジン。
///     TileMap のプラットフォーム群から空間接続関係（跳躍/下落リンク）を自動構築し、
///     Boss AI にリアルタイムのプラットフォーム間追従・跳躍ナビゲーションプランを提供します。
/// </summary>
public sealed class PlatformNavigationGraph {
    private readonly Dictionary<int, List<PlatformLink>> _adjacency = new();
    private readonly Dictionary<PlatformLink, PlatformLink?> _aStarCameFrom = new();

    // A* 経路探索用の一時バッファ（毎フレームの GC 堆割り当てを完全に防止）
    private readonly Dictionary<PlatformLink, float> _aStarGScore = new();
    private readonly PriorityQueue<PlatformLink, float> _aStarOpenSet = new();
    private readonly List<PlatformLink> _links = new();
    private readonly List<PlatformNode> _nodes = new();

    /// <summary>すべての登録済みプラットフォームノード</summary>
    public IReadOnlyList<PlatformNode> Nodes => _nodes;

    /// <summary>すべての登録済みプラットフォーム間リンク</summary>
    public IReadOnlyList<PlatformLink> Links => _links;

    /// <summary>
    ///     プラットフォームノード群および物理パラメータからナビゲーショングラフを構築します。
    /// </summary>
    /// <param name="nodes">プラットフォームノード一覧</param>
    /// <param name="jumpForce">Boss の基準ジャンプ初速度 (正数、例: 600.0f)</param>
    /// <param name="gravity">ワールド重力加速度 (正数、例: 980.0f)</param>
    /// <param name="moveSpeed">地上巡航移動速度 (例: 220.0f)</param>
    public void Build(IReadOnlyList<PlatformNode> nodes, float jumpForce, float gravity, float moveSpeed)
    {
        _nodes.Clear();
        _links.Clear();
        _adjacency.Clear();

        _nodes.AddRange(nodes);
        foreach (var node in _nodes) _adjacency[node.Id] = [];

        var maxJumpHeight = jumpForce * jumpForce / (2.0f * MathF.Max(1.0f, gravity));

        for (var i = 0; i < _nodes.Count; i++)
        {
            var nodeA = _nodes[i];
            for (var j = 0; j < _nodes.Count; j++)
            {
                if (i == j) continue;

                var nodeB = _nodes[j];
                EvaluateAndAddLinks(nodeA, nodeB, jumpForce, gravity, moveSpeed, maxJumpHeight);
            }
        }
    }

    /// <summary>
    ///     跳躍放物線軌道の障害物遮断サンプリングにおける標準分割数。
    /// </summary>
    public const int DefaultTrajectoryRaycastSampleSteps = 16;

    /// <summary>
    ///     障害物矩形群を考慮し、障害物と交差する跳躍・下落リンクを除外したナビゲーショングラフを構築します。
    /// </summary>
    /// <param name="nodes">プラットフォームノード一覧</param>
    /// <param name="obstacles">障害物矩形一覧</param>
    /// <param name="jumpForce">Boss の基準ジャンプ初速度</param>
    /// <param name="gravity">ワールド重力加速度</param>
    /// <param name="moveSpeed">地上巡航移動速度</param>
    /// <param name="sampleSteps">放物線軌道の離散サンプリング分割数</param>
    public void BuildWithObstacles(
        IReadOnlyList<PlatformNode> nodes,
        IReadOnlyList<Box2D> obstacles,
        float jumpForce,
        float gravity,
        float moveSpeed,
        int sampleSteps = DefaultTrajectoryRaycastSampleSteps)
    {
        Build(nodes, jumpForce, gravity, moveSpeed);

        if (obstacles.Count == 0 || _links.Count == 0) return;

        var steps = Math.Max(2, sampleSteps);
        for (var i = _links.Count - 1; i >= 0; i--)
        {
            var link = _links[i];
            if (IsTrajectoryBlocked(link, obstacles, gravity, steps))
            {
                _adjacency[link.FromNode.Id].Remove(link);
                _links.RemoveAt(i);
            }
        }
    }

    private static bool IsTrajectoryBlocked(PlatformLink link, IReadOnlyList<Box2D> obstacles, float gravity, int sampleSteps)
    {
        const float velocityEpsilon = 0.001f;
        const float flightTimeEpsilon = 0.001f;

        var flightDist = link.LandingX - link.TakeoffX;
        var tFlight = MathF.Abs(link.RequiredJumpVx) > velocityEpsilon
            ? MathF.Abs(flightDist / link.RequiredJumpVx)
            : 1.0f;

        if (tFlight <= flightTimeEpsilon) return false;

        var dt = tFlight / sampleSteps;

        for (var step = 1; step < sampleSteps; step++)
        {
            var t = step * dt;
            var sampleX = link.TakeoffX + link.RequiredJumpVx * t;
            var sampleY = link.FromNode.TopY + (link.RequiredJumpVy * t + 0.5f * gravity * t * t);
            Vec2f samplePoint = new(sampleX, sampleY);

            for (var j = 0; j < obstacles.Count; j++)
            {
                if (PhysicsService.Intersects(samplePoint, obstacles[j]))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     指定された足元座標 (x, footY) が位置するプラットフォームノードを検索します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="footY">足元 Y 座標</param>
    /// <param name="toleranceY">垂直許容誤差</param>
    /// <param name="marginX">水平許容マージン（半身はみ出し許容用）</param>
    /// <returns>該当するプラットフォーム、存在しない場合は null</returns>
    public PlatformNode? FindNodeAt(float x, float footY, float toleranceY = 35.0f, float marginX = 55.0f)
    {
        PlatformNode? bestNode = null;
        var bestDistY = float.MaxValue;

        foreach (var node in _nodes)
            if (node.ContainsX(x, marginX))
            {
                var dy = MathF.Abs(footY - node.TopY);
                if (dy <= toleranceY && dy < bestDistY)
                {
                    bestDistY = dy;
                    bestNode = node;
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
    /// <returns>計算されたナビゲーションプラン (<see cref="NavPlan" />)</returns>
    public NavPlan FindPath(
        float currentX,
        float currentFootY,
        float targetX,
        float targetFootY,
        bool isGrounded,
        float moveSpeed)
    {
        var currentNode = FindNodeAt(currentX, currentFootY);
        var targetNode = FindNodeAt(targetX, targetFootY, 45.0f, 35.0f);

        // 1. 同一プラットフォーム上にいる場合は直接歩行追従
        if (currentNode != null && targetNode != null && currentNode.Id == targetNode.Id)
            return NavPlan.CreateDirectWalk(targetX, moveSpeed, currentNode, targetNode);

        // 2. プラットフォームノードが特定できない場合のフォールバック
        if (currentNode == null || targetNode == null)
            return NavPlan.CreateNoPath(targetX, moveSpeed, currentNode, targetNode);

        // 3. A* 経路探索 (Boss の現在 X 座標および目標 X 座標を考慮して最短総合到達経路を選択)
        var pathResult = SearchAStar(currentNode, targetNode, currentX, targetX);
        if (pathResult.IsError || pathResult.Value == null || pathResult.Value.Count == 0)
            return NavPlan.CreateNoPath(targetX, moveSpeed, currentNode, targetNode);

        var path = pathResult.Value;
        var firstLink = path[0];

        // 4. 空中滞空中の場合：着地目標地点に向かって水平推力を維持
        if (!isGrounded)
            return new NavPlan(
                NavStatus.InAir,
                firstLink.LandingX,
                firstLink.TakeoffX,
                firstLink.LandingX,
                firstLink.RequiredJumpVx,
                0.0f,
                currentNode,
                targetNode,
                firstLink);

        // 5. 接地中：動作種別（Jump / Drop）に応じた到達・単方向推進判定
        if (firstLink.Action == NavActionType.Jump)
        {
            var distToTakeoff = MathF.Abs(currentX - firstLink.TakeoffX);
            if (distToTakeoff <= 22.0f)
                return new NavPlan(
                    NavStatus.ExecuteJump,
                    firstLink.LandingX,
                    firstLink.TakeoffX,
                    firstLink.LandingX,
                    firstLink.RequiredJumpVx,
                    firstLink.RequiredJumpVy,
                    currentNode,
                    targetNode,
                    firstLink);

            float walkDir = MathF.Sign(firstLink.TakeoffX - currentX);
            return new NavPlan(
                NavStatus.MoveToTakeoff,
                firstLink.TakeoffX,
                firstLink.TakeoffX,
                firstLink.LandingX,
                walkDir * moveSpeed,
                0.0f,
                currentNode,
                targetNode,
                firstLink);
        }

        // Drop
        var isDroppingLeft = firstLink.LandingX < firstLink.TakeoffX;
        if (isDroppingLeft)
        {
            // 左端から下落：TakeoffX より右側にいるときは左へ走る、TakeoffX 以左に到達したら下落完了まで左へ走り抜ける
            if (currentX > firstLink.TakeoffX + 16.0f)
                return new NavPlan(
                    NavStatus.MoveToTakeoff,
                    firstLink.TakeoffX,
                    firstLink.TakeoffX,
                    firstLink.LandingX,
                    -moveSpeed,
                    0.0f,
                    currentNode,
                    targetNode,
                    firstLink);

            return new NavPlan(
                NavStatus.ExecuteDrop,
                firstLink.LandingX,
                firstLink.TakeoffX,
                firstLink.LandingX,
                -moveSpeed,
                0.0f,
                currentNode,
                targetNode,
                firstLink);
        }

        // isDroppingRight
        // 右端から下落：TakeoffX より左側にいるときは右へ走る、TakeoffX 以右に到達したら下落完了まで右へ走り抜ける
        if (currentX < firstLink.TakeoffX - 16.0f)
            return new NavPlan(
                NavStatus.MoveToTakeoff,
                firstLink.TakeoffX,
                firstLink.TakeoffX,
                firstLink.LandingX,
                moveSpeed,
                0.0f,
                currentNode,
                targetNode,
                firstLink);

        return new NavPlan(
            NavStatus.ExecuteDrop,
            firstLink.LandingX,
            firstLink.TakeoffX,
            firstLink.LandingX,
            moveSpeed,
            0.0f,
            currentNode,
            targetNode,
            firstLink);
    }

    /// <summary>
    ///     A* 最短経路探索を実行し、現在位置 (startX) から目標位置 (targetX) までの最適な遷移リンク列を計算します。
    ///     各リンクの離陸点・着地点、空中移動コスト、および中間・終端プラットフォーム上での歩行距離を厳密に考慮します。
    /// </summary>
    private ErrorOr<List<PlatformLink>> SearchAStar(
        PlatformNode start,
        PlatformNode goal,
        float startX,
        float targetX)
    {
        if (!_adjacency.TryGetValue(start.Id, out var startLinks) || startLinks.Count == 0)
            return Error.NotFound("Nav.NoPath", $"ノード {start.Id} から出る移動リンクが存在しません。");

        // 各リンクごとの最小到達累積コスト G(link)
        _aStarGScore.Clear();
        _aStarCameFrom.Clear();
        _aStarOpenSet.Clear();

        // 終端目標地点（goal, targetX）への最良到達解
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

                if (!_aStarGScore.TryGetValue(nextLink, out var existingG) || tentativeG < existingG)
                {
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
        }

        if (bestGoalLink == null)
            return Error.NotFound("Nav.NoPath", $"ノード {start.Id} から {goal.Id} への移動経路が見つかりませんでした。");

        // 3. 最良終端リンクから始点リンクまでバックトラックして経路を復元
        List<PlatformLink> path = new();
        var curr = bestGoalLink;
        while (curr != null)
        {
            path.Add(curr);
            curr = _aStarCameFrom[curr];
        }

        path.Reverse();
        return path;
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
        var deltaY = a.TopY - b.TopY;

        // 1. 上方跳躍 (nodeB が nodeA より高い位置にある)
        if (deltaY > 15.0f && deltaY <= maxJumpHeight)
        {
            // 上昇時間 t_rise: y(t) = v0*t - 0.5*g*t^2 = deltaY
            // g*t^2 - 2*v0*t + 2*deltaY = 0 -> t = (v0 - sqrt(v0^2 - 2*g*deltaY)) / g
            var discriminant = jumpForce * jumpForce - 2.0f * gravity * deltaY;
            if (discriminant >= 0.0f)
            {
                var tRise = (jumpForce - MathF.Sqrt(discriminant)) / gravity;
                var tApex = jumpForce / gravity;
                var tWindow = tApex * 1.5f;
                var maxReachDist = moveSpeed * tWindow;

                // 水平重なり判定
                var overlapLeft = MathF.Max(a.LeftX, b.LeftX);
                var overlapRight = MathF.Min(a.RightX, b.RightX);

                if (overlapRight >= overlapLeft + 20.0f)
                {
                    // 左側アプローチ斜め跳躍
                    var leftTakeoffX = Math.Clamp(overlapLeft - 25.0f, a.LeftX + 15.0f, a.RightX - 15.0f);
                    var leftLandingX = Math.Clamp(overlapLeft + 35.0f, b.LeftX + 15.0f, b.RightX - 15.0f);
                    AddLink(new PlatformLink(
                        a, b, NavActionType.Jump,
                        leftTakeoffX,
                        leftLandingX,
                        -jumpForce,
                        moveSpeed,
                        deltaY + 40.0f));

                    // 右側アプローチ斜め跳躍
                    var rightTakeoffX = Math.Clamp(overlapRight + 25.0f, a.LeftX + 15.0f, a.RightX - 15.0f);
                    var rightLandingX = Math.Clamp(overlapRight - 35.0f, b.LeftX + 15.0f, b.RightX - 15.0f);
                    AddLink(new PlatformLink(
                        a, b, NavActionType.Jump,
                        rightTakeoffX,
                        rightLandingX,
                        -jumpForce,
                        -moveSpeed,
                        deltaY + 40.0f));
                }
                else if (b.LeftX > a.RightX) // B は A の右側
                {
                    var gap = b.LeftX - a.RightX;
                    if (gap <= maxReachDist)
                    {
                        var takeoffX = Math.Clamp(a.RightX - 15.0f, a.LeftX, a.RightX);
                        var landingX = Math.Clamp(b.LeftX + 30.0f, b.LeftX, b.RightX);
                        var flightDist = landingX - takeoffX;
                        var reqVx = Math.Clamp(flightDist / MathF.Max(0.1f, tWindow), moveSpeed * 0.8f,
                            moveSpeed * 1.5f);

                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX,
                            landingX,
                            -jumpForce,
                            reqVx,
                            deltaY + gap + 60.0f));
                    }
                }
                else if (b.RightX < a.LeftX) // B は A の左側
                {
                    var gap = a.LeftX - b.RightX;
                    if (gap <= maxReachDist)
                    {
                        var takeoffX = Math.Clamp(a.LeftX + 15.0f, a.LeftX, a.RightX);
                        var landingX = Math.Clamp(b.RightX - 30.0f, b.LeftX, b.RightX);
                        var flightDist = takeoffX - landingX;
                        var reqVx = -Math.Clamp(flightDist / MathF.Max(0.1f, tWindow), moveSpeed * 0.8f,
                            moveSpeed * 1.5f);

                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX,
                            landingX,
                            -jumpForce,
                            reqVx,
                            deltaY + gap + 60.0f));
                    }
                }
            }
        }
        // 2. 下方落下 (nodeB が nodeA より低い位置にある)
        else if (deltaY < -20.0f)
        {
            var dropHeight = -deltaY;
            var tFall = MathF.Sqrt(2.0f * dropHeight / gravity);
            var maxDropSpan = moveSpeed * tFall;

            // A の左端から下落
            if (b.ContainsX(a.LeftX - 15.0f, maxDropSpan))
            {
                var takeoffX = Math.Clamp(a.LeftX + 15.0f, a.LeftX, a.RightX);
                var landingX = Math.Clamp(a.LeftX - 35.0f, b.LeftX + 15.0f, b.RightX - 15.0f);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX,
                    landingX,
                    0.0f,
                    -moveSpeed,
                    dropHeight * 0.6f + 30.0f));
            }

            // A の右端から下落
            if (b.ContainsX(a.RightX + 15.0f, maxDropSpan))
            {
                var takeoffX = Math.Clamp(a.RightX - 15.0f, a.LeftX, a.RightX);
                var landingX = Math.Clamp(a.RightX + 35.0f, b.LeftX + 15.0f, b.RightX - 15.0f);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX,
                    landingX,
                    0.0f,
                    moveSpeed,
                    dropHeight * 0.6f + 30.0f));
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