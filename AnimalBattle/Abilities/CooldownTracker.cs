using AnimalBattle.Core;

namespace AnimalBattle.Abilities;

/// <summary>
///     UE GAS 準拠：持続時間付きタグ（Timed Tags）および能力ごとの冷却時間を一元管理する追跡器です。
///     冷却期間中は自動的に ASC へ CooldownTags を付与し、満了時に解除します。
/// </summary>
public sealed class CooldownTracker {
    private readonly Dictionary<StringId, float> _stringCooldowns = new();
    private readonly Dictionary<AbilityId, float> _cooldowns = new();
    private readonly List<TimedTag> _timedTags = new();

    private struct TimedTag {
        public GameplayTag Tag;
        public float Remaining;
    }

    /// <summary>直前に実行された能力識別タグを取得します。</summary>
    public StringId LastExecutedTag { get; private set; } = StringId.Empty;

    /// <summary>互換性プロパティ：直前に実行された能力識別子を取得します。</summary>
    public AbilityId LastExecutedAbility { get; private set; } = AbilityId.None;

    /// <summary>同一能力の連続実行回数を取得します。</summary>
    public int ConsecutiveUseCount { get; private set; }

    /// <summary>
    ///     経過時間に基づいてすべての能力の冷却時間および時限タグを減衰更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数。</param>
    /// <param name="asc">タグを同期する能力システムコンポーネント。</param>
    public void Update(float deltaTime, AbilitySystemComponent asc)
    {
        if (deltaTime <= 0.0f) return;

        // 1. 個別 StringId / AbilityId 冷却時間の減衰
        if (_stringCooldowns.Count > 0)
        {
            foreach (var key in _stringCooldowns.Keys)
            {
                var current = _stringCooldowns[key];
                if (current > 0.0f) _stringCooldowns[key] = MathF.Max(0.0f, current - deltaTime);
            }
        }

        if (_cooldowns.Count > 0)
        {
            foreach (var id in _cooldowns.Keys)
            {
                var current = _cooldowns[id];
                if (current > 0.0f) _cooldowns[id] = MathF.Max(0.0f, current - deltaTime);
            }
        }

        // 2. 時限 CooldownTag の減衰と満了時自動解除
        for (var i = _timedTags.Count - 1; i >= 0; i--)
        {
            var item = _timedTags[i];
            item.Remaining -= deltaTime;

            if (item.Remaining <= 0.0f)
            {
                asc.RemoveTag(item.Tag);
                _timedTags.RemoveAt(i);
            }
            else
            {
                _timedTags[i] = item;
            }
        }
    }

    /// <summary>
    ///     指定能力タグの冷却および時限タグを適用します。
    /// </summary>
    public void Trigger(AbilitySystemComponent asc, StringId tag, float cooldown, GameplayTag cooldownTags = GameplayTag.None)
    {
        if (tag.IsEmpty) return;

        if (tag == LastExecutedTag)
            ConsecutiveUseCount++;
        else
        {
            LastExecutedTag = tag;
            ConsecutiveUseCount = 1;
        }

        if (cooldown > 0.0f)
        {
            _stringCooldowns[tag] = cooldown;

            if (cooldownTags != GameplayTag.None)
            {
                asc.AddTag(cooldownTags);
                _timedTags.Add(new TimedTag { Tag = cooldownTags, Remaining = cooldown });
            }
        }
    }

    /// <summary>
    ///     互換性メソッド：指定能力識別子の冷却および時限タグを適用します。
    /// </summary>
    public void Trigger(AbilitySystemComponent asc, AbilityId id, float cooldown, GameplayTag cooldownTags = GameplayTag.None)
    {
        if (!id.IsValid()) return;

        if (id == LastExecutedAbility)
            ConsecutiveUseCount++;
        else
        {
            LastExecutedAbility = id;
            ConsecutiveUseCount = 1;
        }

        if (cooldown > 0.0f)
        {
            _cooldowns[id] = cooldown;

            if (cooldownTags != GameplayTag.None)
            {
                asc.AddTag(cooldownTags);
                _timedTags.Add(new TimedTag { Tag = cooldownTags, Remaining = cooldown });
            }
        }
    }

    /// <summary>
    ///     指定能力タグが使用可能（個別冷却完了）かどうかを判定します。
    /// </summary>
    public bool IsReady(StringId tag)
    {
        if (tag.IsEmpty) return true;
        return !_stringCooldowns.TryGetValue(tag, out var remaining) || remaining <= 0.0f;
    }

    /// <summary>
    ///     互換性メソッド：指定能力が使用可能（個別冷却完了）かどうかを判定します。
    /// </summary>
    public bool IsReady(AbilityId id)
    {
        if (!id.IsValid()) return false;
        return !_cooldowns.TryGetValue(id, out var remaining) || remaining <= 0.0f;
    }

    /// <summary>
    ///     指定能力タグの残り冷却秒数を取得します。
    /// </summary>
    public float GetRemaining(StringId tag)
    {
        if (tag.IsEmpty || !_stringCooldowns.TryGetValue(tag, out var remaining)) return 0.0f;
        return remaining;
    }

    /// <summary>
    ///     互換性メソッド：指定能力の残り冷却秒数を取得します。
    /// </summary>
    public float GetRemaining(AbilityId id)
    {
        if (!id.IsValid() || !_cooldowns.TryGetValue(id, out var remaining)) return 0.0f;
        return remaining;
    }

    /// <summary>
    ///     すべての能力の冷却時間、時限タグ、および連続使用履歴をリセットします。
    /// </summary>
    public void Reset(AbilitySystemComponent? asc = null)
    {
        if (asc is not null)
        {
            foreach (var item in _timedTags)
            {
                asc.RemoveTag(item.Tag);
            }
        }

        _stringCooldowns.Clear();
        _cooldowns.Clear();
        _timedTags.Clear();
        LastExecutedTag = StringId.Empty;
        LastExecutedAbility = AbilityId.None;
        ConsecutiveUseCount = 0;
    }
}
