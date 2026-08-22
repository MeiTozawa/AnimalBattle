module;

#include <algorithm>
#include <cmath>
#include <numbers>

export module BossTrajectory;

export import PhysicsTypes;

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

/// @brief OutBounce 補間係数を計算します。
constexpr float EvaluateOutBounce(float x) noexcept {
    constexpr float n1 = 7.5625f;
    constexpr float d1 = 2.75f;
    if (x < 1.0f / d1) {
        return n1 * x * x;
    }
    if (x < 2.0f / d1) {
        x -= 1.5f / d1;
        return n1 * x * x + 0.75f;
    }
    if (x < 2.5f / d1) {
        x -= 2.25f / d1;
        return n1 * x * x + 0.9375f;
    }
    x -= 2.625f / d1;
    return n1 * x * x + 0.984375f;
}

/// @brief OutElastic 補間係数を計算します。
float EvaluateOutElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c4 = (2.0f * PI) / 3.0f;
    return std::pow(2.0f, -10.0f * t) * std::sin((t * 10.0f - 0.75f) * c4) + 1.0f;
}

/// @brief InElastic 補間係数を計算します。
float EvaluateInElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c4 = (2.0f * PI) / 3.0f;
    return -std::pow(2.0f, 10.0f * t - 10.0f) * std::sin((t * 10.0f - 10.75f) * c4);
}

/// @brief InOutElastic 補間係数を計算します。
float EvaluateInOutElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c5 = (2.0f * PI) / 4.5f;
    return t < 0.5f
        ? -(std::pow(2.0f, 20.0f * t - 10.0f) * std::sin((20.0f * t - 11.125f) * c5)) * 0.5f
        : (std::pow(2.0f, -20.0f * t + 10.0f) * std::sin((20.0f * t - 11.125f) * c5)) * 0.5f + 1.0f;
}

/// @brief 指定されたイージング型および進捗割合から補間値を計算します。
export float EvaluateEase(ab::EaseType ease_type, float progress) noexcept {
    float p = std::clamp(progress, 0.0f, 1.0f);

    switch (ease_type) {
        case ab::EaseType::Linear:
            return p;
        case ab::EaseType::InSine:
            return 1.0f - std::cos(p * PI * 0.5f);
        case ab::EaseType::OutSine:
            return std::sin(p * PI * 0.5f);
        case ab::EaseType::InOutSine:
            return -(std::cos(PI * p) - 1.0f) * 0.5f;
        case ab::EaseType::InQuad:
            return p * p;
        case ab::EaseType::OutQuad:
            return 1.0f - (1.0f - p) * (1.0f - p);
        case ab::EaseType::InOutQuad:
            return p < 0.5f ? 2.0f * p * p : 1.0f - std::pow(-2.0f * p + 2.0f, 2.0f) * 0.5f;
        case ab::EaseType::InCubic:
            return p * p * p;
        case ab::EaseType::OutCubic:
            return 1.0f - std::pow(1.0f - p, 3.0f);
        case ab::EaseType::InOutCubic:
            return p < 0.5f ? 4.0f * p * p * p : 1.0f - std::pow(-2.0f * p + 2.0f, 3.0f) * 0.5f;
        case ab::EaseType::InExpo:
            return p <= 0.0f ? 0.0f : std::pow(2.0f, 10.0f * p - 10.0f);
        case ab::EaseType::OutExpo:
            return p >= 1.0f ? 1.0f : 1.0f - std::pow(2.0f, -10.0f * p);
        case ab::EaseType::InOutExpo:
            if (p <= 0.0f) return 0.0f;
            if (p >= 1.0f) return 1.0f;
            return p < 0.5f ? std::pow(2.0f, 20.0f * p - 10.0f) * 0.5f : (2.0f - std::pow(2.0f, -20.0f * p + 10.0f)) * 0.5f;
        case ab::EaseType::InElastic:
            return EvaluateInElastic(p);
        case ab::EaseType::OutElastic:
            return EvaluateOutElastic(p);
        case ab::EaseType::InOutElastic:
            return EvaluateInOutElastic(p);
        case ab::EaseType::InBounce:
            return 1.0f - EvaluateOutBounce(1.0f - p);
        case ab::EaseType::OutBounce:
            return EvaluateOutBounce(p);
        case ab::EaseType::InOutBounce:
            return p < 0.5f
                ? (1.0f - EvaluateOutBounce(1.0f - 2.0f * p)) * 0.5f
                : (1.0f + EvaluateOutBounce(2.0f * p - 1.0f)) * 0.5f;
        default:
            return p * p;
    }
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
