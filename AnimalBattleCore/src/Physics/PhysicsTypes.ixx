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

/// @brief C# Transform コンポーネントと完全バイナリ一致する 2D トランスフォーム構造体 (20 bytes)
export struct Transform {
    Vec2f position{0.0f, 0.0f};
    Vec2f scale{1.0f, 1.0f};
    float rotation = 0.0f;
};

/// @brief C# Velocity コンポーネントと完全バイナリ一致する速度構造体 (16 bytes)
export struct Velocity {
    Vec2f value{0.0f, 0.0f};
    Vec2f knockback{0.0f, 0.0f};
};

/// @brief イージング関数の計算タイプを表す列挙型
export enum class EaseType : int32_t {
    Linear = 0,
    InSine = 1,
    OutSine = 2,
    InOutSine = 3,
    InQuad = 4,
    OutQuad = 5,
    InOutQuad = 6,
    InCubic = 7,
    OutCubic = 8,
    InOutCubic = 9,
    InExpo = 10,
    OutExpo = 11,
    InOutExpo = 12,
    InElastic = 13,
    OutElastic = 14,
    InOutElastic = 15,
    InBounce = 16,
    OutBounce = 17,
    InOutBounce = 18
};

}  // namespace ab
