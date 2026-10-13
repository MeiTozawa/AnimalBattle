namespace AnimalBattle.Abilities;

/// <summary>
///     UE GAS 準拠：持続時間付きタグ（Timed Tags）および能力ごとの冷却時間を一元管理する追跡器です。
///     冷却期間中は自動的に ASC へ CooldownTags を付与し、満了時に解除します。
/// </summary>
public sealed class CooldownTracker {
    private readonly Dictionary<AbilityId, float> _cooldowns = new();
    private readonly List<TimedTag> _timedTags = new();

    private struct TimedTag {
        public GameplayTag Tag;
        public float Remaining;
    }

    /// <summary>直前に実行された能力識別子を取得します。</summary>
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

        // 1. 個別 AbilityId 冷却時間の減衰
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
    ///     指定能力の冷却および時限タグを適用します。
    /// </summary>
    /// <param name="asc">対象の能力システムコンポーネント。</param>
    /// <param name="id">能力識別子。</param>
    /// <param name="cooldown">冷却秒数。</param>
    /// <param name="cooldownTags">付与する冷却タグ（CooldownMelee, CooldownGlobal 等）。</param>
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
    ///     指定能力が使用可能（個別冷却完了）かどうかを判定します。
    /// </summary>
    /// <param name="id">対象の能力識別子。</param>
    /// <returns>冷却が完了している場合は true。</returns>
    public bool IsReady(AbilityId id)
    {
        if (!id.IsValid()) return false;
        return !_cooldowns.TryGetValue(id, out var remaining) || remaining <= 0.0f;
    }

    /// <summary>
    ///     指定能力の残り冷却秒数を取得します。
    /// </summary>
    /// <param name="id">対象の能力識別子。</param>
    /// <returns>残り冷却秒数。冷却完了時は 0.0f。</returns>
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

        _cooldowns.Clear();
        _timedTags.Clear();
        LastExecutedAbility = AbilityId.None;
        ConsecutiveUseCount = 0;
    }
}
