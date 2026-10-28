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

/// @brief 円描画バッチコマンドデータ
/// @note 連続するCompactCirclePacket配列の先頭インデックスと要素数を保持します
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

/// @brief 弾幕バッチ描画専用の高密度円パケット構造体
/// @note メモリ帯域圧迫を抑えL1キャッシュヒット率を高める目的で20バイトに切り詰めています
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

/// @brief 矩形切り出し回転拡縮画像描画コマンドデータ
/// @note 独立した水平垂直スケール指定に対応しています
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

/// @brief 文字列描画のアライメント基準
enum class TextAlignment : uint8_t {
    TopLeft = 0, ///< 左上基準
    Left = 1,    ///< 左揃えおよび垂直中央揃え
    Center = 2,  ///< 水平中央および垂直中央揃え
    Right = 3    ///< 右揃えおよび垂直中央揃え
};

/// @brief 文字列描画コマンドデータ
/// @note 文字列の実体データは共有テキストバッファを参照します
struct DrawStringPacket {
    Vec2i pos;
    uint32_t color;
    int32_t text_offset;
    int32_t text_length;
    uint8_t alignment;    ///< 配置基準
    uint8_t padding[3];   ///< 4バイト境界アライメント用パディング
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

/// @brief 言語境界を越えて受け渡す固定長描画コマンドパケット
/// @note C#側のアンマネージドメモリとゼロコピーで双方向転送するため、48バイト均一サイズにアライメントしています
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
