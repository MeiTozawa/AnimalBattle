using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Decision;
using AnimalBattle.Ai.Skills;
using ErrorOr;

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
    ///     発動失敗時は <see cref="ErrorOr{T}" /> で返します。
    /// </summary>
    public ErrorOr<Success> TryActivateAbility(BossSkill skill)
    {
        // 1. 未登録チェック
        if (!_abilities.TryGetValue(skill, out var ability))
        {
            Debug.Assert(false, $"[ASC] 未登録の技能を発動しようとしました: {skill}");
            return Error.NotFound("Ability.NotRegistered", $"技能 '{skill}' は登録されていません。");
        }

        // 2. 冷却時間判定
        if (!_cooldownTracker.IsReady(skill))
        {
            return Error.Conflict("Ability.Cooldown", $"技能 '{skill}' は冷却時間中です。");
        }

        // 3. 排他タグ判定
        if ((ActiveTags & ability.BlockedTags) != 0)
        {
            return Error.Validation(
                "Ability.Blocked",
                $"排他タグ 0x{(ulong)(ActiveTags & ability.BlockedTags):X} により技能 '{skill}' の発動が阻止されました。");
        }

        // 4. 必須タグ判定
        if (ability.RequiredTags != GameplayTag.None && (ActiveTags & ability.RequiredTags) != ability.RequiredTags)
        {
            return Error.Validation(
                "Ability.MissingRequiredTag",
                $"技能 '{skill}' の発動に必要なタグを満たしていません。");
        }

        // 5. 発動成功
        CurrentAbility = ability;
        ActiveTags |= ability.GrantedTags;
        _cooldownTracker.Trigger(skill, ability.Cooldown);
        _skillRunner.Start(ability.Timeline);

        return Result.Success;
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
    public void Update(float dt)
    {
        _cooldownTracker.Update(dt);

        if (CurrentAbility is null) return;

        _skillRunner.Update(dt, false, (comp, _) => ExecuteComponent(comp));
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

    private void ExecuteComponent(AttackComponent comp)
    {
        if (CurrentAbility is null) return;

        switch (comp.Kind)
        {
            case AttackComponentKind.Bullet:
                OnEmitBullet?.Invoke(CurrentAbility.SkillId, comp.Param0, comp.Param1);
                break;
            case AttackComponentKind.Hitbox:
                OnCreateHitbox?.Invoke(CurrentAbility.SkillId, comp.Param0, comp.Param1);
                break;
            case AttackComponentKind.AudioCue:
                OnPlayAudio?.Invoke((int)comp.Param0, comp.Param1);
                break;
            case AttackComponentKind.ScreenShake:
                OnScreenShake?.Invoke(comp.Param0, comp.Param1);
                break;
        }
    }
}
