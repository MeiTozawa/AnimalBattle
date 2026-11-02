module;

#include <cmath>
#include <concepts>
#include <cstdint>
#include "Core/Vector2D.h"

export module PhysicsTypes;

namespace ab {

export using ab::Vector2D;
export using ab::Vec2f;
export using ab::Vec2i;

/// @brief 2次元軸平行矩形 (AABB) 衝突判定形状 (中心座標 + サイズ)
export struct Box2D {
    Vec2f center;
    Vec2f size;

    constexpr float Left() const noexcept { return center.x - size.x * 0.5f; }
    constexpr float Right() const noexcept { return center.x + size.x * 0.5f; }
    constexpr float Top() const noexcept { return center.y - size.y * 0.5f; }
};

/// @brief 地形・ワンウェイ足場着地判定結果構造体
export struct GroundCheckResult {
    bool is_grounded = false;
    float ground_y = 0.0f;
    float adjusted_vy = 0.0f;
};

}  // namespace ab
