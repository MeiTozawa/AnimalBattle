using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// キー入力およびマウスカーソル状態の判定・トラッキングを管理するクラス。
/// </summary>
public partial class InputManager
{
    private const string _dllName = "AnimalBattleCore.dll";

    /// <summary>Escape キーのインプットコード</summary>
    public const int KeyInputEscape = 0x01;
    /// <summary>W キーのインプットコード</summary>
    public const int KeyInputW = 0x11;
    /// <summary>A キーのインプットコード</summary>
    public const int KeyInputA = 0x1E;
    /// <summary>S キーのインプットコード</summary>
    public const int KeyInputS = 0x1F;
    /// <summary>D キーのインプットコード</summary>
    public const int KeyInputD = 0x20;
    /// <summary>F キーのインプットコード</summary>
    public const int KeyInputF = 0x21;
    /// <summary>G キーのインプットコード</summary>
    public const int KeyInputG = 0x22;
    /// <summary>R キーのインプットコード</summary>
    public const int KeyInputR = 0x13;
    /// <summary>Space キーのインプットコード</summary>
    public const int KeyInputSpace = 0x39;
    /// <summary>上矢印キーのインプットコード</summary>
    public const int KeyInputUp = 0xC8;
    /// <summary>下矢印キーのインプットコード</summary>
    public const int KeyInputDown = 0xD0;
    /// <summary>左矢印キーのインプットコード</summary>
    public const int KeyInputLeft = 0xCB;
    /// <summary>右矢印キーのインプットコード</summary>
    public const int KeyInputRight = 0xCD;

    /// <summary>J キーのインプットコード (P1 攻撃)</summary>
    public const int KeyInputJ = 0x24;
    /// <summary>L キーのインプットコード (P2 攻撃)</summary>
    public const int KeyInputL = 0x26;

    /// <summary>F1 キーのインプットコード (Debug 表示トグル)</summary>
    public const int KeyInputF1 = 0x3B;

    /// <summary>数字の 1 キーのインプットコード</summary>
    public const int KeyInput1 = 0x02;
    /// <summary>数字の 2 キーのインプットコード</summary>
    public const int KeyInput2 = 0x03;
    /// <summary>数字の 3 キーのインプットコード</summary>
    public const int KeyInput3 = 0x04;
    /// <summary>数字の 4 キーのインプットコード</summary>
    public const int KeyInput4 = 0x05;

    private readonly int[] _trackedKeys =
    [
        KeyInputEscape, KeyInputF1,
        KeyInputW, KeyInputA, KeyInputS, KeyInputD,
        KeyInputF, KeyInputG, KeyInputR, KeyInputSpace,
        KeyInputJ, KeyInputL,
        KeyInputUp, KeyInputDown, KeyInputLeft, KeyInputRight,
        KeyInput1, KeyInput2, KeyInput3, KeyInput4
    ];

    private readonly Dictionary<int, bool> _currentStates = [];
    private readonly Dictionary<int, bool> _previousStates = [];

    /// <summary>
    /// トラッキング対象キーの状態辞書を初期化して <see cref="InputManager"/> の新しいインスタンスを生成します。
    /// </summary>
    public InputManager()
    {
        foreach (int key in _trackedKeys)
        {
            _currentStates[key] = false;
            _previousStates[key] = false;
        }
    }

    /// <summary>
    /// 各キーの現在および前フレームの押下状態を更新します。
    /// </summary>
    public void Update()
    {
        foreach (int key in _trackedKeys)
        {
            _previousStates[key] = _currentStates[key];
            _currentStates[key] = NativeCheckHitKey(key) != 0;
        }
    }

    /// <summary>
    /// 指定されたキーが現在押されているか判定します。
    /// </summary>
    public bool IsKeyDown(int key)
    {
        return _currentStates.TryGetValue(key, out bool down) && down;
    }

    /// <summary>
    /// 指定されたキーが今フレームで新しく押された瞬間か判定します。
    /// </summary>
    public bool IsKeyPressed(int key)
    {
        bool cur = _currentStates.TryGetValue(key, out bool c) && c;
        bool prev = _previousStates.TryGetValue(key, out bool p) && p;
        return cur && !prev;
    }

