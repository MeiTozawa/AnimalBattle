using AnimalBattle.Abilities.Attack;
using AnimalBattle.Abilities.Skills;

namespace AnimalBattle.Abilities;

/// <summary>
///     UE GAS 準拠：エンティティの技能登録、時限タグおよび冷却管理、状態タグ判定、および能力時序推進を一元管理する汎用コンポーネントです。
/// </summary>
public sealed class AbilitySystemComponent {
    private readonly Dictionary<AbilityId, AbilitySpec> _abilities = new();
    private readonly CooldownTracker _cooldownTracker = new();

    /// <summary>戦闘タグコンテナを取得します。</summary>
    public GameplayTagContainer Tags { get; private set; } = new();

    /// <summary>現在アクティブな戦闘状態タグを取得します（後方互換性）。</summary>
    public GameplayTag ActiveTags => Tags.RawTags;

    /// <summary>現在発動中の技能インスタンスを取得します。</summary>
    public GameplayAbility? CurrentAbility { get; private set; }

    /// <summary>冷却時間追跡器を取得します。</summary>
    public CooldownTracker Cooldowns => _cooldownTracker;

    /// <summary>後方互換性プロパティ：冷却時間追跡器を取得します。</summary>
    public CooldownTracker CooldownTracker => _cooldownTracker;

    /// <summary>時序技能実行器の参照を取得します（TimelineAttackAbility 発動中のみ有効）。</summary>
    public SkillRunner? SkillRunner => (CurrentAbility as TimelineAttackAbility)?.Runner;

    /// <summary>現在何らかの技能を発動中かどうかを取得します。</summary>
    public bool IsCasting => CurrentAbility != null;

    /// <summary>移動速度適用イベント。</summary>
    public event Action<float, float>? OnExecuteMovement;

    /// <summary>弾幕発射イベント。</summary>
    public event Action<AbilityId, float, float>? OnEmitBullet;

    /// <summary>近接判定生成イベント。</summary>
    public event Action<AbilityId, float, float>? OnCreateHitbox;

    /// <summary>音効再生イベント。</summary>
    public event Action<int, float>? OnPlayAudio;

    /// <summary>画面振動イベント。</summary>
    public event Action<float, float>? OnScreenShake;

    /// <summary>技能完了通知イベント。</summary>
    public event Action<AbilityId>? OnSkillCompleted;

    /// <summary>能力をコンポーネントに付与・登録します。</summary>
    public void GiveAbility(GameplayAbility ability)
    {
        ArgumentNullException.ThrowIfNull(ability);
        _abilities[ability.AbilityId] = new AbilitySpec(ability);
    }

    /// <summary>後方互換性：能力を登録します。</summary>
    public void RegisterAbility(GameplayAbility ability) => GiveAbility(ability);

    /// <summary>指定技能の登録を取得します。</summary>
    public bool TryGetAbility(AbilityId id, out GameplayAbility? ability)
    {
        if (_abilities.TryGetValue(id, out var spec))
        {
            ability = spec.Ability;
            return true;
        }

        ability = null;
        return false;
    }

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
    ///     指定技能の発動を試行します。
    ///     UE GAS 準拠：RequiredTags / BlockedTags / CooldownTags / CanActivate 判定パイプラインを通過します。
    /// </summary>
    /// <param name="id">発動対象技能識別子。</param>
    /// <param name="direction">向き係数（正: 右向き、負: 左向き）。</param>
    /// <returns>発動に成功した場合は true、失敗時は false。</returns>
    public bool TryActivateAbility(AbilityId id, float direction = 1.0f)
    {
        // 1. 未登録チェック
        if (!_abilities.TryGetValue(id, out var spec))
        {
            Debug.Assert(false, $"[ASC] 未登録の技能を発動しようとしました: {id}");
            return false;
        }

        var ability = spec.Ability;
        var ctx = new AbilityActivationContext(direction);

        // 2. CanActivate 判定（Tag阻害、CooldownTags、個別CD判定を統合評価）
        if (!ability.CanActivate(this, in ctx))
        {
            return false;
        }

        // 3. 発動コミット
        CurrentAbility = ability;
        spec.IsActive = true;

        if (ability.ActivationOwnedTags != GameplayTag.None)
        {
            AddTag(ability.ActivationOwnedTags);
        }

        CommitCooldown(ability);
        ability.Activate(this, in ctx);

        return true;
    }

    /// <summary>
    ///     技能の冷却時間および時限タグを適用・コミットします。
    /// </summary>
    public void CommitCooldown(GameplayAbility ability)
    {
        _cooldownTracker.Trigger(this, ability.AbilityId, ability.Cooldown, ability.CooldownTags);
    }

    /// <summary>
    ///     現在発動中の技能を正常に完了・終了します。
    /// </summary>
    public void EndCurrentAbility()
    {
        if (CurrentAbility is null) return;

        var finishedId = CurrentAbility.AbilityId;

        if (CurrentAbility.ActivationOwnedTags != GameplayTag.None)
        {
            RemoveTag(CurrentAbility.ActivationOwnedTags);
        }

        CurrentAbility.EndAbility(this);

        if (_abilities.TryGetValue(finishedId, out var spec))
        {
            spec.IsActive = false;
        }

        CurrentAbility = null;
        OnSkillCompleted?.Invoke(finishedId);
    }

    /// <summary>
    ///     現在発動中の技能を即座に中断・打断します。
    /// </summary>
    public void InterruptCurrentAbility()
    {
        if (CurrentAbility is null) return;

        var interruptedId = CurrentAbility.AbilityId;

        if (CurrentAbility.ActivationOwnedTags != GameplayTag.None)
        {
            RemoveTag(CurrentAbility.ActivationOwnedTags);
        }

        AddTag(GameplayTag.Interrupted);
        CurrentAbility.CancelAbility(this);

        if (_abilities.TryGetValue(interruptedId, out var spec))
        {
            spec.IsActive = false;
        }

        CurrentAbility = null;
    }

    /// <summary>
    ///     毎フレームの技能時序推進と冷却時間更新を実行します。
    /// </summary>
    /// <param name="dt">デルタ時間。</param>
    /// <param name="isGrounded">接地状態フラグ。</param>
    public void Update(float dt, bool isGrounded = false)
    {
        // 1. 冷却および時限タグの自動減衰
        _cooldownTracker.Update(dt, this);

        // 2. 実行中アビリティの更新
        CurrentAbility?.Update(dt, this, isGrounded);
    }

    /// <summary>
    ///     アクションステップを実行・派発します。
    /// </summary>
    public void DispatchAction(AttackActionStep step, float dir)
    {
        var currentId = CurrentAbility?.AbilityId ?? AbilityId.None;

        switch (step.Kind)
        {
            case AttackActionKind.Movement:
            case AttackActionKind.VelocityDive:
            case AttackActionKind.EasingMovement:
                OnExecuteMovement?.Invoke(step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.Bullet:
                OnEmitBullet?.Invoke(currentId, step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.Hitbox:
            case AttackActionKind.OnGroundedHitbox:
                OnCreateHitbox?.Invoke(currentId, step.Param0 * dir, step.Param1);
                break;

            case AttackActionKind.AudioCue:
                OnPlayAudio?.Invoke((int)step.Param0, step.Param1);
                break;

            case AttackActionKind.ScreenShake:
                OnScreenShake?.Invoke(step.Param0, step.Param1);
                break;
        }
    }
}
