module;

#include <cstdint>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

export module DxLibRenderService;

export import RenderService;

namespace ab {
    /// @brief DxLib を使用してダイレクトバッチ描画を提供する具象レンダリングサービス
    export class DxLibRenderService : public IRenderService {
    public:
        DxLibRenderService() = default;

        // --- 画面・フレーム制御 ---
        bool ClearScreen() override { return ::ClearDrawScreen() == 0; }
        bool ScreenFlip() override { return ::ScreenFlip() == 0; }

        // --- 高密度図元バッチ高速ディスパッチ ---
        void SubmitCompactCircles(const CompactCirclePacket* circles, int count) override {
            if (!circles || count <= 0) return;

            for (int i = 0; i < count; ++i) {
                const auto& c = circles[i];
                ::DrawCircle(c.center.x, c.center.y, c.radius, c.color, c.fill != 0 ? TRUE : FALSE);
            }
        }

        // --- バッチ描画コマンドダイレクトディスパッチ ---
        void SubmitPackets(
            const RenderPacket* packets,
            int packet_count,
            const wchar_t* text_buffer,
            int text_length,
            const CompactCirclePacket* circle_buffer = nullptr,
            int circle_count = 0) override {
            if (!packets || packet_count <= 0) return;

            for (int i = 0; i < packet_count; ++i) {
                const auto& p = packets[i];
                switch (p.type) {
                case RenderPacketType::DrawCircleBatch:
                    if (circle_buffer != nullptr && p.circle_batch.offset >= 0 &&
                        p.circle_batch.offset + p.circle_batch.count <= circle_count) {
                        SubmitCompactCircles(circle_buffer + p.circle_batch.offset, p.circle_batch.count);
                    }
                    break;
                case RenderPacketType::DrawBox:
                    DrawBox(Rect<int>{p.box.p1, p.box.p2},
                            p.box.color,
                            p.box.fill != 0);
                    break;
                case RenderPacketType::DrawCircle:
                    DrawCircle(p.circle.center,
                               p.circle.radius,
                               p.circle.color,
                               p.circle.fill != 0);
                    break;
                case RenderPacketType::DrawLine:
                    DrawLine(p.line.p1,
                             p.line.p2,
                             p.line.color,
                             p.line.thickness);
                    break;
                case RenderPacketType::DrawGraph:
                    DrawGraph(p.graph.pos,
                              p.graph.handle,
                              p.graph.trans != 0);
                    break;
                case RenderPacketType::DrawRotaGraph:
                    DrawRotaGraph(p.rota_graph.center,
                                  static_cast<double>(p.rota_graph.scale),
                                  static_cast<double>(p.rota_graph.angle),
                                  p.rota_graph.handle,
                                  p.rota_graph.trans != 0);
                    break;
                case RenderPacketType::DrawRectRotaGraph:
                    DrawRectRotaGraph(p.rect_rota_graph.center,
                                      Rect<int>{
                                          p.rect_rota_graph.src_pos,
                                          p.rect_rota_graph.src_pos + p.rect_rota_graph.src_size},
                                      p.rect_rota_graph.scale,
                                      static_cast<double>(p.rect_rota_graph.angle),
                                      p.rect_rota_graph.handle,
                                      p.rect_rota_graph.trans != 0,
                                      p.rect_rota_graph.turn_x != 0);
                    break;
                case RenderPacketType::DrawString:
                    if (text_buffer != nullptr && p.string_data.text_offset >= 0 &&
                        p.string_data.text_offset < text_length) {
                        const wchar_t* str = text_buffer + p.string_data.text_offset;
                        switch (static_cast<TextAlignment>(p.string_data.alignment)) {
                        case TextAlignment::Center:
                            DrawCenterString(p.string_data.pos.x, p.string_data.pos.y, str, p.string_data.color);
                            break;
                        case TextAlignment::Left:
                            DrawLeftString(p.string_data.pos.x, p.string_data.pos.y, str, p.string_data.color);
                            break;
                        case TextAlignment::Right:
                            DrawRightString(p.string_data.pos.x, p.string_data.pos.y, str, p.string_data.color);
                            break;
                        case TextAlignment::TopLeft:
                        default:
                            DrawString(p.string_data.pos, str, p.string_data.color);
                            break;
                        }
                    }
                    break;
                case RenderPacketType::SetDrawBlendMode:
                    SetDrawBlendMode(static_cast<BlendMode>(p.blend.mode), p.blend.pal);
                    break;
                case RenderPacketType::SetDrawBright:
                    SetDrawBright(p.bright.r, p.bright.g, p.bright.b);
                    break;
                default:
                    break;
                }
            }
        }

