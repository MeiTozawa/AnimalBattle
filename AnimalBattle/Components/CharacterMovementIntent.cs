namespace AnimalBattle.Components;

/// <summary>
///     キャラクターの移動制御意図。
///     コントローラーが書き込み、KinematicMovementSystem が消費します。
/// </summary>
public struct CharacterMovementIntent
{
    /// <summary>水平移動入力値（-1.0f: 左, 1.0f: 右, 0.0f: 静止）。</summary>
    public float MoveX;

    /// <summary>今フレームで跳躍キーが押下されたかどうか。</summary>
    public bool JumpTriggered;

    /// <summary>跳躍キーが押し続けられているかどうか（可変高度跳躍用）。</summary>
    public bool JumpHolding;

    /// <summary>
    ///     すべての移動入力を初期状態にリセットします。
    /// </summary>
    public void Reset()
    {
        MoveX = 0.0f;
        JumpTriggered = false;
        JumpHolding = false;
    }
}
