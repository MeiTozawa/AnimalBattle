namespace AnimalBattle.Abilities.Attack;

/// <summary>
///     技能の意思決定条件を定義する検証済みルール構造体です。
///     UE GAS 準拠：技能識別子は StringId：動的タグとして管理されます。
/// </summary>
public readonly struct AttackDecisionRule {
    /// <summary>対象の技能識別タグを取得します。</summary>
    public StringId SkillTag { get; }

    /// <summary>技能の基本冷却時間を取得します。単位は秒。</summary>
    public float Cooldown { get; }

    /// <summary>技能を発動可能な最小有効距離を取得します。</summary>
    public float MinDistance { get; }

    /// <summary>技能を発動可能な最大有効距離を取得します。</summary>
    public float MaxDistance { get; }

    /// <summary>意思決定時の基礎抽選重みを取得します。</summary>
    public float Weight { get; }

    /// <summary>Boss と対象が同一足場に存在することを要求するかどうかを取得します。</summary>
    public bool RequireSamePlatform { get; }

    /// <summary>対象プレイヤーが接地中であることを要求するかどうかを取得します。未指定時は制限なし。</summary>
    public bool? RequirePlayerGrounded { get; }

    /// <summary>対象プレイヤーが空中にいる場合の重み倍率を取得します。既定値は 1.0 です。</summary>
    public float PreferAirborneBonus { get; }

    /// <summary>連続発動可能な最大回数を取得します。</summary>
    public int MaxConsecutiveUses { get; }

    /// <summary>連続発動時の重み減衰率を取得します。</summary>
    public float ConsecutiveDecay { get; }

    /// <summary>
    ///     技能識別タグを指定して意思決定ルールを初期化します。
    /// </summary>
    public AttackDecisionRule(
        StringId skillTag,
        float cooldown = 0.0f,
        float minDistance = 0.0f,
        float maxDistance = 300.0f,
        float weight = 100.0f,
        bool requireSamePlatform = false,
        bool? requirePlayerGrounded = null,
        float preferAirborneBonus = 1.0f,
        int maxConsecutiveUses = 2,
        float consecutiveDecay = 0.5f)
    {
        SkillTag = skillTag;
        Cooldown = cooldown;
        MinDistance = minDistance;
        MaxDistance = maxDistance;
        Weight = weight;
        RequireSamePlatform = requireSamePlatform;
        RequirePlayerGrounded = requirePlayerGrounded;
        PreferAirborneBonus = preferAirborneBonus;
        MaxConsecutiveUses = maxConsecutiveUses;
        ConsecutiveDecay = consecutiveDecay;
    }

    /// <summary>ルール設定が有効かどうかを取得します。</summary>
    public bool IsValid =>
        !SkillTag.IsEmpty &&
        Weight > 0.0f &&
        MinDistance >= 0.0f &&
        MaxDistance >= MinDistance &&
        Cooldown >= 0.0f &&
        PreferAirborneBonus >= 0.0f &&
        MaxConsecutiveUses >= 0 &&
        ConsecutiveDecay is >= 0.0f and <= 1.0f;

    /// <summary>
    ///     TOML 意思決定ルール設定から検証済みの意思決定ルールを生成します。
    /// </summary>
    public static AttackDecisionRule FromConfig(AttackRuleConfig config)
    {
        Debug.Assert(config != null, "AttackRuleConfig は null であってはなりません。");
        if (config == null) return default;
        return new AttackDecisionRule(
            new StringId(config.Skill),
            config.Cooldown,
            config.MinDistance,
            config.MaxDistance,
            config.Weight,
            config.RequireSamePlatform,
            config.RequirePlayerGrounded,
            config.PreferAirborneBonus > 0.0f ? config.PreferAirborneBonus : 1.0f);
    }
}