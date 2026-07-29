using System;
using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Inputs;

public partial class InputManager
{
    private bool CheckActionState(int playerIndex, InputAction action, Func<InputBinding, bool> predicate)
    {
        InputContext context = CurrentContext;

        if (_actionMappings.TryGetValue((context, playerIndex), out var map) &&
            map.TryGetValue(action, out var bindings))
        {
            foreach (var binding in bindings)
            {
                if (predicate(binding)) return true;
            }
        }

        // Global fallback (PlayerIndex 0)
        if (playerIndex != 0 &&
            _actionMappings.TryGetValue((context, 0), out var globalMap) &&
            globalMap.TryGetValue(action, out var globalBindings))
        {
            foreach (var binding in globalBindings)
            {
                if (predicate(binding)) return true;
            }
        }

        return false;
    }

    private bool IsBindingPressed(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyPressed(binding.KeyCode)) return true;
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadPressed(playerIndex, binding.PadMask)) return true;
        }
        return false;
    }

    private bool IsBindingHolding(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyDown(binding.KeyCode)) return true;
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadDown(playerIndex, binding.PadMask)) return true;
        }
        return false;
    }

    private bool IsBindingReleased(int playerIndex, InputBinding binding)
    {
        if (binding.KeyCode != 0 && _service.IsKeyReleased(binding.KeyCode)) return true;
        if (binding.PadMask != 0 && playerIndex >= 1 && playerIndex <= 4)
        {
            if (_service.IsPadReleased(playerIndex, binding.PadMask)) return true;
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
        var key = (context, playerIndex);
        if (!_actionMappings.TryGetValue(key, out var map))
        {
            map = [];
            _actionMappings[key] = map;
        }
        if (!map.TryGetValue(action, out var list))
        {
            list = [];
            map[action] = list;
        }
        list.Add(binding);
    }

    private void RegisterActionMappings()
    {
        // ------------------ InGame Context ------------------

        // Global / Common System Controls
        AddBinding(InputContext.InGame, 0, InputAction.ToggleMenu, new InputBinding(keyCode: InputService.KeyInputEscape, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.DebugToggle, new InputBinding(keyCode: InputService.KeyInputF1, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount1, new InputBinding(keyCode: InputService.KeyInput1, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount2, new InputBinding(keyCode: InputService.KeyInput2, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount3, new InputBinding(keyCode: InputService.KeyInput3, padMask: 0));
        AddBinding(InputContext.InGame, 0, InputAction.PlayerCount4, new InputBinding(keyCode: InputService.KeyInput4, padMask: 0));

        // Player 1 (Keyboard WASD/J/Space + Gamepad 1)
        AddBinding(InputContext.InGame, 1, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputA, padMask: InputService.PadInputLeft));
        AddBinding(InputContext.InGame, 1, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputD, padMask: InputService.PadInputRight));
        AddBinding(InputContext.InGame, 1, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputW, padMask: InputService.PadInputUp));
        AddBinding(InputContext.InGame, 1, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputS, padMask: InputService.PadInputDown));
        AddBinding(InputContext.InGame, 1, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputW, padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
        AddBinding(InputContext.InGame, 1, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputSpace, padMask: 0));
        AddBinding(InputContext.InGame, 1, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputJ, padMask: InputService.PadInputButton2 | InputService.PadInputButton3));
        AddBinding(InputContext.InGame, 1, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputF, padMask: 0));

        // Player 2 (Keyboard Arrows/L + Gamepad 2)
        AddBinding(InputContext.InGame, 2, InputAction.MoveLeft, new InputBinding(keyCode: InputService.KeyInputLeft, padMask: InputService.PadInputLeft));
        AddBinding(InputContext.InGame, 2, InputAction.MoveRight, new InputBinding(keyCode: InputService.KeyInputRight, padMask: InputService.PadInputRight));
        AddBinding(InputContext.InGame, 2, InputAction.MoveUp, new InputBinding(keyCode: InputService.KeyInputUp, padMask: InputService.PadInputUp));
        AddBinding(InputContext.InGame, 2, InputAction.MoveDown, new InputBinding(keyCode: InputService.KeyInputDown, padMask: InputService.PadInputDown));
        AddBinding(InputContext.InGame, 2, InputAction.Jump, new InputBinding(keyCode: InputService.KeyInputUp, padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
        AddBinding(InputContext.InGame, 2, InputAction.Attack, new InputBinding(keyCode: InputService.KeyInputL, padMask: InputService.PadInputButton2 | InputService.PadInputButton3));

        // Player 3 & 4 (Gamepad Only)
        for (int p = 3; p <= 4; p++)
        {
            AddBinding(InputContext.InGame, p, InputAction.MoveLeft, new InputBinding(padMask: InputService.PadInputLeft));
            AddBinding(InputContext.InGame, p, InputAction.MoveRight, new InputBinding(padMask: InputService.PadInputRight));
            AddBinding(InputContext.InGame, p, InputAction.MoveUp, new InputBinding(padMask: InputService.PadInputUp));
            AddBinding(InputContext.InGame, p, InputAction.MoveDown, new InputBinding(padMask: InputService.PadInputDown));
            AddBinding(InputContext.InGame, p, InputAction.Jump, new InputBinding(padMask: InputService.PadInputButton1 | InputService.PadInputButton4 | InputService.PadInputUp));
            AddBinding(InputContext.InGame, p, InputAction.Attack, new InputBinding(padMask: InputService.PadInputButton2 | InputService.PadInputButton3));
        }

        // ------------------ Menu Context ------------------

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
