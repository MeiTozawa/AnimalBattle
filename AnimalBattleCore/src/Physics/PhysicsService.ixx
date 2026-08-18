module;

#include <cstdint>

export module PhysicsService;

export import PhysicsTypes;

namespace ab {

/// @brief バックエンド非依存の幾何・物理計算サービス抽象インターフェース
export class IPhysicsService {
public:
    virtual ~IPhysicsService() = default;

    // --- 2D 幾何形状交差判定 ---
    virtual bool IntersectsCircleCircle(const Circle2D& c1, const Circle2D& c2) const noexcept = 0;
    virtual bool IntersectsBoxBox(const Box2D& b1, const Box2D& b2) const noexcept = 0;
    virtual bool IntersectsCircleBox(const Circle2D& circle, const Box2D& box) const noexcept = 0;
    virtual bool IntersectsPointCircle(Vec2f point, const Circle2D& circle) const noexcept = 0;
    virtual bool IntersectsPointBox(Vec2f point, const Box2D& box) const noexcept = 0;
    virtual RaycastHit2D RaycastSegment(const Ray2D& ray, const Segment2D& segment) const noexcept = 0;

    // --- 地形・足場・衝突応答計算 ---
    virtual GroundCheckResult CheckPlatformGround(float prev_y, float next_y, const Box2D& player_box, const Box2D& platform_box) const noexcept = 0;
    virtual PenetrationResult2D CheckPenetrationBoxBox(const Box2D& b1, const Box2D& b2) const noexcept = 0;

    // --- 運動積分およびベクトル数学 ---
    virtual Vec2f IntegrateVelocity(Vec2f position, Vec2f velocity, float delta_time) const noexcept = 0;
    virtual void ApplyGravity(float& vy, float gravity, float max_fall_speed, float delta_time) const noexcept = 0;
    virtual float CalculateDistance(Vec2f a, Vec2f b) const noexcept = 0;
    virtual float CalculateDistanceSquared(Vec2f a, Vec2f b) const noexcept = 0;
    virtual float CalculateAngle(Vec2f from, Vec2f to) const noexcept = 0;
    virtual Vec2f DirectionToVector(float angle_rad, float speed) const noexcept = 0;
    virtual float Clamp(float value, float min_val, float max_val) const noexcept = 0;
    virtual float Lerp(float a, float b, float t) const noexcept = 0;
};

}  // namespace ab
