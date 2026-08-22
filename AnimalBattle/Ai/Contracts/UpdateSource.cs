namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// AI データを更新した責務の出所を定義します。
/// </summary>
public enum UpdateSource
{
    /// <summary>
    /// 更新元を判定できないことを示します。
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 新しい Boss AI ランタイムを示します。
    /// </summary>
    NewRuntime = 1,

    /// <summary>
    /// 新旧データを変換する Migration Adapter を示します。
    /// </summary>
    MigrationAdapter = 2,

    /// <summary>
    /// 移行期間中の旧コンポーネントを示します。
    /// </summary>
    LegacyComponent = 3
}
