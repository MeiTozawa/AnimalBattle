module;

#include <vector>
#include <memory>
#include <cmath>
#include <algorithm>
#include <variant>
#include <concepts>

export module PhysicsEngine;

import PlatformConcept;

namespace ab {

    export enum class PlatformType : int {
        Solid = 0,
        SemiSolid = 1
    };

    export enum class CollisionFlags : int {
        kNone = 0,
        kFloor = 1 << 0,
        kCeiling = 1 << 1,
        kWallLeft = 1 << 2,
        kWallRight = 1 << 3,
        kOnSemisolid = 1 << 4
    };

    export constexpr CollisionFlags operator|(CollisionFlags a, CollisionFlags b) noexcept {
        return static_cast<CollisionFlags>(static_cast<int>(a) | static_cast<int>(b));
    }

    export constexpr CollisionFlags operator&(CollisionFlags a, CollisionFlags b) noexcept {
        return static_cast<CollisionFlags>(static_cast<int>(a) & static_cast<int>(b));
    }

    export constexpr CollisionFlags& operator|=(CollisionFlags& a, CollisionFlags b) noexcept {
        a = a | b;
        return a;
    }

    export constexpr CollisionFlags& operator&=(CollisionFlags& a, CollisionFlags& b) noexcept {
        a = a & b;
        return a;
    }

    export constexpr CollisionFlags operator~(CollisionFlags a) noexcept {
        return static_cast<CollisionFlags>(~static_cast<int>(a));
    }

    export struct PhysicsObject {
        int id = 0;
        
        // Position
        float x = 0.0f;
        float y = 0.0f;
        
        // Controlled velocity
        float vx = 0.0f;
        float vy = 0.0f;
        
        // Knockback velocity
        float knockback_x = 0.0f;
        float knockback_y = 0.0f;
        
        // Bounding box size (width & height, anchored at bottom-center of player)
        float width = 40.0f;
        float height = 70.0f;
        
        // Character attributes
        float weight = 100.0f;
        float damage_percent = 0.0f;
        float hitstun_timer = 0.0f;
        float drop_through_timer = 0.0f;
        CollisionFlags collision_flags = CollisionFlags::kNone; 
        
        struct AABB {
            float min_x = 0.0f;
            float min_y = 0.0f;
            float max_x = 0.0f;
            float max_y = 0.0f;
        };
        
        AABB GetAABB() const {
            return AABB{
                x - width / 2.0f,
                y - height,
                x + width / 2.0f,
                y
            };
        }
    };

    // --- Platform Variants and Concept ---

    export struct SolidPlatform {
        int id = 0;
        float x1 = 0.0f;
        float y1 = 0.0f;
        float x2 = 0.0f;
        float y2 = 0.0f;
    };

    export struct SemiSolidPlatform {
        int id = 0;
        float x1 = 0.0f;
        float y1 = 0.0f;
        float x2 = 0.0f;
        float y2 = 0.0f;
    };

    export using PlatformVariant = std::variant<SolidPlatform, SemiSolidPlatform>;

    static_assert(PlatformConcept<SolidPlatform>, "SolidPlatform must satisfy PlatformConcept");
    static_assert(PlatformConcept<SemiSolidPlatform>, "SemiSolidPlatform must satisfy PlatformConcept");

    export class PhysicsEngine {
    public:
        PhysicsEngine() = default;
        ~PhysicsEngine() = default;

        int AddObject(float x, float y, float width, float height, float weight) {
            PhysicsObject obj;
            obj.id = next_object_id_++;
            obj.x = x;
            obj.y = y;
            obj.width = width;
            obj.height = height;
            obj.weight = weight;
            objects_.push_back(obj);
            return obj.id;
        }

        bool RemoveObject(int id) {
            auto it = std::remove_if(objects_.begin(), objects_.end(), [id](const auto& obj) { return obj.id == id; });
            if (it != objects_.end()) {
                objects_.erase(it, objects_.end());
                return true;
            }
            return false;
        }

        PhysicsObject* GetObject(int id) {
            for (auto& obj : objects_) {
                if (obj.id == id) return &obj;
            }
            return nullptr;
        }

        int AddPlatform(float x1, float y1, float x2, float y2, PlatformType type) {
            int id = next_platform_id_++;
            if (type == PlatformType::Solid) {
                platforms_.push_back(SolidPlatform{id, x1, y1, x2, y2});
            } else {
                platforms_.push_back(SemiSolidPlatform{id, x1, y1, x2, y2});
            }
            return id;
        }

        bool RemovePlatform(int id) {
            auto it = std::remove_if(platforms_.begin(), platforms_.end(), [id](const auto& plat) {
                return std::visit([id](const auto& p) { return p.id == id; }, plat);
            });
            if (it != platforms_.end()) {
                platforms_.erase(it, platforms_.end());
                return true;
            }
            return false;
        }

        const std::vector<PhysicsObject>& GetObjects() const { return objects_; }
        const std::vector<PlatformVariant>& GetPlatforms() const { return platforms_; }

        void Clear() {
            objects_.clear();
            platforms_.clear();
            next_object_id_ = 1;
            next_platform_id_ = 1;
        }

        void ApplyKnockback(int object_id, float damage, float angle_rad, float base_knockback, float scaling) {
            auto* obj = GetObject(object_id);
            if (!obj) return;

            obj->damage_percent += damage;

            constexpr float KNOCKBACK_WEIGHT_OFFSET = 100.0f;
            constexpr float KNOCKBACK_DAMAGE_SCALE_1 = 10.0f;
            constexpr float KNOCKBACK_DAMAGE_SCALE_2 = 20.0f;
            constexpr float KNOCKBACK_SCALE_MULTIPLIER = 1.4f;
            constexpr float KNOCKBACK_BASE_MULTIPLIER = 18.0f;
            constexpr float HITSTUN_MULTIPLIER = 0.00667f;
            constexpr float MIN_HITSTUN = 0.1f;

            float percent = obj->damage_percent;
            float weight_term = KNOCKBACK_WEIGHT_OFFSET / (obj->weight + KNOCKBACK_WEIGHT_OFFSET);
            
            float knockback_magnitude = (((percent / KNOCKBACK_DAMAGE_SCALE_1 + (percent * damage) / KNOCKBACK_DAMAGE_SCALE_2) * weight_term * KNOCKBACK_SCALE_MULTIPLIER) + KNOCKBACK_BASE_MULTIPLIER) * scaling + base_knockback;

            float knockback_vx = knockback_magnitude * std::cos(angle_rad);
            float knockback_vy = -knockback_magnitude * std::sin(angle_rad);

            obj->knockback_x = knockback_vx;
            obj->knockback_y = knockback_vy;

            obj->hitstun_timer = std::max(knockback_magnitude * HITSTUN_MULTIPLIER, MIN_HITSTUN);
        }

        void SetKnockbackDecay(float decay) { knockback_decay_ = decay; }
        void SetMaxDeltaTime(float max_dt) { max_delta_time_ = max_dt; }

        void Step(float delta_time);

    private:
        float knockback_decay_ = 400.0f;
        float max_delta_time_ = 0.1f;

        std::vector<PhysicsObject> objects_;
        std::vector<PlatformVariant> platforms_;
        int next_object_id_ = 1;
        int next_platform_id_ = 1;
    };

}  // namespace ab