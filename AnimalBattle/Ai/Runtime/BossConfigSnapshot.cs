namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 初期化時または設定更新時に検証済みとして公開する Boss AI 設定 snapshot です。
/// </summary>
public readonly struct BossConfigSnapshot
{
    /// <summary>未初期化の設定 snapshot を表す値です。</summary>
    public static BossConfigSnapshot Empty => default;

    /// <summary>設定 snapshot の世代を取得します。</summary>
    public long Version { get; }

    /// <summary>設定が検証済みで実行に使用できるかどうかを取得します。</summary>
    public bool IsValid { get; }

    /// <summary>設定の入力元を識別する不変の名前を取得します。</summary>
    public string? SourceName { get; }

    /// <summary>検証済み設定 snapshot を生成します。</summary>
    /// <param name="version">設定 version。0 以上である必要があります。</param>
    /// <param name="sourceName">設定を識別する不変の名前。通常の Frame_Update では読み取りません。</param>
    public BossConfigSnapshot(long version, string? sourceName = null)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Version = version;
        IsValid = true;
        SourceName = sourceName;
    }
}

/// <summary>
/// State_Module が参照する検証済み設定 snapshot の提供境界です。
/// </summary>
public interface IConfigProvider
{
    /// <summary>現在有効な設定 snapshot を取得します。</summary>
    BossConfigSnapshot Current { get; }
}
