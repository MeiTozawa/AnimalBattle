using AnimalBattle.Abilities;

namespace AnimalBattle.Configs;

/// <summary>
///     フェーズ遷移能力：PhaseTransitionAbilityの TOML 設定データクラス。
/// </summary>
public sealed class PhaseTransitionAbilityConfig {
    /// <summary>遷移演出の持続時間秒単位</summary>
    public float Duration { get; set; } = 1.0f;

    /// <summary>発動中に自身へ付与されるタグ名リスト</summary>
    public List<string> ActivationOwnedTags { get; set; } = [];

    /// <summary>発動を禁止するタグ名リスト</summary>
    public List<string> BlockedTags { get; set; } = [];

    /// <summary>パース済みの付与タグフラグを取得します。</summary>
    public GameplayTag ParsedOwnedTags => GameplayTagHelper.ParseTags(ActivationOwnedTags);

    /// <summary>パース済みの阻害タグフラグを取得します。</summary>
    public GameplayTag ParsedBlockedTags => GameplayTagHelper.ParseTags(BlockedTags);
}
