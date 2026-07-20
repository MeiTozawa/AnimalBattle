using System;
using System.Collections.Generic;
using AnimalBattle.Bindings;

namespace AnimalBattle;

public class InputManager
{
    public const int KEY_INPUT_ESCAPE = 0x01;
    public const int KEY_INPUT_W = 0x11;
    public const int KEY_INPUT_A = 0x1E;
    public const int KEY_INPUT_S = 0x1F;
    public const int KEY_INPUT_D = 0x20;
    public const int KEY_INPUT_F = 0x21;
    public const int KEY_INPUT_G = 0x22;
    public const int KEY_INPUT_R = 0x13;
    public const int KEY_INPUT_SPACE = 0x39;
    public const int KEY_INPUT_UP = 0xC8;
    public const int KEY_INPUT_DOWN = 0xD0;
    public const int KEY_INPUT_LEFT = 0xCB;
    public const int KEY_INPUT_RIGHT = 0xCD;

    public const int KEY_INPUT_1 = 0x02;
    public const int KEY_INPUT_2 = 0x03;
    public const int KEY_INPUT_3 = 0x04;
    public const int KEY_INPUT_4 = 0x05;

    private readonly int[] _trackedKeys =
    [
        KEY_INPUT_ESCAPE,
        KEY_INPUT_W, KEY_INPUT_A, KEY_INPUT_S, KEY_INPUT_D,
        KEY_INPUT_F, KEY_INPUT_G, KEY_INPUT_R, KEY_INPUT_SPACE,
        KEY_INPUT_UP, KEY_INPUT_DOWN, KEY_INPUT_LEFT, KEY_INPUT_RIGHT,
        KEY_INPUT_1, KEY_INPUT_2, KEY_INPUT_3, KEY_INPUT_4
    ];

    private readonly Dictionary<int, bool> _currentStates = [];
    private readonly Dictionary<int, bool> _previousStates = [];

    public InputManager()
    {
        foreach (int key in _trackedKeys)
        {
            _currentStates[key] = false;
            _previousStates[key] = false;
        }
    }

    public void Update()
    {
        foreach (int key in _trackedKeys)
        {
            _previousStates[key] = _currentStates[key];
            _currentStates[key] = NativeEngine.CheckHitKey(key) != 0;
        }
    }

    public bool IsKeyDown(int key)
    {
        return _currentStates.TryGetValue(key, out bool down) && down;
    }

    public bool IsKeyPressed(int key)
    {
        bool current = _currentStates.TryGetValue(key, out bool cur) && cur;
        bool previous = _previousStates.TryGetValue(key, out bool prev) && prev;
        return current && !previous;
    }

    public bool IsKeyReleased(int key)
    {
        bool current = _currentStates.TryGetValue(key, out bool cur) && cur;
        bool previous = _previousStates.TryGetValue(key, out bool prev) && prev;
        return !current && previous;
    }

    // --- Action Helpers ---

    public float GetHorizontalAxis(int playerIndex = 1)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KEY_INPUT_A)) axis -= 1.0f;
            if (IsKeyDown(KEY_INPUT_D)) axis += 1.0f;
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KEY_INPUT_LEFT)) axis -= 1.0f;
            if (IsKeyDown(KEY_INPUT_RIGHT)) axis += 1.0f;
        }
        return axis;
    }

    public float GetVerticalAxis(int playerIndex = 1)
    {
        float axis = 0.0f;
        if (playerIndex == 1)
        {
            if (IsKeyDown(KEY_INPUT_W)) axis -= 1.0f;
            if (IsKeyDown(KEY_INPUT_S)) axis += 1.0f;
        }
        else if (playerIndex == 2)
        {
            if (IsKeyDown(KEY_INPUT_UP)) axis -= 1.0f;
            if (IsKeyDown(KEY_INPUT_DOWN)) axis += 1.0f;
        }
        return axis;
    }

    public bool IsJumpPressed(int playerIndex = 1)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KEY_INPUT_SPACE) || IsKeyPressed(KEY_INPUT_W);
        }
        else
        {
            return IsKeyPressed(KEY_INPUT_UP);
        }
    }

    public bool IsDownPressed(int playerIndex = 1)
    {
        if (playerIndex == 1)
        {
            return IsKeyPressed(KEY_INPUT_S);
        }
        else
        {
            return IsKeyPressed(KEY_INPUT_DOWN);
        }
    }

    public bool IsResetPressed()
    {
        return IsKeyPressed(KEY_INPUT_R);
    }

    public bool IsKnockbackTestPressed(int playerIndex = 1)
    {
        return playerIndex == 1 ? IsKeyPressed(KEY_INPUT_F) : IsKeyPressed(KEY_INPUT_G);
    }

    public int GetRequestedMapSwitchIndex()
    {
        if (IsKeyPressed(KEY_INPUT_1)) return 0;
        if (IsKeyPressed(KEY_INPUT_2)) return 1;
        if (IsKeyPressed(KEY_INPUT_3)) return 2;
        if (IsKeyPressed(KEY_INPUT_4)) return 3;
        return -1;
    }
}
