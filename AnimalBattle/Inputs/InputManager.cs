using System;
using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Inputs;

/// <summary>
/// <see cref="InputService"/> 低レベルサービスを利用し、アクションマッピング、コンテキストスタック管理、およびマルチプレイヤー入力制御を行う高レベルマネージャークラス。
/// </summary>
public partial class InputManager
{
    private readonly InputService _service;
    private readonly InputDevice[] _activeDevicePerPlayer = new InputDevice[5];
    private readonly List<InputContext> _contextStack = [];

    // アクションバインディング構造体（キーボードまたはパッドのバインド）
    private readonly struct InputBinding(int keyCode = 0, int padMask = 0)
    {
        public int KeyCode { get; } = keyCode;
        public int PadMask { get; } = padMask;
    }

    // [Context, PlayerIndex (0=Global, 1..4=P1..P4)] -> [Action -> List<InputBinding>]
    private readonly Dictionary<(InputContext Context, int PlayerIndex), Dictionary<InputAction, List<InputBinding>>> _actionMappings = [];

    /// <summary>
    /// <see cref="InputService"/> インスタンスを指定して <see cref="InputManager"/> の新しいインスタンスを生成します。
    /// </summary>
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
    /// 内部で保持している低レベル <see cref="InputService"/> インスタンスを取得します。
    /// </summary>
    public InputService Service => _service;

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
}
