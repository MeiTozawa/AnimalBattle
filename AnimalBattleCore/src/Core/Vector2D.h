#ifndef ANIMALBATTLE_CORE_CORE_VECTOR2D_H_
#define ANIMALBATTLE_CORE_CORE_VECTOR2D_H_

#include <cmath>
#include <concepts>
#include <cstdint>

namespace ab {

/// @brief 2次元汎用ベクトル構造体
/// @tparam T 整数型または浮動小数点型を指定します
template <typename T>
requires std::integral<T> || std::floating_point<T>
struct Vector2D {
    T x;
    T y;

    constexpr Vector2D() : x(0), y(0) {}
    constexpr Vector2D(T x_val, T y_val) : x(x_val), y(y_val) {}

    constexpr Vector2D operator+(const Vector2D& other) const noexcept {
        return Vector2D{x + other.x, y + other.y};
    }
};

using Vec2f = Vector2D<float>;
using Vec2i = Vector2D<int>;

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_CORE_VECTOR2D_H_
