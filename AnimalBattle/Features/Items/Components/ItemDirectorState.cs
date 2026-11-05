namespace AnimalBattle.Features.Items.Components;

/// <summary>
///     ドロップアイテムの出現頻度および同時存在数を統括制御するディレクター状態コンポーネント。
/// </summary>
public struct ItemDirectorState
{
    /// <summary>ドロップ判定の内部クールダウン残り秒数（ICD）。</summary>
    public float IcdRemaining;

    /// <summary>現在フィールド上に存在する未回収アイテムの総数。</summary>
    public int ActiveHealthPackCount;
}
