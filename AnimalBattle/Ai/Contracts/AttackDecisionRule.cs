namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     技能の意思決定条件を定義する検証済みルール構造体です。
/// </summary>
/// <remarks>
///     技能意思決定ルールを初期化します。
/// </remarks>
/// <param name="skill">技能識別子。</param>
/// <param name="cooldown">基本冷却時間。単位は秒。</param>
/// <param name="minDistance">最小有効距離。</param>
/// <param name="maxDistance">最大有効距離。</param>
/// <param name="weight">基礎抽選重み。</param>
/// <param name="requireSamePlatform">同一足場を要求するかどうか。</param>
/// <param name="requirePlayerGrounded">接地を要求するかどうか。</param>
/// <param name="preferAirborneBonus">空中ボーナス倍率。</param>
/// <param name="maxConsecutiveUses">連続発動可能な最大回数。超過時は抽選除外。</param>
/// <param name="consecutiveDecay">連続発動時の重み減衰率。範囲は0.0fから1.0f。</param>
public readonly struct AttackDecisionRule(
    AbilityId skill,
    float cooldown = 0.0f,
    float minDistance = 0.0f,
    float maxDistance = 300.0f,
    float weight = 100.0f,
    bool requireSamePlatform = false,
    bool? requirePlayerGrounded = null,
    float preferAirborneBonus = 1.0f,
    int maxConsecutiveUses = 2,
    float consecutiveDecay = 0.5f) {
    /// <summary>対象の技能識別子を取得します。</summary>
    public AbilityId Skill { get; } = skill;

    /// <summary>技能の基本冷却時間を取得します。単位は秒。</summary>
    public float Cooldown { get; } = cooldown;

    /// <summary>技能を発動可能な最小有効距離を取得します。</summary>
    public float MinDistance { get; } = minDistance;

    /// <summary>技能を発動可能な最大有効距離を取得します。</summary>
    public float MaxDistance { get; } = maxDistance;

    /// <summary>意思決定時の基礎抽選重みを取得します。</summary>
    public float Weight { get; } = weight;

    /// <summary>Boss と対象が同一足場に存在することを要求するかどうかを取得します。</summary>
    public bool RequireSamePlatform { get; } = requireSamePlatform;

    /// <summary>対象プレイヤーが接地中であることを要求するかどうかを取得します。未指定時は制限なし。</summary>
    public bool? RequirePlayerGrounded { get; } = requirePlayerGrounded;

    /// <summary>対象プレイヤーが空中にいる場合の重み倍率を取得します。既定値は 1.0 です。</summary>
    public float PreferAirborneBonus { get; } = preferAirborneBonus;

    /// <summary>連続発動可能な最大回数を取得します。</summary>
    public int MaxConsecutiveUses { get; } = maxConsecutiveUses;

    /// <summary>連続発動時の重み減衰率を取得します。</summary>
    public float ConsecutiveDecay { get; } = consecutiveDecay;

    /// <summary>ルール設定が有効かどうかを取得します。</summary>
    public bool IsValid => Skill.IsValid()
     && Cooldown >= 0.0f
     && MinDistance >= 0.0f
     && MaxDistance >= MinDistance
     && Weight > 0.0f
     && PreferAirborneBonus > 0.0f
     && MaxConsecutiveUses >= 1
     && ConsecutiveDecay >= 0.0f
     && ConsecutiveDecay <= 1.0f
     && !float.IsNaN(Cooldown)
     && !float.IsNaN(MinDistance)
     && !float.IsNaN(MaxDistance)
     && !float.IsNaN(Weight)
     && !float.IsNaN(PreferAirborneBonus)
     && !float.IsNaN(ConsecutiveDecay);
}