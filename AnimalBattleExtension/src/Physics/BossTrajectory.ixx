module;

#include <algorithm>
#include <cmath>
#include <numbers>

export module BossTrajectory;

export import PhysicsTypes;
import Easing;

namespace ab::extension {

constexpr float PI = std::numbers::pi_v<float>;

}  // namespace ab::extension

export namespace ab::extension {

/// @brief Boss のホバリング調和軌道（2軸正弦・余弦波動＋プレイヤー追従重み＋平滑 Lerp 補間）を計算します。
/// @param current_pos 現在の Boss 座標
/// @param base_pos 基準アンカー座標 (base_x, hover_base_y)
/// @param player_pos プレイヤーの現在座標 (player_x, player_y)
/// @param hover_timer ホバリング累積タイマー
/// @param hover_speed_x X 軸波動角速度
/// @param hover_speed_y Y 軸波動角速度
/// @param hover_amp_x X 軸波動振幅
/// @param hover_amp_y Y 軸波動振幅
/// @param player_pursuit_weight プレイヤー位置追従の重み
/// @param hover_lerp_speed 座標追従の平滑補間速度
/// @param delta_time フレーム経過時間 (秒)
/// @return 次フレームの Boss ホバリング座標
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

/// @brief Boss の急降下突進 (DiveRam) 軌道（指定されたイージング曲線による始点・終点補間）を計算します。
/// @param start_pos 突進開始座標
/// @param target_pos ロックオン目標座標
/// @param state_timer 現在のスキル状態タイマー
/// @param state_duration スキル突進の合計持続時間
/// @param ease_type 適用するイージング関数種別
/// @return 現在フレームの突進補間座標
ab::Vec2f CalculateBossDiveRamPosition(
    ab::Vec2f start_pos,
    ab::Vec2f target_pos,
    float state_timer,
    float state_duration,
    ab::EaseType ease_type = ab::EaseType::InQuad) noexcept {
    float duration = std::max(0.01f, state_duration);
    float progress = std::clamp(state_timer / duration, 0.0f, 1.0f);
    float ease_progress = EvaluateEase(ease_type, progress);

    float next_x = start_pos.x + (target_pos.x - start_pos.x) * ease_progress;
    float next_y = start_pos.y + (target_pos.y - start_pos.y) * ease_progress;
    return ab::Vec2f{next_x, next_y};
}

/// @brief Boss の帰還・初期位置復帰軌道（目標アンカーへの指数平滑補間）を計算します。
/// @param current_pos 現在の Boss 座標
/// @param target_base_pos 帰還先の目標アンカー座標 (base_x, hover_base_y)
/// @param return_speed 帰還飛行速度係数
/// @param delta_time フレーム経過時間 (秒)
/// @return 次フレームの Boss 座標
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
