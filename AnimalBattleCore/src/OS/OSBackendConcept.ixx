module;

#include <concepts>

export module OSBackendConcept;

namespace ab {
    /// @brief OS レベル操作のバックエンドが満たすべき C++20 Concept（戻り値の型 void を厳密に検証）
    export template <typename T>
    concept OSBackendConcept = requires(T t) {
        { t.SetCursorPointer() } -> std::same_as<void>;
        { t.SetCursorArrow() } -> std::same_as<void>;
    };
} // namespace ab
