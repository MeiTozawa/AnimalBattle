using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ コア (AnimalBattleCore.dll) と直接やり取りを行い、生のキーボード、ゲームパッド、マウス状態のトラッキングを提供する低レベル基礎入力サービス。
/// </summary>
public partial class InputService
{
    private const string _dllName = "AnimalBattleCore.dll";

    #region Key Input Codes

    public const int KeyInputEscape = 0x01;
    public const int KeyInputW = 0x11;
    public const int KeyInputA = 0x1E;
    public const int KeyInputS = 0x1F;
    public const int KeyInputD = 0x20;
    public const int KeyInputF = 0x21;
    public const int KeyInputG = 0x22;
    public const int KeyInputR = 0x13;
    public const int KeyInputSpace = 0x39;
    public const int KeyInputReturn = 0x1C;
    public const int KeyInputUp = 0xC8;
    public const int KeyInputDown = 0xD0;
    public const int KeyInputLeft = 0xCB;
    public const int KeyInputRight = 0xCD;

    public const int KeyInputJ = 0x24;
    public const int KeyInputL = 0x26;
    public const int KeyInputF1 = 0x3B;

    public const int KeyInput1 = 0x02;
    public const int KeyInput2 = 0x03;
    public const int KeyInput3 = 0x04;
    public const int KeyInput4 = 0x05;

    public const int PadInputDown = 0x0001;
    public const int PadInputLeft = 0x0002;
    public const int PadInputRight = 0x0004;
    public const int PadInputUp = 0x0008;
    public const int PadInputButton1 = 0x0010; // A / Cross
    public const int PadInputButton2 = 0x0020; // B / Circle
    public const int PadInputButton3 = 0x0040; // X / Square
    public const int PadInputButton4 = 0x0080; // Y / Triangle

    #endregion

    private readonly int[] _trackedKeys =
    [
        KeyInputEscape, KeyInputF1, KeyInputReturn,
        KeyInputW, KeyInputA, KeyInputS, KeyInputD,
        KeyInputF, KeyInputG, KeyInputR, KeyInputSpace,
        KeyInputJ, KeyInputL,
        KeyInputUp, KeyInputDown, KeyInputLeft, KeyInputRight,
        KeyInput1, KeyInput2, KeyInput3, KeyInput4
    ];

    private readonly Dictionary<int, bool> _currentKeyStates = [];
    private readonly Dictionary<int, bool> _previousKeyStates = [];

    private readonly int[] _padCurrentState = new int[5];
    private readonly int[] _padPreviousState = new int[5];

    /// <summary>
    /// 生の入力状態辞書を初期化して <see cref="InputService"/> の新しいインスタンスを生成します。
    /// </summary>
    public InputService()
    {
        foreach (int key in _trackedKeys)
        {
            _currentKeyStates[key] = false;
            _previousKeyStates[key] = false;
        }
    }

    /// <summary>
    /// 毎フレーム呼び出し。キーボードおよびゲームパッド 1～4 の生の物理入力状態をポーリング・更新します。
    /// </summary>
    public void Update()
    {
        foreach (int key in _trackedKeys)
        {
            _previousKeyStates[key] = _currentKeyStates[key];
            _currentKeyStates[key] = NativeCheckHitKey(key) != 0;
        }

        for (int p = 1; p <= 4; p++)
        {
            _padPreviousState[p] = _padCurrentState[p];
            _padCurrentState[p] = NativeGetJoypadInputState(p);
        }
    }

    /// <summary>
    /// 指定されたキーが現在押されているか判定します。
    /// </summary>
    public bool IsKeyDown(int key)
    {
        return _currentKeyStates.TryGetValue(key, out bool down) && down;
    }

    /// <summary>
    /// 指定されたキーが今フレームで押し始められた瞬間か判定します。
    /// </summary>
    public bool IsKeyPressed(int key)
    {
        bool cur = _currentKeyStates.TryGetValue(key, out bool c) && c;
        bool prev = _previousKeyStates.TryGetValue(key, out bool p) && p;
        return cur && !prev;
    }

    /// <summary>
    /// 指定されたキーが今フレームで離された瞬間か判定します。
    /// </summary>
    public bool IsKeyReleased(int key)
    {
        bool cur = _currentKeyStates.TryGetValue(key, out bool c) && c;
        bool prev = _previousKeyStates.TryGetValue(key, out bool p) && p;
        return !cur && prev;
    }

    /// <summary>
    /// 指定されたゲームパッド (1-4) の特定ボタン/方向キーが現在押されているか判定します。
    /// </summary>
    public bool IsPadDown(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4) return false;
        return (_padCurrentState[padIndex] & mask) != 0;
    }

    /// <summary>
    /// 指定されたゲームパッド (1-4) の特定ボタン/方向キーが今フレームで押し始められた瞬間か判定します。
    /// </summary>
    public bool IsPadPressed(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4) return false;
        bool cur = (_padCurrentState[padIndex] & mask) != 0;
        bool prev = (_padPreviousState[padIndex] & mask) != 0;
        return cur && !prev;
    }

    /// <summary>
    /// 指定されたゲームパッド (1-4) の特定ボタン/方向キーが今フレームで離された瞬間か判定します。
    /// </summary>
    public bool IsPadReleased(int padIndex, int mask)
    {
        if (padIndex < 1 || padIndex > 4) return false;
        bool cur = (_padCurrentState[padIndex] & mask) != 0;
        bool prev = (_padPreviousState[padIndex] & mask) != 0;
        return !cur && prev;
    }

    /// <summary>
    /// 指定されたパッド (1-4) に入力があるかを返します。
    /// </summary>
    public bool HasPadInput(int padIndex)
    {
        if (padIndex < 1 || padIndex > 4) return false;
        return _padCurrentState[padIndex] != 0;
    }

    /// <summary>
    /// マウスカーソルを手の形（ポインター）に変更します。
    /// </summary>
    public static void SetCursorPointer() => NativeSetCursorPointer();

    /// <summary>
    /// マウスカーソルを標準の矢印に変更します。
    /// </summary>
    public static void SetCursorArrow() => NativeSetCursorArrow();

    /// <summary>
    /// 現在のマウスカーソル座標を取得します。
    /// </summary>
    public static unsafe void GetMousePoint(out int x, out int y)
    {
        int tempX = 0;
        int tempY = 0;
        NativeGetMousePoint(&tempX, &tempY);
        x = tempX;
        y = tempY;
    }

    #region P/Invoke Bindings

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    private static partial void NativeSetCursorPointer();

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    private static partial void NativeSetCursorArrow();

    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    private static partial int NativeCheckHitKey(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadInputState")]
    [SuppressGCTransition]
    private static partial int NativeGetJoypadInputState(int inputType);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadNum")]
    [SuppressGCTransition]
    private static partial int NativeGetJoypadNum();

    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    private static unsafe partial void NativeGetMousePoint(int* x, int* y);

    #endregion
}
