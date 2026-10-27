namespace AnimalBattle.Configs;

/// <summary>
/// TOML から読み込まれる技能意思決定ルールの設定クラス。
/// </summary>
public sealed class AttackRuleConfig
{
    /// <summary>技能識別名（例: "HeavySlash", "DoubleCleave"）</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>技能の基本クールダウン時間（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>最小有効距離（px）</summary>
    public float MinDistance { get; set; }

    /// <summary>最大有効距離（px）</summary>
    public float MaxDistance { get; set; }

    /// <summary>基本重み（優先度）</summary>
    public float Weight { get; set; }

    /// <summary>同一足場を要求するかどうか</summary>
    public bool RequireSamePlatform { get; set; }

    /// <summary>プレイヤーの接地を要求するかどうか（null の場合は問わない）</summary>
    public bool? RequirePlayerGrounded { get; set; }

    /// <summary>プレイヤー空中時の優先加重倍率</summary>
    public float PreferAirborneBonus { get; set; }
}
