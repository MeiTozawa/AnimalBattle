using AnimalBattle.Ai.Contracts;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     攻撃の意思決定ルールを流暢に構築するビルダーです。
/// </summary>
public sealed class DecisionRuleBuilder {
    private float _cooldown;
    private float _maxDistance = 1000.0f;
    private float _minDistance;
    private float _preferAirborneBonus = 1.0f;
    private bool? _requirePlayerGrounded;
    private bool _requireSamePlatform;
    private float _weight = 100.0f;
    private int _maxConsecutiveUses = 2;
    private float _consecutiveDecay = 0.5f;

    /// <summary>
    ///     スキルのクールダウン秒数を設定します。
    /// </summary>
    /// <param name="seconds">クールダウン秒数。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder Cooldown(float seconds)
    {
        _cooldown = seconds;
        return this;
    }

    /// <summary>
    ///     プレイヤーとの有効距離範囲を設定します。
    /// </summary>
    /// <param name="minDistance">最小有効距離。</param>
    /// <param name="maxDistance">最大有効距離。</param>
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
    /// <param name="weight">抽選重み。正の値を指定します。</param>
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
    ///     連続発動可能な最大回数および減衰比率を設定します。
    /// </summary>
    /// <param name="maxUses">最大連続回数。既定値は 2 です。</param>
    /// <param name="decay">加重減衰比率。既定値は 0.5 です。</param>
    /// <returns>現在のビルダーインスタンス。</returns>
    public DecisionRuleBuilder MaxConsecutive(int maxUses, float decay = 0.5f)
    {
        _maxConsecutiveUses = Math.Max(1, maxUses);
        _consecutiveDecay = Math.Clamp(decay, 0.0f, 1.0f);
        return this;
    }

    /// <summary>
    ///     設定されたパラメータから不変の AttackDecisionRule を構築します。
    /// </summary>
    /// <param name="skillTag">対象の技能識別タグ。</param>
    /// <returns>検証済みの AttackDecisionRule。</returns>
    public AttackDecisionRule Build(StringId skillTag)
    {
        return new AttackDecisionRule(
            skillTag,
            _cooldown,
            _minDistance,
            _maxDistance,
            _weight,
            _requireSamePlatform,
            _requirePlayerGrounded,
            _preferAirborneBonus,
            _maxConsecutiveUses,
            _consecutiveDecay);
    }
}