module;

#include <cmath>
#include <concepts>
#include <cstdint>

export module PhysicsTypes;

namespace ab {

/// @brief 2次元汎用ベクトル構造体
export template <typename T>
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

export using Vec2f = Vector2D<float>;
export using Vec2i = Vector2D<int>;

/// @brief 2次元円形衝突判定形状 (中心座標 + 半径)
export struct Circle2D {
    Vec2f center;
    float radius;
};

/// @brief 2次元軸平行矩形 (AABB) 衝突判定形状 (中心座標 + サイズ)
export struct Box2D {
    Vec2f center;
    Vec2f size;

    constexpr float Left() const noexcept { return center.x - size.x * 0.5f; }
    constexpr float Right() const noexcept { return center.x + size.x * 0.5f; }
    constexpr float Top() const noexcept { return center.y - size.y * 0.5f; }
    constexpr float Bottom() const noexcept { return center.y + size.y * 0.5f; }
};

/// @brief 2次元線分形状 (始点 + 終点)
export struct Segment2D {
    Vec2f start;
    Vec2f end;
};

/// @brief 2次元レイ (原点 + 方向)
export struct Ray2D {
    Vec2f origin;
    Vec2f direction;
};

/// @brief 2次元レイキャストヒット結果構造体
export struct RaycastHit2D {
    bool has_hit = false;
    Vec2f point{};
    Vec2f normal{};
    float distance = 0.0f;
};

/// @brief 2次元オブジェクト貫通深度および押し出しベクトル構造体
export struct PenetrationResult2D {
    bool is_penetrating = false;
    Vec2f normal{};
    float depth = 0.0f;
};

/// @brief 地形・ワンウェイ足場着地判定結果構造体
export struct GroundCheckResult {
    bool is_grounded = false;
    float ground_y = 0.0f;
    float adjusted_vy = 0.0f;
};

}  // namespace ab
