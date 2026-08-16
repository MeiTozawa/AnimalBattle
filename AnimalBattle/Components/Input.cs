namespace AnimalBattle.Components;

/// <summary>
/// プレイヤー操作入力を表すコンポーネント。
/// </summary>
public struct Input
{
    /// <summary>左移動入力フラグ</summary>
    public bool MoveLeft;

    /// <summary>右移動入力フラグ</summary>
    public bool MoveRight;

    /// <summary>ジャンプ入力フラグ</summary>
    public bool Jump;

    /// <summary>攻撃入力（長押し含む）フラグ</summary>
    public bool Attack;

    /// <summary>攻撃入力（押した瞬間のみ true）フラグ</summary>
    public bool AttackJustPressed;

    /// <summary>前フレームの攻撃入力状態</summary>
    public bool PrevAttack;
}
