module;

#include <algorithm>
#include <cstdint>

export module BatchComputeProcessor;

export import ComputeTypes;
import SpecialBulletMath;

export namespace ab::extension {

/// @brief 特殊弾幕変調配列の一括並列計算（AVX2/SIMD）を実行します。
void ProcessBatchSpecialBullets(SpecialBulletBatchItem* items, int32_t count) noexcept {
    if (!items || count <= 0) return;

    // 高負荷計算：GCC は OpenMP で多スレッド化。MSVC は C++23 モジュールと /openmp の競合を避けて AVX2 ベクトル化
#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp parallel for
#endif
    for (int32_t i = 0; i < count; ++i) {
        CalculateSpecialBulletModifier(
            items[i].type,
            items[i].elapsed_timer,
            items[i].base_vx,
            items[i].base_vy,
            items[i].base_radius,
            &items[i].out_vx,
            &items[i].out_vy,
            &items[i].out_radius);
    }
}

/// @brief 特殊弾幕変調の一括計算を同期並列実行します（完全無状態純粋計算核）。
void ProcessBatchCompute(
    SpecialBulletBatchItem* bullet_items,
    int32_t bullet_count) noexcept {
    if (bullet_items && bullet_count > 0) {
        ProcessBatchSpecialBullets(bullet_items, bullet_count);
    }
}

}  // namespace ab::extension
