using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Decision;
using AnimalBattle.Ai.Skills;

namespace AnimalBattle.Ai.Abilities;

/// <summary>
///     エンティティの技能登録、冷却時間管理、状態タグ判定、および時序推進を一元管理するコンポーネントです。
/// </summary>
public sealed class AbilitySystemComponent {
    private readonly Dictionary<BossSkill, GameplayAbility> _abilities = new();
    private readonly BossCooldownTracker _cooldownTracker = new();
    private readonly SkillRunner _skillRunner = new();

    /// <summary>現在アクティブな戦闘状態タグを取得します。</summary>
    public GameplayTag ActiveTags { get; private set; } = GameplayTag.None;

    /// <summary>現在発動中の技能定義を取得します。</summary>
    public GameplayAbility? CurrentAbility { get; private set; }

    /// <summary>冷却時間追跡器を取得します。</summary>
    public BossCooldownTracker CooldownTracker => _cooldownTracker;

    /// <summary>技能時序実行器を取得します。</summary>
    public SkillRunner SkillRunner => _skillRunner;

    /// <summary>現在何らかの技能を発動中かどうかを取得します。</summary>
    public bool IsCasting => CurrentAbility != null && !_skillRunner.IsComplete;

    /// <summary>移動速度適用イベント。</summary>
    public event Action<float, float>? OnExecuteMovement;

    /// <summary>弾幕発射イベント。</summary>
    public event Action<BossSkill, float, float>? OnEmitBullet;

    /// <summary>近接判定生成イベント。</summary>
    public event Action<BossSkill, float, float>? OnCreateHitbox;

    /// <summary>音効再生イベント。</summary>
    public event Action<int, float>? OnPlayAudio;

    /// <summary>画面振動イベント。</summary>
    public event Action<float, float>? OnScreenShake;

    /// <summary>技能完了通知イベント。</summary>
    public event Action<BossSkill>? OnSkillCompleted;

    /// <summary>技能をコンポーネントに登録します。</summary>
    public void RegisterAbility(GameplayAbility ability)
    {
        ArgumentNullException.ThrowIfNull(ability);
        _abilities[ability.SkillId] = ability;
    }

    /// <summary>指定技能の登録を取得します。</summary>
    public bool TryGetAbility(BossSkill skill, out GameplayAbility? ability)
    {
        return _abilities.TryGetValue(skill, out ability);
    }

    /// <summary>戦闘タグを直接付与します。</summary>
    public void AddTag(GameplayTag tag) => ActiveTags |= tag;

    /// <summary>戦闘タグを解除します。</summary>
    public void RemoveTag(GameplayTag tag) => ActiveTags &= ~tag;

    /// <summary>指定タグをすべて保持しているか判定します。</summary>
    public bool HasTag(GameplayTag tag) => (ActiveTags & tag) == tag;

    /// <summary>指定タグのいずれかを保持しているか判定します。</summary>
    public bool HasAnyTag(GameplayTag tag) => (ActiveTags & tag) != GameplayTag.None;

    /// <summary>
    ///     指定技能の発動を試行します。
    /// </summary>
    /// <param name="skill">発動対象技能。</param>
    /// <param name="direction">向き係数。</param>
    /// <returns>発動に成功した場合は true、失敗時は false。</returns>
    public bool TryActivateAbility(BossSkill skill, float direction = 1.0f)
    {
        // 1. 未登録チェック
        if (!_abilities.TryGetValue(skill, out var ability))
        {
            Debug.Assert(false, $"[ASC] 未登録の技能を発動しようとしました: {skill}");
            return false;
        }

        // 2. 冷却時間判定
        if (!_cooldownTracker.IsReady(skill))
        {
            return false;
        }

        // 3. 排他タグ判定
        if ((ActiveTags & ability.BlockedTags) != 0)
        {
            return false;
        }

        // 4. 必須タグ判定
        if (ability.RequiredTags != GameplayTag.None && (ActiveTags & ability.RequiredTags) != ability.RequiredTags)
        {
            return false;
        }

        // 5. 発動成功
        CurrentAbility = ability;
        ActiveTags |= ability.GrantedTags;
        _cooldownTracker.Trigger(skill, ability.Cooldown);
        _skillRunner.Start(ability.Timeline, direction);

        return true;
    }

    /// <summary>
    ///     現在発動中の技能を即座に中断・打断します。
    /// </summary>
    public void InterruptCurrentAbility()
    {
        if (CurrentAbility is null) return;

        ActiveTags &= ~CurrentAbility.GrantedTags;
        ActiveTags &= ~(GameplayTag.Telegraph | GameplayTag.Active | GameplayTag.Recovery | GameplayTag.Vulnerable | GameplayTag.Casting);
        CurrentAbility = null;
        _skillRunner.Reset();
    }

    /// <summary>
    ///     毎フレームの技能時序推進と冷却時間更新を実行します。
    /// </summary>
    /// <param name="dt">デルタ時間。</param>
    /// <param name="isGrounded">接地状態フラグ。</param>
    public void Update(float dt, bool isGrounded = false)
    {
        _cooldownTracker.Update(dt);

        if (CurrentAbility is null) return;

        _skillRunner.Update(dt, isGrounded, (step, dir) => ExecuteAction(step, dir));
        UpdatePhaseTags(_skillRunner.CurrentPhase);

        if (_skillRunner.IsComplete)
        {
            var finishedSkill = CurrentAbility.SkillId;
            ActiveTags &= ~CurrentAbility.GrantedTags;
            ActiveTags &= ~(GameplayTag.Telegraph | GameplayTag.Active | GameplayTag.Recovery | GameplayTag.Vulnerable | GameplayTag.Casting);
            CurrentAbility = null;
            _skillRunner.Reset();

            OnSkillCompleted?.Invoke(finishedSkill);
        }
    }

    private void UpdatePhaseTags(SkillPhase phase)
    {
        ActiveTags &= ~(GameplayTag.Telegraph | GameplayTag.Active | GameplayTag.Recovery | GameplayTag.Vulnerable);

        switch (phase)
        {
            case SkillPhase.Telegraph:
                ActiveTags |= GameplayTag.Telegraph;
                break;
            case SkillPhase.Active:
                ActiveTags |= GameplayTag.Active;
                break;
            case SkillPhase.Recovery:
                ActiveTags |= GameplayTag.Recovery | GameplayTag.Vulnerable;
                break;
        }
    }

    private void ExecuteAction(AttackActionStep step, float dir)
    {
        if (CurrentAbility is null) return;

        switch (step.Kind)
        {
            case AttackActionKind.Movement:
            case AttackActionKind.VelocityDive:
                OnExecuteMovement?.Invoke(step.Param0 * dir, step.Param1);
                break;
            case AttackActionKind.Bullet:
                OnEmitBullet?.Invoke(CurrentAbility.SkillId, step.Param0 * dir, step.Param1);
                break;
            case AttackActionKind.Hitbox:
            case AttackActionKind.OnGroundedHitbox:
                OnCreateHitbox?.Invoke(CurrentAbility.SkillId, step.Param0 * dir, step.Param1);
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
