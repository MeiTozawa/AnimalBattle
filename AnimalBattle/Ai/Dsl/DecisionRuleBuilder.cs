using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     攻撃の意思決定ルール（AttackDecisionRule）を流暢に構築するビルダーです。
/// </summary>
public sealed class DecisionRuleBuilder {
    private float _cooldown;
    private float _maxDistance = 1000.0f;
    private float _minDistance;
    private float _preferAirborneBonus = 1.0f;
    private bool? _requirePlayerGrounded;
    private bool _requireSamePlatform;
    private float _weight = 100.0f;

    /// <summary>
    ///     スキルのクールダウン秒数を設定します。
    /// </summary>
    /// <param name="seconds">クールダウン時間（秒）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder Cooldown(float seconds)
    {
        _cooldown = seconds;
        return this;
    }

    /// <summary>
    ///     プレイヤーとの有効距離範囲を設定します。
    /// </summary>
    /// <param name="minDistance">最小有効距離（px）。</param>
    /// <param name="maxDistance">最大有効距離（px）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder Distance(float minDistance, float maxDistance)
    {
        _minDistance = minDistance;
        _maxDistance = maxDistance;
        return this;
    }

    /// <summary>
    ///     発動の基礎抽選重みを設定します。
    /// </summary>
    /// <param name="weight">抽選重み（正の値）。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder Weight(float weight)
    {
        _weight = weight;
        return this;
    }

    /// <summary>
    ///     プレイヤーと同一プラットフォーム上にいることを必須条件にするかを設定します。
    /// </summary>
    /// <param name="require">同一足場が必須の場合は true。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder RequireSamePlatform(bool require = true)
    {
        _requireSamePlatform = require;
        return this;
    }

    /// <summary>
    ///     プレイヤーが接地していることを必須条件にするかを設定します。
    /// </summary>
    /// <param name="require">接地が必須の場合は true。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder RequirePlayerGrounded(bool require = true)
    {
        _requirePlayerGrounded = require;
        return this;
    }

    /// <summary>
    ///     プレイヤーが空中にいる場合の優先度ボーナス倍率を設定します。
    /// </summary>
    /// <param name="bonus">ボーナス重み倍率。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder PreferAirborneBonus(float bonus = 1.5f)
    {
        _preferAirborneBonus = bonus;
        return this;
    }

    /// <summary>
    ///     設定されたパラメータから不変の AttackDecisionRule を構築します。
    /// </summary>
    /// <param name="skill">対象の技能識別子。</param>
    /// <returns>検証済みの AttackDecisionRule。</returns>
    public AttackDecisionRule Build(BossSkill skill)
    {
        return new AttackDecisionRule(
            skill,
            _cooldown,
            _minDistance,
            _maxDistance,
            _weight,
            _requireSamePlatform,
            _requirePlayerGrounded,
            _preferAirborneBonus);
    }
}