    /// <summary>
    /// 指定されたキーが今フレームで離された瞬間か判定します。
    /// </summary>
    public bool IsKeyReleased(int key)
    {
        bool cur = _currentStates.TryGetValue(key, out bool c) && c;
        bool prev = _previousStates.TryGetValue(key, out bool p) && p;
        return !cur && prev;
    }

    /// <summary>
    /// プレイヤー（1 または 2）の移動入力値 (-1.0f ～ 1.0f) を取得します。
    /// </summary>
    public float GetAxisX(int playerIndex)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KeyInputD)) axis += 1.0f;
            if (IsKeyDown(KeyInputA)) axis -= 1.0f;
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KeyInputRight)) axis += 1.0f;
            if (IsKeyDown(KeyInputLeft)) axis -= 1.0f;
        }
        return axis;
    }

    /// <summary>
    /// プレイヤー（1 または 2）の下方向入力判定を取得します。
    /// </summary>
    public bool GetAxisDown(int playerIndex)
    {
        if (playerIndex == 1)
        {
            return IsKeyDown(KeyInputS);
        }
        if (playerIndex == 2)
        {
            return IsKeyDown(KeyInputDown);
        }
        return false;
    }

    /// <summary>
    /// プレイヤー（1 または 2）のジャンプボタンが押されたか判定します。
    /// </summary>
    public bool IsJumpPressed(int playerIndex)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KeyInputW) || IsKeyPressed(KeyInputSpace);
        }
        if (playerIndex == 2)
        {
            return IsKeyPressed(KeyInputUp);
        }
        return false;
    }

    /// <summary>
    /// プレイヤー（1 または 2）の攻撃ボタンが押されたか判定します。
    /// </summary>
    public bool IsAttackPressed(int playerIndex)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KeyInputJ);
        }
        if (playerIndex == 2)
        {
            return IsKeyPressed(KeyInputL);
        }
        return false;
    }

    /// <summary>
    /// プレイヤー（1 または 2）の水平移動軸 (-1.0f ～ 1.0f) を取得します。
    /// </summary>
    public float GetHorizontalAxis(int playerIndex) => GetAxisX(playerIndex);

    /// <summary>
    /// プレイヤー（1 または 2）の垂直移動軸 (-1.0f ～ 1.0f) を取得します。
    /// </summary>
    public float GetVerticalAxis(int playerIndex)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KeyInputS)) axis += 1.0f;
            if (IsKeyDown(KeyInputW)) axis -= 1.0f;
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KeyInputDown)) axis += 1.0f;
            if (IsKeyDown(KeyInputUp)) axis -= 1.0f;
        }
        return axis;
    }

    /// <summary>
    /// プレイヤー（1 または 2）の下入力が押されたか判定します。
    /// </summary>
    public bool IsDownPressed(int playerIndex) => GetAxisDown(playerIndex);

    /// <summary>
    /// デバッグ表示トグルキー (F1) が押されたか判定します。
    /// </summary>
    public bool IsDebugTogglePressed() => IsKeyPressed(KeyInputF1);

    /// <summary>
    /// マップ切り替えキー (1, 2, 3, 4) が押された場合、そのマップインデックス (1-4) を返します。押されていない場合は -1 を返します。
    /// </summary>
    public int GetRequestedMapSwitchIndex()
    {
        if (IsKeyPressed(KeyInput1)) return 1;
        if (IsKeyPressed(KeyInput2)) return 2;
        if (IsKeyPressed(KeyInput3)) return 3;
        if (IsKeyPressed(KeyInput4)) return 4;
        return -1;
    }

    /// <summary>
    /// マウスカーソルを手の形（ポインター）に変更します。
    /// </summary>
    public static void SetCursorPointer()
    {
        NativeSetCursorPointer();
    }

    /// <summary>
    /// マウスカーソルを標準の矢印に変更します。
    /// </summary>
    public static void SetCursorArrow()
    {
        NativeSetCursorArrow();
    }

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

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    private static partial void NativeSetCursorPointer();

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    private static partial void NativeSetCursorArrow();

    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    private static partial int NativeCheckHitKey(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    private static unsafe partial void NativeGetMousePoint(int* x, int* y);
}
