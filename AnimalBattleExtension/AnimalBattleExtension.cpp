#include <cstdint>

import BatchPhysics;
import BossTrajectory;
import ComputeTypes;
import Easing;
import SpecialBulletMath;
import BatchComputeProcessor;

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
    ab::EaseType ease_type,
    ab::Vec2f* out_pos) noexcept {
    try {
        if (!start_pos || !target_pos || !out_pos) {
            return;
        }
        *out_pos = CalculateBossDiveRamPosition(
            *start_pos, *target_pos, state_timer, state_duration, ease_type);
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

__declspec(dllexport) void Extension_ProcessBatchComputePacket(
    BatchComputePacket* packet) noexcept {
    try {
        ProcessBatchComputePacket(packet);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_ProcessBatchEasing(
    EaseBatchItem* items,
    int32_t count) noexcept {
    try {
        ProcessBatchEasing(items, count);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_ProcessBatchSpecialBullets(
    SpecialBulletBatchItem* items,
    int32_t count) noexcept {
    try {
        ProcessBatchSpecialBullets(items, count);
    } catch (...) {
    }
}

__declspec(dllexport) float Extension_EvaluateEase(
    ab::EaseType ease_type,
    float progress) noexcept {
    try {
        return EvaluateEase(ease_type, progress);
    } catch (...) {
        return progress;
    }
}

__declspec(dllexport) void Extension_CalculateSpecialBulletModifier(
    int32_t type,
    float elapsed_timer,
    float base_vx,
    float base_vy,
    float base_radius,
    float* out_vx,
    float* out_vy,
    float* out_radius) noexcept {
    try {
        CalculateSpecialBulletModifier(
            type, elapsed_timer, base_vx, base_vy, base_radius,
            out_vx, out_vy, out_radius);
    } catch (...) {
        if (out_vx) *out_vx = base_vx;
        if (out_vy) *out_vy = base_vy;
        if (out_radius) *out_radius = base_radius;
    }
}

__declspec(dllexport) void Extension_CalculateOrbitPosition(
    float center_x,
    float center_y,
    float orbit_distance,
    float angle,
    float* out_x,
    float* out_y) noexcept {
    try {
        CalculateOrbitPosition(center_x, center_y, orbit_distance, angle, out_x, out_y);
    } catch (...) {
        if (out_x) *out_x = center_x;
        if (out_y) *out_y = center_y;
    }
}

}
