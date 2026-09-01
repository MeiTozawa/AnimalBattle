using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 技能判定時に必要な戦闘コンテキストパラメータを保持する軽量構造体です。
/// </summary>
public readonly struct AttackDecisionContext
{
    /// <summary>Boss とプレイヤー間の直線または水平距離（ピクセル）を取得します。</summary>
    public float Distance { get; }

    /// <summary>Boss とプレイヤーが同一足場に存在するかどうかを取得します。</summary>
    public bool IsSamePlatform { get; }

    /// <summary>プレイヤーが接地状態かどうかを取得します。</summary>
    public bool IsPlayerGrounded { get; }

    /// <summary>プレイヤーが滞空状態かどうかを取得します。</summary>
    public bool IsPlayerAirborne { get; }

    /// <summary>
    /// 技能判定コンテキストを生成します。
    /// </summary>
    /// <param name="distance">Boss とプレイヤー間の距離。</param>
    /// <param name="isSamePlatform">同一足場に存在するかどうか。</param>
    /// <param name="isPlayerGrounded">プレイヤーが接地状態かどうか。</param>
    /// <param name="isPlayerAirborne">プレイヤーが滞空状態かどうか。</param>
    public AttackDecisionContext(
        float distance,
        bool isSamePlatform,
        bool isPlayerGrounded,
        bool isPlayerAirborne)
    {
        Distance = distance;
        IsSamePlatform = isSamePlatform;
        IsPlayerGrounded = isPlayerGrounded;
        IsPlayerAirborne = isPlayerAirborne;
    }
}

/// <summary>
/// 技能意思決定ルールリストと現在の戦闘コンテキストを評価し、最適な出招技能を選択する評価器です。
/// </summary>
public static class BossDecisionEvaluator
{
    /// <summary>
    /// 候補ルールの中で最もスコアが高い技能を決定論的に選択します。
    /// </summary>
    /// <param name="rules">判定対象の技能意思決定ルール一覧。</param>
    /// <param name="cooldownTracker">冷却時間追跡器。</param>
    /// <param name="context">現在の戦闘判定コンテキスト。</param>
    /// <returns>選択された攻撃識別子。条件に合致する技能がない場合は AttackId.Unknown。</returns>
    public static AttackId EvaluateBest(
        IReadOnlyList<AttackDecisionRule> rules,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(cooldownTracker);

        AttackId bestAttack = AttackId.Unknown;
        float bestScore = -1.0f;

        for (int index = 0; index < rules.Count; index++)
        {
            AttackDecisionRule rule = rules[index];
            if (!IsRuleEligible(rule, cooldownTracker, in context, out float score))
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestAttack = rule.AttackId;
            }
        }

        return bestAttack;
    }

    /// <summary>
    /// 候補ルールの中からスコアに応じた加重ランダム抽選で技能を選択します。
    /// </summary>
    /// <param name="rules">判定対象の技能意思決定ルール一覧。</param>
    /// <param name="cooldownTracker">冷却時間追跡器。</param>
    /// <param name="context">現在の戦闘判定コンテキスト。</param>
    /// <param name="random">乱数生成器（null の場合は既定の Random を使用）。</param>
    /// <returns>選択された攻撃識別子。条件に合致する技能がない場合は AttackId.Unknown。</returns>
    public static AttackId EvaluateWeighted(
        IReadOnlyList<AttackDecisionRule> rules,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(cooldownTracker);

        Span<AttackId> candidateIds = stackalloc AttackId[rules.Count];
        Span<float> candidateScores = stackalloc float[rules.Count];
        int count = 0;
        float totalScore = 0.0f;

        for (int index = 0; index < rules.Count; index++)
        {
            AttackDecisionRule rule = rules[index];
            if (!IsRuleEligible(rule, cooldownTracker, in context, out float score))
            {
                continue;
            }

            candidateIds[count] = rule.AttackId;
            candidateScores[count] = score;
            totalScore += score;
            count++;
        }

        if (count == 0 || totalScore <= 0.0f)
        {
            return AttackId.Unknown;
        }

        if (count == 1)
        {
            return candidateIds[0];
        }

        random ??= Random.Shared;
        float roll = (float)random.NextDouble() * totalScore;
        float cumulative = 0.0f;

        for (int index = 0; index < count; index++)
        {
            cumulative += candidateScores[index];
            if (roll <= cumulative)
            {
                return candidateIds[index];
            }
        }

        return candidateIds[count - 1];
    }

    private static bool IsRuleEligible(
        in AttackDecisionRule rule,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context,
        out float score)
    {
        score = 0.0f;

        if (!rule.IsValid)
        {
            return false;
        }

        // 1. 冷却時間フィルタ
        if (!cooldownTracker.IsReady(rule.AttackId))
        {
            return false;
        }

        // 2. 距離フィルタ
        if (context.Distance < rule.MinDistance || context.Distance > rule.MaxDistance)
        {
            return false;
        }

        // 3. 足場制約フィルタ
        if (rule.RequireSamePlatform && !context.IsSamePlatform)
        {
            return false;
        }

        // 4. 接地制約フィルタ
        if (rule.RequirePlayerGrounded.HasValue && rule.RequirePlayerGrounded.Value != context.IsPlayerGrounded)
        {
            return false;
        }

        // 5. スコア計算
        score = rule.Weight;
        if (context.IsPlayerAirborne && rule.PreferAirborneBonus > 0.0f)
        {
            score *= rule.PreferAirborneBonus;
        }

        return score > 0.0f;
    }
}
