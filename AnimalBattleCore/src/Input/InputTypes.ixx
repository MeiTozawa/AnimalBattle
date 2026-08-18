module;

#include <cstdint>

export module InputTypes;

export import RenderTypes;

namespace ab {
    /// @brief ゲーム内で使用される抽象入力アクション
    export enum class InputAction {
        Up,          ///< 上移動 / 上選択
        Down,        ///< 下移動 / 下選択
        Left,        ///< 左移動 / 左選択
        Right,       ///< 右移動 / 右選択
        Jump,        ///< ジャンプ
        Attack,      ///< 通常攻撃 / 射撃
        Skill,       ///< スキル / 特殊アクション
        Confirm,     ///< 決定 / アクション
        Cancel,      ///< キャンセル / 戻る
        ToggleMenu,  ///< メニュー開閉 / ポーズ
        MouseClick,  ///< マウスクリック
        DrawCard,    ///< カードを引く / 特殊操作
        Restart,     ///< リスタート
    };

    /// @brief 入力コンテキスト（入力状態の分離スタック用）
    export enum class InputContext {
        InGame,         ///< ゲームプレイ中
        Menu,           ///< メニュー / UI 操作中
        Cutscene,       ///< イベント / カットシーン中
        Pause,          ///< ポーズ中
        VolumeControl,  ///< 音量調整等のサブメニュー
    };

    /// @brief 最後に検出されたアクティブな入力デバイス種別
    export enum class InputDevice {
        Keyboard,  ///< キーボード
        Mouse,     ///< マウス
        Gamepad,   ///< ゲームパッド / コントローラー
    };

    /// @brief キーボード、マウス、ゲームパッドを統一的に扱うボタン識別子
    export enum class InputButton : int {
        None = 0,

        // --- キーボード (DxLib DirectInput キーコード互換: 1 ~ 255) ---
        KeyEscape     = 0x01,
        Key1          = 0x02,
        Key2          = 0x03,
        Key3          = 0x04,
        Key4          = 0x05,
        Key5          = 0x06,
        Key6          = 0x07,
        Key7          = 0x08,
        Key8          = 0x09,
        Key9          = 0x0A,
        Key0          = 0x0B,
        KeyMinus      = 0x0C,
        KeyEquals     = 0x0D,
        KeyBack       = 0x0E,
        KeyTab        = 0x0F,
        KeyQ          = 0x10,
        KeyW          = 0x11,
        KeyE          = 0x12,
        KeyR          = 0x13,
        KeyT          = 0x14,
        KeyY          = 0x15,
        KeyU          = 0x16,
        KeyI          = 0x17,
        KeyO          = 0x18,
        KeyP          = 0x19,
        KeyReturn     = 0x1C,
        KeyLControl   = 0x1D,
        KeyA          = 0x1E,
        KeyS          = 0x1F,
        KeyD          = 0x20,
        KeyF          = 0x21,
        KeyG          = 0x22,
        KeyH          = 0x23,
        KeyJ          = 0x24,
        KeyK          = 0x25,
        KeyL          = 0x26,
        KeyLShift     = 0x2A,
        KeyZ          = 0x2C,
        KeyX          = 0x2D,
        KeyC          = 0x2E,
        KeyV          = 0x2F,
        KeyB          = 0x30,
        KeyN          = 0x31,
        KeyM          = 0x32,
        KeySpace      = 0x39,
        KeyUp         = 0xC8,
        KeyLeft       = 0xCB,
        KeyRight      = 0xCD,
        KeyDown       = 0xD0,
        KeyRShift     = 0x36,
        KeyRControl   = 0x9D,

        // --- マウスボタン (300 ~ 399) ---
        MouseLeft     = 300,
        MouseRight    = 301,
        MouseMiddle   = 302,

        // --- ゲームパッド (400 ~ 499, DxLib PAD_INPUT_* に対応) ---
        PadDown       = 400,  ///< PAD_INPUT_DOWN (0x0001)
        PadLeft       = 401,  ///< PAD_INPUT_LEFT (0x0002)
        PadRight      = 402,  ///< PAD_INPUT_RIGHT (0x0004)
        PadUp         = 403,  ///< PAD_INPUT_UP (0x0008)
        Pad1          = 404,  ///< PAD_INPUT_1 / Button A (0x0010)
        Pad2          = 405,  ///< PAD_INPUT_2 / Button B (0x0020)
        Pad3          = 406,  ///< PAD_INPUT_3 / Button X (0x0040)
        Pad4          = 407,  ///< PAD_INPUT_4 / Button Y (0x0080)
        PadL1         = 408,  ///< PAD_INPUT_5 / L1 (0x0100)
        PadR1         = 409,  ///< PAD_INPUT_6 / R1 (0x0200)
        PadL2         = 410,  ///< PAD_INPUT_7 / L2 (0x0400)
        PadR2         = 411,  ///< PAD_INPUT_8 / R2 (0x0800)
        PadStart      = 412,  ///< PAD_INPUT_9 / Start (0x1000)
        PadSelect     = 413,  ///< PAD_INPUT_10 / Select (0x2000)
    };

    /// @brief ボタンがキーボードキーかを判定する
    export constexpr bool IsKeyboardButton(InputButton button) noexcept {
        return static_cast<int>(button) >= 1 && static_cast<int>(button) <= 255;
    }

    /// @brief ボタンがマウスボタンかを判定する
    export constexpr bool IsMouseButton(InputButton button) noexcept {
        return static_cast<int>(button) >= 300 && static_cast<int>(button) < 400;
    }

    /// @brief ボタンがゲームパッドボタンかを判定する
    export constexpr bool IsPadButton(InputButton button) noexcept {
        return static_cast<int>(button) >= 400 && static_cast<int>(button) < 500;
    }
} // namespace ab
