module;

#include <cmath>
#include <algorithm>
#include <string>
#include <fstream>
#include <vector>

module PhysicsEngine;

namespace ab {

    static bool CheckOverlap(const PhysicsObject::AABB& a, const Platform& p) {
        return a.minX < p.x2 && a.maxX > p.x1 && a.minY < p.y2 && a.maxY > p.y1;
    }

    void PhysicsEngine::LoadConfig(const std::string& filename) {
        std::vector<std::string> paths = {
            filename,
            "../" + filename,
            "../../" + filename,
            "../../../" + filename,
            "../../../../" + filename
        };
        std::ifstream file;
        for (const auto& path : paths) {
            file.open(path);
            if (file.is_open()) {
                break;
            }
        }
        if (!file.is_open()) {
            return;
        }

        std::string line;
        while (std::getline(file, line)) {
            // Strip comments
            size_t commentPos = line.find('#');
            if (commentPos != std::string::npos) {
                line = line.substr(0, commentPos);
            }
            
            size_t eqPos = line.find('=');
            if (eqPos == std::string::npos) continue;
            
            std::string key = line.substr(0, eqPos);
            std::string valueStr = line.substr(eqPos + 1);
            
            // Trim whitespace
            auto trim = [](std::string& s) {
                s.erase(s.begin(), std::find_if(s.begin(), s.end(), [](unsigned char ch) {
                    return !std::isspace(ch);
                }));
                s.erase(std::find_if(s.rbegin(), s.rend(), [](unsigned char ch) {
                    return !std::isspace(ch);
                }).base(), s.end());
            };
            
            trim(key);
            trim(valueStr);
            
            if (key.empty() || valueStr.empty()) continue;
            
            try {
                float val = std::stof(valueStr);
                if (key == "gravity") config.gravity = val;
                else if (key == "terminalVelocity") config.terminalVelocity = val;
                else if (key == "fastFallVelocity") config.fastFallVelocity = val;
                else if (key == "walkAcceleration") config.walkAcceleration = val;
                else if (key == "runAcceleration") config.runAcceleration = val;
                else if (key == "maxWalkSpeed") config.maxWalkSpeed = val;
                else if (key == "maxRunSpeed") config.maxRunSpeed = val;
                else if (key == "airAcceleration") config.airAcceleration = val;
                else if (key == "maxAirSpeed") config.maxAirSpeed = val;
                else if (key == "groundFriction") config.groundFriction = val;
                else if (key == "airDrag") config.airDrag = val;
                else if (key == "knockbackDecay") config.knockbackDecay = val;
                else if (key == "jumpForce") config.jumpForce = val;
                else if (key == "doubleJumpForce") config.doubleJumpForce = val;
                else if (key == "maxDeltaTime") config.maxDeltaTime = val;
                else if (key == "dropThroughDuration") config.dropThroughDuration = val;
                else if (key == "dropThroughPushOffset") config.dropThroughPushOffset = val;
                else if (key == "hitstunAirControlScale") config.hitstunAirControlScale = val;
                else if (key == "runThreshold") config.runThreshold = val;
                else if (key == "fastFallStartMultiplier") config.fastFallStartMultiplier = val;
                else if (key == "landingTolerance") config.landingTolerance = val;
                else if (key == "kbWeightOffset") config.kbWeightOffset = val;
                else if (key == "kbDamageScale1") config.kbDamageScale1 = val;
                else if (key == "kbDamageScale2") config.kbDamageScale2 = val;
                else if (key == "kbScaleMultiplier") config.kbScaleMultiplier = val;
                else if (key == "kbBaseMultiplier") config.kbBaseMultiplier = val;
                else if (key == "hitstunMultiplier") config.hitstunMultiplier = val;
                else if (key == "minHitstun") config.minHitstun = val;
                else if (key == "respawnY") config.respawnY = val;
            } catch (...) {
                // Ignore parse errors
            }
        }
    }

