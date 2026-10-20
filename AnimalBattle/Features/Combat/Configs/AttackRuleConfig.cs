namespace AnimalBattle.Configs;

/// <summary>
///     Config/boss1.toml または boss2.toml から読み込まれる技能意思決定ルールの設定クラスです。
/// </summary>
public sealed class AttackRuleConfig {
    /// <summary>技能識別名（例: "HeavySlash", "DoubleCleave"）</summary>
    public string Skill { get; set; } = string.Empty;

    /// <summary>技能の基本冷却時間（秒）</summary>
    public float Cooldown { get; set; }

    /// <summary>最小有効距離（ピクセル）</summary>
    public float MinDistance { get; set; }

    /// <summary>最大有効距離（ピクセル）</summary>
    public float MaxDistance { get; set; }

    /// <summary>基礎加重（欲望値）</summary>
    public float Weight { get; set; }

    /// <summary>同一足場を要求するかどうか</summary>
    public bool RequireSamePlatform { get; set; }

    /// <summary>プレイヤーの接地を要求するかどうか（null の場合は問わない）</summary>
    public bool? RequirePlayerGrounded { get; set; }

    /// <summary>プレイヤー空中時の加重倍率</summary>
    public float PreferAirborneBonus { get; set; }
}
