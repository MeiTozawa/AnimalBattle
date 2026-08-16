namespace AnimalBattle.Components;

/// <summary>
/// プレイヤー番号 (1～4) を識別する純粋コンポーネント。
/// </summary>
public struct PlayerId
{
    /// <summary>プレイヤー番号 (1, 2, 3, 4)</summary>
    public int Id;

    /// <summary>
    /// 指定されたプレイヤー番号で <see cref="PlayerId"/> の新しいインスタンスを初期化します。
    /// </summary>
    public PlayerId(int id)
    {
        Id = id;
    }
}
