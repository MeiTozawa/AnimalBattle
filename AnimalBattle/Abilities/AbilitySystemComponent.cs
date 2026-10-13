using System.Diagnostics;
using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     エンティティの技能登録、時限タグおよび冷却管理、状態タグ判定、および能力時序推進を一元管理する汎用コンポーネントです。
///     能力は動的 AbilityTag（StringId）によって管理・駆動され、全域 Enum への依存を持ちません。
/// </summary>
public sealed class AbilitySystemComponent {
    private readonly Dictionary<StringId, GameplayAbility> _abilities = new();
    private readonly CooldownTracker _cooldownTracker = new();

    /// <summary>戦闘タグコンテナを取得します。</summary>
    public GameplayTagContainer Tags { get; private set; } = new();

    /// <summary>現在発動中の技能インスタンスを取得します。</summary>
    public GameplayAbility? CurrentAbility { get; private set; }

    /// <summary>冷却時間追跡器を取得します。</summary>
    public CooldownTracker Cooldowns => _cooldownTracker;

    /// <summary>現在発動中の技能フェーズを取得します。</summary>
    public SkillPhase CurrentPhase => CurrentAbility?.CurrentPhase ?? SkillPhase.None;

    /// <summary>現在フェーズ内の経過時間（秒）を取得します。</summary>
    public float PhaseElapsed => CurrentAbility?.PhaseElapsed ?? 0.0f;

    /// <summary>現在フェーズの目標持続時間（秒）を取得します。</summary>
    public float PhaseDuration => CurrentAbility?.PhaseDuration ?? 0.0f;

    /// <summary>現在フェーズの進行度（0.0 ～ 1.0）を取得します。</summary>
    public float PhaseProgress => CurrentAbility?.PhaseProgress ?? 0.0f;

    /// <summary>現在何らかの技能を発動中かどうかを取得します。</summary>
    public bool IsCasting => CurrentAbility != null;

    /// <summary>移動速度適用イベント。</summary>
    public event Action<float, float>? OnExecuteMovement;

    /// <summary>弾幕発射イベント。</summary>
    public event Action<StringId, float, float>? OnEmitBullet;

    /// <summary>近接判定生成イベント。</summary>
    public event Action<StringId, float, float>? OnCreateHitbox;

    /// <summary>画面振動イベント。</summary>
    public event Action<float, float>? OnScreenShake;

    /// <summary>技能完了通知イベント。</summary>
    public event Action<StringId>? OnSkillCompleted;

    /// <summary>能力をコンポーネントに付与・登録します。</summary>
    public void GiveAbility(GameplayAbility ability)
    {
        ArgumentNullException.ThrowIfNull(ability);

        if (!ability.AbilityTag.IsEmpty)
        {
            _abilities[ability.AbilityTag] = ability;
        }
    }

    /// <summary>技能識別タグによって登録済み能力を取得します（UE GAS 準拠）。</summary>
    public bool TryGetAbility(StringId abilityTag, out GameplayAbility? ability) =>
        _abilities.TryGetValue(abilityTag, out ability);

    /// <summary>戦闘タグを直接付与します。</summary>
    public void AddTag(GameplayTag tag)
    {
        var tags = Tags;
        tags.Add(tag);
        Tags = tags;
    }

    /// <summary>戦闘タグを解除します。</summary>
    public void RemoveTag(GameplayTag tag)
    {
        var tags = Tags;
        tags.Remove(tag);
        Tags = tags;
    }

    /// <summary>指定タグをすべて保持しているか判定します。</summary>
    public bool HasTag(GameplayTag tag) => Tags.HasTag(tag);

    /// <summary>指定タグのいずれかを保持しているか判定します。</summary>
    public bool HasAnyTag(GameplayTag tag) => Tags.HasAny(tag);

    /// <summary>
    ///     技能識別タグを指定して技能の発動を試行します（UE GAS 準拠）。
    /// </summary>
    public bool TryActivateAbility(StringId abilityTag, float direction = 1.0f)
    {
        if (!_abilities.TryGetValue(abilityTag, out var ability))
        {
            Debug.Assert(false, $"[ASC] 未登録のタグを発動しようとしました: {abilityTag}");
            return false;
        }

        return InternalActivateAbility(ability, direction);
    }

    private bool InternalActivateAbility(GameplayAbility ability, float direction)
    {
        var ctx = new AbilityActivationContext(direction);

        // CanActivate 判定（Tag阻害および個別CD判定を統合評価）
        if (!ability.CanActivate(this, in ctx))
        {
            return false;
        }

        // 発動コミット
        CurrentAbility = ability;

        if (ability.ActivationOwnedTags != GameplayTag.None)
        {
            AddTag(ability.ActivationOwnedTags);
        }

        CommitCooldown(ability);
        ability.Activate(this, in ctx);

        return true;
    }

    /// <summary>
    ///     技能の冷却時間を適用・コミットします。
    /// </summary>
    public void CommitCooldown(GameplayAbility ability)
    {
        if (!ability.AbilityTag.IsEmpty)
        {
            _cooldownTracker.Trigger(ability.AbilityTag, ability.Cooldown);
        }
    }

    /// <summary>
    ///     現在発動中の技能を正常に完了・終了します。
    /// </summary>
    public void EndCurrentAbility()
    {
        if (CurrentAbility is null) return;

        var ability = CurrentAbility;
        var finishedTag = ability.AbilityTag;

        if (ability.ActivationOwnedTags != GameplayTag.None)
        {
            RemoveTag(ability.ActivationOwnedTags);
        }

        ability.EndAbility(this);
        CurrentAbility = null;
        OnSkillCompleted?.Invoke(finishedTag);
    }

    /// <summary>
    ///     現在発動中の技能を即座に中断・打断します。
    /// </summary>
    public void InterruptCurrentAbility()
    {
        if (CurrentAbility is null) return;

        var ability = CurrentAbility;

        if (ability.ActivationOwnedTags != GameplayTag.None)
        {
            RemoveTag(ability.ActivationOwnedTags);
        }

        AddTag(GameplayTag.Interrupted);
        ability.CancelAbility(this);
        CurrentAbility = null;
    }

    /// <summary>
    ///     毎フレームの技能時序推進と冷却時間更新を実行します。
    /// </summary>
    public void Update(float dt, bool isGrounded = false)
    {
        // 1. 冷却の自動減衰
        _cooldownTracker.Update(dt);

        // 2. 実行中アビリティの更新
        CurrentAbility?.Update(dt, this, isGrounded);
    }

    /// <summary>
    ///     アクションステップを実行・派発します。
    /// </summary>
    public void DispatchAction(AttackActionStep step, float dir)
    {
        var currentTag = CurrentAbility?.AbilityTag ?? StringId.Empty;

        switch (step.Kind)
        {
            case AttackActionKind.Movement:
            case AttackActionKind.VelocityDive:
            case AttackActionKind.EasingMovement:
                OnExecuteMovement?.Invoke(step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.Bullet:
                OnEmitBullet?.Invoke(currentTag, step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.Hitbox:
            case AttackActionKind.OnGroundedHitbox:
                OnCreateHitbox?.Invoke(currentTag, step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.ScreenShake:
                OnScreenShake?.Invoke(step.Param0, step.Param1);
                break;
        }
    }
}
