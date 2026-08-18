module;

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <omp.h>
#include "Core/EngineCore.h"

export module BatchRender;

export import RenderService;
export import PhysicsTypes;

export namespace ab::extension::render {

/// @brief 大量の円（弾幕・パーティクル等）に対して、SIMD 並列視口カリングと一括描画を実行します。
/// @param transforms Transform 連続メモリ配列
/// @param count 処理対象の円の個数
/// @param radius 半径 (px)
/// @param color 描画カラー (0xAARRGGBB)
/// @param fill 塗りつぶすかどうか
/// @param clip_min_x 画面外カリング最小 X
/// @param clip_max_x 画面外カリング最大 X
/// @param clip_min_y 画面外カリング最小 Y
/// @param clip_max_y 画面外カリング最大 Y
void BatchDrawCircles(
    const ab::Transform* transforms,
    int count,
    int radius,
    uint32_t color,
    bool fill,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    if (transforms == nullptr || count <= 0) {
        return;
    }
    auto* render = ab::GetRenderService();
    if (render == nullptr) {
        return;
    }

#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp simd
#endif
    for (int i = 0; i < count; ++i) {
        int x = static_cast<int>(transforms[i].position.x);
        int y = static_cast<int>(transforms[i].position.y);
        if (x < clip_min_x || x > clip_max_x || y < clip_min_y || y > clip_max_y) {
            continue;
        }
        render->DrawCircle({x, y}, radius, color, fill);
    }
}

/// @brief プレイヤー銃弾（発光外層＋内層コア＋ビームトレイル）に対して、SIMD 並列視口カリングと一括描画を実行します。
/// @param transforms Transform 連続メモリ配列
/// @param velocities Velocity 連続メモリ配列
/// @param count 処理対象の銃弾数
/// @param outer_radius 外層グロー半径
/// @param outer_color 外層グロー色
/// @param inner_radius 内層コア半径
/// @param inner_color 内層コア色
/// @param trail_offset ビームトレイルオフセット
/// @param trail_radius ビームトレイル半径
/// @param trail_color ビームトレイル色
/// @param clip_min_x 画面外カリング最小 X
/// @param clip_max_x 画面外カリング最大 X
/// @param clip_min_y 画面外カリング最小 Y
/// @param clip_max_y 画面外カリング最大 Y
void BatchDrawPlayerBullets(
    const ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    int outer_radius,
    uint32_t outer_color,
    int inner_radius,
    uint32_t inner_color,
    int trail_offset,
    int trail_radius,
    uint32_t trail_color,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    if (transforms == nullptr || count <= 0) {
        return;
    }
    auto* render = ab::GetRenderService();
    if (render == nullptr) {
        return;
    }

    const bool has_vel = (velocities != nullptr);

#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp simd
#endif
    for (int i = 0; i < count; ++i) {
        int px = static_cast<int>(transforms[i].position.x);
        int py = static_cast<int>(transforms[i].position.y);
        if (px < clip_min_x || px > clip_max_x || py < clip_min_y || py > clip_max_y) {
            continue;
        }

        if (has_vel) {
            float vx = velocities[i].value.x;
            float vy = velocities[i].value.y;
            float len_sq = vx * vx + vy * vy;
            if (len_sq > 0.0001f) {
                float inv_len = 1.0f / std::sqrt(len_sq);
                int tx = static_cast<int>(px - (vx * inv_len) * trail_offset);
                int ty = static_cast<int>(py - (vy * inv_len) * trail_offset);
                render->DrawCircle({tx, ty}, trail_radius, trail_color, true);
            }
        }

        render->DrawCircle({px, py}, outer_radius, outer_color, true);
        render->DrawCircle({px, py}, inner_radius, inner_color, true);
    }
}

/// @brief Boss 弾幕（多層同心光暈・枠線・コア）に対して、SIMD 並列視口カリングと一括描画を実行します。
/// @param transforms Transform 連続メモリ配列
/// @param count 処理対象の弾幕数
/// @param radius 基本半径
/// @param fill_color 塗りつぶし色
/// @param outline_color 外枠色
/// @param outline_padding 外枠オフセット幅
/// @param core_radius_ratio ホワイトコア半径比率
/// @param core_min_radius ホワイトコア最小ピクセル半径
/// @param core_color ホワイトコア色
/// @param clip_min_x 画面外カリング最小 X
/// @param clip_max_x 画面外カリング最大 X
/// @param clip_min_y 画面外カリング最小 Y
/// @param clip_max_y 画面外カリング最大 Y
void BatchDrawBossBullets(
    const ab::Transform* transforms,
    int count,
    int radius,
    uint32_t fill_color,
    uint32_t outline_color,
    int outline_padding,
    float core_radius_ratio,
    int core_min_radius,
    uint32_t core_color,
    int clip_min_x, int clip_max_x, int clip_min_y, int clip_max_y) noexcept {
    if (transforms == nullptr || count <= 0) {
        return;
    }
    auto* render = ab::GetRenderService();
    if (render == nullptr) {
        return;
    }

    const int outline_radius = radius + outline_padding;
    const int core_radius = std::max(core_min_radius, static_cast<int>(radius * core_radius_ratio));

#if defined(_MSC_VER) && !defined(__clang__)
    #pragma loop(ivdep)
#else
    #pragma omp simd
#endif
    for (int i = 0; i < count; ++i) {
        int bx = static_cast<int>(transforms[i].position.x);
        int by = static_cast<int>(transforms[i].position.y);
        if (bx < clip_min_x || bx > clip_max_x || by < clip_min_y || by > clip_max_y) {
            continue;
        }

        render->DrawCircle({bx, by}, outline_radius, outline_color, true);
        render->DrawCircle({bx, by}, radius, fill_color, true);
        render->DrawCircle({bx, by}, core_radius, core_color, true);
    }
}

}  // namespace ab::extension::render
