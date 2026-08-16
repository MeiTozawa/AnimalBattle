namespace AnimalBattle.Components;

/// <summary>
/// キャラクターの向き（左向き判定）を表すコンポーネント。
/// </summary>
public struct Facing
{
    /// <summary>左向きであるかフラグ</summary>
    public bool IsFacingLeft;

    /// <summary>
    /// 指定された向きで <see cref="Facing"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Facing(bool isFacingLeft)
    {
        IsFacingLeft = isFacingLeft;
    }
}
