using System;
using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Inputs;
/// <summary>
/// <see cref="InputService"/> 低レベルサービスを利用し、アクションマッピング、コンテキストスタック管理、およびマルチプレイヤー入力制御を行う高レベルマネージャークラス。
/// </summary>
public class InputManager
{
    private readonly struct InputBinding(int keyCode = 0, int padMask = 0)
    {
        public int KeyCode { get; } = keyCode;
        public int PadMask { get; } = padMask;
    }

    private readonly InputService _service;
    private readonly InputDevice[] _activeDevicePerPlayer = new InputDevice[5];
    private readonly List<InputContext> _contextStack = [];
    private readonly Dictionary<(InputContext Context, int PlayerIndex), Dictionary<InputAction, List<InputBinding>>> _actionMappings = [];

    /// <summary>
    /// 内部で保持している低レベル <see cref="InputService"/> インスタンスを取得します。
    /// </summary>
    public InputService Service => _service;

    /// <summary>
    /// 現在最上位のアクティブな入力コンテキストを取得します。
    /// </summary>
    public InputContext CurrentContext => _contextStack.Count > 0 ? _contextStack[^1] : InputContext.InGame;

    /// <summary>
    /// <see cref="InputService"/> インスタンスを指定して <see cref="InputManager"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="service">使用する低レベル入力サービス</param>
    public InputManager(InputService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        for (int p = 1; p <= 4; p++)
        {
            _activeDevicePerPlayer[p] = InputDevice.Keyboard;
        }

        _contextStack.Add(InputContext.InGame);
        RegisterActionMappings();
    }

    /// <summary>
    /// デフォルトの <see cref="InputService"/> インスタンスで <see cref="InputManager"/> を初期化します。
    /// </summary>
    public InputManager() : this(new InputService())
    {
    }

    /// <summary>
    /// 毎フレーム呼び出し。低レベル <see cref="InputService"/> を更新し、アクティブデバイスを自動検出します。
    /// </summary>
    public void Update()
    {
        _service.Update();

        for (int p = 1; p <= 4; p++)
        {
            if (_service.HasPadInput(p))
            {
                _activeDevicePerPlayer[p] = InputDevice.Gamepad;
            }
            else if (IsPlayerKeyboardActive(p))
            {
                _activeDevicePerPlayer[p] = InputDevice.Keyboard;
            }
        }
    }

    /// <summary>
    /// 指定されたプレイヤーのアクティブデバイス (Keyboard または Gamepad) を取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <returns>アクティブな入力デバイス種別</returns>
    public InputDevice GetActiveDevice(int playerIndex = 1)
    {
        if (playerIndex < 1 || playerIndex > 4)
        {
            return InputDevice.Keyboard;
        }

        return _activeDevicePerPlayer[playerIndex];
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが今フレームで押された瞬間か判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <param name="action">判定するアクション</param>
    /// <returns>押された瞬間であれば true</returns>
    public bool IsPressed(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingPressed(playerIndex, binding));
    }

    /// <summary>
    /// グローバル共通アクションが今フレームで押された瞬間か判定します。
    /// </summary>
    /// <param name="action">判定するアクション</param>
    /// <returns>押された瞬間であれば true</returns>
    public bool IsPressed(InputAction action)
    {
        return IsPressed(0, action) || IsPressed(1, action);
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが押しっぱなし中か判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <param name="action">判定するアクション</param>
    /// <returns>押下中であれば true</returns>
    public bool IsHolding(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingHolding(playerIndex, binding));
    }

    /// <summary>
    /// グローバル共通アクションが押しっぱなし中か判定します。
    /// </summary>
    /// <param name="action">判定するアクション</param>
    /// <returns>押下中であれば true</returns>
    public bool IsHolding(InputAction action)
    {
        return IsHolding(0, action) || IsHolding(1, action);
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが今フレームで離された瞬間か判定します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <param name="action">判定するアクション</param>
    /// <returns>離された瞬間であれば true</returns>
    public bool IsReleased(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingReleased(playerIndex, binding));
    }

    /// <summary>
    /// プレイヤー (1..4) の水平移動軸 (-1.0f ～ 1.0f) をアクション経由で取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <returns>水平移動入力値 (-1.0f～1.0f)</returns>
    public float GetHorizontalAxis(int playerIndex)
    {
        float axis = 0.0f;
        if (IsHolding(playerIndex, InputAction.MoveRight))
        {
            axis += 1.0f;
        }
        if (IsHolding(playerIndex, InputAction.MoveLeft))
        {
            axis -= 1.0f;
        }

        return Math.Clamp(axis, -1.0f, 1.0f);
    }

