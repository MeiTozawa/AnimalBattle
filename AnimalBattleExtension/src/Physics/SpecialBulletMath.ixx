module;

#include <algorithm>
#include <cmath>
#include <cstdint>

export module SpecialBulletMath;

export namespace ab::extension {

constexpr float CYAN_WAVE_AMPLITUDE = 28.0f;
constexpr float CYAN_WAVE_FREQUENCY = 7.0f;
constexpr float MAGENTA_PULSE_AMPLITUDE = 0.35f;
constexpr float MAGENTA_PULSE_FREQUENCY = 6.0f;

/// @brief 単一の特殊弾幕（品紅・黄・青・黒）の周期アニメーションおよび法線速度・判定半径変調を計算します。
/// @param type 特殊弾幕種別 (0:Normal, 1:Magenta, 2:Yellow, 3:Cyan, 4:Black)
/// @param elapsed_timer 弾幕生成からの累積タイマー (秒)
/// @param base_vx 発射時基準 X 速度
/// @param base_vy 発射時基準 Y 速度
/// @param base_radius 基準半径 (px)
/// @param out_vx 変調後の X 速度出力先
/// @param out_vy 変調後の Y 速度出力先
/// @param out_radius 変調後の半径出力先
void CalculateSpecialBulletModifier(
    int32_t type,
    float elapsed_timer,
    float base_vx,
    float base_vy,
    float base_radius,
    float* out_vx,
    float* out_vy,
    float* out_radius) noexcept {
    float vx = base_vx;
    float vy = base_vy;
    float radius = base_radius;

    // 1. 青色 (3) および 黒色 (4)：進行方向に対する法線方向の左右正弦波往復
    if (type == 3 || type == 4) {
        float speed_sq = base_vx * base_vx + base_vy * base_vy;
        if (speed_sq > 1.0f) {
            float inv_speed = 1.0f / std::sqrt(speed_sq);
            float nx = -base_vy * inv_speed;
            float ny = base_vx * inv_speed;

            float wave_velocity = CYAN_WAVE_AMPLITUDE * CYAN_WAVE_FREQUENCY * std::cos(elapsed_timer * CYAN_WAVE_FREQUENCY);
            vx = base_vx + nx * wave_velocity;
            vy = base_vy + ny * wave_velocity;
        }
    }

    // 2. 品紅色 (1) および 黒色 (4)：呼吸拡縮脈動（当たり判定半径の動的更新）
    if (type == 1 || type == 4) {
        float scale = 1.0f + MAGENTA_PULSE_AMPLITUDE * std::sin(elapsed_timer * MAGENTA_PULSE_FREQUENCY);
        radius = std::max(2.0f, base_radius * scale);
    }

    if (out_vx) *out_vx = vx;
    if (out_vy) *out_vy = vy;
    if (out_radius) *out_radius = radius;
}

/// @brief 黄色および黒色弾幕の衛星小円の公転座標を計算します。
void CalculateOrbitPosition(
    float center_x,
    float center_y,
    float orbit_distance,
    float angle,
    float* out_x,
    float* out_y) noexcept {
    if (out_x) *out_x = center_x + std::cos(angle) * orbit_distance;
    if (out_y) *out_y = center_y + std::sin(angle) * orbit_distance;
}

}  // namespace ab::extension
