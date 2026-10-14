namespace AnimalBattle.Abilities;

/// <summary>
///     アビリティシステム上で現在持続適用されている軽量効果（Mini-GameplayEffect）情報。
///     付与タグ、持続時間、残り時間、および発生源エンティティ句柄を保持します。
/// </summary>
public struct ActiveGameplayEffect
{
    /// <summary>この効果スロットが有効（稼働中）であるか。</summary>
    public bool IsActive;

    /// <summary>この効果によって付与される戦闘タグ。</summary>
    public GameplayTag GrantedTags;

    /// <summary>残り持続秒数。</summary>
    public float Remaining;

    /// <summary>初期持続秒数。</summary>
    public float TotalDuration;

    /// <summary>付与元エンティティの ID または発生源ハンドル。</summary>
    public ulong SourceHandle;

    public ActiveGameplayEffect(GameplayTag grantedTags, float duration, ulong sourceHandle = 0)
    {
        IsActive = true;
        GrantedTags = grantedTags;
        Remaining = duration;
        TotalDuration = duration;
        SourceHandle = sourceHandle;
    }
}
