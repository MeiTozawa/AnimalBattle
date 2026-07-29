using System;

namespace AnimalBattle.Inputs;

public partial class InputManager
{
    /// <summary>
    /// 指定されたプレイヤーのアクティブデバイス (Keyboard または Gamepad) を取得します。
    /// </summary>
    public InputDevice GetActiveDevice(int playerIndex = 1)
    {
        if (playerIndex < 1 || playerIndex > 4) return InputDevice.Keyboard;
        return _activeDevicePerPlayer[playerIndex];
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが今フレームで押された瞬間か判定します。
    /// </summary>
    public bool IsPressed(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingPressed(playerIndex, binding));
    }

    /// <summary>
    /// グローバル共通アクションが今フレームで押された瞬間か判定します。
    /// </summary>
    public bool IsPressed(InputAction action)
    {
        return IsPressed(0, action) || IsPressed(1, action);
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが押しっぱなし中か判定します。
    /// </summary>
    public bool IsHolding(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingHolding(playerIndex, binding));
    }

    /// <summary>
    /// グローバル共通アクションが押しっぱなし中か判定します。
    /// </summary>
    public bool IsHolding(InputAction action)
    {
        return IsHolding(0, action) || IsHolding(1, action);
    }

    /// <summary>
    /// 指定されたプレイヤー (1..4) のアクションが今フレームで離された瞬間か判定します。
    /// </summary>
    public bool IsReleased(int playerIndex, InputAction action)
    {
        return CheckActionState(playerIndex, action, binding => IsBindingReleased(playerIndex, binding));
    }

    /// <summary>
    /// プレイヤー (1..4) の水平移動軸 (-1.0f ～ 1.0f) をアクション経由で取得します。
    /// </summary>
    public float GetHorizontalAxis(int playerIndex)
    {
        float axis = 0.0f;
        if (IsHolding(playerIndex, InputAction.MoveRight)) axis += 1.0f;
        if (IsHolding(playerIndex, InputAction.MoveLeft)) axis -= 1.0f;
        return Math.Clamp(axis, -1.0f, 1.0f);
    }

    /// <summary>
    /// プレイヤー (1..4) の垂直移動軸 (-1.0f ～ 1.0f) をアクション経由で取得します。
    /// </summary>
    public float GetVerticalAxis(int playerIndex)
    {
        float axis = 0.0f;
        if (IsHolding(playerIndex, InputAction.MoveDown)) axis += 1.0f;
        if (IsHolding(playerIndex, InputAction.MoveUp)) axis -= 1.0f;
        return Math.Clamp(axis, -1.0f, 1.0f);
    }

    /// <summary>
    /// プレイヤー人数の切り替えリクエスト (1-4) が押されたか取得します。押されていない場合は -1。
    /// </summary>
    public int GetRequestedPlayerCountSwitch()
    {
        if (IsPressed(InputAction.PlayerCount1)) return 1;
        if (IsPressed(InputAction.PlayerCount2)) return 2;
        if (IsPressed(InputAction.PlayerCount3)) return 3;
        if (IsPressed(InputAction.PlayerCount4)) return 4;
        return -1;
    }

    /// <summary>
    /// 後方互換用: GetRequestedMapSwitchIndex
    /// </summary>
    public int GetRequestedMapSwitchIndex() => GetRequestedPlayerCountSwitch();
}
