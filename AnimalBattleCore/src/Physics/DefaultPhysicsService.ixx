module;

#include <algorithm>
#include <cmath>
#include <cstdint>

export module DefaultPhysicsService;

export import PhysicsService;

namespace ab {

/// @brief 純粋幾何・数学・物理計算を提供するデフォルトの高速物理サービス具象クラス
export class DefaultPhysicsService : public IPhysicsService {
public:
    DefaultPhysicsService() = default;

    // --- 2D 幾何形状交差判定 ---

    bool IntersectsCircleCircle(const Circle2D& c1, const Circle2D& c2) const noexcept override {
        Vec2f diff = c1.center - c2.center;
        float combined_radius = c1.radius + c2.radius;
        return diff.LengthSquared() <= (combined_radius * combined_radius);
    }

    bool IntersectsBoxBox(const Box2D& b1, const Box2D& b2) const noexcept override {
        Vec2f diff = b1.center - b2.center;
        float dx = std::abs(diff.x);
        float dy = std::abs(diff.y);
        return dx <= (b1.size.x + b2.size.x) * 0.5f && dy <= (b1.size.y + b2.size.y) * 0.5f;
    }

    bool IntersectsCircleBox(const Circle2D& circle, const Box2D& box) const noexcept override {
        float closest_x = std::clamp(circle.center.x, box.Left(), box.Right());
        float closest_y = std::clamp(circle.center.y, box.Top(), box.Bottom());

        Vec2f diff{circle.center.x - closest_x, circle.center.y - closest_y};
        return diff.LengthSquared() <= (circle.radius * circle.radius);
    }

    bool IntersectsPointCircle(Vec2f point, const Circle2D& circle) const noexcept override {
        Vec2f diff = point - circle.center;
        return diff.LengthSquared() <= (circle.radius * circle.radius);
    }

    bool IntersectsPointBox(Vec2f point, const Box2D& box) const noexcept override {
        return point.x >= box.Left() && point.x <= box.Right() &&
               point.y >= box.Top() && point.y <= box.Bottom();
    }

    RaycastHit2D RaycastSegment(const Ray2D& ray, const Segment2D& segment) const noexcept override {
        RaycastHit2D result{};
        Vec2f v1 = ray.origin - segment.start;
        Vec2f v2 = segment.end - segment.start;
        Vec2f v3{-ray.direction.y, ray.direction.x};

        float dot = v2.Dot(v3);
        if (std::abs(dot) < 0.000001f) {
            return result;
        }

        float t1 = (v2.x * v1.y - v2.y * v1.x) / dot;
        float t2 = (v1.x * v3.x + v1.y * v3.y) / dot;

        if (t1 >= 0.0f && (t2 >= 0.0f && t2 <= 1.0f)) {
            result.has_hit = true;
            result.distance = t1;
            result.point = ray.origin + ray.direction * t1;

            float seg_len = v2.Length();
            if (seg_len > 0.0f) {
                result.normal = Vec2f{-v2.y / seg_len, v2.x / seg_len};
            }
        }
        return result;
    }

    // --- 地形・足場・衝突応答計算 ---

    GroundCheckResult CheckPlatformGround(float prev_y, float next_y, const Box2D& player_box, const Box2D& platform_box) const noexcept override {
        GroundCheckResult result{};
        float plat_left = platform_box.Left();
        float plat_right = platform_box.Right();
        float plat_top = platform_box.Top();

        float p_left = player_box.Left();
        float p_right = player_box.Right();
        float p_bottom_prev = prev_y + player_box.size.y * 0.5f;
        float p_bottom_next = next_y + player_box.size.y * 0.5f;

        if (p_right > plat_left && p_left < plat_right) {
            if (p_bottom_prev <= plat_top + 4.0f && p_bottom_next >= plat_top) {
                result.is_grounded = true;
                result.ground_y = plat_top - player_box.size.y * 0.5f;
                result.adjusted_vy = 0.0f;
                return result;
            }
        }

        result.is_grounded = false;
        result.ground_y = next_y;
        return result;
    }

    PenetrationResult2D CheckPenetrationBoxBox(const Box2D& b1, const Box2D& b2) const noexcept override {
        PenetrationResult2D result{};
        Vec2f diff = b1.center - b2.center;
        float px = (b1.size.x + b2.size.x) * 0.5f - std::abs(diff.x);
        if (px <= 0.0f) return result;

        float py = (b1.size.y + b2.size.y) * 0.5f - std::abs(diff.y);
        if (py <= 0.0f) return result;

        result.is_penetrating = true;
        if (px < py) {
            result.depth = px;
            result.normal = Vec2f{(diff.x < 0.0f) ? -1.0f : 1.0f, 0.0f};
        } else {
            result.depth = py;
            result.normal = Vec2f{0.0f, (diff.y < 0.0f) ? -1.0f : 1.0f};
        }
        return result;
    }

    // --- 運動積分およびベクトル数学 ---

    Vec2f IntegrateVelocity(Vec2f position, Vec2f velocity, float delta_time) const noexcept override {
        return position + velocity * delta_time;
    }

    void ApplyGravity(float& vy, float gravity, float max_fall_speed, float delta_time) const noexcept override {
        vy += gravity * delta_time;
        if (vy > max_fall_speed) {
            vy = max_fall_speed;
        }
    }

    float CalculateDistance(Vec2f a, Vec2f b) const noexcept override {
        return (a - b).Length();
    }

    float CalculateDistanceSquared(Vec2f a, Vec2f b) const noexcept override {
        return (a - b).LengthSquared();
    }

    float CalculateAngle(Vec2f from, Vec2f to) const noexcept override {
        return std::atan2(to.y - from.y, to.x - from.x);
    }

    Vec2f DirectionToVector(float angle_rad, float speed) const noexcept override {
        return Vec2f{std::cos(angle_rad) * speed, std::sin(angle_rad) * speed};
    }

    float Clamp(float value, float min_val, float max_val) const noexcept override {
        return std::clamp(value, min_val, max_val);
    }

    float Lerp(float a, float b, float t) const noexcept override {
        return a + (b - a) * t;
    }

    void BatchBulletMovement(
        Transform* transforms,
        const Velocity* velocities,
        int count,
        float delta_time,
        float min_x, float max_x, float min_y, float max_y,
        uint8_t* out_despawn_flags) const noexcept override {
        if (transforms == nullptr || velocities == nullptr || count <= 0) {
            return;
        }

        #pragma omp simd
        for (int i = 0; i < count; ++i) {
            transforms[i].position.x += velocities[i].value.x;
            transforms[i].position.y += velocities[i].value.y;

            if (out_despawn_flags != nullptr) {
                out_despawn_flags[i] = (transforms[i].position.x < min_x || transforms[i].position.x > max_x ||
                                        transforms[i].position.y < min_y || transforms[i].position.y > max_y) ? 1 : 0;
            }
        }
    }
};

}  // namespace ab
