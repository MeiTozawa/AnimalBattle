namespace AnimalBattle.Components;

/// <summary>
///     エンティティのプール貸出状態および所属プール代際を識別するコンポーネント。
/// </summary>
public struct PoolLeaseState {
    /// <summary>所属プールの固有識別子。</summary>
    public ulong PoolId;

    /// <summary>エンティティの貸出世代バージョン（再貸出ごとにインクリメントされ、ABA問題を防止）。</summary>
    public uint Version;

    /// <summary>現在貸出中かどうか。</summary>
    public bool IsRented;

    /// <summary>プール内での固定スロットインデックス。</summary>
    public int SlotIndex;
}
