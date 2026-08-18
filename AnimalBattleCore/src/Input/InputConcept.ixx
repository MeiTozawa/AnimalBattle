module;

#include <concepts>
#include <cstdint>

export module InputConcept;

export import InputTypes;

namespace ab {
    /// @brief 入力バックエンドが満たすべき C++20 Concept
    /// オブジェクト指向の仮想継承（IInputService）を排し、静的多態によって低オーバヘッドで入力をポーリングする
    export template <typename T>
    concept InputBackendConcept = requires(
        T backend,
        const T const_backend,
        int key_code,
        char* key_state_buf,
        int input_type,
        int* out_x,
        int* out_y,
        bool disp_flag
    ) {
        { const_backend.CheckHitKey(key_code) } -> std::convertible_to<int>;
        { const_backend.GetHitKeyStateAll(key_state_buf) } -> std::convertible_to<int>;

        { const_backend.GetJoypadInputState(input_type) } -> std::convertible_to<int>;
        { const_backend.GetJoypadNum() } -> std::convertible_to<int>;

        { const_backend.GetMousePoint(out_x, out_y) } -> std::same_as<void>;
        { const_backend.GetMouseInput() } -> std::convertible_to<int>;
        { const_backend.GetMouseWheelRotVol() } -> std::convertible_to<int>;
        { backend.SetMouseDispFlag(disp_flag) } -> std::same_as<void>;
    };
} // namespace ab
