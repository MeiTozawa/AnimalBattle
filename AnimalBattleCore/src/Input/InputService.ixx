module;

#include <cstdint>
#include <vector>

export module InputService;

export import InputTypes;

namespace ab {
    /// @brief バックエンド非依存の入力サービス抽象インターフェース
    export class IInputService {
    public:
        virtual ~IInputService() = default;

        // --- 更新与快照 ---
        virtual void Update() = 0;
        virtual void Snapshot() = 0;

        // --- アクション判定 ---
        virtual bool IsPressed(ActionId action) const = 0;
        virtual bool IsHolding(ActionId action) const = 0;
        virtual bool IsReleased(ActionId action) const = 0;

        // --- ボタン直接判定 ---
        virtual bool IsButtonPressed(InputButton button) const = 0;
        virtual bool IsButtonHolding(InputButton button) const = 0;
        virtual bool IsButtonReleased(InputButton button) const = 0;

        // --- コンテキスト管理 ---
        virtual void PushContext(ContextId context) = 0;
        virtual void PopContext() = 0;
        virtual void ClearAndSetContext(ContextId context) = 0;
        virtual ContextId GetCurrentContext() const noexcept = 0;

        // --- バインディング管理 ---
        virtual void BindAction(ContextId context, ActionId action, std::vector<InputButton> buttons) = 0;
        virtual void AddActionButton(ContextId context, ActionId action, InputButton button) = 0;
        virtual void ClearActions(ContextId context) = 0;

        // --- マウス・デバイス照会 ---
        virtual Vec2i GetMousePosition() const noexcept = 0;
        virtual Vec2i GetMouseDelta() const noexcept = 0;
        virtual int GetMouseWheelDelta() const noexcept = 0;
        virtual bool IsMouseOver(const Rect<int>& rect) const noexcept = 0;
        virtual bool IsMouseOver(Vec2i p1, Vec2i p2) const noexcept = 0;
        virtual InputDevice GetActiveDevice() const noexcept = 0;

        // --- 低レベルキー・パッド照会 ---
        virtual bool CheckHitKey(int key_code) const = 0;
        virtual int GetJoypadInputState(int input_type) const = 0;
        virtual int GetJoypadNum() const = 0;
        virtual void GetMousePoint(Vec2i* out_pos) const = 0;
    };
} // namespace ab
