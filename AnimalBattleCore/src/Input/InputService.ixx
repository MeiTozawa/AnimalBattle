module;

#include <array>
#include <cstdint>
#include <unordered_map>
#include <utility>
#include <vector>

export module InputService;

export import InputTypes;
import InputConcept;

namespace ab {
    /// @brief バックエンドを合成してコンテキストスタック、アクションマッピング、エッジトリガー判定を提供する静的多態 InputService テンプレートクラス
    /// @tparam Backend 入力バックエンド型 (InputBackendConcept を満たすこと)
    export template <InputBackendConcept Backend>
    class InputService {
    private:
        Backend backend_{};

        std::array<char, 256> current_keys_{};
        std::array<char, 256> previous_keys_{};

        int current_mouse_buttons_ = 0;
        int previous_mouse_buttons_ = 0;
        Point<int> current_mouse_pos_{0, 0};
        Point<int> previous_mouse_pos_{0, 0};
        int mouse_wheel_delta_ = 0;

        int current_pad_state_ = 0;
        int previous_pad_state_ = 0;

        InputDevice active_device_ = InputDevice::Keyboard;

        std::vector<InputContext> context_stack_{InputContext::InGame};

        std::unordered_map<InputContext, std::unordered_map<InputAction, std::vector<InputButton>>> action_mappings_{};

    public:
        InputService() = default;

        explicit InputService(Backend backend) : backend_(std::move(backend)) {}

        Backend& GetBackend() noexcept { return backend_; }
        const Backend& GetBackend() const noexcept { return backend_; }

        /// @brief 毎フレーム呼び出し、入力デバイス状態のサンプリング・エッジトリガー計算・アクティブデバイス判定を行う
        void Update() {
            previous_keys_ = current_keys_;
            previous_mouse_buttons_ = current_mouse_buttons_;
            previous_mouse_pos_ = current_mouse_pos_;
            previous_pad_state_ = current_pad_state_;

            backend_.GetHitKeyStateAll(current_keys_.data());
            current_mouse_buttons_ = backend_.GetMouseInput();
            backend_.GetMousePoint(&current_mouse_pos_.x, &current_mouse_pos_.y);
            mouse_wheel_delta_ = backend_.GetMouseWheelRotVol();
            current_pad_state_ = backend_.GetJoypadInputState(1);

            if (current_pad_state_ != 0) {
                active_device_ = InputDevice::Gamepad;
                return;
            }

            if (current_mouse_buttons_ != 0 ||
                current_mouse_pos_.x != previous_mouse_pos_.x ||
                current_mouse_pos_.y != previous_mouse_pos_.y ||
                mouse_wheel_delta_ != 0) {
                active_device_ = InputDevice::Mouse;
                return;
            }

            for (int i = 0; i < 256; ++i) {
                if (current_keys_[i] != 0) {
                    active_device_ = InputDevice::Keyboard;
                    return;
                }
            }
        }