    /// <summary>
    /// プレイヤー (1..4) の垂直移動軸 (-1.0f ～ 1.0f) をアクション経由で取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <returns>垂直移動入力値 (-1.0f～1.0f)</returns>
    public float GetVerticalAxis(int playerIndex)
    {
        float axis = 0.0f;
        if (IsHolding(playerIndex, InputAction.MoveDown))
        {
            axis += 1.0f;
        }
        if (IsHolding(playerIndex, InputAction.MoveUp))
        {
            axis -= 1.0f;
        }

        return Math.Clamp(axis, -1.0f, 1.0f);
    }

    /// <summary>
    /// プレイヤー人数の切り替えリクエスト (1-4) が押されたか取得します。押されていない場合は -1。
    /// </summary>
    /// <returns>リクエストされた人数 (1～4)、押されていない場合は -1</returns>
    public int GetRequestedPlayerCountSwitch()
    {
        if (IsPressed(InputAction.PlayerCount1))
        {
            return 1;
        }
        if (IsPressed(InputAction.PlayerCount2))
        {
            return 2;
        }
        if (IsPressed(InputAction.PlayerCount3))
        {
            return 3;
        }
        if (IsPressed(InputAction.PlayerCount4))
        {
            return 4;
        }

        return -1;
    }

    /// <summary>
    /// 後方互換用: GetRequestedMapSwitchIndex
    /// </summary>
    /// <returns>リクエストされたステージインデックス</returns>
    public int GetRequestedMapSwitchIndex() => GetRequestedPlayerCountSwitch();

    /// <summary>
    /// 新しい入力コンテキストをスタックの最上位にプッシュします。
    /// </summary>
    /// <param name="context">プッシュするコンテキスト</param>
    public void PushContext(InputContext context)
    {
        if (_contextStack.Count == 0 || _contextStack[^1] != context)
        {
            _contextStack.Add(context);
        }
    }

    /// <summary>
    /// 最上位の入力コンテキストをポップし、1つ前のコンテキストに戻します。
    /// </summary>
    public void PopContext()
    {
        if (_contextStack.Count > 1)
        {
            _contextStack.RemoveAt(_contextStack.Count - 1);
        }
    }

    /// <summary>
    /// コンテキストスタックをクリアし、指定されたコンテキストを単一でセットします。
    /// </summary>
    /// <param name="context">セットするコンテキスト</param>
    public void ClearAndSetContext(InputContext context)
    {
        _contextStack.Clear();
        _contextStack.Add(context);
    }

    private bool CheckActionState(int playerIndex, InputAction action, Func<InputBinding, bool> predicate)
    {
        InputContext context = CurrentContext;

        if (_actionMappings.TryGetValue((context, playerIndex), out Dictionary<InputAction, List<InputBinding>>? map) && map != null &&
            map.TryGetValue(action, out List<InputBinding>? bindings) && bindings != null)
        {
            foreach (InputBinding binding in bindings)
            {
                if (predicate(binding))
                {
                    return true;
                }
            }
        }

        if (playerIndex != 0 &&
            _actionMappings.TryGetValue((context, 0), out Dictionary<InputAction, List<InputBinding>>? globalMap) && globalMap != null &&
            globalMap.TryGetValue(action, out List<InputBinding>? globalBindings) && globalBindings != null)
        {
            foreach (InputBinding binding in globalBindings)
            {
                if (predicate(binding))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsBindingPressed(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyPressed(binding.KeyCode))
        {
            return true;
        }
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadPressed(playerIndex, binding.PadMask))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBindingHolding(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyDown(binding.KeyCode))
        {
            return true;
        }
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadDown(playerIndex, binding.PadMask))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsBindingReleased(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyReleased(binding.KeyCode))
        {
            return true;
        }
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadReleased(playerIndex, binding.PadMask))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPlayerKeyboardActive(int playerIndex)
    {
        if (playerIndex == 1)
        {
            return _service.IsKeyDown(InputService.KeyInputW) || _service.IsKeyDown(InputService.KeyInputA) || _service.IsKeyDown(InputService.KeyInputS) || _service.IsKeyDown(InputService.KeyInputD) || _service.IsKeyDown(InputService.KeyInputJ) || _service.IsKeyDown(InputService.KeyInputF) || _service.IsKeyDown(InputService.KeyInputSpace);
        }
        if (playerIndex == 2)
        {
            return _service.IsKeyDown(InputService.KeyInputUp) || _service.IsKeyDown(InputService.KeyInputDown) || _service.IsKeyDown(InputService.KeyInputLeft) || _service.IsKeyDown(InputService.KeyInputRight) || _service.IsKeyDown(InputService.KeyInputL);
        }

        return false;
    }

