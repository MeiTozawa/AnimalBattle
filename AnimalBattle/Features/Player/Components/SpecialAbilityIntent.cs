namespace AnimalBattle.Player.Components;

/// <summary>
///     自機の特殊能力（必殺技）発動意図コンポーネント。
///     コントローラーが書き込み、PlayerSpecialAbilitySystem が消費します。
/// </summary>
public struct SpecialAbilityIntent
{
    /// <summary>今フレームで特殊能力トリガーが引かれたかどうか</summary>
    public bool Triggered;

    /// <summary>照準方向ベクトル</summary>
    public Vec2f AimDirection;

    /// <summary>すべての特殊能力入力を初期状態にリセットします</summary>
    public void Reset()
    {
        Triggered = false;
        AimDirection = Vec2f.Zero;
    }
}
