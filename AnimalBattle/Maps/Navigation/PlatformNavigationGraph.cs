using System;
using System.Collections.Generic;

namespace AnimalBattle.Maps.Navigation;

/// <summary>
/// 2D プラットフォームナビゲーションのトポロジーグラフおよび A* 経路探索エンジン。
/// TileMap のプラットフォーム群から空間接続関係（跳躍/下落リンク）を自動構築し、
/// Boss AI にリアルタイムのプラットフォーム間追従・跳躍ナビゲーションプランを提供します。
/// </summary>
public sealed class PlatformNavigationGraph
{
    private readonly List<PlatformNode> _nodes = new();
    private readonly List<PlatformLink> _links = new();
    private readonly Dictionary<int, List<PlatformLink>> _adjacency = new();

    /// <summary>すべての登録済みプラットフォームノード</summary>
    public IReadOnlyList<PlatformNode> Nodes => _nodes;

    /// <summary>すべての登録済みプラットフォーム間リンク</summary>
    public IReadOnlyList<PlatformLink> Links => _links;

    /// <summary>
    /// プラットフォームノード群および物理パラメータからナビゲーショングラフを構築します。
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
        foreach (PlatformNode node in _nodes)
        {
            _adjacency[node.Id] = new List<PlatformLink>();
        }

        float maxJumpHeight = (jumpForce * jumpForce) / (2.0f * MathF.Max(1.0f, gravity));

