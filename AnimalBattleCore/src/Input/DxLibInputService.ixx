module;

#include <array>
#include <cstdint>
#include <unordered_map>
#include <utility>
#include <vector>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

export module DxLibInputService;

export import InputService;

namespace ab {

namespace {

int GetMouseButtonMask(InputButton button) noexcept {
    switch (button) {
    case InputButton::MouseLeft:
        return MOUSE_INPUT_LEFT;
    case InputButton::MouseRight:
        return MOUSE_INPUT_RIGHT;
    case InputButton::MouseMiddle:
        return MOUSE_INPUT_MIDDLE;
    default:
        return 0;
    }
}

int GetPadButtonMask(InputButton button) noexcept {
    switch (button) {
    case InputButton::PadDown:
        return PAD_INPUT_DOWN;
    case InputButton::PadLeft:
        return PAD_INPUT_LEFT;
    case InputButton::PadRight:
        return PAD_INPUT_RIGHT;
    case InputButton::PadUp:
        return PAD_INPUT_UP;
    case InputButton::Pad1:
        return PAD_INPUT_1;
    case InputButton::Pad2:
        return PAD_INPUT_2;
    case InputButton::Pad3:
        return PAD_INPUT_3;
    case InputButton::Pad4:
        return PAD_INPUT_4;
    case InputButton::PadL1:
        return PAD_INPUT_5;
    case InputButton::PadR1:
        return PAD_INPUT_6;
    case InputButton::PadL2:
        return PAD_INPUT_7;
    case InputButton::PadR2:
        return PAD_INPUT_8;
    case InputButton::PadStart:
        return PAD_INPUT_9;
    case InputButton::PadSelect:
        return PAD_INPUT_10;
    default:
        return 0;
    }
}

}  // namespace

    /// @brief DxLib を使用して入力サンプリング・コンテキストスタック・アクションマッピングを提供する具象入力サービス
    export class DxLibInputService : public IInputService {
    private:
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

        std::vector<ContextId> context_stack_{DEFAULT_CONTEXT};
        std::unordered_map<ContextId, std::unordered_map<ActionId, std::vector<InputButton>>> action_mappings_{};

        template <typename Func>
        bool CheckActionState(ActionId action, Func&& evaluator) const {
            ContextId current_context = GetCurrentContext();
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

    public:
        DxLibInputService() = default;

        // --- 更新 ---
        void Update() override {
            previous_keys_ = current_keys_;
            previous_mouse_buttons_ = current_mouse_buttons_;
            previous_mouse_pos_ = current_mouse_pos_;
            previous_pad_state_ = current_pad_state_;

            ::GetHitKeyStateAll(current_keys_.data());
            current_mouse_buttons_ = ::GetMouseInput();
            ::GetMousePoint(&current_mouse_pos_.x, &current_mouse_pos_.y);
            mouse_wheel_delta_ = ::GetMouseWheelRotVol();
            current_pad_state_ = ::GetJoypadInputState(1);

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

        // --- アクション判定 ---
        bool IsPressed(ActionId action) const override {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonPressed(button);
            });
        }

        bool IsHolding(ActionId action) const override {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonHolding(button);
            });
        }

        bool IsReleased(ActionId action) const override {
            return CheckActionState(action, [this](InputButton button) {
                return IsButtonReleased(button);
            });
        }

        // --- ボタン直接判定 ---
        bool IsButtonHolding(InputButton button) const override {
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

        bool IsButtonPressed(InputButton button) const override {
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

        bool IsButtonReleased(InputButton button) const override {
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

        // --- コンテキスト管理 ---
        void PushContext(ContextId context) override {
            if (context_stack_.empty() || context_stack_.back() != context) {
                context_stack_.push_back(context);
            }
        }

        void PopContext() override {
            if (context_stack_.size() > 1) {
                context_stack_.pop_back();
            }
        }

        void ClearAndSetContext(ContextId context) override {
            context_stack_.clear();
            context_stack_.push_back(context);
        }

        ContextId GetCurrentContext() const noexcept override {
            return context_stack_.empty() ? DEFAULT_CONTEXT : context_stack_.back();
        }

        // --- バインディング管理 ---
        void BindAction(ContextId context, ActionId action, std::vector<InputButton> buttons) override {
            action_mappings_[context][action] = std::move(buttons);
        }

        void AddActionButton(ContextId context, ActionId action, InputButton button) override {
            action_mappings_[context][action].push_back(button);
        }

        void ClearActions(ContextId context) override {
            action_mappings_.erase(context);
        }

        // --- マウス・デバイス照会 ---
        Point<int> GetMousePosition() const noexcept override {
            return current_mouse_pos_;
        }

        Point<int> GetMouseDelta() const noexcept override {
            return Point<int>{current_mouse_pos_.x - previous_mouse_pos_.x,
                              current_mouse_pos_.y - previous_mouse_pos_.y};
        }

        int GetMouseWheelDelta() const noexcept override {
            return mouse_wheel_delta_;
        }

        bool IsMouseOver(const Rect<int>& rect) const noexcept override {
            return current_mouse_pos_.In(rect);
        }

        Point<int> OnMouseClick(ActionId action) const override {
            if (IsPressed(action)) {
                return current_mouse_pos_;
            }
            return Point<int>{-1, -1};
        }

        InputDevice GetActiveDevice() const noexcept override {
            return active_device_;
        }

        // --- 低レベルキー・パッド照会 ---
        bool CheckHitKey(int key_code) const override {
            return ::CheckHitKey(key_code) == 1;
        }

        int GetJoypadInputState(int input_type) const override {
            return ::GetJoypadInputState(input_type);
        }

        int GetJoypadNum() const override {
            return ::GetJoypadNum();
        }

        void GetMousePoint(int* out_x, int* out_y) const override {
            if (out_x && out_y) {
                ::GetMousePoint(out_x, out_y);
            }
        }
    };
} // namespace ab
