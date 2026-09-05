module;

#include <cstdint>

export module BatchComputeProcessor;

export import ComputeTypes;
import Easing;
import SpecialBulletMath;

export namespace ab::extension {

/// @brief イージング要求配列の一括並列評価を実行します。
void ProcessBatchEasing(EaseBatchItem* items, int32_t count) noexcept {
    if (!items || count <= 0) return;

#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp simd
#endif
    for (int32_t i = 0; i < count; ++i) {
        items[i].result = EvaluateEase(items[i].ease_type, items[i].progress);
    }
}

/// @brief 特殊弾幕変調配列の一括並列計算（AVX2/SIMD）を実行します。
void ProcessBatchSpecialBullets(SpecialBulletBatchItem* items, int32_t count) noexcept {
    if (!items || count <= 0) return;

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

/// @brief 統合バッチパケットに含まれる全計算項目を一括実行します。
void ProcessBatchComputePacket(BatchComputePacket* packet) noexcept {
    if (!packet) return;

    if (packet->ease_items && packet->ease_count > 0) {
        ProcessBatchEasing(packet->ease_items, packet->ease_count);
    }
    if (packet->bullet_items && packet->bullet_count > 0) {
        ProcessBatchSpecialBullets(packet->bullet_items, packet->bullet_count);
    }
}

}  // namespace ab::extension
