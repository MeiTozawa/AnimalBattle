namespace AnimalBattle.Framework.Pooling;

/// <summary>
///     エンティティのプール貸出状態および所属プールを識別するコンポーネント。
/// </summary>
public struct PoolLeaseState {
    /// <summary>所属プールの固有識別子。</summary>
    public ulong PoolId;

    /// <summary>現在貸出中かどうか。</summary>
    public bool IsRented;

    /// <summary>プール内での固定スロットインデックス。</summary>
    public int SlotIndex;
}
