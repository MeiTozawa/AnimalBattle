module;

#include <cstdint>
#include "RenderPacket.h"

export module RenderService;

export import RenderTypes;

namespace ab {
    export using ab::RenderPacket;
    export using ab::RenderPacketType;
    export using ab::CompactCirclePacket;

    /// @brief バックエンド非依存のレンダリングサービス抽象インターフェース
    export class IRenderService {
    public:
        virtual ~IRenderService() = default;

        // --- 画面・フレーム制御 ---
        virtual bool ClearScreen() = 0;
        virtual bool ScreenFlip() = 0;
        virtual void ClearCommands() = 0;
        virtual void FlushCommands() = 0;

        // --- バッチ描画コマンド受付 ---
        virtual void SubmitPackets(const RenderPacket* packets, int packet_count, const wchar_t* text_buffer, int text_length, const CompactCirclePacket* circle_buffer = nullptr, int circle_count = 0) = 0;
        virtual void SubmitCompactCircles(const CompactCirclePacket* circles, int count) = 0;

        // --- テクスチャ・アセット管理 ---
        virtual int LoadGraph(const wchar_t* file_path) = 0;
        virtual bool DeleteGraph(int handle) = 0;
        virtual Vec2i GetGraphSize(int handle) const = 0;
        virtual void GetGraphSize(int handle, Vec2i* out_size) const = 0;

        // --- 2D プリミティブ・テキスト描画 ---
        virtual void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) = 0;
        virtual void DrawBox(Vec2i p1, Vec2i p2, uint32_t color, bool fill_flag) = 0;
        virtual void DrawCircle(Vec2i center, int radius, uint32_t color, bool fill_flag) = 0;
        virtual void DrawLine(Vec2i p1, Vec2i p2, uint32_t color, int thickness) = 0;
        virtual void DrawString(Vec2i pos, const wchar_t* text, uint32_t color) = 0;
        virtual void DrawGraph(Vec2i pos, int handle, bool trans_flag) = 0;
        virtual void DrawRotaGraph(Vec2i center, double ext_rate, double angle, int graph_handle, bool trans_flag) = 0;
        virtual void DrawRectRotaGraph(Vec2i center, Rect<int> src, Vec2f scale, double angle, int graph_handle, bool trans_flag, bool turn_flag) = 0;
        virtual void DrawRectRotaGraph(Vec2i center, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) {
            DrawRectRotaGraph(center, src, Vec2f{static_cast<float>(ext_rate), static_cast<float>(ext_rate)}, angle, graph_handle, trans_flag, turn_flag);
        }

        // --- カメラ・状態設定 ---
        virtual void SetCameraEnabled(bool enabled) = 0;
        virtual void SetCameraState(Vec2f pos, float scale) = 0;
        virtual void SetDrawBlendMode(BlendMode mode, int pal) = 0;
        virtual void SetDrawBright(int r, int g, int b) = 0;
    };
} // namespace ab
