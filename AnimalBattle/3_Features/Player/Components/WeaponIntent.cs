namespace AnimalBattle.Components;

/// <summary>
///     キャラクターの射撃・攻撃制御意図。
///     コントローラーが書き込み、WeaponSystem が消費します。
/// </summary>
public struct WeaponIntent
{
    /// <summary>今フレームで射撃トリガーが引かれたかどうか。</summary>
    public bool ShootTriggered;

    /// <summary>瞄準方向の X 成分。</summary>
    public float AimDirectionX;

    /// <summary>瞄準方向の Y 成分。</summary>
    public float AimDirectionY;

    /// <summary>
    ///     すべての射撃入力を初期状態にリセットします。
    /// </summary>
    public void Reset()
    {
        ShootTriggered = false;
        AimDirectionX = 0.0f;
        AimDirectionY = 0.0f;
    }
}
