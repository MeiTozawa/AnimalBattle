using AnimalBattle.Abilities.Skills;
using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     戦闘技能・能力の実行ライフサイクル、タグ制約、および時序推進を定義する多態基底クラスです。
///     UE GAS 準拠：能力識別は AbilityTag（StringId）によって動的に行われ、全域 Enum に依存しません。
/// </summary>
public abstract class GameplayAbility {
    /// <summary>能力の識別タグ（UE の AbilityTags に相当）を取得または初期化します。</summary>
    public StringId AbilityTag { get; init; } = StringId.Empty;

    /// <summary>HUD やログ等で表示される能力表示名を取得または初期化します。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>基本冷却時間（秒）を取得または設定します。</summary>
    public float Cooldown { get; set; }

    /// <summary>発動に必要な必須タグ制約を取得します。</summary>
    public GameplayTag RequiredTags { get; init; } = GameplayTag.None;

    /// <summary>発動を禁止する排他タグを取得します。</summary>
    public GameplayTag BlockedTags { get; init; } = GameplayTag.Stunned;

    /// <summary>発動後に自身へ付与され、再発動を阻害する冷却組タグ（CooldownMelee, CooldownRanged 等）。</summary>
    public GameplayTag CooldownTags { get; init; } = GameplayTag.None;

    /// <summary>実行中に自身へ付与されるタグ（Activation Owned Tags）を取得します。</summary>
    public GameplayTag ActivationOwnedTags { get; init; } = GameplayTag.Casting;

    /// <summary>現在の技能フェーズを取得します。</summary>
    public virtual SkillPhase CurrentPhase => SkillPhase.None;

    /// <summary>現在フェーズ内の経過時間（秒）を取得します。</summary>
    public virtual float PhaseElapsed => 0.0f;

    /// <summary>現在フェーズの目標持続時間（秒）を取得します。</summary>
    public virtual float PhaseDuration => 0.0f;

    /// <summary>現在フェーズの進行度（0.0 ～ 1.0）を取得します。</summary>
    public virtual float PhaseProgress => PhaseDuration > 0.0f
        ? Math.Clamp(PhaseElapsed / PhaseDuration, 0.0f, 1.0f)
        : 0.0f;

    /// <summary>
    ///     現在のコンテキストとタグ状態に基づいて技能が発動可能かを判定します。
    ///     UE GAS 準拠：CooldownTags は BlockedTags の一部として統合評価されます。
    /// </summary>
    public virtual bool CanActivate(AbilitySystemComponent asc, in AbilityActivationContext ctx)
    {
        // 1. Tag 判定：必須タグを満たし、かつ BlockedTags と CooldownTags のいずれも持っていないこと
        if (!asc.Tags.Matches(RequiredTags, BlockedTags | CooldownTags))
            return false;

        // 2. 個別 AbilityTag 冷却判定
        if (!AbilityTag.IsEmpty && !asc.Cooldowns.IsReady(AbilityTag))
            return false;

        return true;
    }

    /// <summary>
    ///     技能の発動処理を開始します。
    /// </summary>
    public abstract void Activate(AbilitySystemComponent asc, in AbilityActivationContext ctx);

    /// <summary>
    ///     毎フレームの技能時序推進とロジック更新を実行します。
    /// </summary>
    public virtual void Update(float dt, AbilitySystemComponent asc, bool isGrounded = false) { }

    /// <summary>
    ///     技能が正常に終了・完了した際のクリーンアップ処理を実行します。
    /// </summary>
    public virtual void EndAbility(AbilitySystemComponent asc) { }

    /// <summary>
    ///     技能が中断・打断された際のキャンセル処理を実行します。
    /// </summary>
    public virtual void CancelAbility(AbilitySystemComponent asc) { }

    /// <summary>
    ///     技能の前兆（Telegraph）演出を描画します。
    ///     UE GAS 準拠：技能自身がその視覚表現の責務を持ちます。
    /// </summary>
    public virtual void DrawTelegraph(in AbilityTelegraphContext ctx) { }
}
