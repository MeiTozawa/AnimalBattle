namespace AnimalBattle.Enemies.Services;

using System.Collections.Generic;
using Flecs.NET.Core;

/// <summary>
///     雑魚敵の最適な生成座標を決定する選定サービス。
///     全プレイヤーとの安全距離を確保し、既存の小怪群から最も離れた生成点を選出します。
/// </summary>
public static class MinionSpawnService
{
    [ThreadStatic]
    private static List<Vec2f>? s_cachedPlayerPositions;

    [ThreadStatic]
    private static List<Vec2f>? s_cachedMinionPositions;

    [ThreadStatic]
    private static List<Vec2f>? s_cachedSafeCandidates;

    /// <summary>
    ///     指定された雑魚敵種別の最適な生成座標を選定します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    /// <param name="type">雑魚敵種別</param>
    /// <returns>選定された生成座標</returns>
    public static Vec2f SelectBestSpawnPoint(World world, MinionType type)
    {
        var points = SelectBestSpawnPoints(world, type, 1);
        return points.Count > 0 ? points[0] : Vec2f.Zero;
    }

    /// <summary>
    ///     指定された雑魚敵種別の最適な生成座標群を選定します。
    ///     全プレイヤーとの安全距離を確保し、既存および同一バッチ内で選定済みの小怪群から最も離れた点を順次選出します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    /// <param name="type">雑魚敵種別</param>
    /// <param name="count">選定する座標の個数</param>
    /// <returns>選定された生成座標リスト</returns>
    public static List<Vec2f> SelectBestSpawnPoints(World world, MinionType type, int count)
    {
        if (count <= 0)
        {
            return new List<Vec2f>(0);
        }

        var minionCfg = world.Get<MinionConfig>();
        var (spawnPoints, minPlayerDist, fallbackOffset) = type switch
        {
            MinionType.Flying => (minionCfg.FlyingMinion.SpawnPoints, minionCfg.FlyingMinion.MinPlayerDistance, minionCfg.FlyingMinion.SpawnPos),
            MinionType.Ground => (minionCfg.GroundMinion.SpawnPoints, minionCfg.GroundMinion.MinPlayerDistance, minionCfg.GroundMinion.SpawnPos),
            _ => AssertHelper.FailWithDefault(
                $"[MinionSpawnService 警告] 未知の MinionType です: {type}",
                (minionCfg.GroundMinion.SpawnPoints, minionCfg.GroundMinion.MinPlayerDistance, minionCfg.GroundMinion.SpawnPos))
        };

        s_cachedPlayerPositions ??= new List<Vec2f>(4);
        s_cachedPlayerPositions.Clear();
        var playerPositions = s_cachedPlayerPositions;

        using var playerQuery = world.QueryBuilder<Transform>().With<PlayerTag>().Without<DeadTag>().Build();
        playerQuery.Each((ref pt) =>
        {
            playerPositions.Add(pt.Position);
        });

        var results = new List<Vec2f>(count);

        // 候補点リストが空の場合は従来の相対オフセット方式へ退避
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            var anchor = playerPositions.Count > 0 ? playerPositions[0] : Vec2f.Zero;
            var fallback = playerPositions.Count > 0 ? anchor + fallbackOffset : fallbackOffset;
            for (var i = 0; i < count; i++)
            {
                results.Add(fallback);
            }
            return results;
        }

        s_cachedMinionPositions ??= new List<Vec2f>(16);
        s_cachedMinionPositions.Clear();
        var minionPositions = s_cachedMinionPositions;

        using var minionQuery = world.QueryBuilder<Transform>().With<MinionTag>().Without<DeadTag>().Build();
        minionQuery.Each((ref mt) =>
        {
            minionPositions.Add(mt.Position);
        });

        // 1. 全プレイヤーとの距離判定（各候補点に対し、最も近いプレイヤーとの距離を計算）
        var minPlayerDistSq = minPlayerDist * minPlayerDist;
        s_cachedSafeCandidates ??= new List<Vec2f>(spawnPoints.Count);
        s_cachedSafeCandidates.Clear();
        var safeCandidates = s_cachedSafeCandidates;
        var bestFallbackPoint = spawnPoints[0];
        var maxNearestPlayerDistSq = -1.0f;

        for (var i = 0; i < spawnPoints.Count; i++)
        {
            var p = spawnPoints[i];

            // プレイヤーが存在しない場合は全点安全とみなす
            var nearestPlayerDistSq = float.MaxValue;
            for (var pl = 0; pl < playerPositions.Count; pl++)
            {
                var dSq = (p - playerPositions[pl]).LengthSquared();
                if (dSq < nearestPlayerDistSq)
                {
                    nearestPlayerDistSq = dSq;
                }
            }

            // 全候補点が近すぎる場合のフォールバック用に、プレイヤー群から最も離れた点を追跡
            if (nearestPlayerDistSq > maxNearestPlayerDistSq)
            {
                maxNearestPlayerDistSq = nearestPlayerDistSq;
                bestFallbackPoint = p;
            }

            // 最も近いプレイヤーとの距離が安全閾値以上であれば合格
            if (nearestPlayerDistSq >= minPlayerDistSq)
            {
                safeCandidates.Add(p);
            }
        }

        // 全候補点がプレイヤーのいずれかに近すぎる場合の降級退避
        if (safeCandidates.Count == 0)
        {
            for (var i = 0; i < count; i++)
            {
                results.Add(bestFallbackPoint);
            }
            return results;
        }

        // 2 & 3. 順次候補点を選出（Max-Min 準則、選定された点は minionPositions に蓄積して分散を最大化）
        for (var step = 0; step < count; step++)
        {
            Vec2f chosen;
            if (minionPositions.Count == 0)
            {
                // 既存小怪が存在しない初手：安全候補点のうちプレイヤー群から最も離れた点を選出
                var bestP = safeCandidates[0];
                var maxDistSq = -1.0f;
                for (var i = 0; i < safeCandidates.Count; i++)
                {
                    var p = safeCandidates[i];
                    var nearestPlayerDistSq = float.MaxValue;
                    for (var pl = 0; pl < playerPositions.Count; pl++)
                    {
                        var dSq = (p - playerPositions[pl]).LengthSquared();
                        if (dSq < nearestPlayerDistSq)
                        {
                            nearestPlayerDistSq = dSq;
                        }
                    }

                    if (nearestPlayerDistSq > maxDistSq)
                    {
                        maxDistSq = nearestPlayerDistSq;
                        bestP = p;
                    }
                }
                chosen = bestP;
            }
            else
            {
                // 既存または選定済み小怪が存在する場合：Max-Min 準則（最近傍小怪との距離が最大となる点）
                var bestCandidate = safeCandidates[0];
                var maxNearestMinionDistSq = -1.0f;

                for (var i = 0; i < safeCandidates.Count; i++)
                {
                    var p = safeCandidates[i];
                    var minMinionDistSq = float.MaxValue;

                    for (var m = 0; m < minionPositions.Count; m++)
                    {
                        var dSq = (p - minionPositions[m]).LengthSquared();
                        if (dSq < minMinionDistSq)
                        {
                            minMinionDistSq = dSq;
                        }
                    }

                    if (minMinionDistSq > maxNearestMinionDistSq)
                    {
                        maxNearestMinionDistSq = minMinionDistSq;
                        bestCandidate = p;
                    }
                }
                chosen = bestCandidate;
            }

            results.Add(chosen);
            minionPositions.Add(chosen);
        }

        return results;
    }
}
