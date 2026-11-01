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

        // --- ボタン直接判定 ---
        virtual bool IsButtonPressed(InputButton button) const = 0;
        virtual bool IsButtonHolding(InputButton button) const = 0;

        // --- コンテキスト管理 ---
        virtual void ClearAndSetContext(ContextId context) = 0;

        // --- バインディング管理 ---
        virtual void BindAction(ContextId context, ActionId action, std::vector<InputButton> buttons) = 0;

        // --- マウス照会 ---
        virtual Vec2i GetMousePosition() const noexcept = 0;

        // --- 低レベルキー照会 ---
        virtual bool CheckHitKey(int key_code) const = 0;
    };
} // namespace ab
