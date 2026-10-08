module;

#include <array>
#include <cstdint>
#include <mutex>
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
        mutable std::mutex state_mutex_{};

        // ハードウェアサンプリング状態 (メインスレッドの Update が収集・累積)
        std::array<char, 256> raw_current_keys_{};
        std::array<char, 256> raw_previous_keys_{};
        std::array<char, 256> latched_pressed_keys_{};
        std::array<char, 256> latched_released_keys_{};

        int raw_mouse_buttons_ = 0;
        int raw_previous_mouse_buttons_ = 0;
        int latched_mouse_pressed_ = 0;
        int latched_mouse_released_ = 0;
        Vec2i raw_mouse_pos_{0, 0};
        Vec2i raw_previous_mouse_pos_{0, 0};
        int raw_mouse_wheel_delta_ = 0;
        int latched_mouse_wheel_delta_ = 0;

        int raw_pad_state_ = 0;
        int raw_previous_pad_state_ = 0;
        int latched_pad_pressed_ = 0;
        int latched_pad_released_ = 0;

        InputDevice active_device_ = InputDevice::Keyboard;

        // 論理フレーム入力スナップショット (論理スレッドが BeginFrame でアトミックにラッチ取得し、フレーム中不変)
        std::array<char, 256> frame_current_keys_{};
        std::array<char, 256> frame_pressed_keys_{};
        std::array<char, 256> frame_released_keys_{};

        int frame_mouse_buttons_ = 0;
        int frame_mouse_pressed_ = 0;
        int frame_mouse_released_ = 0;
        Vec2i frame_mouse_pos_{0, 0};
        Vec2i frame_prev_mouse_pos_{0, 0};
        int frame_mouse_wheel_delta_ = 0;

        int frame_pad_state_ = 0;
        int frame_pad_pressed_ = 0;
        int frame_pad_released_ = 0;

        InputDevice frame_active_device_ = InputDevice::Keyboard;

        std::vector<ContextId> context_stack_{DEFAULT_CONTEXT};
        std::unordered_map<ContextId, std::unordered_map<ActionId, std::vector<InputButton>>> action_mappings_{};

        template <typename Func>
        bool CheckActionState(ActionId action, Func&& evaluator) const {
            std::lock_guard lock(state_mutex_);
            ContextId current_context = GetCurrentContextUnsafe();
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

        ContextId GetCurrentContextUnsafe() const noexcept {
            return context_stack_.empty() ? DEFAULT_CONTEXT : context_stack_.back();
        }

    public:
        DxLibInputService() = default;

        // --- ハードウェアサンプリング (メインスレッド専任) ---
        void Update() override {
            std::array<char, 256> new_keys{};
            ::GetHitKeyStateAll(new_keys.data());

            int new_mouse = ::GetMouseInput();
            Vec2i new_mouse_pos{0, 0};
            ::GetMousePoint(&new_mouse_pos.x, &new_mouse_pos.y);
            int wheel_vol = ::GetMouseWheelRotVol();
            int new_pad = ::GetJoypadInputState(1);

            {
                std::lock_guard lock(state_mutex_);

                for (int i = 0; i < 256; ++i) {
                    char curr = new_keys[i];
                    char prev = raw_current_keys_[i];
                    if (curr != 0 && prev == 0) {
                        latched_pressed_keys_[i] = 1;
                    }
                    if (curr == 0 && prev != 0) {
                        latched_released_keys_[i] = 1;
                    }
                    raw_previous_keys_[i] = prev;
                    raw_current_keys_[i] = curr;
                }

                int mouse_pressed = new_mouse & ~raw_mouse_buttons_;
                int mouse_released = ~new_mouse & raw_mouse_buttons_;
                latched_mouse_pressed_ |= mouse_pressed;
                latched_mouse_released_ |= mouse_released;
                raw_previous_mouse_buttons_ = raw_mouse_buttons_;
                raw_mouse_buttons_ = new_mouse;
                raw_previous_mouse_pos_ = raw_mouse_pos_;
                raw_mouse_pos_ = new_mouse_pos;
                latched_mouse_wheel_delta_ += wheel_vol;

                int pad_pressed = new_pad & ~raw_pad_state_;
                int pad_released = ~new_pad & raw_pad_state_;
                latched_pad_pressed_ |= pad_pressed;
                latched_pad_released_ |= pad_released;
                raw_previous_pad_state_ = raw_pad_state_;
                raw_pad_state_ = new_pad;

                if (new_pad != 0) {
                    active_device_ = InputDevice::Gamepad;
                } else if (new_mouse != 0 || wheel_vol != 0 ||
                           new_mouse_pos.x != raw_previous_mouse_pos_.x ||
                           new_mouse_pos.y != raw_previous_mouse_pos_.y) {
                    active_device_ = InputDevice::Mouse;
                } else {
                    for (int i = 0; i < 256; ++i) {
                        if (new_keys[i] != 0) {
                            active_device_ = InputDevice::Keyboard;
                            break;
                        }
                    }
                }
            }
        }

        // --- 論理フレーム入力スナップショット確定 (論理スレッド専任: BeginRecording 時に同期) ---
        void Snapshot() override {
            std::lock_guard lock(state_mutex_);

            frame_current_keys_ = raw_current_keys_;
            frame_pressed_keys_ = latched_pressed_keys_;
            latched_pressed_keys_.fill(0);

            frame_released_keys_ = latched_released_keys_;
            latched_released_keys_.fill(0);

            frame_mouse_buttons_ = raw_mouse_buttons_;
            frame_mouse_pressed_ = latched_mouse_pressed_;
            latched_mouse_pressed_ = 0;

            frame_mouse_released_ = latched_mouse_released_;
            latched_mouse_released_ = 0;

            frame_prev_mouse_pos_ = frame_mouse_pos_;
            frame_mouse_pos_ = raw_mouse_pos_;

            frame_mouse_wheel_delta_ = latched_mouse_wheel_delta_;
            latched_mouse_wheel_delta_ = 0;

            frame_pad_state_ = raw_pad_state_;
            frame_pad_pressed_ = latched_pad_pressed_;
            latched_pad_pressed_ = 0;

            frame_pad_released_ = latched_pad_released_;
            latched_pad_released_ = 0;

            frame_active_device_ = active_device_;
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

        // --- ボタン直接判定 (論理フレームスナップショット参照) ---
        bool IsButtonHolding(InputButton button) const override {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 && frame_current_keys_[key_code] != 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (frame_mouse_buttons_ & mask) != 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (frame_pad_state_ & mask) != 0;
            }
            return false;
        }

        bool IsButtonPressed(InputButton button) const override {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 && frame_pressed_keys_[key_code] != 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (frame_mouse_pressed_ & mask) != 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (frame_pad_pressed_ & mask) != 0;
            }
            return false;
        }

        bool IsButtonReleased(InputButton button) const override {
            if (IsKeyboardButton(button)) {
                int key_code = static_cast<int>(button);
                return key_code >= 0 && key_code < 256 && frame_released_keys_[key_code] != 0;
            }
            if (IsMouseButton(button)) {
                int mask = GetMouseButtonMask(button);
                return (frame_mouse_released_ & mask) != 0;
            }
            if (IsPadButton(button)) {
                int mask = GetPadButtonMask(button);
                return (frame_pad_released_ & mask) != 0;
            }
            return false;
        }

        // --- コンテキスト管理 ---
        void PushContext(ContextId context) override {
            std::lock_guard lock(state_mutex_);
            if (context_stack_.empty() || context_stack_.back() != context) {
                context_stack_.push_back(context);
            }
        }

        void PopContext() override {
            std::lock_guard lock(state_mutex_);
            if (context_stack_.size() > 1) {
                context_stack_.pop_back();
            }
        }

        void ClearAndSetContext(ContextId context) override {
            std::lock_guard lock(state_mutex_);
            context_stack_.clear();
            context_stack_.push_back(context);
        }

        ContextId GetCurrentContext() const noexcept override {
            std::lock_guard lock(state_mutex_);
            return GetCurrentContextUnsafe();
        }

        // --- バインディング管理 ---
        void BindAction(ContextId context, ActionId action, std::vector<InputButton> buttons) override {
            std::lock_guard lock(state_mutex_);
            action_mappings_[context][action] = std::move(buttons);
        }

        void AddActionButton(ContextId context, ActionId action, InputButton button) override {
            std::lock_guard lock(state_mutex_);
            action_mappings_[context][action].push_back(button);
        }

        void ClearActions(ContextId context) override {
            std::lock_guard lock(state_mutex_);
            action_mappings_.erase(context);
        }

        // --- マウス・デバイス照会 ---
        Vec2i GetMousePosition() const noexcept override {
            return frame_mouse_pos_;
        }

        Vec2i GetMouseDelta() const noexcept override {
            return Vec2i{frame_mouse_pos_.x - frame_prev_mouse_pos_.x,
                         frame_mouse_pos_.y - frame_prev_mouse_pos_.y};
        }

        int GetMouseWheelDelta() const noexcept override {
            return frame_mouse_wheel_delta_;
        }

        bool IsMouseOver(const Rect<int>& rect) const noexcept override {
            return rect.Contains(frame_mouse_pos_);
        }

        bool IsMouseOver(Vec2i p1, Vec2i p2) const noexcept override {
            return Rect<int>{p1, p2}.Contains(frame_mouse_pos_);
        }

        InputDevice GetActiveDevice() const noexcept override {
            return frame_active_device_;
        }

        // --- 低レベルキー・パッド照会 ---
        bool CheckHitKey(int key_code) const override {
            if (key_code >= 0 && key_code < 256) {
                return frame_current_keys_[key_code] != 0;
            }
            return false;
        }

        int GetJoypadInputState(int input_type) const override {
            if (input_type == 1) {
                return frame_pad_state_;
            }
            return ::GetJoypadInputState(input_type);
        }

        int GetJoypadNum() const override {
            return ::GetJoypadNum();
        }

        void GetMousePoint(Vec2i* out_pos) const override {
            if (out_pos != nullptr) {
                *out_pos = frame_mouse_pos_;
            }
        }
    };
} // namespace ab
