using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     キャラクターの射撃・攻撃制御意図。
///     コントローラーが書き込み、WeaponSystem が消費します。
/// </summary>
public struct WeaponIntent
{
    /// <summary>今フレームで射撃トリガーが引かれたかどうか。</summary>
    public bool ShootTriggered;

    /// <summary>照準方向ベクトル Vec2f型</summary>
    public Vec2f AimDirection;

    /// <summary>
    ///     すべての射撃入力を初期状態にリセットします。
    /// </summary>
    public void Reset()
    {
        ShootTriggered = false;
        AimDirection = Vec2f.Zero;
    }
}
