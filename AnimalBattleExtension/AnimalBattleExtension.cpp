#include <cstdint>
#include <omp.h>

import PhysicsTypes;

namespace ab::extension {

void BatchBulletMovementInternal(
    ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    float delta_time,
    float min_x, float max_x, float min_y, float max_y,
    uint8_t* out_despawn_flags) noexcept {
    if (transforms == nullptr || velocities == nullptr || count <= 0) {
        return;
    }

#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp simd
#endif
    for (int i = 0; i < count; ++i) {
        transforms[i].position.x += velocities[i].value.x * delta_time;
        transforms[i].position.y += velocities[i].value.y * delta_time;

        if (out_despawn_flags != nullptr) {
            out_despawn_flags[i] = (transforms[i].position.x < min_x || transforms[i].position.x > max_x ||
                                    transforms[i].position.y < min_y || transforms[i].position.y > max_y) ? 1 : 0;
        }
    }
}

}  // namespace ab::extension

extern "C" {

__declspec(dllexport) void Extension_BatchBulletMovement(
    ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    float delta_time,
    float min_x, float max_x, float min_y, float max_y,
    uint8_t* out_despawn_flags) noexcept {
    try {
        ab::extension::BatchBulletMovementInternal(
            transforms, velocities, count, delta_time,
            min_x, max_x, min_y, max_y, out_despawn_flags);
    } catch (...) {
    }
}

}
