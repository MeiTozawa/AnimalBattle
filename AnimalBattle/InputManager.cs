using System;
using System.Collections.Generic;
using AnimalBattle.Bindings;

namespace AnimalBattle;

/// <summary>
/// キー入力の状態判定・トラッキングおよびアクション入力を取得するマネージャークラス。
/// </summary>
public class InputManager
{
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
        KeyInputEscape,
        KeyInputW, KeyInputA, KeyInputS, KeyInputD,
        KeyInputF, KeyInputG, KeyInputR, KeyInputSpace,
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
            _currentStates[key] = NativeEngine.CheckHitKey(key) != 0;
        }
    }

    /// <summary>
    /// 指定されたキーが現在押されているか判定します。
    /// </summary>
    /// <param name="key">判定対象のキーコード</param>
    /// <return>押されている場合は true</return>
    public bool IsKeyDown(int key)
    {
        return _currentStates.TryGetValue(key, out bool down) && down;
    }

    /// <summary>
    /// 指定されたキーが今フレームで押された瞬間か判定します。
    /// </summary>
    /// <param name="key">判定対象のキーコード</param>
    /// <return>押された瞬間の場合は true</return>
    public bool IsKeyPressed(int key)
    {
        bool current = _currentStates.TryGetValue(key, out bool cur) && cur;
        bool previous = _previousStates.TryGetValue(key, out bool prev) && prev;
        return current && !previous;
    }

    /// <summary>
    /// 指定されたキーが今フレームで離された瞬間か判定します。
    /// </summary>
    /// <param name="key">判定対象のキーコード</param>
    /// <return>離された瞬間の場合は true</return>
    public bool IsKeyReleased(int key)
    {
        bool current = _currentStates.TryGetValue(key, out bool cur) && cur;
        bool previous = _previousStates.TryGetValue(key, out bool prev) && prev;
        return !current && previous;
    }

    /// <summary>
    /// 指定されたプレイヤーの水平方向移動入力軸 (-1.0 ～ 1.0) を取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <return>水平入力軸の値</return>
    public float GetHorizontalAxis(int playerIndex = 1)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KeyInputA))
            {
                axis -= 1.0f;
            }
            if (IsKeyDown(KeyInputD))
            {
                axis += 1.0f;
            }
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KeyInputLeft))
            {
                axis -= 1.0f;
            }
            if (IsKeyDown(KeyInputRight))
            {
                axis += 1.0f;
            }
        }
        return axis;
    }

    /// <summary>
    /// 指定されたプレイヤーの垂直方向移動入力軸 (-1.0 ～ 1.0) を取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <return>垂直入力軸の値</return>
    public float GetVerticalAxis(int playerIndex = 1)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KeyInputW))
            {
                axis -= 1.0f;
            }
            if (IsKeyDown(KeyInputS))
            {
                axis += 1.0f;
            }
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KeyInputUp))
            {
                axis -= 1.0f;
            }
            if (IsKeyDown(KeyInputDown))
            {
                axis += 1.0f;
            }
        }
        return axis;
    }

    /// <summary>
    /// 指定されたプレイヤーのジャンプボタンが押されたか判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <return>押された場合は true</return>
    public bool IsJumpPressed(int playerIndex = 1)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KeyInputSpace) || IsKeyPressed(KeyInputW);
        }
        else
        {
            return IsKeyPressed(KeyInputUp);
        }
    }

    /// <summary>
    /// 指定されたプレイヤーの下方向（すり抜け・急降下）入力が押されたか判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <return>押された場合は true</return>
    public bool IsDownPressed(int playerIndex = 1)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KeyInputS);
        }
        else
        {
            return IsKeyPressed(KeyInputDown);
        }
    }

    /// <summary>
    /// リセットキー (R) が押されたか判定します。
    /// </summary>
    /// <return>押された場合は true</return>
    public bool IsResetPressed()
    {
        return IsKeyPressed(KeyInputR);
    }

    /// <summary>
    /// テスト用ヒット撃ち込みキー (F または G) が押されたか判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤーインデックス (1 または 2)</param>
    /// <return>押された場合は true</return>
    public bool IsKnockbackTestPressed(int playerIndex = 1)
    {
        return playerIndex == 1 ? IsKeyPressed(KeyInputF) : IsKeyPressed(KeyInputG);
    }

    /// <summary>
    /// 数字キー (1-4) によるステージ切替要求インデックスを取得します。
    /// </summary>
    /// <return>切り替え先マップインデックス（無効時は -1）</return>
    public int GetRequestedMapSwitchIndex()
    {
        if (IsKeyPressed(KeyInput1))
        {
            return 0;
        }
        if (IsKeyPressed(KeyInput2))
        {
            return 1;
        }
        if (IsKeyPressed(KeyInput3))
        {
            return 2;
        }
        if (IsKeyPressed(KeyInput4))
        {
            return 3;
        }
        return -1;
    }
}
