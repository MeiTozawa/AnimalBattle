module;

#include <cstdint>
#include <omp.h>

export module BatchPhysics;

export import PhysicsTypes;

export namespace ab::extension {

/// @brief 大規模な弾丸 Transform および Velocity 配列に対して、SIMD / OpenMP 並列による一括物理移動と画面外境界判定を実行します。
/// @param transforms Transform 連続メモリ配列
/// @param velocities Velocity 連続メモリ配列
/// @param count 処理対象の弾丸数
/// @param delta_time フレーム経過時間 (秒)
/// @param min_x 画面最小 X 境界
/// @param max_x 画面最大 X 境界
/// @param min_y 画面最小 Y 境界
/// @param max_y 画面最大 Y 境界
/// @param out_despawn_flags 画面外デスポーンフラグ配列への出力ポインタ（1: デスポーン, 0: 生存）
void BatchBulletMovement(
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
