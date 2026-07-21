module;

#include <cmath>
#include <algorithm>
#include <vector>

module PhysicsEngine;

namespace ab {

    static bool CheckOverlap(const PhysicsObject::AABB& a, const Platform& p) {
        return a.min_x < p.x2 && a.max_x > p.x1 && a.min_y < p.y2 && a.max_y > p.y1;
    }

    void PhysicsEngine::Step(float dt) {
        if (dt <= 0.0f) return;
        if (dt > max_delta_time_) dt = max_delta_time_;

        for (auto& obj : objects_) {
            // --- 1. Timers Update ---
            if (obj.drop_through_timer > 0.0f) {
                obj.drop_through_timer -= dt;
            }
            if (obj.hitstun_timer > 0.0f) {
                obj.hitstun_timer -= dt;
            }

            // --- 2. Decay Knockback Velocity ---
            float kb_speed = std::hypot(obj.kbx, obj.kby);
            if (kb_speed > 0.0f) {
                float decay = knockback_decay_ * dt;
                if (kb_speed <= decay) {
                    obj.kbx = 0.0f;
                    obj.kby = 0.0f;
                } else {
                    obj.kbx -= (obj.kbx / kb_speed) * decay;
                    obj.kby -= (obj.kby / kb_speed) * decay;
                }
            }

            // --- 3. Integrate Movement & Resolve Collisions ---
            float prev_y = obj.y;
            obj.collision_flags = CollisionFlags::kNone;

            float total_vx = obj.vx + obj.kbx;
            float total_vy = obj.vy + obj.kby;

            // X-axis Movement and Collision Resolution
            obj.x += total_vx * dt;
            auto aabb_x = obj.GetAABB();
            for (const auto& plat : platforms_) {
                if (plat.type != PlatformType::Solid) continue;
                if (CheckOverlap(aabb_x, plat)) {
                    float left_overlap = aabb_x.max_x - plat.x1;
                    float right_overlap = plat.x2 - aabb_x.min_x;
                    if (left_overlap < right_overlap) {
                        obj.x -= left_overlap;
                        obj.collision_flags |= CollisionFlags::kWallRight;
                    } else {
                        obj.x += right_overlap;
                        obj.collision_flags |= CollisionFlags::kWallLeft;
                    }
                    obj.vx = 0.0f;
                    obj.kbx = 0.0f;
                    aabb_x = obj.GetAABB();
                }
            }

            // Y-axis Movement and Collision Resolution
            obj.y += total_vy * dt;
            auto aabb_y = obj.GetAABB();
            
            bool hit_grounded = false;
            // Solid platform collision
            for (const auto& plat : platforms_) {
                if (plat.type != PlatformType::Solid) continue;
                if (CheckOverlap(aabb_y, plat)) {
                    float top_overlap = aabb_y.max_y - plat.y1;
                    float bottom_overlap = plat.y2 - aabb_y.min_y;
                    if (top_overlap < bottom_overlap) {
                        // Floor landing
                        obj.y -= top_overlap;
                        obj.vy = 0.0f;
                        obj.kby = 0.0f;
                        obj.collision_flags |= CollisionFlags::kFloor;
                        hit_grounded = true;
                    } else {
                        // Ceiling collision
                        obj.y += bottom_overlap;
                        obj.vy = 0.0f;
                        obj.kby = 0.0f;
                        obj.collision_flags |= CollisionFlags::kCeiling;
                    }
                    aabb_y = obj.GetAABB();
                }
            }

            // SemiSolid platform collision (landing from above when not dropping through)
            if (!hit_grounded && obj.drop_through_timer <= 0.0f && total_vy >= 0.0f) {
                float feet_prev_y = prev_y;
                float feet_curr_y = obj.y;

                for (const auto& plat : platforms_) {
                    if (plat.type != PlatformType::SemiSolid) continue;

                    float p_min_x = obj.x - obj.width / 2.0f;
                    float p_max_x = obj.x + obj.width / 2.0f;

                    if (p_max_x > plat.x1 && p_min_x < plat.x2) {
                        if (feet_prev_y <= plat.y1 && feet_curr_y >= plat.y1) {
                            obj.y = plat.y1;
                            obj.vy = 0.0f;
                            obj.kby = 0.0f;
                            obj.collision_flags |= (CollisionFlags::kFloor | CollisionFlags::kOnSemisolid);
                            break;
                        }
                    }
                }
            }
        }
    }
}  // namespace ab
