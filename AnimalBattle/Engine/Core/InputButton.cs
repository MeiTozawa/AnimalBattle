namespace AnimalBattle.Core;

/// <summary>
///     キーボード、マウス、ゲームパッドを統一的に扱うボタン識別子。
/// </summary>
public enum InputButton {
    // --- キーボード DxLibキーコード互換 ---
    KeyEscape = 0x01,
    Key1 = 0x02,
    Key2 = 0x03,
    Key3 = 0x04,
    Key4 = 0x05,
    KeyW = 0x11,
    KeyR = 0x13,
    KeyT = 0x14,
    KeyReturn = 0x1C,
    KeyA = 0x1E,
    KeyS = 0x1F,
    KeyD = 0x20,
    KeyF = 0x21,
    KeyH = 0x23,
    KeyK = 0x25,
    KeyLShift = 0x2A,
    KeyZ = 0x2C,
    KeyC = 0x2E,
    KeySpace = 0x39,
    KeyUp = 0xC8,
    KeyLeft = 0xCB,
    KeyRight = 0xCD,
    KeyDown = 0xD0,
    KeyRShift = 0x36,
    KeyF1 = 0x3B,
    KeyF2 = 0x3C,
    KeyF3 = 0x3D,
    KeyF4 = 0x3E,
    KeyF5 = 0x3F,
    KeyF6 = 0x40,
    KeyF7 = 0x41,
    KeyF8 = 0x42,
    KeyF9 = 0x43,

    // --- マウスボタン 300から399の範囲 ---
    MouseLeft = 300,
    MouseRight = 301,

    // --- ゲームパッド DxLibゲームパッド入力対応 ---
    PadDown = 400,
    PadLeft = 401,
    PadRight = 402,
    PadUp = 403,
    Pad1 = 404,
    Pad2 = 405,
    Pad3 = 406,
    PadStart = 412
}
