module;

#include <algorithm>
#include <cmath>

export module BossTrajectory;

export import PhysicsTypes;

export namespace ab::extension {

/// @brief Boss 懸停調和軌跡（雙軸正弦/餘弦波動 + 玩家追隨權重 + 平滑 Lerp 插值）計算。
/// @param current_pos 當前 Boss 坐標
/// @param base_pos 基準錨點坐標 (base_x, hover_base_y)
/// @param player_pos 玩家當前坐標 (player_x, player_y)
/// @param hover_timer 懸停累計計時器
/// @param hover_speed_x X 軸波動角速度
/// @param hover_speed_y Y 軸波動角速度
/// @param hover_amp_x X 軸波動振幅
/// @param hover_amp_y Y 軸波動振幅
/// @param player_pursuit_weight 玩家位置追隨影響權重
/// @param hover_lerp_speed 坐標平滑追隨插值速度
/// @param delta_time 幀間隔時間 (秒)
/// @return 下一幀 Boss 懸停坐標
ab::Vec2f CalculateBossHoverPosition(
    ab::Vec2f current_pos,
    ab::Vec2f base_pos,
    ab::Vec2f player_pos,
    float hover_timer,
    float hover_speed_x,
    float hover_speed_y,
    float hover_amp_x,
    float hover_amp_y,
    float player_pursuit_weight,
    float hover_lerp_speed,
    float delta_time) noexcept {
    float target_x = base_pos.x + std::sin(hover_timer * hover_speed_x) * hover_amp_x +
                     (player_pos.x - base_pos.x) * player_pursuit_weight;
    float target_y = base_pos.y + std::cos(hover_timer * hover_speed_y) * hover_amp_y;
    float lerp_factor = std::min(1.0f, hover_lerp_speed * delta_time);

    float next_x = current_pos.x + (target_x - current_pos.x) * lerp_factor;
    float next_y = current_pos.y + (target_y - current_pos.y) * lerp_factor;
    return ab::Vec2f{next_x, next_y};
}

/// @brief Boss 俯衝突進 (DiveRam) 軌跡（基於 InQuad 二次緩動曲線的起止點插值）計算。
/// @param start_pos 俯衝起始坐標
/// @param target_pos 鎖定的目標坐標（玩家鎖定點）
/// @param state_timer 當前技能狀態計時器
/// @param state_duration 技能突進總時長
/// @return 當前幀突進插值坐標
ab::Vec2f CalculateBossDiveRamPosition(
    ab::Vec2f start_pos,
    ab::Vec2f target_pos,
    float state_timer,
    float state_duration) noexcept {
    float duration = std::max(0.01f, state_duration);
    float progress = std::clamp(state_timer / duration, 0.0f, 1.0f);
    float ease_progress = progress * progress;  // InQuad 緩動

    float next_x = start_pos.x + (target_pos.x - start_pos.x) * ease_progress;
    float next_y = start_pos.y + (target_pos.y - start_pos.y) * ease_progress;
    return ab::Vec2f{next_x, next_y};
}

/// @brief Boss 回程/平滑歸位軌跡（向目標錨點的指數平滑插值）計算。
/// @param current_pos 當前 Boss 坐標
/// @param target_base_pos 回程目標錨點 (base_x, hover_base_y)
/// @param return_speed 回程飛行速度系數
/// @param delta_time 幀間隔時間 (秒)
/// @return 下一幀 Boss 回程坐標
ab::Vec2f CalculateBossReturnPosition(
    ab::Vec2f current_pos,
    ab::Vec2f target_base_pos,
    float return_speed,
    float delta_time) noexcept {
    float factor = std::min(1.0f, return_speed * delta_time);
    float next_x = current_pos.x + (target_base_pos.x - current_pos.x) * factor;
    float next_y = current_pos.y + (target_base_pos.y - current_pos.y) * factor;
    return ab::Vec2f{next_x, next_y};
}

}  // namespace ab::extension
