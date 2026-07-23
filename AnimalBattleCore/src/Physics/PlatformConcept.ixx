module;

#include <concepts>

export module PlatformConcept;

namespace ab {
    /// @brief 物理プラットフォーム構造体が満たすべき C++20 Concept
    export template <typename T>
    concept PlatformConcept = requires(const T& p) {
        { p.id } -> std::convertible_to<int>;
        { p.x1 } -> std::convertible_to<float>;
        { p.y1 } -> std::convertible_to<float>;
        { p.x2 } -> std::convertible_to<float>;
        { p.y2 } -> std::convertible_to<float>;
    };
} // namespace ab
