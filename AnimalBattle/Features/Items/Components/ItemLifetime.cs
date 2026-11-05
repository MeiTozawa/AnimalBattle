namespace AnimalBattle.Features.Items.Components;

/// <summary>
///     ドロップアイテムの生存期間および点滅演出を管理するコンポーネント。
/// </summary>
public struct ItemLifetime
{
    /// <summary>残存寿命秒数。</summary>
    public float Remaining;

    /// <summary>消失直前の警告点滅を開始する残り秒数閾値。</summary>
    public float BlinkThreshold;
}
