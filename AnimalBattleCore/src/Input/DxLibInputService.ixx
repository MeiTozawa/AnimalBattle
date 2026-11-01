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

    /// @brief DxLib を使用して入力サンプリング・コンテキストスタック・アクションマッピングを提供する具象入力サービス (単一メインスレッドで駆動する決定論的無ロック設計)
    export class DxLibInputService : public IInputService {
    private:
        // ハードウェアサンプリング状態 (メインスレッドの Update が収集・累積)
        std::array<char, 256> raw_current_keys_{};
        std::array<char, 256> latched_pressed_keys_{};

        int raw_mouse_buttons_ = 0;
        int latched_mouse_pressed_ = 0;
        Vec2i raw_mouse_pos_{0, 0};

        int raw_pad_state_ = 0;
        int latched_pad_pressed_ = 0;

        // 論理フレーム入力スナップショット (論理フレーム開始時にスナップショット確定、フレーム中不変)
        std::array<char, 256> frame_current_keys_{};
        std::array<char, 256> frame_pressed_keys_{};

        int frame_mouse_buttons_ = 0;
        int frame_mouse_pressed_ = 0;
        Vec2i frame_mouse_pos_{0, 0};

        int frame_pad_state_ = 0;
        int frame_pad_pressed_ = 0;

        std::vector<ContextId> context_stack_{DEFAULT_CONTEXT};
        std::unordered_map<ContextId, std::unordered_map<ActionId, std::vector<InputButton>>> action_mappings_{};

        template <typename Func>
        bool CheckActionState(ActionId action, Func&& evaluator) const {
            ContextId current_context = context_stack_.empty() ? DEFAULT_CONTEXT : context_stack_.back();
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

        // --- ハードウェアサンプリング (メインスレッド専任) ---
        void Update() override {
            std::array<char, 256> new_keys{};
            ::GetHitKeyStateAll(new_keys.data());

            int new_mouse = ::GetMouseInput();
            Vec2i new_mouse_pos{0, 0};
            ::GetMousePoint(&new_mouse_pos.x, &new_mouse_pos.y);
            int new_pad = ::GetJoypadInputState(1);

            for (int i = 0; i < 256; ++i) {
                char curr = new_keys[i];
                char prev = raw_current_keys_[i];
                if (curr != 0 && prev == 0) {
                    latched_pressed_keys_[i] = 1;
                }
                raw_current_keys_[i] = curr;
            }

            int mouse_pressed = new_mouse & ~raw_mouse_buttons_;
            latched_mouse_pressed_ |= mouse_pressed;
            raw_mouse_buttons_ = new_mouse;
            raw_mouse_pos_ = new_mouse_pos;

            int pad_pressed = new_pad & ~raw_pad_state_;
            latched_pad_pressed_ |= pad_pressed;
            raw_pad_state_ = new_pad;
        }

        // --- 論理フレームスナップショット確定 (論理スレッド専任) ---
        void Snapshot() override {
            frame_current_keys_ = raw_current_keys_;
            frame_pressed_keys_ = latched_pressed_keys_;
            latched_pressed_keys_.fill(0);

            frame_mouse_buttons_ = raw_mouse_buttons_;
            frame_mouse_pressed_ = latched_mouse_pressed_;
            latched_mouse_pressed_ = 0;
            frame_mouse_pos_ = raw_mouse_pos_;

            frame_pad_state_ = raw_pad_state_;
            frame_pad_pressed_ = latched_pad_pressed_;
            latched_pad_pressed_ = 0;
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

        // --- コンテキスト管理 ---
        void ClearAndSetContext(ContextId context) override {
            context_stack_.clear();
            context_stack_.push_back(context);
        }

        // --- バインディング管理 ---
        void BindAction(ContextId context, ActionId action, std::vector<InputButton> buttons) override {
            action_mappings_[context][action] = std::move(buttons);
        }

        // --- マウス照会 ---
        Vec2i GetMousePosition() const noexcept override {
            return frame_mouse_pos_;
        }

        // --- 低レベルキー照会 ---
        bool CheckHitKey(int key_code) const override {
            if (key_code >= 0 && key_code < 256) {
                return frame_current_keys_[key_code] != 0;
            }
            return false;
        }
    };
} // namespace ab
