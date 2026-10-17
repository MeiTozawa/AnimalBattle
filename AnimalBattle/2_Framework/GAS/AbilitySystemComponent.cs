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
    private const int MaxActiveEffects = 8;
    private const int MaxTagBits = 16;
    private readonly Dictionary<StringId, GameplayAbility> _abilities = new();
    private readonly CooldownTracker _cooldownTracker = new();
    private readonly ActiveGameplayEffect[] _activeEffects = new ActiveGameplayEffect[MaxActiveEffects];
    private readonly int[] _tagCounts = new int[MaxTagBits];
    private GameplayTag _activeTagsMask = GameplayTag.None;

    /// <summary>戦闘タグコンテナを取得します。</summary>
    public GameplayTagContainer Tags => new(_activeTagsMask);

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
        Debug.Assert(ability != null, "ability は null であってはなりません。");
        if (ability == null) return;

        if (!ability.AbilityTag.IsEmpty)
        {
            _abilities[ability.AbilityTag] = ability;
        }
    }

    /// <summary>技能識別タグによって登録済み能力を取得します（UE GAS 準拠）。</summary>
    public bool TryGetAbility(StringId abilityTag, out GameplayAbility? ability) =>
        _abilities.TryGetValue(abilityTag, out ability);

    private void IncrementTagCounts(GameplayTag tags)
    {
        var bits = (ulong)tags;
        for (var i = 0; i < MaxTagBits; i++)
        {
            if ((bits & (1UL << i)) != 0)
            {
                if (_tagCounts[i]++ == 0)
                {
                    _activeTagsMask |= (GameplayTag)(1UL << i);
                }
            }
        }
    }

    private void DecrementTagCounts(GameplayTag tags)
    {
        var bits = (ulong)tags;
        for (var i = 0; i < MaxTagBits; i++)
        {
            if ((bits & (1UL << i)) != 0)
            {
                if (_tagCounts[i] > 0 && --_tagCounts[i] == 0)
                {
                    _activeTagsMask &= ~(GameplayTag)(1UL << i);
                }
            }
        }
    }

    /// <summary>戦闘タグを直接付与（参照カウント加算）します。</summary>
    public void AddTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;
        IncrementTagCounts(tag);
    }

    /// <summary>戦闘タグを直接解除（参照カウント減算）します。</summary>
    public void RemoveTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;
        DecrementTagCounts(tag);
    }

    /// <summary>
    ///     時限ミニ効果（Mini-GameplayEffect）を適用します。
    ///     同一発生源および同一タグの効果が存在する場合は持続時間を更新し、
    ///     存在しない場合は定長プール ActiveEffect[8] の空きスロットへ割り当ててタグ参照カウントを加算します。
    /// </summary>
    /// <param name="grantedTags">付与する戦闘タグ</param>
    /// <param name="duration">持続時間（秒）</param>
    /// <param name="sourceHandle">付与元エンティティのハンドル・ID</param>
    /// <returns>適用に成功した場合は true</returns>
    public bool ApplyGameplayEffect(GameplayTag grantedTags, float duration, ulong sourceHandle = 0)
    {
        if (duration <= 0.0f || grantedTags == GameplayTag.None) return false;

        // 1. 同一発生源 & 同一タグの既存エフェクトが存在する場合は持続時間を更新
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref var effect = ref _activeEffects[i];
            if (effect.IsActive && effect.SourceHandle == sourceHandle && effect.GrantedTags == grantedTags)
            {
                effect.Remaining = MathF.Max(effect.Remaining, duration);
                effect.TotalDuration = MathF.Max(effect.TotalDuration, duration);
                return true;
            }
        }

        // 2. 空きスロットへ新規割り当て
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref var effect = ref _activeEffects[i];
            if (!effect.IsActive)
            {
                effect = new ActiveGameplayEffect(grantedTags, duration, sourceHandle);
                IncrementTagCounts(grantedTags);
                return true;
            }
        }

        // 3. プール満杯時：残り時間が最も短いエフェクトを上書き置換
        var shortestIdx = 0;
        var shortestRemaining = float.MaxValue;
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            if (_activeEffects[i].Remaining < shortestRemaining)
            {
                shortestRemaining = _activeEffects[i].Remaining;
                shortestIdx = i;
            }
        }

        ref var victim = ref _activeEffects[shortestIdx];
        DecrementTagCounts(victim.GrantedTags);
        victim = new ActiveGameplayEffect(grantedTags, duration, sourceHandle);
        IncrementTagCounts(grantedTags);
        return true;
    }

    /// <summary>指定された秒数だけ有効な時限タグを付与します（ApplyGameplayEffect のエイリアス）。</summary>
    public bool AddTimedTag(GameplayTag tag, float duration, ulong sourceHandle = 0) =>
        ApplyGameplayEffect(tag, duration, sourceHandle);

    /// <summary>指定されたタグを含むすべてのアクティブエフェクトを解除します。</summary>
    public void RemoveActiveEffectsWithTag(GameplayTag tag)
    {
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref var effect = ref _activeEffects[i];
            if (effect.IsActive && (effect.GrantedTags & tag) != GameplayTag.None)
            {
                effect.IsActive = false;
                DecrementTagCounts(effect.GrantedTags);
            }
        }
    }

    /// <summary>指定された発生源ハンドルから付与されたすべてのアクティブエフェクトを解除します。</summary>
    public void RemoveActiveEffectsFromSource(ulong sourceHandle)
    {
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref var effect = ref _activeEffects[i];
            if (effect.IsActive && effect.SourceHandle == sourceHandle)
            {
                effect.IsActive = false;
                DecrementTagCounts(effect.GrantedTags);
            }
        }
    }

    /// <summary>指定された時限タグの最大残り秒数を取得します。</summary>
    public float GetTagRemainingTime(GameplayTag tag)
    {
        if ((_activeTagsMask & tag) == GameplayTag.None)
            return 0.0f;

        var maxRemaining = 0.0f;
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref readonly var effect = ref _activeEffects[i];
            if (effect.IsActive && (effect.GrantedTags & tag) != GameplayTag.None)
            {
                maxRemaining = MathF.Max(maxRemaining, effect.Remaining);
            }
        }

        return maxRemaining;
    }

    /// <summary>すべての戦闘タグ（アクティブエフェクトおよび参照カウント）を初期化クリアします。</summary>
    public void ClearTags()
    {
        for (var i = 0; i < MaxActiveEffects; i++)
        {
            _activeEffects[i] = default;
        }
        Array.Clear(_tagCounts);
        _activeTagsMask = GameplayTag.None;
    }

    /// <summary>指定タグをすべて保持しているか判定します（参照カウント > 0）。</summary>
    public bool HasTag(GameplayTag tag) => (_activeTagsMask & tag) == tag;

    /// <summary>指定タグのいずれかを保持しているか判定します（参照カウント > 0）。</summary>
    public bool HasAnyTag(GameplayTag tag) => (_activeTagsMask & tag) != GameplayTag.None;

    /// <summary>毎フレームのアクティブエフェクト時間減衰およびタグ参照カウント管理を推進します。</summary>
    public void UpdateTimedTags(float dt)
    {
        if (dt <= 0.0f) return;

        for (var i = 0; i < MaxActiveEffects; i++)
        {
            ref var effect = ref _activeEffects[i];
            if (!effect.IsActive) continue;

            effect.Remaining -= dt;
            if (effect.Remaining <= 0.0f)
            {
                effect.IsActive = false;
                DecrementTagCounts(effect.GrantedTags);
            }
        }
    }

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
