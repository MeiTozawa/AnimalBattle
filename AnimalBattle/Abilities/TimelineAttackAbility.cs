using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;

namespace AnimalBattle.Abilities;

/// <summary>
///     前隙（Telegraph）、判定（Active）、後隙（Recovery）の三段階時序を持つ戦闘招式能力です。
/// </summary>
public sealed class TimelineAttackAbility : GameplayAbility {
    /// <summary>検証済みの不変時序定義を取得します。</summary>
    public AttackDefinition Timeline { get; }

    /// <summary>時序推進ランナーを取得します。</summary>
    public SkillRunner Runner { get; } = new();

    /// <summary>
    ///     時序攻撃能力を初期化します。
    /// </summary>
    /// <param name="id">能力識別子。</param>
    /// <param name="timeline">攻撃時序定義。</param>
    /// <param name="cooldown">冷却時間（秒）。</param>
    /// <param name="cooldownTags">冷却期間中に自身へ付与されるタグ（CooldownMelee, CooldownRanged 等）。</param>
    /// <param name="requiredTags">発動に必要なタグ制約。</param>
    /// <param name="blockedTags">発動を阻害するタグ制約。</param>
    public TimelineAttackAbility(
        AbilityId id,
        AttackDefinition timeline,
        float cooldown = 0.0f,
        GameplayTag cooldownTags = GameplayTag.None,
        GameplayTag requiredTags = GameplayTag.None,
        GameplayTag blockedTags = GameplayTag.Stunned)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        AbilityId = id;
        Timeline = timeline;
        Cooldown = cooldown;
        CooldownTags = cooldownTags;
        RequiredTags = requiredTags;
        BlockedTags = blockedTags;
        ActivationOwnedTags = GameplayTag.Casting;
    }

    /// <inheritdoc />
    public override void Activate(AbilitySystemComponent asc, in AbilityActivationContext ctx)
    {
        Runner.Start(Timeline, ctx.Direction);
    }

    /// <inheritdoc />
    public override void Update(float dt, AbilitySystemComponent asc, bool isGrounded = false)
    {
        Runner.Update(dt, isGrounded, (step, dir) => asc.DispatchAction(step, dir));
        UpdatePhaseTags(asc, Runner.CurrentPhase);

        if (Runner.IsComplete)
        {
            asc.EndCurrentAbility();
        }
    }

    /// <inheritdoc />
    public override void CancelAbility(AbilitySystemComponent asc)
    {
        ClearPhaseTags(asc);
        Runner.Reset();
    }

    /// <inheritdoc />
    public override void EndAbility(AbilitySystemComponent asc)
    {
        ClearPhaseTags(asc);
        Runner.Reset();
    }

    private void UpdatePhaseTags(AbilitySystemComponent asc, SkillPhase phase)
    {
        ClearPhaseTags(asc);

        switch (phase)
        {
            case SkillPhase.Telegraph:
                asc.AddTag(GameplayTag.Telegraph);
                break;

            case SkillPhase.Active:
                asc.AddTag(GameplayTag.Active);
                if (Timeline.ArmorTag != GameplayTag.None)
                {
                    asc.AddTag(Timeline.ArmorTag);
                }
                break;

            case SkillPhase.Recovery:
                asc.AddTag(GameplayTag.Recovery | GameplayTag.Vulnerable);
                break;
        }
    }

    private void ClearPhaseTags(AbilitySystemComponent asc)
    {
        asc.RemoveTag(GameplayTag.Telegraph | GameplayTag.Active | GameplayTag.Recovery | GameplayTag.Vulnerable | Timeline.ArmorTag);
    }
}
