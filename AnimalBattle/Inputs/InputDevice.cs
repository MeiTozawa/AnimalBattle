namespace AnimalBattle.Inputs;

/// <summary>
/// 最後に入力を検出したアクティブデバイスの種別を定義する列挙型。
/// UI のボタンガイド表示などの動的変更に使用されます。
/// </summary>
public enum InputDevice
{
    /// <summary>キーボード入力</summary>
    Keyboard,

    /// <summary>ゲームパッド (Joypad) 入力</summary>
    Gamepad
}
