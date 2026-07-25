module;

#include <cmath>
#include <algorithm>
#include <vector>
#include <variant>
#include <type_traits>

module PhysicsEngine;

namespace ab {

    template <PlatformConcept T>
    static bool CheckOverlap(const PhysicsObject::AABB& a, const T& p) {
        return a.min_x < p.x2 && a.max_x > p.x1 && a.min_y < p.y2 && a.max_y > p.y1;
    }

    void PhysicsEngine::Step(float delta_time) {
        if (delta_time <= 0.0f) return;
        if (delta_time > max_delta_time_) delta_time = max_delta_time_;

        for (auto& obj : objects_) {
            // --- 1. Timers Update ---
            if (obj.drop_through_timer > 0.0f) {
                obj.drop_through_timer -= delta_time;
            }
            if (obj.hitstun_timer > 0.0f) {
                obj.hitstun_timer -= delta_time;
            }

            // --- 2. Decay Knockback Velocity ---
            float knockback_speed = std::hypot(obj.knockback_x, obj.knockback_y);
            if (knockback_speed > 0.0f) {
                float decay = knockback_decay_ * delta_time;
                if (knockback_speed <= decay) {
                    obj.knockback_x = 0.0f;
                    obj.knockback_y = 0.0f;
                } else {
                    obj.knockback_x -= (obj.knockback_x / knockback_speed) * decay;
                    obj.knockback_y -= (obj.knockback_y / knockback_speed) * decay;
                }
            }

            // --- 3. Integrate Movement & Resolve Collisions ---
            float prev_y = obj.y;
            obj.collision_flags = CollisionFlags::kNone;

            float total_vx = obj.vx + obj.knockback_x;
            float total_vy = obj.vy + obj.knockback_y;

            // X-axis Movement and Collision Resolution (Solid Platforms)
            obj.x += total_vx * delta_time;
            auto aabb_x = obj.GetAABB();
            for (const auto& plat_var : platforms_) {
#if defined(__GNUC__) && !defined(__clang__)
                if (auto* plat = std::get_if<SolidPlatform>(&plat_var)) {
                    if (CheckOverlap(aabb_x, *plat)) {
                        float left_overlap = aabb_x.max_x - plat->x1;
                        float right_overlap = plat->x2 - aabb_x.min_x;
                        if (left_overlap < right_overlap) {
                            obj.x -= left_overlap;
                            obj.collision_flags |= CollisionFlags::kWallRight;
                        } else {
                            obj.x += right_overlap;
                            obj.collision_flags |= CollisionFlags::kWallLeft;
                        }
                        obj.vx = 0.0f;
                        obj.knockback_x = 0.0f;
                        aabb_x = obj.GetAABB();
                    }
                }
#else
                std::visit([&](const auto& plat) {
                    using T = std::decay_t<decltype(plat)>;
                    if constexpr (std::is_same_v<T, SolidPlatform>) {
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
                            obj.knockback_x = 0.0f;
                            aabb_x = obj.GetAABB();
                        }
                    }
                }, plat_var);
#endif
            }

            // Y-axis Movement and Collision Resolution
            obj.y += total_vy * delta_time;
            auto aabb_y = obj.GetAABB();
            
            bool hit_grounded = false;
            // Solid platform collision
            for (const auto& plat_var : platforms_) {
#if defined(__GNUC__) && !defined(__clang__)
                if (auto* plat = std::get_if<SolidPlatform>(&plat_var)) {
                    if (CheckOverlap(aabb_y, *plat)) {
                        float top_overlap = aabb_y.max_y - plat->y1;
                        float bottom_overlap = plat->y2 - aabb_y.min_y;
                        if (top_overlap < bottom_overlap) {
                            // Floor landing
                            obj.y -= top_overlap;
                            obj.vy = 0.0f;
                            obj.knockback_y = 0.0f;
                            obj.collision_flags |= CollisionFlags::kFloor;
                            hit_grounded = true;
                        } else {
                            // Ceiling collision
                            obj.y += bottom_overlap;
                            obj.vy = 0.0f;
                            obj.knockback_y = 0.0f;
                            obj.collision_flags |= CollisionFlags::kCeiling;
                        }
                        aabb_y = obj.GetAABB();
                    }
                }
#else
                std::visit([&](const auto& plat) {
                    using T = std::decay_t<decltype(plat)>;
                    if constexpr (std::is_same_v<T, SolidPlatform>) {
                        if (CheckOverlap(aabb_y, plat)) {
                            float top_overlap = aabb_y.max_y - plat.y1;
                            float bottom_overlap = plat.y2 - aabb_y.min_y;
                            if (top_overlap < bottom_overlap) {
                                // Floor landing
                                obj.y -= top_overlap;
                                obj.vy = 0.0f;
                                obj.knockback_y = 0.0f;
                                obj.collision_flags |= CollisionFlags::kFloor;
                                hit_grounded = true;
                            } else {
                                // Ceiling collision
                                obj.y += bottom_overlap;
                                obj.vy = 0.0f;
                                obj.knockback_y = 0.0f;
                                obj.collision_flags |= CollisionFlags::kCeiling;
                            }
                            aabb_y = obj.GetAABB();
                        }
                    }
                }, plat_var);
#endif
            }

            // SemiSolid platform collision
            if (!hit_grounded && obj.drop_through_timer <= 0.0f && total_vy >= 0.0f) {
                float feet_prev_y = prev_y;
                float feet_curr_y = obj.y;

                for (const auto& plat_var : platforms_) {
#if defined(__GNUC__) && !defined(__clang__)
                    if (auto* plat = std::get_if<SemiSolidPlatform>(&plat_var)) {
                        float p_min_x = obj.x - obj.width / 2.0f;
                        float p_max_x = obj.x + obj.width / 2.0f;

                        if (p_max_x > plat->x1 && p_min_x < plat->x2) {
                            if (feet_prev_y <= plat->y1 && feet_curr_y >= plat->y1) {
                                obj.y = plat->y1;
                                obj.vy = 0.0f;
                                obj.knockback_y = 0.0f;
                                obj.collision_flags |= (CollisionFlags::kFloor | CollisionFlags::kOnSemisolid);
                                break;
                            }
                        }
                    }
#else
                    bool landed = false;
                    std::visit([&](const auto& plat) {
                        using T = std::decay_t<decltype(plat)>;
                        if constexpr (std::is_same_v<T, SemiSolidPlatform>) {
                            float p_min_x = obj.x - obj.width / 2.0f;
                            float p_max_x = obj.x + obj.width / 2.0f;

                            if (p_max_x > plat.x1 && p_min_x < plat.x2) {
                                if (feet_prev_y <= plat.y1 && feet_curr_y >= plat.y1) {
                                    obj.y = plat.y1;
                                    obj.vy = 0.0f;
                                    obj.knockback_y = 0.0f;
                                    obj.collision_flags |= (CollisionFlags::kFloor | CollisionFlags::kOnSemisolid);
                                    landed = true;
                                }
                            }
                        }
                    }, plat_var);
                    if (landed) break;
#endif
                }
            }
        }
    }
}  // namespace ab
