module;

export module DefaultPhysicsService;

export import PhysicsService;

namespace ab {

/// @brief 純粋幾何・数学・物理計算を提供するデフォルトの高速物理サービス具象クラス
export class DefaultPhysicsService : public IPhysicsService {
public:
    DefaultPhysicsService() = default;

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
};

}  // namespace ab
