using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Decision;

/// <summary>
///     技能判定時に必要な戦闘コンテキストパラメータを保持する軽量構造体です。
/// </summary>
/// <remarks>
///     技能判定コンテキストを生成します。
/// </remarks>
/// <param name="distance">Boss とプレイヤー間の距離。</param>
/// <param name="isSamePlatform">同一足場に存在するかどうか。</param>
/// <param name="isPlayerGrounded">プレイヤーが接地状態かどうか。</param>
/// <param name="isPlayerAirborne">プレイヤーが滞空状態かどうか。</param>
public readonly struct AttackDecisionContext(
    float distance,
    bool isSamePlatform,
    bool isPlayerGrounded,
    bool isPlayerAirborne) {
    /// <summary>Boss とプレイヤー間の距離を取得します。</summary>
    public float Distance { get; } = distance;

    /// <summary>Boss とプレイヤーが同一足場に存在するかどうかを取得します。</summary>
    public bool IsSamePlatform { get; } = isSamePlatform;

    /// <summary>プレイヤーが接地状態かどうかを取得します。</summary>
    public bool IsPlayerGrounded { get; } = isPlayerGrounded;

    /// <summary>プレイヤーが滞空状態かどうかを取得します。</summary>
    public bool IsPlayerAirborne { get; } = isPlayerAirborne;
}

/// <summary>
///     技能意思決定ルールリストと現在の戦闘コンテキストを評価し、最適な出招技能を選択する評価器です。
/// </summary>
public static class BossDecisionEvaluator {
    /// <summary>
    ///     候補ルールの中で最もスコアが高い技能を選択します。
    /// </summary>
    /// <param name="rules">判定対象の技能意思決定ルール一覧。</param>
    /// <param name="cooldownTracker">冷却時間追跡器。</param>
    /// <param name="context">現在の戦闘判定コンテキスト。</param>
    /// <returns>選択された技能識別子。合致する技能がない場合は BossSkill.None。</returns>
    public static BossSkill EvaluateBest(
        IReadOnlyList<AttackDecisionRule> rules,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(cooldownTracker);

        var bestSkill = BossSkill.None;
        var bestScore = -1.0f;

        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            if (!IsRuleEligible(rule, cooldownTracker, in context, out var score)) continue;

            if (score > bestScore)
            {
                bestScore = score;
                bestSkill = rule.Skill;
            }
        }

        return bestSkill;
    }

    /// <summary>
    ///     候補ルールの中からスコアに応じた加重ランダム抽選で技能を選択します。
    /// </summary>
    /// <param name="rules">判定対象の技能意思決定ルール一覧。</param>
    /// <param name="cooldownTracker">冷却時間追跡器。</param>
    /// <param name="context">現在の戦闘判定コンテキスト。</param>
    /// <param name="random">乱数生成器。未指定時は共有の乱数を使用します。</param>
    /// <returns>選択された技能識別子。合致する技能がない場合は BossSkill.None。</returns>
    public static BossSkill EvaluateWeighted(
        IReadOnlyList<AttackDecisionRule> rules,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context,
        Random? random = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(cooldownTracker);

        Span<BossSkill> candidateSkills = stackalloc BossSkill[rules.Count];
        Span<float> candidateScores = stackalloc float[rules.Count];
        var count = 0;
        var totalScore = 0.0f;

        for (var index = 0; index < rules.Count; index++)
        {
            var rule = rules[index];
            if (!IsRuleEligible(rule, cooldownTracker, in context, out var score)) continue;

            candidateSkills[count] = rule.Skill;
            candidateScores[count] = score;
            totalScore += score;
            count++;
        }

        if (count == 0 || totalScore <= 0.0f) return BossSkill.None;

        if (count == 1) return candidateSkills[0];

        random ??= Random.Shared;
        var roll = (float)random.NextDouble() * totalScore;
        var cumulative = 0.0f;

        for (var index = 0; index < count; index++)
        {
            cumulative += candidateScores[index];
            if (roll <= cumulative) return candidateSkills[index];
        }

        return candidateSkills[count - 1];
    }

    private static bool IsRuleEligible(
        in AttackDecisionRule rule,
        BossCooldownTracker cooldownTracker,
        in AttackDecisionContext context,
        out float score)
    {
        score = 0.0f;

        if (!rule.IsValid) return false;

        // 1. 冷却時間判定
        if (!cooldownTracker.IsReady(rule.Skill)) return false;

        // 2. 連続発動回数制限判定
        if (rule.Skill == cooldownTracker.LastExecutedSkill &&
            cooldownTracker.ConsecutiveUseCount >= rule.MaxConsecutiveUses)
            return false;

        // 3. 距離判定
        if (context.Distance < rule.MinDistance || context.Distance > rule.MaxDistance) return false;

        // 4. 足場制約判定
        if (rule.RequireSamePlatform && !context.IsSamePlatform) return false;

        // 5. 接地制約判定
        if (rule.RequirePlayerGrounded.HasValue &&
            rule.RequirePlayerGrounded.Value != context.IsPlayerGrounded) return false;

        // 6. スコア計算
        score = rule.Weight;
        if (context.IsPlayerAirborne && rule.PreferAirborneBonus > 0.0f) score *= rule.PreferAirborneBonus;

        // 7. 連続使用時の重み減衰計算
        if (rule.Skill == cooldownTracker.LastExecutedSkill && cooldownTracker.ConsecutiveUseCount > 0)
        {
            var decay = MathF.Pow(rule.ConsecutiveDecay, cooldownTracker.ConsecutiveUseCount);
            score *= decay;
        }

        return score > 0.0f;
    }
}
