using System.Drawing;

namespace AnimalBattle.Core;

/// <summary>
///     Native C++ InputService と連携し、コンテキストスタック・抽象アクション入力判定を提供する高レベルサービスクラス。
/// </summary>
public static unsafe partial class InputService {
    private static bool _isCursorVisible = true;

    /// <summary>
    ///     現在の OS マウスカーソル可視状態を取得します。
    /// </summary>
    public static bool IsCursorVisible => _isCursorVisible;

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
    ///     入力状態を明示的に更新します。通常は Engine_ProcessMessages 内で自動実行されます。
    /// </summary>
    public static void Update()
    {
        NativeBindings.Input_Update();
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
    ///     指定されたアクションが押下継続中であるかを判定します。
    /// </summary>
    /// <param name="action">判定対象のアクション</param>
    /// <returns>押されている状態であれば true</returns>
    public static bool IsHolding(InputAction action)
    {
        return NativeBindings.Input_IsHolding((int)action);
    }

    /// <summary>
    ///     指定されたアクションがこのフレームで解放されたかを判定します。
    /// </summary>
    /// <param name="action">判定対象のアクション</param>
    /// <returns>離された瞬間であれば true</returns>
    public static bool IsReleased(InputAction action)
    {
        return NativeBindings.Input_IsReleased((int)action);
    }

    /// <summary>
    ///     新しい入力コンテキストをスタックにプッシュします。
    /// </summary>
    /// <param name="context">プッシュするコンテキスト</param>
    public static void PushContext(InputContext context)
    {
        NativeBindings.Input_PushContext((int)context);
    }

    /// <summary>
    ///     現在の入力コンテキストをポップし、1つ前のコンテキストに戻します。
    /// </summary>
    public static void PopContext()
    {
        NativeBindings.Input_PopContext();
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
    ///     現在アクティブな入力コンテキストを取得します。
    /// </summary>
    /// <returns>現在アクティブなコンテキスト</returns>
    public static InputContext GetCurrentContext()
    {
        return (InputContext)NativeBindings.Input_GetCurrentContext();
    }

    /// <summary>
    ///     最後に入力を検知したアクティブなデバイスを取得します。
    /// </summary>
    /// <returns>アクティブデバイス種別</returns>
    public static InputDevice GetActiveDevice()
    {
        return (InputDevice)NativeBindings.Input_GetActiveDevice();
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
    ///     前フレームからのマウスカーソルの移動量を取得します。
    /// </summary>
    /// <returns>移動デルタベクトル</returns>
    public static Vec2i GetMouseDelta()
    {
        var delta = Vec2i.Zero;
        NativeBindings.Input_GetMouseDelta(&delta);
        return delta;
    }

    /// <summary>
    ///     マウスホイールの回転量を取得します。
    /// </summary>
    /// <returns>ホイール回転量</returns>
    public static int GetMouseWheelDelta()
    {
        return NativeBindings.Input_GetMouseWheelDelta();
    }

    /// <summary>
    ///     マウスカーソルが指定した矩形領域内にあるかを判定します。
    /// </summary>
    /// <param name="p1">矩形左上座標</param>
    /// <param name="p2">矩形右下座標</param>
    /// <returns>領域内にあれば true</returns>
    public static bool IsMouseOver(Vec2i p1, Vec2i p2)
    {
        return NativeBindings.Input_IsMouseOver(p1, p2);
    }

    /// <summary>
    ///     マウスカーソルが指定した矩形領域内にあるかを判定します。
    /// </summary>
    /// <param name="bounds">判定対象の矩形領域</param>
    /// <returns>領域内にあれば true</returns>
    public static bool IsMouseOver(Rectangle bounds)
    {
        return NativeBindings.Input_IsMouseOver(new Vec2i(bounds.Left, bounds.Top),
            new Vec2i(bounds.Right, bounds.Bottom));
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
    ///     指定されたコンテキストとアクションにボタンを追加します。
    /// </summary>
    /// <param name="context">入力コンテキスト</param>
    /// <param name="action">入力アクション</param>
    /// <param name="button">追加するボタン</param>
    public static void AddActionButton(InputContext context, InputAction action, InputButton button)
    {
        NativeBindings.Input_AddActionButton((int)context, (int)action, (int)button);
    }

    /// <summary>
    ///     指定されたコンテキストのアクションマッピングをすべて消去します。
    /// </summary>
    /// <param name="context">入力コンテキスト</param>
    public static void ClearActions(InputContext context)
    {
        NativeBindings.Input_ClearActions((int)context);
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
        BindAction(InputContext.InGame, InputAction.SelectBoss1, InputButton.Key1);
        BindAction(InputContext.InGame, InputAction.SelectBoss2, InputButton.Key2);
        BindAction(InputContext.InGame, InputAction.SelectCharacter1, InputButton.Key3);
        BindAction(InputContext.InGame, InputAction.SelectCharacter2, InputButton.Key4);
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

        // VolumeControl
        BindAction(InputContext.VolumeControl, InputAction.Left, InputButton.KeyLeft, InputButton.KeyA,
            InputButton.PadLeft);
        BindAction(InputContext.VolumeControl, InputAction.Right, InputButton.KeyRight, InputButton.KeyD,
            InputButton.PadRight);
        BindAction(InputContext.VolumeControl, InputAction.Confirm, InputButton.KeySpace, InputButton.KeyReturn,
            InputButton.Pad1);
        BindAction(InputContext.VolumeControl, InputAction.Cancel, InputButton.KeyEscape, InputButton.Pad2);
        BindAction(InputContext.VolumeControl, InputAction.ToggleMenu, InputButton.KeyEscape, InputButton.PadStart,
            InputButton.Pad2);
        BindAction(InputContext.VolumeControl, InputAction.MouseClick, InputButton.MouseLeft);

        // Cutscene
        BindAction(InputContext.Cutscene, InputAction.Confirm, InputButton.KeySpace, InputButton.KeyReturn,
            InputButton.Pad1, InputButton.MouseLeft);
        BindAction(InputContext.Cutscene, InputAction.Cancel, InputButton.KeyEscape, InputButton.Pad2);
        BindAction(InputContext.Cutscene, InputAction.ToggleMenu, InputButton.KeyEscape, InputButton.PadStart);

        // Pause
        BindAction(InputContext.Pause, InputAction.Confirm, InputButton.KeySpace, InputButton.KeyReturn,
            InputButton.Pad1);
        BindAction(InputContext.Pause, InputAction.Cancel, InputButton.KeyEscape, InputButton.Pad2);
        BindAction(InputContext.Pause, InputAction.ToggleMenu, InputButton.KeyEscape, InputButton.PadStart);
        BindAction(InputContext.Pause, InputAction.Up, InputButton.KeyUp, InputButton.KeyW, InputButton.PadUp);
        BindAction(InputContext.Pause, InputAction.Down, InputButton.KeyDown, InputButton.KeyS, InputButton.PadDown);
        BindAction(InputContext.Pause, InputAction.Left, InputButton.KeyLeft, InputButton.KeyA, InputButton.PadLeft);
        BindAction(InputContext.Pause, InputAction.Right, InputButton.KeyRight, InputButton.KeyD, InputButton.PadRight);
        BindAction(InputContext.Pause, InputAction.MouseClick, InputButton.MouseLeft);
    }
}