        for (int i = 0; i < _nodes.Count; i++)
        {
            PlatformNode nodeA = _nodes[i];
            for (int j = 0; j < _nodes.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                PlatformNode nodeB = _nodes[j];
                EvaluateAndAddLinks(nodeA, nodeB, jumpForce, gravity, moveSpeed, maxJumpHeight);
            }
        }
    }

    /// <summary>
    /// 指定された足元座標 (x, footY) が位置するプラットフォームノードを検索します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="footY">足元 Y 座標</param>
    /// <param name="toleranceY">垂直許容誤差</param>
    /// <param name="marginX">水平許容マージン（半身はみ出し許容用）</param>
    /// <returns>該当するプラットフォーム、存在しない場合は null</returns>
    public PlatformNode? FindNodeAt(float x, float footY, float toleranceY = 35.0f, float marginX = 55.0f)
    {
        PlatformNode? bestNode = null;
        float bestDistY = float.MaxValue;

        foreach (PlatformNode node in _nodes)
        {
            if (node.ContainsX(x, margin: marginX))
            {
                float dy = MathF.Abs(footY - node.TopY);
                if (dy <= toleranceY && dy < bestDistY)
                {
                    bestDistY = dy;
                    bestNode = node;
                }
            }
        }

        return bestNode;
    }

    /// <summary>
    /// 現在位置からプレイヤー目標位置への次フレームナビゲーションプランを計算します。
    /// </summary>
    /// <param name="currentX">Boss の中心 X 座標</param>
    /// <param name="currentFootY">Boss の足元 Y 座標</param>
    /// <param name="targetX">プレイヤーの中心 X 座標</param>
    /// <param name="targetFootY">プレイヤーの足元 Y 座標</param>
    /// <param name="isGrounded">Boss が接地中かどうか</param>
    /// <param name="moveSpeed">巡航移動速度</param>
    /// <returns>計算されたナビゲーションプラン (<see cref="NavPlan"/>)</returns>
    public NavPlan FindPath(
        float currentX,
        float currentFootY,
        float targetX,
        float targetFootY,
        bool isGrounded,
        float moveSpeed)
    {
        PlatformNode? currentNode = FindNodeAt(currentX, currentFootY, toleranceY: 35.0f, marginX: 55.0f);
        PlatformNode? targetNode = FindNodeAt(targetX, targetFootY, toleranceY: 45.0f, marginX: 35.0f);

        // 1. 同一プラットフォーム上にいる場合は直接歩行追従
        if (currentNode != null && targetNode != null && currentNode.Id == targetNode.Id)
        {
            return NavPlan.CreateDirectWalk(targetX, moveSpeed, currentNode, targetNode);
        }

        // 2. プラットフォームノードが特定できない場合のフォールバック
        if (currentNode == null || targetNode == null)
        {
            return NavPlan.CreateNoPath(targetX, moveSpeed, currentNode, targetNode);
        }

        // 3. A* 経路探索 (Boss の現在 X 座標を考慮して最近の起跳点を選択)
        List<PlatformLink>? path = SearchAStar(currentNode, targetNode, currentX);
        if (path == null || path.Count == 0)
        {
            return NavPlan.CreateNoPath(targetX, moveSpeed, currentNode, targetNode);
        }

        PlatformLink firstLink = path[0];

        // 4. 空中滞空中の場合：着地目標地点に向かって水平推力を維持
        if (!isGrounded)
        {
            return new NavPlan(
                NavStatus.InAir,
                targetX: firstLink.LandingX,
                takeoffX: firstLink.TakeoffX,
                landingX: firstLink.LandingX,
                requiredVx: firstLink.RequiredJumpVx,
                requiredVy: 0.0f,
                currentNode: currentNode,
                targetNode: targetNode,
                nextLink: firstLink);
        }

        // 5. 接地中：動作種別（Jump / Drop）に応じた到達・単方向推進判定
        if (firstLink.Action == NavActionType.Jump)
        {
            float distToTakeoff = MathF.Abs(currentX - firstLink.TakeoffX);
            if (distToTakeoff <= 22.0f)
            {
                return new NavPlan(
                    NavStatus.ExecuteJump,
                    targetX: firstLink.LandingX,
                    takeoffX: firstLink.TakeoffX,
                    landingX: firstLink.LandingX,
                    requiredVx: firstLink.RequiredJumpVx,
                    requiredVy: firstLink.RequiredJumpVy,
                    currentNode: currentNode,
                    targetNode: targetNode,
                    nextLink: firstLink);
            }
            else
            {
                float walkDir = MathF.Sign(firstLink.TakeoffX - currentX);
                return new NavPlan(
                    NavStatus.MoveToTakeoff,
                    targetX: firstLink.TakeoffX,
                    takeoffX: firstLink.TakeoffX,
                    landingX: firstLink.LandingX,
                    requiredVx: walkDir * moveSpeed,
                    requiredVy: 0.0f,
                    currentNode: currentNode,
                    targetNode: targetNode,
                    nextLink: firstLink);
            }
        }
        else // Drop
        {
            bool isDroppingLeft = firstLink.LandingX < firstLink.TakeoffX;
            if (isDroppingLeft)
            {
                // 左端から下落：TakeoffX より右側にいるときは左へ走る、TakeoffX 以左に到達したら下落完了まで左へ走り抜ける
                if (currentX > firstLink.TakeoffX + 16.0f)
                {
                    return new NavPlan(
                        NavStatus.MoveToTakeoff,
                        targetX: firstLink.TakeoffX,
                        takeoffX: firstLink.TakeoffX,
                        landingX: firstLink.LandingX,
                        requiredVx: -moveSpeed,
                        requiredVy: 0.0f,
                        currentNode: currentNode,
                        targetNode: targetNode,
                        nextLink: firstLink);
                }
                else
                {
                    return new NavPlan(
                        NavStatus.ExecuteDrop,
                        targetX: firstLink.LandingX,
                        takeoffX: firstLink.TakeoffX,
                        landingX: firstLink.LandingX,
                        requiredVx: -moveSpeed,
                        requiredVy: 0.0f,
                        currentNode: currentNode,
                        targetNode: targetNode,
                        nextLink: firstLink);
                }
            }
            else // isDroppingRight
            {
                // 右端から下落：TakeoffX より左側にいるときは右へ走る、TakeoffX 以右に到達したら下落完了まで右へ走り抜ける
                if (currentX < firstLink.TakeoffX - 16.0f)
                {
                    return new NavPlan(
                        NavStatus.MoveToTakeoff,
                        targetX: firstLink.TakeoffX,
                        takeoffX: firstLink.TakeoffX,
                        landingX: firstLink.LandingX,
                        requiredVx: moveSpeed,
                        requiredVy: 0.0f,
                        currentNode: currentNode,
                        targetNode: targetNode,
                        nextLink: firstLink);
                }
                else
                {
                    return new NavPlan(
                        NavStatus.ExecuteDrop,
                        targetX: firstLink.LandingX,
                        takeoffX: firstLink.TakeoffX,
                        landingX: firstLink.LandingX,
                        requiredVx: moveSpeed,
                        requiredVy: 0.0f,
                        currentNode: currentNode,
                        targetNode: targetNode,
                        nextLink: firstLink);
                }
            }
        }
    }

    /// <summary>
    /// A* 最短経路探索を実行します。
    /// </summary>
    private List<PlatformLink>? SearchAStar(PlatformNode start, PlatformNode goal, float startX)
    {
        Dictionary<int, float> gScore = new();
        Dictionary<int, PlatformLink> cameFromLink = new();
        Dictionary<int, int> cameFromNode = new();
        PriorityQueue<int, float> openSet = new();

        foreach (PlatformNode node in _nodes)
        {
            gScore[node.Id] = float.MaxValue;
        }

        gScore[start.Id] = 0.0f;
        openSet.Enqueue(start.Id, Heuristic(start, goal));
        HashSet<int> closedSet = new();

        while (openSet.Count > 0)
        {
            int currentId = openSet.Dequeue();
            if (currentId == goal.Id)
            {
                return ReconstructPath(start.Id, goal.Id, cameFromLink, cameFromNode);
            }

            if (!closedSet.Add(currentId) || !_adjacency.TryGetValue(currentId, out List<PlatformLink>? links))
            {
                continue;
            }

            foreach (PlatformLink link in links)
            {
                int neighborId = link.ToNode.Id;
                if (closedSet.Contains(neighborId))
                {
                    continue;
                }

                float linkTravelCost = currentId == start.Id
                    ? MathF.Abs(startX - link.TakeoffX) + link.Cost
                    : link.Cost;

                float tentativeG = gScore[currentId] + linkTravelCost;
                if (tentativeG < gScore[neighborId])
                {
                    gScore[neighborId] = tentativeG;
                    cameFromLink[neighborId] = link;
                    cameFromNode[neighborId] = currentId;
                    openSet.Enqueue(neighborId, tentativeG + Heuristic(link.ToNode, goal));
                }
            }
        }

        return null;
    }

    private static List<PlatformLink> ReconstructPath(
        int startId,
        int goalId,
        Dictionary<int, PlatformLink> cameFromLink,
        Dictionary<int, int> cameFromNode)
    {
        List<PlatformLink> path = new();
        int curr = goalId;
        while (curr != startId && cameFromLink.TryGetValue(curr, out PlatformLink? link))
        {
            path.Add(link);
            curr = cameFromNode[curr];
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// 2 プラットフォーム間のユークリッド距離ヒューリスティック関数。
    /// </summary>
    private static float Heuristic(PlatformNode a, PlatformNode b)
    {
        float dx = a.CenterX - b.CenterX;
        float dy = a.TopY - b.TopY;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// 2 つのプラットフォーム間の物理跳躍・下落リンク可能性を評価し、グラフに追加します。
    /// </summary>
    private void EvaluateAndAddLinks(
        PlatformNode a,
        PlatformNode b,
        float jumpForce,
        float gravity,
        float moveSpeed,
        float maxJumpHeight)
    {
        float deltaY = a.TopY - b.TopY;

        // 1. 上方跳躍 (nodeB が nodeA より高い位置にある)
        if (deltaY > 15.0f && deltaY <= maxJumpHeight)
        {
            // 上昇時間 t_rise: y(t) = v0*t - 0.5*g*t^2 = deltaY
            // g*t^2 - 2*v0*t + 2*deltaY = 0 -> t = (v0 - sqrt(v0^2 - 2*g*deltaY)) / g
            float discriminant = jumpForce * jumpForce - 2.0f * gravity * deltaY;
            if (discriminant >= 0.0f)
            {
                float tRise = (jumpForce - MathF.Sqrt(discriminant)) / gravity;
                float tApex = jumpForce / gravity;
                float tWindow = tApex * 1.5f;
                float maxReachDist = moveSpeed * tWindow;

                // 水平重なり判定
                float overlapLeft = MathF.Max(a.LeftX, b.LeftX);
                float overlapRight = MathF.Min(a.RightX, b.RightX);

                if (overlapRight >= overlapLeft + 20.0f)
                {
                    // 左側アプローチ斜め跳躍
                    float leftTakeoffX = Math.Max(a.LeftX + 20.0f, overlapLeft - 25.0f);
                    float leftLandingX = Math.Min(b.RightX - 20.0f, overlapLeft + 35.0f);
                    AddLink(new PlatformLink(
                        a, b, NavActionType.Jump,
                        takeoffX: leftTakeoffX,
                        landingX: leftLandingX,
                        requiredJumpVy: -jumpForce,
                        requiredJumpVx: moveSpeed,
                        cost: deltaY + 40.0f));

                    // 右側アプローチ斜め跳躍
                    float rightTakeoffX = Math.Min(a.RightX - 20.0f, overlapRight + 25.0f);
                    float rightLandingX = Math.Max(b.LeftX + 20.0f, overlapRight - 35.0f);
                    AddLink(new PlatformLink(
                        a, b, NavActionType.Jump,
                        takeoffX: rightTakeoffX,
                        landingX: rightLandingX,
                        requiredJumpVy: -jumpForce,
                        requiredJumpVx: -moveSpeed,
                        cost: deltaY + 40.0f));
                }
                else if (b.LeftX > a.RightX) // B は A の右側
                {
                    float gap = b.LeftX - a.RightX;
                    if (gap <= maxReachDist)
                    {
                        float takeoffX = a.RightX - 15.0f;
                        float landingX = b.LeftX + 30.0f;
                        float flightDist = landingX - takeoffX;
                        float reqVx = Math.Clamp(flightDist / MathF.Max(0.1f, tWindow), moveSpeed * 0.8f, moveSpeed * 1.5f);

                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX: takeoffX,
                            landingX: landingX,
                            requiredJumpVy: -jumpForce,
                            requiredJumpVx: reqVx,
                            cost: deltaY + gap + 60.0f));
                    }
                }
                else if (b.RightX < a.LeftX) // B は A の左側
                {
                    float gap = a.LeftX - b.RightX;
                    if (gap <= maxReachDist)
                    {
                        float takeoffX = a.LeftX + 15.0f;
                        float landingX = b.RightX - 30.0f;
                        float flightDist = takeoffX - landingX;
                        float reqVx = -Math.Clamp(flightDist / MathF.Max(0.1f, tWindow), moveSpeed * 0.8f, moveSpeed * 1.5f);

                        AddLink(new PlatformLink(
                            a, b, NavActionType.Jump,
                            takeoffX: takeoffX,
                            landingX: landingX,
                            requiredJumpVy: -jumpForce,
                            requiredJumpVx: reqVx,
                            cost: deltaY + gap + 60.0f));
                    }
                }
            }
        }
        // 2. 下方落下 (nodeB が nodeA より低い位置にある)
        else if (deltaY < -20.0f)
        {
            float dropHeight = -deltaY;
            float tFall = MathF.Sqrt(2.0f * dropHeight / gravity);
            float maxDropSpan = moveSpeed * tFall;

            // A の左端から下落
            if (b.ContainsX(a.LeftX - 15.0f, margin: maxDropSpan))
            {
                float takeoffX = a.LeftX + 15.0f;
                float landingX = Math.Clamp(a.LeftX - 35.0f, b.LeftX + 20.0f, b.RightX - 20.0f);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX: takeoffX,
                    landingX: landingX,
                    requiredJumpVy: 0.0f,
                    requiredJumpVx: -moveSpeed,
                    cost: dropHeight * 0.6f + 30.0f));
            }

            // A の右端から下落
            if (b.ContainsX(a.RightX + 15.0f, margin: maxDropSpan))
            {
                float takeoffX = a.RightX - 15.0f;
                float landingX = Math.Clamp(a.RightX + 35.0f, b.LeftX + 20.0f, b.RightX - 20.0f);

                AddLink(new PlatformLink(
                    a, b, NavActionType.Drop,
                    takeoffX: takeoffX,
                    landingX: landingX,
                    requiredJumpVy: 0.0f,
                    requiredJumpVx: moveSpeed,
                    cost: dropHeight * 0.6f + 30.0f));
            }
        }
    }

    private void AddLink(PlatformLink link)
    {
        _links.Add(link);
        if (_adjacency.TryGetValue(link.FromNode.Id, out List<PlatformLink>? list))
        {
            list.Add(link);
        }
    }
}
