using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;

namespace AnimalBattle.Abilities;

/// <summary>
///     前隙（Telegraph）、判定（Active）、後隙（Recovery）の三段階時序を持つ戦闘招式能力です。
///     自身の前兆視覚表現（TelegraphRenderer）を内包し、表現層の switch-case 依存を排除します。
/// </summary>
public sealed class TimelineAttackAbility : GameplayAbility {
    /// <summary>検証済みの不変時序定義を取得します。</summary>
    public AttackDefinition Timeline { get; }

    /// <summary>時序推進ランナーを取得します。</summary>
    public SkillRunner Runner { get; } = new();

    /// <inheritdoc />
    public override SkillPhase CurrentPhase => Runner.CurrentPhase;

    /// <inheritdoc />
    public override float PhaseElapsed => Runner.PhaseElapsed;

    /// <inheritdoc />
    public override float PhaseDuration => Runner.PhaseDuration;

    /// <inheritdoc />
    public override float PhaseProgress => Runner.PhaseDuration > 0.0f
        ? Math.Clamp(Runner.PhaseElapsed / Runner.PhaseDuration, 0.0f, 1.0f)
        : 0.0f;

    /// <summary>前兆（Telegraph）描画処理デリゲートを取得または設定します。</summary>
    public Action<AbilityTelegraphContext>? TelegraphRenderer { get; init; }

    /// <summary>
    ///     時序攻撃能力を初期化します（UE GAS 準拠：StringId 識別）。
    /// </summary>
    public TimelineAttackAbility(
        StringId tag,
        AttackDefinition timeline,
        float cooldown = 0.0f,
        GameplayTag requiredTags = GameplayTag.None,
        GameplayTag blockedTags = GameplayTag.Stunned,
        string? displayName = null,
        Action<AbilityTelegraphContext>? telegraphRenderer = null)
    {
        Debug.Assert(timeline != null, "timeline は null であってはなりません。");

        AbilityTag = tag;
        DisplayName = displayName ?? tag.ToString();
        TelegraphRenderer = telegraphRenderer;
        Timeline = timeline;
        Cooldown = cooldown;
        RequiredTags = requiredTags;
        BlockedTags = blockedTags;
        ActivationOwnedTags = GameplayTag.Casting;
    }

    /// <inheritdoc />
    public override void DrawTelegraph(in AbilityTelegraphContext ctx)
    {
        TelegraphRenderer?.Invoke(ctx);
    }

    /// <inheritdoc />
    public override void Activate(AbilitySystemComponent asc, in AbilityActivationContext ctx)
    {
        Runner.Start(Timeline, ctx.Direction);
    }

    /// <inheritdoc />
    public override void Update(float dt, AbilitySystemComponent asc, bool isGrounded = false)
    {
        Runner.Update(dt, isGrounded, asc.DispatchAction);
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
