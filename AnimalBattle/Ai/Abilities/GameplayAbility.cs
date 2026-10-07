using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Abilities;

/// <summary>
///     単一の戦闘技能の契約メタデータと実行タイムラインを保持する定義クラスです。
/// </summary>
public sealed class GameplayAbility {
    /// <summary>技能識別子を取得します。</summary>
    public BossSkill SkillId { get; init; }

    /// <summary>冷却時間を取得または設定します。</summary>
    public float Cooldown { get; set; }

    /// <summary>発動に必要な必須タグ制約を取得します。</summary>
    public GameplayTag RequiredTags { get; init; } = GameplayTag.None;

    /// <summary>発動を禁止する排他タグを取得します。</summary>
    public GameplayTag BlockedTags { get; init; } = GameplayTag.Stunned;

    /// <summary>実行中に自身へ付与されるタグを取得します。</summary>
    public GameplayTag GrantedTags { get; init; } = GameplayTag.Casting;

    /// <summary>コンパイル済みの不変時序定義を取得します。</summary>
    public AttackDefinition Timeline { get; init; } = null!;
}