        // --- テクスチャ・アセット管理 ---
        int LoadGraph(const wchar_t* file_path) override {
            if (!file_path) return -1;
            return ::LoadGraph(file_path);
        }
        bool DeleteGraph(int handle) override {
            if (handle >= 0) return ::DeleteGraph(handle) == 0;
            return false;
        }
        void GetGraphSize(int handle, Vec2i* out_size) const override {
            if (handle >= 0 && out_size != nullptr) {
                ::GetGraphSize(handle, &out_size->x, &out_size->y);
            }
        }

        // --- 2D プリミティブ・テキスト描画 ---
        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) override {
            ::DrawBox(r.p1.x, r.p1.y, r.p2.x, r.p2.y, color, fill_flag ? TRUE : FALSE);
        }
        void DrawCircle(Vec2i center, int radius, uint32_t color, bool fill_flag) override {
            ::DrawCircle(center.x, center.y, radius, color, fill_flag ? TRUE : FALSE);
        }
        void DrawLine(Vec2i p1, Vec2i p2, uint32_t color, int thickness) override {
            ::DrawLine(p1.x, p1.y, p2.x, p2.y, color, thickness);
        }
        void DrawString(Vec2i pos, const wchar_t* text, uint32_t color) override {
            if (!text) return;
            ::DrawString(pos.x, pos.y, text, color);
        }
        /// @brief 水平中央・垂直中央揃えで文字列を描画
        void DrawCenterString(int x, int y, const wchar_t* text, uint32_t color) override {
            if (!text) return;
            const int drawX = x - ::GetDrawStringWidth(text, -1) / 2;
            const int drawY = y - ::GetFontSize() / 2;
            DrawString(Vec2i{drawX, drawY}, text, color);
        }
        /// @brief 左揃え・垂直中央揃えで文字列を描画
        void DrawLeftString(int x, int y, const wchar_t* text, uint32_t color) override {
            if (!text) return;
            const int drawY = y - ::GetFontSize() / 2;
            DrawString(Vec2i{x, drawY}, text, color);
        }
        /// @brief 右揃え・垂直中央揃えで文字列を描画
        void DrawRightString(int x, int y, const wchar_t* text, uint32_t color) override {
            if (!text) return;
            const int drawX = x - ::GetDrawStringWidth(text, -1);
            const int drawY = y - ::GetFontSize() / 2;
            DrawString(Vec2i{drawX, drawY}, text, color);
        }
        void DrawGraph(Vec2i pos, int handle, bool trans_flag) override {
            ::DrawGraph(pos.x, pos.y, handle, trans_flag ? TRUE : FALSE);
        }
        void DrawRotaGraph(Vec2i center, double ext_rate, double angle, int graph_handle, bool trans_flag) override {
            ::DrawRotaGraph(center.x, center.y, ext_rate, angle, graph_handle, trans_flag ? TRUE : FALSE);
        }
        void DrawRectRotaGraph(Vec2i center, Rect<int> src, Vec2f scale, double angle, int graph_handle, bool trans_flag, bool turn_flag) override {
            int src_w = src.Width();
            int src_h = src.Height();
            ::DrawRectRotaGraph3(center.x, center.y, src.p1.x, src.p1.y, src_w, src_h,
                                 src_w * 0.5, src_h * 0.5,
                                 scale.x, scale.y, angle, graph_handle,
                                 trans_flag ? TRUE : FALSE, turn_flag ? TRUE : FALSE);
        }

        // --- 状態設定 ---
        void SetDrawBlendMode(BlendMode mode, int pal) override {
            int dx_mode;
            switch (mode) {
            case BlendMode::Alpha: dx_mode = DX_BLENDMODE_ALPHA; break;
            case BlendMode::Add:   dx_mode = DX_BLENDMODE_ADD; break;
            case BlendMode::Sub:   dx_mode = DX_BLENDMODE_SUB; break;
            case BlendMode::Mul:   dx_mode = DX_BLENDMODE_MULA; break;
            default:               dx_mode = DX_BLENDMODE_NOBLEND; break;
            }
            ::SetDrawBlendMode(dx_mode, pal);
        }
        void SetDrawBright(int r, int g, int b) override {
            ::SetDrawBright(r, g, b);
        }
    };
} // namespace ab
