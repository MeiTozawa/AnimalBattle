namespace AnimalBattle.Player.Components;

/// <summary>
///     キャラクターの移動制御意図。
///     コントローラーが書き込み、MovementSystem が消費します。
/// </summary>
public struct MovementIntent
{
    /// <summary>水平移動入力値：-1.0f: 左, 1.0f: 右, 0.0f: 静止。</summary>
    public float MoveX;

    /// <summary>今フレームで跳躍キーが押下されたかどうか。</summary>
    public bool JumpTriggered;

    /// <summary>跳躍キーが押し続けられているかどうか：可変高度跳躍用。</summary>
    public bool JumpHolding;

    /// <summary>今フレームでダッシュ・回避キーが押下されたかどうか。</summary>
    public bool DashTriggered;

    /// <summary>水平指向方向：-1.0f: 左向き, 1.0f: 右向き。</summary>
    public float FacingX;

    /// <summary>
    ///     すべての移動入力を初期状態にリセットします。
    /// </summary>
    public void Reset()
    {
        MoveX = 0.0f;
        JumpTriggered = false;
        JumpHolding = false;
        DashTriggered = false;
        FacingX = 1.0f;
    }
}
