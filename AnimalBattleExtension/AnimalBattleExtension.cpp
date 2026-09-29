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
    if (!transforms || !velocities || !out_despawn_flags || count <= 0) {
        return;
    }

    try {
        BatchBulletMovement(
            transforms, velocities, count, delta_time,
            min_x, max_x, min_y, max_y, out_despawn_flags);
    } catch (...) {
    }
}

__declspec(dllexport) void Extension_ProcessBatchCompute(
    SpecialBulletBatchItem* bullet_items,
    int32_t bullet_count) noexcept {
    if (!bullet_items || bullet_count <= 0) {
        return;
    }

    try {
        ProcessBatchCompute(bullet_items, bullet_count);
    } catch (...) {
    }
}

}
