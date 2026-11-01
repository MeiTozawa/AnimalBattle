namespace AnimalBattle.Core;

/// <summary>
///     Native C++ InputService と連携し、コンテキストスタック・抽象アクション入力判定を提供する高レベルサービスクラス。
/// </summary>
public static unsafe partial class InputService {
    private static bool _isCursorVisible = true;

    /// <summary>
    ///     OS マウスカーソルの表示状態を切り替えます。多重呼び出しに対しても安全に動作します。
    /// </summary>
    /// <param name="visible">表示する場合は true、非表示にする場合は false。</param>
    public static void SetCursorVisible(bool visible)
    {
        if (_isCursorVisible == visible) return;
        _isCursorVisible = visible;
        NativeBindings.Engine_SetMouseDispFlag(visible);
    }

    /// <summary>
    ///     指定されたアクションがこのフレームで押下開始されたかを判定します。
    /// </summary>
    /// <param name="action">判定対象のアクション</param>
    /// <returns>押された瞬間であれば true</returns>
    public static bool IsPressed(InputAction action)
    {
        return NativeBindings.Input_IsPressed((int)action);
    }

    /// <summary>
    ///     指定された物理ボタンがこのフレームで押下開始されたかを判定します。
    /// </summary>
    public static bool IsButtonPressed(InputButton button)
    {
        return NativeBindings.Input_IsButtonPressed((int)button);
    }

    /// <summary>
    ///     指定されたアクションが押下継続中であるかを判定します。
    /// </summary>
    /// <param name="action">判定対象のアクション</param>
    /// <returns>押されている状態であれば true</returns>
    public static bool IsHolding(InputAction action)
    {
        return NativeBindings.Input_IsHolding((int)action);
    }

    /// <summary>
    ///     コンテキストスタックをクリアし、指定したコンテキストを唯一のものとして設定します。
    /// </summary>
    /// <param name="context">設定するコンテキスト</param>
    public static void ClearAndSetContext(InputContext context)
    {
        NativeBindings.Input_ClearAndSetContext((int)context);
    }

    /// <summary>
    ///     マウスカーソルの現在座標を取得します。
    /// </summary>
    /// <returns>画面上の現在座標</returns>
    public static Vec2i GetMousePosition()
    {
        var pos = Vec2i.Zero;
        NativeBindings.Input_GetMousePosition(&pos);
        return pos;
    }

    /// <summary>
    ///     指定されたコンテキストとアクションに対応するボタン一覧を設定します。
    /// </summary>
    /// <param name="context">入力コンテキスト</param>
    /// <param name="action">入力アクション</param>
    /// <param name="buttons">割り当てるボタン一覧</param>
    public static void BindAction(InputContext context, InputAction action, params InputButton[] buttons)
    {
        if (buttons == null || buttons.Length == 0) return;

        fixed (InputButton* ptr = buttons)
        {
            NativeBindings.Input_BindAction((int)context, (int)action, (int*)ptr, buttons.Length);
        }
    }

    /// <summary>
    ///     C#層からゲームの標準アクションマッピングを一括登録します。
    /// </summary>
    public static void RegisterDefaultActions()
    {
        // InGame
        BindAction(InputContext.InGame, InputAction.Confirm, InputButton.KeySpace, InputButton.KeyReturn,
            InputButton.Pad1);
        BindAction(InputContext.InGame, InputAction.Cancel, InputButton.KeyEscape, InputButton.Pad2);
        BindAction(InputContext.InGame, InputAction.Up, InputButton.KeyUp, InputButton.KeyW, InputButton.PadUp);
        BindAction(InputContext.InGame, InputAction.Down, InputButton.KeyDown, InputButton.KeyS, InputButton.PadDown);
        BindAction(InputContext.InGame, InputAction.Left, InputButton.KeyLeft, InputButton.KeyA, InputButton.PadLeft);
        BindAction(InputContext.InGame, InputAction.Right, InputButton.KeyRight, InputButton.KeyD,
            InputButton.PadRight);
        BindAction(InputContext.InGame, InputAction.Jump, InputButton.KeySpace, InputButton.KeyZ, InputButton.Pad1);
        BindAction(InputContext.InGame, InputAction.Attack, InputButton.MouseLeft, InputButton.Pad2);
        BindAction(InputContext.InGame, InputAction.Skill, InputButton.KeyK, InputButton.KeyC, InputButton.Pad3,
            InputButton.MouseRight, InputButton.KeyLShift, InputButton.KeyRShift);
        BindAction(InputContext.InGame, InputAction.Restart, InputButton.KeyR);
        BindAction(InputContext.InGame, InputAction.SelectBossSnake, InputButton.Key1);
        BindAction(InputContext.InGame, InputAction.SelectBossLion, InputButton.Key2);
        BindAction(InputContext.InGame, InputAction.SelectCharacter1, InputButton.Key3);
        BindAction(InputContext.InGame, InputAction.SelectCharacter2, InputButton.Key4);
        BindAction(InputContext.InGame, InputAction.Interact, InputButton.KeyF, InputButton.Pad1);
        BindAction(InputContext.InGame, InputAction.ToggleMenu, InputButton.KeyEscape, InputButton.PadStart);
        BindAction(InputContext.InGame, InputAction.MouseClick, InputButton.MouseLeft);

        // Menu
        BindAction(InputContext.Menu, InputAction.Confirm, InputButton.KeySpace, InputButton.KeyReturn,
            InputButton.Pad1);
        BindAction(InputContext.Menu, InputAction.Cancel, InputButton.KeyEscape, InputButton.Pad2);
        BindAction(InputContext.Menu, InputAction.ToggleMenu, InputButton.KeyEscape, InputButton.PadStart,
            InputButton.Pad2);
        BindAction(InputContext.Menu, InputAction.Up, InputButton.KeyUp, InputButton.KeyW, InputButton.PadUp);
        BindAction(InputContext.Menu, InputAction.Down, InputButton.KeyDown, InputButton.KeyS, InputButton.PadDown);
        BindAction(InputContext.Menu, InputAction.Left, InputButton.KeyLeft, InputButton.KeyA, InputButton.PadLeft);
        BindAction(InputContext.Menu, InputAction.Right, InputButton.KeyRight, InputButton.KeyD, InputButton.PadRight);
        BindAction(InputContext.Menu, InputAction.MouseClick, InputButton.MouseLeft);
    }
}
