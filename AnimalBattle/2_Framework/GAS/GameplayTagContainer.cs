namespace AnimalBattle.Abilities;

/// <summary>
///     ヒープ割り当てを生じさせない零 GC 戦闘タグコンテナです。
///     タグの追加、削除、部分合致および排他制約判定を提供します。
/// </summary>
public struct GameplayTagContainer(GameplayTag initialTags = GameplayTag.None) {
    /// <summary>生のビットマスク値を取得します。</summary>
    public GameplayTag RawTags { get; private set; } = initialTags;

    /// <summary>指定されたタグをコンテナに付与します。</summary>
    public void Add(GameplayTag tag) => RawTags |= tag;

    /// <summary>指定されたタグをコンテナから解除します。</summary>
    public void Remove(GameplayTag tag) => RawTags &= ~tag;

    /// <summary>すべてのタグをクリアします。</summary>
    public void Clear() => RawTags = GameplayTag.None;

    /// <summary>指定タグのフラグがすべて立っているか判定します。</summary>
    public readonly bool HasTag(GameplayTag tag) => (RawTags & tag) == tag;

    /// <summary>指定タグのいずれかが含まれているか判定します。</summary>
    public readonly bool HasAny(GameplayTag tags) => (RawTags & tags) != GameplayTag.None;

    /// <summary>指定タグがすべて含まれているか判定します。</summary>
    public readonly bool HasAll(GameplayTag tags) => (RawTags & tags) == tags;

    /// <summary>必須タグと排他タグの制約を満たしているか判定します。</summary>
    public readonly bool Matches(GameplayTag required, GameplayTag blocked) =>
        (required == GameplayTag.None || HasAll(required)) && !HasAny(blocked);

    /// <summary>暗黙的に生のビットマスクを取得します。</summary>
    public static implicit operator GameplayTag(GameplayTagContainer container) => container.RawTags;
}
