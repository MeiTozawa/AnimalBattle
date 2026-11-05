namespace AnimalBattle.Features.Items.Components;

/// <summary>
///     プレイヤーとの接触判定および回復効果を定義するコンポーネント。
/// </summary>
public struct ItemPickup
{
    /// <summary>接触回収の有効半径（px）。</summary>
    public float Radius;

    /// <summary>拾得時に回復する HP 量。</summary>
    public float HealAmount;
}
