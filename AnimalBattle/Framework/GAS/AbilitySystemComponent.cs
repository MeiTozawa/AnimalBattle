using System.Diagnostics;
using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     エンティティの技能登録、冷却管理、タグ制約を一元管理する汎用能力システムオーケストレーター。
///     特定のゲーム演出・アクション（弾幕や判定等）から完全に分離された、純粋なフレームワーク層の GAS コンポーネントです。
/// </summary>
public sealed class AbilitySystemComponent {
    private const int MaxTagBits = 16;
    private readonly Dictionary<StringId, GameplayAbility> _abilities = new();
    private readonly CooldownTracker _cooldownTracker = new();
    private readonly int[] _tagCounts = new int[MaxTagBits];
    private GameplayTag _activeTagsMask = GameplayTag.None;

    /// <summary>戦闘タグコンテナを取得します。</summary>
    public GameplayTagContainer Tags => new(_activeTagsMask);

    /// <summary>現在発動中の技能インスタンスを取得します。</summary>
    public GameplayAbility? CurrentAbility { get; private set; }

    /// <summary>冷却時間追跡器を取得します。</summary>
    public CooldownTracker Cooldowns => _cooldownTracker;

    /// <summary>現在発動中の技能の経過時間（秒）を取得します。</summary>
    public float Elapsed => CurrentAbility?.Elapsed ?? 0.0f;

    /// <summary>現在発動中の技能の目標持続時間（秒）を取得します。</summary>
    public float Duration => CurrentAbility?.Duration ?? 0.0f;

    /// <summary>現在発動中の技能の進行度（0.0 ～ 1.0）を取得します。</summary>
    public float Progress => CurrentAbility?.Progress ?? 0.0f;

    /// <summary>現在何らかの技能を発動中かどうかを取得します。</summary>
    public bool IsCasting => CurrentAbility != null;

    /// <summary>戦闘タグ状態が変更された際に発火するイベント。</summary>
    public event Action<GameplayTag>? OnTagsChanged;

    /// <summary>技能完了通知イベント。</summary>
    public event Action<StringId>? OnSkillCompleted;

    /// <summary>能力を登録します。</summary>
    public void GiveAbility(GameplayAbility ability)
    {
        Debug.Assert(ability != null, "ability は null であってはなりません。");

        if (!ability.AbilityTag.IsEmpty)
        {
            _abilities[ability.AbilityTag] = ability;
        }
    }

    /// <summary>技能識別タグによって登録済み能力を取得します。</summary>
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
        OnTagsChanged?.Invoke(_activeTagsMask);
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
        OnTagsChanged?.Invoke(_activeTagsMask);
    }

    /// <summary>戦闘タグを付与します。</summary>
    public void AddTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;
        IncrementTagCounts(tag);
    }

    /// <summary>戦闘タグを解除します。</summary>
    public void RemoveTag(GameplayTag tag)
    {
        if (tag == GameplayTag.None) return;
        DecrementTagCounts(tag);
    }

    /// <summary>すべての戦闘タグをクリアします。</summary>
    public void ClearTags()
    {
        Array.Clear(_tagCounts);
        _activeTagsMask = GameplayTag.None;
        OnTagsChanged?.Invoke(_activeTagsMask);
    }

    /// <summary>指定タグをすべて保持しているか判定します。</summary>
    public bool HasTag(GameplayTag tag) => (_activeTagsMask & tag) == tag;

    /// <summary>指定タグのいずれかを保持しているか判定します。</summary>
    public bool HasAnyTag(GameplayTag tag) => (_activeTagsMask & tag) != GameplayTag.None;



    /// <summary>技能識別タグを指定して発動を試行します。</summary>
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

        if (!ability.CanActivate(this, in ctx))
        {
            return false;
        }

        CurrentAbility = ability;

        if (ability.ActivationOwnedTags != GameplayTag.None)
        {
            AddTag(ability.ActivationOwnedTags);
        }

        CommitCooldown(ability);
        ability.Activate(this, in ctx);

        return true;
    }

    /// <summary>技能の冷却時間を適用します。</summary>
    public void CommitCooldown(GameplayAbility ability)
    {
        if (!ability.AbilityTag.IsEmpty)
        {
            _cooldownTracker.Trigger(ability.AbilityTag, ability.Cooldown);
        }
    }

    /// <summary>現在発動中の技能を完了・終了します。</summary>
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

    /// <summary>現在発動中の技能を即座に中断します。</summary>
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

    /// <summary>毎フレームの技能時序推進と冷却時間更新を実行します。</summary>
    public void Update(float dt, bool isGrounded = false)
    {
        _cooldownTracker.Update(dt);
        CurrentAbility?.Update(dt, this, isGrounded);
    }
}
