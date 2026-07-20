module;

#include <vector>
#include <memory>
#include <cmath>
#include <algorithm>
#include <string>
#include <fstream>

export module PhysicsEngine;

namespace ab {

    export enum class PlatformType : int {
        Solid = 0,
        SemiSolid = 1
    };

    export constexpr int COLLISION_NONE = 0;
    export constexpr int COLLISION_FLOOR = 1 << 0;
    export constexpr int COLLISION_CEILING = 1 << 1;
    export constexpr int COLLISION_WALL_LEFT = 1 << 2;
    export constexpr int COLLISION_WALL_RIGHT = 1 << 3;

    export struct PhysicsConfig {
        float gravity = 1500.0f;          // pixels/sec^2
        float terminalVelocity = 1000.0f; // max fall speed
        float fastFallVelocity = 1500.0f; // max fast fall speed
        float walkAcceleration = 800.0f;
        float runAcceleration = 1400.0f;
        float maxWalkSpeed = 300.0f;
        float maxRunSpeed = 600.0f;
        float airAcceleration = 800.0f;
        float maxAirSpeed = 450.0f;
        float groundFriction = 1200.0f;   // deceleration when no input
        float airDrag = 200.0f;           // air friction/drag
        float knockbackDecay = 400.0f;    // knockback velocity decay per second
        float jumpForce = 650.0f;
        float doubleJumpForce = 550.0f;

        // Configuration variables to eliminate magic numbers
        float maxDeltaTime = 0.1f;
        float dropThroughDuration = 0.25f;
        float dropThroughPushOffset = 3.0f;
        float hitstunAirControlScale = 0.2f;
        float runThreshold = 0.7f;
        float fastFallStartMultiplier = 0.6f;
        float landingTolerance = 0.05f;

        // Knockback formula coefficients
        float kbWeightOffset = 100.0f;
        float kbDamageScale1 = 10.0f;
        float kbDamageScale2 = 20.0f;
        float kbScaleMultiplier = 1.4f;
        float kbBaseMultiplier = 18.0f;
        float hitstunMultiplier = 0.00667f;
        float minHitstun = 0.1f;

        // Respawn height
        float respawnY = 150.0f;
    };

    export struct PhysicsObject {
        int id;
        
        // Position & Velocities
        float x = 0.0f;
        float y = 0.0f;
        
        // Controlled velocity (joystick/movement)
        float vx = 0.0f;
        float vy = 0.0f;
        
        // Knockback velocity (launches)
        float kbx = 0.0f;
        float kby = 0.0f;
        
        // Bounding box size (width & height, anchored at bottom-center of player)
        float width = 40.0f;
        float height = 70.0f;
        
        // Character attributes
        float weight = 100.0f; // weight affects knockback
        float damagePercent = 0.0f;
        float hitstunTimer = 0.0f; // seconds
        float dropThroughTimer = 0.0f; // Drop-through timer to temporarily ignore semi-solid platform collision
        int collisionFlags = 0; 
        
        // AABB box helper
        struct AABB {
            float minX, minY, maxX, maxY;
        };
        
        AABB GetAABB() const {
            // Anchor is bottom-center
            return AABB{
                x - width / 2.0f,
                y - height,
                x + width / 2.0f,
                y
            };
        }
    };

    export struct Platform {
        int id;
        float x1, y1, x2, y2; // AABB bounding box
        PlatformType type;
    };

    export class PhysicsEngine {
    private:
        PhysicsConfig config;
        std::vector<PhysicsObject> objects;
        std::vector<Platform> platforms;
        int nextObjectId = 1;
        int nextPlatformId = 1;

    public:
        PhysicsEngine() = default;
        ~PhysicsEngine() = default;



        int AddObject(float x, float y, float width, float height, float weight) {
            PhysicsObject obj;
            obj.id = nextObjectId++;
            obj.x = x;
            obj.y = y;
            obj.width = width;
            obj.height = height;
            obj.weight = weight;
            objects.push_back(obj);
            return obj.id;
        }

        bool RemoveObject(int id) {
            auto it = std::remove_if(objects.begin(), objects.end(), [id](const auto& obj) { return obj.id == id; });
            if (it != objects.end()) {
                objects.erase(it, objects.end());
                return true;
            }
            return false;
        }

        PhysicsObject* GetObject(int id) {
            for (auto& obj : objects) {
                if (obj.id == id) return &obj;
            }
            return nullptr;
        }

        int AddPlatform(float x1, float y1, float x2, float y2, PlatformType type) {
            Platform plat;
            plat.id = nextPlatformId++;
            plat.x1 = x1;
            plat.y1 = y1;
            plat.x2 = x2;
            plat.y2 = y2;
            plat.type = type;
            platforms.push_back(plat);
            return plat.id;
        }

        bool RemovePlatform(int id) {
            auto it = std::remove_if(platforms.begin(), platforms.end(), [id](const auto& plat) { return plat.id == id; });
            if (it != platforms.end()) {
                platforms.erase(it, platforms.end());
                return true;
            }
            return false;
        }

        const std::vector<PhysicsObject>& GetObjects() const { return objects; }
        const std::vector<Platform>& GetPlatforms() const { return platforms; }

        void Clear() {
            objects.clear();
            platforms.clear();
            nextObjectId = 1;
            nextPlatformId = 1;
        }

        // Apply knockback to an object
        void ApplyKnockback(int objectId, float damage, float angleRad, float baseKb, float scaling) {
            auto* obj = GetObject(objectId);
            if (!obj) return;

            obj->damagePercent += damage;

            float percent = obj->damagePercent;
            float weightTerm = config.kbWeightOffset / (obj->weight + config.kbWeightOffset);
            
            float kbMagnitude = (((percent / config.kbDamageScale1 + (percent * damage) / config.kbDamageScale2) * weightTerm * config.kbScaleMultiplier) + config.kbBaseMultiplier) * scaling + baseKb;

            // In typical 2D, angles are measured: cos(angleRad) is X, -sin(angleRad) is Y
            float kbvx = kbMagnitude * std::cos(angleRad);
            float kbvy = -kbMagnitude * std::sin(angleRad);

            obj->kbx = kbvx;
            obj->kby = kbvy;

            // Hitstun seconds
            obj->hitstunTimer = kbMagnitude * config.hitstunMultiplier;
            if (obj->hitstunTimer < config.minHitstun) obj->hitstunTimer = config.minHitstun;

        }

        void LoadConfig(const std::string& filename);
        void Step(float dt);
    };

}