namespace AnimalBattle.Abilities;

/// <summary>
///     能力ごとの冷却時間を一元管理する高速追跡器です。
///     ヒープ割り当てを生じさせない零 GC 構造として動作します。
/// </summary>
public sealed class CooldownTracker {
    private readonly Dictionary<StringId, float> _stringCooldowns = new();

    /// <summary>直前に実行された能力識別タグを取得します。</summary>
    public StringId LastExecutedTag { get; private set; } = StringId.Empty;

    /// <summary>同一能力の連続実行回数を取得します。</summary>
    public int ConsecutiveUseCount { get; private set; }

    /// <summary>
    ///     経過時間に基づいてすべての能力の冷却時間を減衰更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数。</param>
    public void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f || _stringCooldowns.Count == 0) return;

        foreach (var key in _stringCooldowns.Keys)
        {
            var current = _stringCooldowns[key];
            if (current > 0.0f) _stringCooldowns[key] = MathF.Max(0.0f, current - deltaTime);
        }
    }

    /// <summary>
    ///     指定能力タグの冷却を適用します。
    /// </summary>
    public void Trigger(StringId tag, float cooldown)
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
}
