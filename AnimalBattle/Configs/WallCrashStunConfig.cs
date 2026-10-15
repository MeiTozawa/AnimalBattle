using AnimalBattle.Abilities;

namespace AnimalBattle.Configs;

/// <summary>
///     壁激突気絶（スタン）効果の TOML 設定データクラス。
///     コード内ハードコードを排除し、付与される状態タグ（Stunned, Vulnerable 等）を設定から動的供給します。
/// </summary>
public sealed class WallCrashStunConfig {
    /// <summary>スタン硬直持続時間（秒）</summary>
    public float Duration { get; set; } = 1.2f;

    /// <summary>付与される状態タグ一覧（例: ["Stunned", "Vulnerable"]）</summary>
    public List<string> GrantedTags { get; set; } = [];

    /// <summary>パース済みの付与タグフラグを取得します。</summary>
    public GameplayTag ParsedGrantedTags => GameplayTagHelper.ParseTags(GrantedTags);
}
