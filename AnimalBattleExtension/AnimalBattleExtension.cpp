#include <cstdint>

import BatchRender;
import BatchPhysics;
import BossTrajectory;

using namespace ab::extension;

extern "C" {

__declspec(dllexport) void Extension_BatchBulletMovement(
    ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    float delta_time,
    float min_x, float max_x, float min_y, float max_y,
    uint8_t* out_despawn_flags) noexcept {
    try {
        BatchBulletMovement(
            transforms, velocities, count, delta_time,
            min_x, max_x, min_y, max_y, out_despawn_flags);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_BatchDrawCircles(
    const ab::Transform* transforms,
    int count,
    int radius,
    unsigned int color,
    bool fill,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    try {
        BatchDrawCircles(
            transforms, count, radius, color, fill,
            clip_min_x, clip_max_x, clip_min_y, clip_max_y);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_BatchDrawPlayerBullets(
    const ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    int outer_radius,
    unsigned int outer_color,
    int inner_radius,
    unsigned int inner_color,
    int trail_offset,
    int trail_radius,
    unsigned int trail_color,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    try {
        BatchDrawPlayerBullets(
            transforms, velocities, count,
            outer_radius, outer_color,
            inner_radius, inner_color,
            trail_offset, trail_radius, trail_color,
            clip_min_x, clip_max_x, clip_min_y, clip_max_y);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_BatchDrawBossBullets(
    const ab::Transform* transforms,
    int count,
    int radius,
    unsigned int fill_color,
    unsigned int outline_color,
    int outline_padding,
    float core_radius_ratio,
    int core_min_radius,
    unsigned int core_color,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    try {
        BatchDrawBossBullets(
            transforms, count,
            radius, fill_color, outline_color,
            outline_padding, core_radius_ratio, core_min_radius, core_color,
            clip_min_x, clip_max_x, clip_min_y, clip_max_y);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_CalculateBossHoverPosition(
    const ab::Vec2f* current_pos,
    const ab::Vec2f* base_pos,
    const ab::Vec2f* player_pos,
    float hover_timer,
    float hover_speed_x,
    float hover_speed_y,
    float hover_amp_x,
    float hover_amp_y,
    float player_pursuit_weight,
    float hover_lerp_speed,
    float delta_time,
    ab::Vec2f* out_pos) noexcept {
    try {
        if (!current_pos || !base_pos || !player_pos || !out_pos) {
            return;
        }
        *out_pos = CalculateBossHoverPosition(
            *current_pos, *base_pos, *player_pos,
            hover_timer, hover_speed_x, hover_speed_y, hover_amp_x, hover_amp_y,
            player_pursuit_weight, hover_lerp_speed, delta_time);
    } catch (...) {
        if (current_pos && out_pos) {
            *out_pos = *current_pos;
        }
    }
}

__declspec(dllexport) void Extension_CalculateBossDiveRamPosition(
    const ab::Vec2f* start_pos,
    const ab::Vec2f* target_pos,
    float state_timer,
    float state_duration,
    ab::Vec2f* out_pos) noexcept {
    try {
        if (!start_pos || !target_pos || !out_pos) {
            return;
        }
        *out_pos = CalculateBossDiveRamPosition(
            *start_pos, *target_pos, state_timer, state_duration);
    } catch (...) {
        if (start_pos && out_pos) {
            *out_pos = *start_pos;
        }
    }
}

__declspec(dllexport) void Extension_CalculateBossReturnPosition(
    const ab::Vec2f* current_pos,
    const ab::Vec2f* target_base_pos,
    float return_speed,
    float delta_time,
    ab::Vec2f* out_pos) noexcept {
    try {
        if (!current_pos || !target_base_pos || !out_pos) {
            return;
        }
        *out_pos = CalculateBossReturnPosition(
            *current_pos, *target_base_pos, return_speed, delta_time);
    } catch (...) {
        if (current_pos && out_pos) {
            *out_pos = *current_pos;
        }
    }
}

}

