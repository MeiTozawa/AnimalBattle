using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;

namespace AnimalBattle.Abilities;

/// <summary>
///     予兆、判定、硬直の三段階タイムラインを持つ攻撃アビリティです。
///     予兆演出の描画処理を内包し、外部の条件分岐依存を排除します。
/// </summary>
public sealed class TimelineAttackAbility : GameplayAbility {
    /// <summary>検証済みの不変タイムライン定義を取得します。</summary>
    public AttackDefinition Timeline { get; }

    /// <summary>タイムライン進行ランナーを取得します。</summary>
    public SkillRunner Runner { get; } = new();

    /// <summary>現在の技能フェーズを取得します。</summary>
    public SkillPhase CurrentPhase => Runner.CurrentPhase;

    /// <summary>現在フェーズ内の経過時間を秒単位で取得します。</summary>
    public float PhaseElapsed => Runner.PhaseElapsed;

    /// <summary>現在フェーズの目標持続時間を秒単位で取得します。</summary>
    public float PhaseDuration => Runner.PhaseDuration;

    /// <summary>現在フェーズの進行度をゼロから1.0の範囲で取得します。</summary>
    public float PhaseProgress => Runner.PhaseDuration > 0.0f
        ? Math.Clamp(Runner.PhaseElapsed / Runner.PhaseDuration, 0.0f, 1.0f)
        : 0.0f;

    /// <inheritdoc />
    public override float Elapsed => Runner.PhaseElapsed;

    /// <inheritdoc />
    public override float Duration => Runner.PhaseDuration;

    /// <inheritdoc />
    public override float Progress => PhaseProgress;

    /// <summary>予兆描画処理デリゲートを取得または設定します。</summary>
    public Action<AbilityTelegraphContext>? TelegraphRenderer { get; init; }

    /// <summary>アクションステップ実行コールバックを取得または設定します。</summary>
    public Action<AttackActionStep, float>? ActionDispatcher { get; set; }

    /// <summary>
    ///     文字列識別子に基づいてタイムライン攻撃能力を初期化します。
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

    private SkillPhase _appliedPhase = SkillPhase.None;

    /// <inheritdoc />
    public override void Activate(AbilitySystemComponent asc, in AbilityActivationContext ctx)
    {
        ClearPhaseTags(asc);
        Runner.Start(Timeline, ctx.Direction);
        UpdatePhaseTags(asc, Runner.CurrentPhase);
    }

    /// <inheritdoc />
    public override void Update(float dt, AbilitySystemComponent asc, bool isGrounded = false)
    {
        var prevPhase = Runner.CurrentPhase;
        if (ActionDispatcher != null)
        {
            Runner.Update(dt, isGrounded, ActionDispatcher);
        }
        else
        {
            Runner.Update(dt, isGrounded, static (_, _) => { });
        }

        if (prevPhase != Runner.CurrentPhase)
        {
            UpdatePhaseTags(asc, Runner.CurrentPhase);
        }

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
        if (_appliedPhase == phase)
        {
            return;
        }

        ClearPhaseTags(asc);

        _appliedPhase = phase;

        switch (phase)
        {
            case SkillPhase.Telegraph:
                asc.AddTag(GameplayTag.Telegraph);
                break;

            case SkillPhase.Active:
                var activeTags = GameplayTag.Active;
                if (Timeline.ArmorTag != GameplayTag.None)
                {
                    activeTags |= Timeline.ArmorTag;
                }
                asc.AddTag(activeTags);
                break;

            case SkillPhase.Recovery:
                var recoveryTags = GameplayTag.Recovery;
                if (Timeline.RecoveryTag != GameplayTag.None)
                {
                    recoveryTags |= Timeline.RecoveryTag;
                }
                asc.AddTag(recoveryTags);
                break;
        }
    }

    private void ClearPhaseTags(AbilitySystemComponent asc)
    {
        switch (_appliedPhase)
        {
            case SkillPhase.Telegraph:
                asc.RemoveTag(GameplayTag.Telegraph);
                break;

            case SkillPhase.Active:
                var activeRemoval = GameplayTag.Active;
                if (Timeline.ArmorTag != GameplayTag.None)
                {
                    activeRemoval |= Timeline.ArmorTag;
                }
                asc.RemoveTag(activeRemoval);
                break;

            case SkillPhase.Recovery:
                var recoveryRemoval = GameplayTag.Recovery;
                if (Timeline.RecoveryTag != GameplayTag.None)
                {
                    recoveryRemoval |= Timeline.RecoveryTag;
                }
                asc.RemoveTag(recoveryRemoval);
                break;
        }

        _appliedPhase = SkillPhase.None;
    }
}
