#ifndef ANIMALBATTLE_CORE_RENDER_RENDER_PACKET_H_
#define ANIMALBATTLE_CORE_RENDER_RENDER_PACKET_H_

#include <cstdint>
#include "Core/Vector2D.h"

namespace ab {

/// @brief レンダリングコマンド種別を表す列挙型
enum class RenderPacketType : uint16_t {
    None = 0,
    DrawBox = 1,
    DrawCircle = 2,
    DrawLine = 3,
    DrawGraph = 4,
    DrawRotaGraph = 5,
    DrawRectRotaGraph = 6,
    DrawString = 7,
    SetDrawBlendMode = 8,
    SetDrawBright = 9,
    DrawCircleBatch = 10
};

#pragma pack(push, 4)

/// @brief 円描画バッチ（連続する CompactCirclePacket の範囲）コマンドデータ
struct DrawCircleBatchPacket {
    int32_t offset;
    int32_t count;
};

/// @brief 矩形描画コマンドデータ
struct DrawBoxPacket {
    Vec2i p1;
    Vec2i p2;
    uint32_t color;
    uint8_t fill;
    uint8_t padding[3];
};

/// @brief 円描画コマンドデータ
struct DrawCirclePacket {
    Vec2i center;
    int32_t radius;
    uint32_t color;
    uint8_t fill;
    uint8_t padding[3];
};

/// @brief 20 バイト緊密配置の高密度円描画パケット（弾幕・パーティクルバッチ用）
struct CompactCirclePacket {
    Vec2i center;
    int32_t radius;
    uint32_t color;
    uint8_t fill;
    uint8_t padding[3];
};
static_assert(sizeof(CompactCirclePacket) == 20, "CompactCirclePacket size must be exactly 20 bytes");

/// @brief 線分描画コマンドデータ
struct DrawLinePacket {
    Vec2i p1;
    Vec2i p2;
    uint32_t color;
    int32_t thickness;
};

/// @brief 単純画像描画コマンドデータ
struct DrawGraphPacket {
    Vec2i pos;
    int32_t handle;
    uint8_t trans;
    uint8_t padding[3];
};

/// @brief 回転拡縮画像描画コマンドデータ
struct DrawRotaGraphPacket {
    Vec2i center;
    float scale;
    float angle;
    int32_t handle;
    uint8_t trans;
    uint8_t padding[3];
};

/// @brief 矩形切り出し回転拡縮画像描画コマンドデータ（独立 X/Y 軸拡縮対応）
struct DrawRectRotaGraphPacket {
    Vec2i center;
    Vec2i src_pos;
    Vec2i src_size;
    Vec2f scale;
    float angle;
    int32_t handle;
    uint8_t trans;
    uint8_t turn_x;
    uint8_t padding[2];
};

/// @brief 文字列描画コマンドデータ（文字列実体は一括テキストバッファを参照）
struct DrawStringPacket {
    Vec2i pos;
    uint32_t color;
    int32_t text_offset;
    int32_t text_length;
};

/// @brief 描画ブレンドモード設定コマンドデータ
struct SetBlendModePacket {
    int32_t mode;
    int32_t pal;
};

/// @brief 描画輝度設定コマンドデータ
struct SetBrightPacket {
    int32_t r;
    int32_t g;
    int32_t b;
};

/// @brief C# - C++ 間でやり取りされる 48 バイト固定長の描画コマンドパケット
struct RenderPacket {
    RenderPacketType type;
    uint16_t reserved;
    union {
        DrawBoxPacket box;
        DrawCirclePacket circle;
        DrawLinePacket line;
        DrawGraphPacket graph;
        DrawRotaGraphPacket rota_graph;
        DrawRectRotaGraphPacket rect_rota_graph;
        DrawStringPacket string_data;
        SetBlendModePacket blend;
        SetBrightPacket bright;
        DrawCircleBatchPacket circle_batch;
        uint8_t raw_data[44];
    };
};

#pragma pack(pop)

static_assert(sizeof(RenderPacket) == 48, "RenderPacket size must be exactly 48 bytes for binary compatibility");

}  // namespace ab

#endif  // ANIMALBATTLE_CORE_RENDER_RENDER_PACKET_H_
