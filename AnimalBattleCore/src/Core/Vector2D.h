#ifndef ANIMALBATTLE_CORE_CORE_VECTOR2D_H_
#define ANIMALBATTLE_CORE_CORE_VECTOR2D_H_

#include <cmath>
#include <concepts>
#include <cstdint>

namespace ab {

/// @brief 2次元汎用ベクトル構造体
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

    constexpr Vector2D operator-(const Vector2D& other) const noexcept {
        return Vector2D{x - other.x, y - other.y};
    }

    constexpr Vector2D operator*(T scalar) const noexcept {
        return Vector2D{x * scalar, y * scalar};
    }

    constexpr Vector2D operator/(T scalar) const noexcept {
        return Vector2D{x / scalar, y / scalar};
    }

    constexpr T Dot(const Vector2D& other) const noexcept {
        return x * other.x + y * other.y;
    }

    constexpr T LengthSquared() const noexcept {
        return x * x + y * y;
    }

    float Length() const noexcept {
        return std::sqrt(static_cast<float>(LengthSquared()));
    }

    Vector2D Normalized() const noexcept {
        float len = Length();
        if (len > 0.00001f) {
            return *this / static_cast<T>(len);
        }
        return Vector2D{0, 0};
    }
};

using Vec2f = Vector2D<float>;
using Vec2i = Vector2D<int>;

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_CORE_VECTOR2D_H_