    void PhysicsEngine::Step(float dt) {
        LoadConfig("Config/physics.toml");

        if (dt <= 0.0f) return;
        if (dt > config.maxDeltaTime) dt = config.maxDeltaTime; // clamp delta time to prevent giant teleports

        for (auto& obj : objects) {
            // --- 1. State & Timers Updates ---
            if (obj.dropThroughTimer > 0.0f) {
                obj.dropThroughTimer -= dt;
            }
            if (obj.hitstunTimer > 0.0f) {
                obj.hitstunTimer -= dt;
            }

            // --- 2. Decay Knockback ---
            float kbSpeed = std::hypot(obj.kbx, obj.kby);
            if (kbSpeed > 0.0f) {
                float decay = config.knockbackDecay * dt;
                if (kbSpeed <= decay) {
                    obj.kbx = 0.0f;
                    obj.kby = 0.0f;
                } else {
                    obj.kbx -= (obj.kbx / kbSpeed) * decay;
                    obj.kby -= (obj.kby / kbSpeed) * decay;
                }
            }

            // --- 3. Move and Resolve Collisions ---
            float prevY = obj.y;
            obj.collisionFlags = COLLISION_NONE;

            // Total velocities (vx and vy are calculated in C# state machine)
            float totalVx = obj.vx + obj.kbx;
            float totalVy = obj.vy + obj.kby;

            // Step X and resolve
            obj.x += totalVx * dt;
            auto aabbX = obj.GetAABB();
            for (const auto& plat : platforms) {
                if (plat.type != PlatformType::Solid) continue;
                if (CheckOverlap(aabbX, plat)) {
                    float leftOverlap = aabbX.maxX - plat.x1;
                    float rightOverlap = plat.x2 - aabbX.minX;
                    if (leftOverlap < rightOverlap) {
                        obj.x -= leftOverlap;
                        obj.collisionFlags |= COLLISION_WALL_RIGHT;
                    } else {
                        obj.x += rightOverlap;
                        obj.collisionFlags |= COLLISION_WALL_LEFT;
                    }
                    obj.vx = 0.0f;
                    obj.kbx = 0.0f;
                    aabbX = obj.GetAABB();
                }
            }

            // Step Y and resolve
            obj.y += totalVy * dt;
            auto aabbY = obj.GetAABB();
            
            bool hitGrounded = false;
            // Check solid platforms
            for (const auto& plat : platforms) {
                if (plat.type != PlatformType::Solid) continue;
                if (CheckOverlap(aabbY, plat)) {
                    float topOverlap = aabbY.maxY - plat.y1;
                    float bottomOverlap = plat.y2 - aabbY.minY;
                    if (topOverlap < bottomOverlap) {
                        // Land
                        obj.y -= topOverlap;
                        obj.vy = 0.0f;
                        obj.kby = 0.0f;
                        obj.collisionFlags |= COLLISION_FLOOR;
                        hitGrounded = true;
                    } else {
                        // Ceiling hit
                        obj.y += bottomOverlap;
                        obj.vy = 0.0f;
                        obj.kby = 0.0f;
                        obj.collisionFlags |= COLLISION_CEILING;
                    }
                    aabbY = obj.GetAABB();
                }
            }

            // Check semi-solid platforms (pass-through from below/sides, land from above)
            if (!hitGrounded && obj.dropThroughTimer <= 0.0f && totalVy >= 0.0f) {
                float feetPrevY = prevY;
                float feetCurrY = obj.y;

                for (const auto& plat : platforms) {
                    if (plat.type != PlatformType::SemiSolid) continue;

                    float pMinX = obj.x - obj.width / 2.0f;
                    float pMaxX = obj.x + obj.width / 2.0f;

                    if (pMaxX > plat.x1 && pMinX < plat.x2) {
                        // Did character cross the platform surface downwards?
                        if (feetPrevY <= plat.y1 && feetCurrY >= plat.y1) {
                            obj.y = plat.y1;
                            obj.vy = 0.0f;
                            obj.kby = 0.0f;
                            obj.collisionFlags |= COLLISION_FLOOR;
                            break;
                        }
                    }
                }
            }
        }
    }

}
