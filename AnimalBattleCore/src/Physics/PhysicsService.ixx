module;

#include <cstdint>

export module PhysicsService;

export import PhysicsTypes;

namespace ab {

/// @brief バックエンド非依存の幾何・物理計算サービス抽象インターフェース
export class IPhysicsService {
public:
    virtual ~IPhysicsService() = default;

    // --- 地形・足場・衝突応答計算 ---
    virtual GroundCheckResult CheckPlatformGround(float prev_y, float next_y, const Box2D& player_box, const Box2D& platform_box) const noexcept = 0;
};

}  // namespace ab
