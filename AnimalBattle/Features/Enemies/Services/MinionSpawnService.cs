namespace AnimalBattle.Enemies.Services;

using System;
using System.Collections.Generic;
using Flecs.NET.Core;

/// <summary>
///     雑魚敵の最適な生成座標を決定する選定サービス。
///     全プレイヤーとの安全距離を確保し、既存の小怪群から最も離れた生成点を選出します。
/// </summary>
public static class MinionSpawnService
{
    /// <summary>
    ///     指定された雑魚敵種別の最適な生成座標を選定します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    /// <param name="type">雑魚敵種別</param>
    /// <returns>選定された生成座標</returns>
    public static Vec2f SelectBestSpawnPoint(World world, MinionType type)
    {
        var minionCfg = world.Get<MinionConfig>();
        var (spawnPoints, minPlayerDist, fallbackOffset) = type switch
        {
            MinionType.Flying => (minionCfg.FlyingMinion.SpawnPoints, minionCfg.FlyingMinion.MinPlayerDistance, minionCfg.FlyingMinion.SpawnPos),
            MinionType.Ground => (minionCfg.GroundMinion.SpawnPoints, minionCfg.GroundMinion.MinPlayerDistance, minionCfg.GroundMinion.SpawnPos),
            _ => AssertHelper.FailWithDefault(
                $"[MinionSpawnService 警告] 未知の MinionType です: {type}",
                (minionCfg.GroundMinion.SpawnPoints, minionCfg.GroundMinion.MinPlayerDistance, minionCfg.GroundMinion.SpawnPos))
        };

        // 生存中の全プレイヤー位置を収集（将来のマルチプレイ対応）
        var playerPositions = new List<Vec2f>();
        using var playerQuery = world.QueryBuilder<Transform>().With<PlayerTag>().Without<DeadTag>().Build();
        playerQuery.Each((ref Transform pt) =>
        {
            playerPositions.Add(pt.Position);
        });

        // 候補点リストが空の場合は従来の相対オフセット方式へ退避
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            var anchor = playerPositions.Count > 0 ? playerPositions[0] : Vec2f.Zero;
            return playerPositions.Count > 0 ? anchor + fallbackOffset : fallbackOffset;
        }

        // 既存の生存小怪位置を収集
        var minionPositions = new List<Vec2f>();
        using var minionQuery = world.QueryBuilder<Transform>().With<MinionTag>().Without<DeadTag>().Build();
        minionQuery.Each((ref Transform mt) =>
        {
            minionPositions.Add(mt.Position);
        });

        // 1. 全プレイヤーとの距離判定（各候補点に対し、最も近いプレイヤーとの距離を計算）
        var minPlayerDistSq = minPlayerDist * minPlayerDist;
        var safeCandidates = new List<Vec2f>();
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
            return bestFallbackPoint;
        }

        // 2. 既存小怪が存在しない場合：安全候補点のうちプレイヤー群から最も離れた点を選出
        if (minionPositions.Count == 0)
        {
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
            return bestP;
        }

        // 3. 既存小怪が存在する場合：Max-Min 準則（最近傍小怪との距離が最大となる点）
        var bestCandidate = safeCandidates[0];
        var maxNearestMinionDistSq = -1.0f;

        foreach (var p in safeCandidates)
        {
            var minMinionDistSq = float.MaxValue;

            foreach (var m in minionPositions)
            {
                var dSq = (p - m).LengthSquared();
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

        return bestCandidate;
    }
}