        /// @brief 指定したアクションがこのフレームで押されたか（立ち上がりエッジ）を判定する
        bool IsPressed(InputAction action) const {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonPressed(button);
            });
        }

        /// @brief 指定したアクションが現在押され続けているか（ホールド中）を判定する
        bool IsHolding(InputAction action) const {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonHolding(button);
            });
        }

        /// @brief 指定したアクションがこのフレームで離されたか（立ち下がりエッジ）を判定する
        bool IsReleased(InputAction action) const {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonReleased(button);
            });
        }

        /// @brief ボタンが押され続けているかを判定する
        bool IsButtonHolding(InputButton button) const {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 && current_keys_[key_code] != 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (current_mouse_buttons_ & mask) != 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (current_pad_state_ & mask) != 0;
            }
            return false;
        }

        /// @brief ボタンがこのフレームで押された瞬間かを判定する
        bool IsButtonPressed(InputButton button) const {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 &&
                       current_keys_[key_code] != 0 && previous_keys_[key_code] == 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (current_mouse_buttons_ & mask) != 0 && (previous_mouse_buttons_ & mask) == 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (current_pad_state_ & mask) != 0 && (previous_pad_state_ & mask) == 0;
            }
            return false;
        }

        /// @brief ボタンがこのフレームで離された瞬間かを判定する
        bool IsButtonReleased(InputButton button) const {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 &&
                       current_keys_[key_code] == 0 && previous_keys_[key_code] != 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (current_mouse_buttons_ & mask) == 0 && (previous_mouse_buttons_ & mask) != 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (current_pad_state_ & mask) == 0 && (previous_pad_state_ & mask) != 0;
            }
            return false;
        }

        /// @brief 入力コンテキストをスタックにプッシュする（同一コンテキストの重複プッシュは無視）
        void PushContext(InputContext context) {
            if (context_stack_.empty() || context_stack_.back() != context) {
                context_stack_.push_back(context);
            }
        }

        /// @brief 現在の入力コンテキストをポップし、1つ前のコンテキストに戻す（スタックが空になることは防止）
        void PopContext() {
            if (context_stack_.size() > 1) {
                context_stack_.pop_back();
            }
        }

        /// @brief コンテキストスタックをクリアし、指定したコンテキストを唯一のものとして設定する
        void ClearAndSetContext(InputContext context) {
            context_stack_.clear();
            context_stack_.push_back(context);
        }

        /// @brief 現在アクティブな入力コンテキストを取得する
        InputContext GetCurrentContext() const noexcept {
            return context_stack_.empty() ? InputContext::InGame : context_stack_.back();
        }

        /// @brief コンテキストのアクションに対するボタン割り当てを設定する
        void BindAction(InputContext context, InputAction action, std::vector<InputButton> buttons) {
            action_mappings_[context][action] = std::move(buttons);
        }

        /// @brief コンテキストのアクションにボタンを追加する
        void AddActionButton(InputContext context, InputAction action, InputButton button) {
            action_mappings_[context][action].push_back(button);
        }

        /// @brief コンテキストの全マッピングをクリアする
        void ClearActions(InputContext context) {
            action_mappings_.erase(context);
        }

        // --- マウスおよびアクティブデバイス問い合わせ API ---

        /// @brief マウスカーソルの現在画面座標を取得する
        Point<int> GetMousePosition() const noexcept {
            return current_mouse_pos_;
        }

        /// @brief 前フレームからのマウスカーソルの移動量を取得する
        Point<int> GetMouseDelta() const noexcept {
            return Point<int>{current_mouse_pos_.x - previous_mouse_pos_.x,
                              current_mouse_pos_.y - previous_mouse_pos_.y};
        }

        /// @brief マウスホイールの回転量を取得する
        int GetMouseWheelDelta() const noexcept {
            return mouse_wheel_delta_;
        }

        /// @brief マウスカーソルが指定した矩形領域内にあるかを判定する
        bool IsMouseOver(const Rect<int>& rect) const noexcept {
            return current_mouse_pos_.In(rect);
        }

        /// @brief アクションが押された瞬間のマウス座標を返す（押されていない場合は {-1, -1}）
        Point<int> OnMouseClick(InputAction action) const {
            if (IsPressed(action)) {
                return current_mouse_pos_;
            }
            return Point<int>{-1, -1};
        }

        /// @brief 最後に入力を検知したアクティブデバイスを取得する
        InputDevice GetActiveDevice() const noexcept {
            return active_device_;
        }

    private:
        template <typename Func>
        bool CheckActionState(InputAction action, Func&& evaluator) const {
            InputContext current_context = GetCurrentContext();
            auto context_it = action_mappings_.find(current_context);
            if (context_it == action_mappings_.end()) {
                return false;
            }

            auto action_it = context_it->second.find(action);
            if (action_it == context_it->second.end()) {
                return false;
            }

            for (InputButton button : action_it->second) {
                if (evaluator(button)) {
                    return true;
                }
            }
            return false;
        }

        static int GetMouseButtonMask(InputButton button) noexcept {
            switch (button) {
            case InputButton::MouseLeft:   return 0x0001; // MOUSE_INPUT_LEFT
            case InputButton::MouseRight:  return 0x0002; // MOUSE_INPUT_RIGHT
            case InputButton::MouseMiddle: return 0x0004; // MOUSE_INPUT_MIDDLE
            default:                       return 0;
            }
        }

        static int GetPadButtonMask(InputButton button) noexcept {
            switch (button) {
            case InputButton::PadDown:   return 0x0001; // PAD_INPUT_DOWN
            case InputButton::PadLeft:   return 0x0002; // PAD_INPUT_LEFT
            case InputButton::PadRight:  return 0x0004; // PAD_INPUT_RIGHT
            case InputButton::PadUp:     return 0x0008; // PAD_INPUT_UP
            case InputButton::Pad1:      return 0x0010; // PAD_INPUT_1 / Button A
            case InputButton::Pad2:      return 0x0020; // PAD_INPUT_2 / Button B
            case InputButton::Pad3:      return 0x0040; // PAD_INPUT_3 / Button X
            case InputButton::Pad4:      return 0x0080; // PAD_INPUT_4 / Button Y
            case InputButton::PadL1:     return 0x0100; // PAD_INPUT_5 / L1
            case InputButton::PadR1:     return 0x0200; // PAD_INPUT_6 / R1
            case InputButton::PadL2:     return 0x0400; // PAD_INPUT_7 / L2
            case InputButton::PadR2:     return 0x0800; // PAD_INPUT_8 / R2
            case InputButton::PadStart:  return 0x1000; // PAD_INPUT_9 / Start
            case InputButton::PadSelect: return 0x2000; // PAD_INPUT_10 / Select
            default:                     return 0;
            }
        }
    };
} // namespace ab
