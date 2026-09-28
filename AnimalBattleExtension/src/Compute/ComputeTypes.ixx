module;

#include <cstdint>

export module ComputeTypes;

export import PhysicsTypes;

export namespace ab::extension {

/// @brief イージング一括計算バッチ要求・結果構造体 (12 bytes, C# EaseBatchItem と完全バイナリ一致)
struct EaseBatchItem {
    ab::EaseType ease_type;  // 4 bytes
    float progress;          // 4 bytes
    float result;            // 4 bytes (C++ 出力先)
};

/// @brief 特殊弾幕変調一括計算バッチ要求・結果構造体 (32 bytes, 8 floats, AVX2 SIMD 整合)
struct SpecialBulletBatchItem {
    int32_t type;            // 4 bytes (BulletType: 0:Normal, 1:Magenta, 2:Yellow, 3:Cyan, 4:Black)
    float elapsed_timer;     // 4 bytes
    float base_vx;           // 4 bytes
    float base_vy;           // 4 bytes
    float base_radius;       // 4 bytes
    float out_vx;            // 4 bytes (C++ 出力先)
    float out_vy;            // 4 bytes (C++ 出力先)
    float out_radius;        // 4 bytes (C++ 出力先)
};

}  // namespace ab::extension
