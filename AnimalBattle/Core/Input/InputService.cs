using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// DirectInput キーコードおよびゲームパッドボタンマスクによる低レベル入力状態管理サービス。
/// </summary>
public class InputService
{
    public const int KeyInputEscape = 0x01;
    public const int KeyInput1 = 0x02;
    public const int KeyInput2 = 0x03;
    public const int KeyInput3 = 0x04;
    public const int KeyInput4 = 0x05;
    public const int KeyInput5 = 0x06;
    public const int KeyInput6 = 0x07;
    public const int KeyInput7 = 0x08;
    public const int KeyInput8 = 0x09;
    public const int KeyInput9 = 0x0A;
    public const int KeyInput0 = 0x0B;
    public const int KeyInputReturn = 0x1C;
    public const int KeyInputSpace = 0x39;
    public const int KeyInputLeft = 0xCB;
    public const int KeyInputUp = 0xC8;
    public const int KeyInputRight = 0xCD;
    public const int KeyInputDown = 0xD0;
    public const int KeyInputW = 0x11;
    public const int KeyInputA = 0x1E;
    public const int KeyInputS = 0x1F;
    public const int KeyInputD = 0x20;
    public const int KeyInputJ = 0x24;
    public const int KeyInputF = 0x21;
    public const int KeyInputL = 0x26;
    public const int KeyInputF1 = 0x3B;

    public const int PadInputDown = 0x0001;
    public const int PadInputLeft = 0x0002;
    public const int PadInputRight = 0x0004;
    public const int PadInputUp = 0x0008;
    public const int PadInputButton1 = 0x0010;
    public const int PadInputButton2 = 0x0020;
    public const int PadInputButton3 = 0x0040;
    public const int PadInputButton4 = 0x0080;

    private readonly byte[] _currentKeys = new byte[256];
    private readonly byte[] _prevKeys = new byte[256];
    private readonly int[] _currentPads = new int[5];
    private readonly int[] _prevPads = new int[5];

    /// <summary>
    /// 毎フレーム呼び出し。キーボードおよび接続ゲームパッドの入力状態をサンプリング・更新します。
    /// </summary>
    public void Update()
    {
        Array.Copy(_currentKeys, _prevKeys, 256);
        for (int i = 0; i < 256; i++)
        {
            _currentKeys[i] = (byte)(NativeBindings.Engine_CheckHitKey(i) != 0 ? 1 : 0);
        }

        Array.Copy(_currentPads, _prevPads, 5);
        for (int p = 1; p <= 4; p++)
        {
            _currentPads[p] = NativeBindings.Engine_GetJoypadInputState(p - 1);
        }
    }

    /// <summary>
    /// 指定キーが押された瞬間であるか判定します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>押された瞬間であれば true</returns>
    public bool IsKeyPressed(int keyCode)
    {
        if (keyCode < 0 || keyCode >= 256)
        {
            return false;
        }
        return _currentKeys[keyCode] != 0 && _prevKeys[keyCode] == 0;
    }

    /// <summary>
    /// 指定キーが押し続けられているか判定します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>押下中であれば true</returns>
    public bool IsKeyDown(int keyCode)
    {
        if (keyCode < 0 || keyCode >= 256)
        {
            return false;
        }
        return _currentKeys[keyCode] != 0;
    }

    /// <summary>
    /// 指定キーが離された瞬間であるか判定します。
    /// </summary>
    /// <param name="keyCode">DirectInput キーコード</param>
    /// <returns>離された瞬間であれば true</returns>
    public bool IsKeyReleased(int keyCode)
    {
        if (keyCode < 0 || keyCode >= 256)
        {
            return false;
        }
        return _currentKeys[keyCode] == 0 && _prevKeys[keyCode] != 0;
    }

    /// <summary>
    /// 指定ゲームパッドのボタンマスクが押された瞬間であるか判定します。
    /// </summary>
    /// <param name="padIndex">パッド番号 (1～4)</param>
    /// <param name="mask">判定ビットマスク</param>
    /// <returns>押された瞬間であれば true</returns>
    public bool IsPadPressed(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4)
        {
            return false;
        }
        return (_currentPads[padIndex] & mask) != 0 && (_prevPads[padIndex] & mask) == 0;
    }

    /// <summary>
    /// 指定ゲームパッドのボタンマスクが押し続けられているか判定します。
    /// </summary>
    /// <param name="padIndex">パッド番号 (1～4)</param>
    /// <param name="mask">判定ビットマスク</param>
    /// <returns>押下中であれば true</returns>
    public bool IsPadDown(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4)
        {
            return false;
        }
        return (_currentPads[padIndex] & mask) != 0;
    }

    /// <summary>
    /// 指定ゲームパッドのボタンマスクが離された瞬間であるか判定します。
    /// </summary>
    /// <param name="padIndex">パッド番号 (1～4)</param>
    /// <param name="mask">判定ビットマスク</param>
    /// <returns>離された瞬間であれば true</returns>
    public bool IsPadReleased(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4)
        {
            return false;
        }
        return (_currentPads[padIndex] & mask) == 0 && (_prevPads[padIndex] & mask) != 0;
    }

    /// <summary>
    /// 指定ゲームパッドに入力があるか判定します。
    /// </summary>
    /// <param name="padIndex">パッド番号 (1～4)</param>
    /// <returns>入力が存在すれば true</returns>
    public bool HasPadInput(int padIndex)
    {
        if (padIndex < 1 || padIndex > 4)
        {
            return false;
        }
        return _currentPads[padIndex] != 0;
    }
}