    private void AddBinding(InputContext context, int playerIndex, InputAction action, InputBinding binding)
    {
        (InputContext Context, int PlayerIndex) key = (context, playerIndex);
        if (!_actionMappings.TryGetValue(key, out Dictionary<InputAction, List<InputBinding>>? map) || map == null)
        {
            map = [];
            _actionMappings[key] = map;
        }
        if (!map.TryGetValue(action, out List<InputBinding>? list) || list == null)
        {
            list = [];
            map[action] = list;
        }
        list.Add(binding);
    }

    private void RegisterActionMappings()
    {
        AddBinding(InputContext.InGame, 0, InputAction.ToggleMenu, new InputBinding(keyCode: InputService.KeyInputEscape, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.DebugToggle, new InputBinding(keyCode: InputService.KeyInputF1, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount1, new InputBinding(keyCode: InputService.KeyInput1, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount2, new InputBinding(keyCode: InputService.KeyInput2, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount3, new InputBinding(keyCode: InputService.KeyInput3, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount4, new InputBinding(keyCode: InputService.KeyInput4, padMask: 0));

        AddBinding(InputContext.InGame, 1, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputA, padMask: InputService.PadInputLeft));
        AddBinding(InputContext.InGame, 1, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputD, padMask: InputService.PadInputRight));
        AddBinding(InputContext.InGame, 1, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputW, padMask: InputService.PadInputUp));
        AddBinding(InputContext.InGame, 1, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputS, padMask: InputService.PadInputDown));
        AddBinding(InputContext.InGame, 1, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputW, padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
        AddBinding(InputContext.InGame, 1, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputSpace, padMask: 0));
        AddBinding(InputContext.InGame, 1, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputJ, padMask: InputService.PadInputButton2 | InputService.PadInputButton3));
        AddBinding(InputContext.InGame, 1, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputF, padMask: 0));

        AddBinding(InputContext.InGame, 2, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputLeft, padMask: InputService.PadInputLeft));
        AddBinding(InputContext.InGame, 2, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputRight, padMask: InputService.PadInputRight));
        AddBinding(InputContext.InGame, 2, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputUp, padMask: InputService.PadInputUp));
        AddBinding(InputContext.InGame, 2, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputDown, padMask: InputService.PadInputDown));
        AddBinding(InputContext.InGame, 2, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputUp, padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
        AddBinding(InputContext.InGame, 2, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputL, padMask: InputService.PadInputButton2 | InputService.PadInputButton3));

        for (int p = 3; p <= 4; p++)
        {
            AddBinding(InputContext.InGame, p, InputAction.MoveLeft, new InputBinding(padMask: InputService.PadInputLeft));
            AddBinding(InputContext.InGame, p, InputAction.MoveRight, new InputBinding(padMask: InputService.PadInputRight));
            AddBinding(InputContext.InGame, p, InputAction.MoveUp, new InputBinding(padMask: InputService.PadInputUp));
            AddBinding(InputContext.InGame, p, InputAction.MoveDown, new InputBinding(padMask: InputService.PadInputDown));
            AddBinding(InputContext.InGame, p, InputAction.Jump, new InputBinding(padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
            AddBinding(InputContext.InGame, p, InputAction.Attack, new InputBinding(padMask: InputService.PadInputButton2 | InputService.PadInputButton3));
        }

        AddBinding(InputContext.Menu, 0, InputAction.Confirm, new InputBinding(keyCode: InputService.KeyInputSpace, padMask: InputService.PadInputButton1));
        AddBinding(InputContext.Menu, 0, InputAction.Confirm, new InputBinding(keyCode: InputService.KeyInputReturn, padMask: 0));
        AddBinding(InputContext.Menu, 0, InputAction.Cancel, new InputBinding(keyCode: InputService.KeyInputEscape, padMask: InputService.PadInputButton2));
        AddBinding(InputContext.Menu, 0, InputAction.ToggleMenu, new InputBinding(keyCode: InputService.KeyInputEscape, padMask: InputService.PadInputButton2));
        AddBinding(InputContext.Menu, 0, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputUp, padMask: InputService.PadInputUp));
        AddBinding(InputContext.Menu, 0, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputW, padMask: 0));
        AddBinding(InputContext.Menu, 0, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputDown, padMask: InputService.PadInputDown));
        AddBinding(InputContext.Menu, 0, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputS, padMask: 0));
        AddBinding(InputContext.Menu, 0, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputLeft, padMask: InputService.PadInputLeft));
        AddBinding(InputContext.Menu, 0, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputA, padMask: 0));
        AddBinding(InputContext.Menu, 0, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputRight, padMask: InputService.PadInputRight));
        AddBinding(InputContext.Menu, 0, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputD, padMask: 0));
    }
}
