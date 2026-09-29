#include <cstdint>

import BatchPhysics;
import ComputeTypes;
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

__declspec(dllexport) void Extension_ProcessBatchCompute(
    EaseBatchItem* ease_items,
    int32_t ease_count,
    SpecialBulletBatchItem* bullet_items,
    int32_t bullet_count) noexcept {
    try {
        ProcessBatchCompute(ease_items, ease_count, bullet_items, bullet_count);
    } catch (...) {
    }
}

}
