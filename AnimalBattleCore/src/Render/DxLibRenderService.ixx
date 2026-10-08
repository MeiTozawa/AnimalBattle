module;

#include <algorithm>
#include <cmath>
#include <cstdint>
#include <string>
#include <utility>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

export module DxLibRenderService;

export import RenderService;

extern "C" bool g_is_exit_requested;

namespace ab {
    /// @brief DxLib を使用してダイレクトバッチ描画・カメラ変換を提供する具象レンダリングサービス
    export class DxLibRenderService : public IRenderService {
    private:
        int viewport_width_ = 0;
        int viewport_height_ = 0;
        bool camera_enabled_ = false;
        float camera_x_ = 640.0f;
        float camera_y_ = 360.0f;
        float camera_scale_ = 1.0f;

        [[nodiscard]] Vec2i ApplyCamera(Vec2i pos) const noexcept {
            if (!camera_enabled_) return pos;
            return Vec2i{
                static_cast<int>((pos.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f),
                static_cast<int>((pos.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f)
            };
        }

    public:
        DxLibRenderService() = default;
        DxLibRenderService(int width, int height) : viewport_width_(width), viewport_height_(height) {}

        // --- 画面・フレーム制御 ---
        bool ClearScreen() override { return ::ClearDrawScreen() == 0; }
        bool ScreenFlip() override { return ::ScreenFlip() == 0; }
        void ClearCommands() override {}
        void FlushCommands() override {}

        // --- 高密度図元バッチ高速ディスパッチ ---
        void SubmitCompactCircles(const CompactCirclePacket* circles, int count) override {
            if (!circles || count <= 0) return;

            if (camera_enabled_) {
                const float scale = camera_scale_;
                const float half_w = viewport_width_ / 2.0f;
                const float half_h = viewport_height_ / 2.0f;
                const float cx = camera_x_;
                const float cy = camera_y_;

                for (int i = 0; i < count; ++i) {
                    const auto& c = circles[i];
                    int px = static_cast<int>((c.center_x - cx) * scale + half_w);
                    int py = static_cast<int>((c.center_y - cy) * scale + half_h);
                    int r = static_cast<int>(c.radius * scale);
                    ::DrawCircle(px, py, r, c.color, c.fill != 0 ? TRUE : FALSE);
                }
            } else {
                for (int i = 0; i < count; ++i) {
                    const auto& c = circles[i];
                    ::DrawCircle(c.center_x, c.center_y, c.radius, c.color, c.fill != 0 ? TRUE : FALSE);
                }
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
                    DrawBox(Rect<int>{Vec2i{p.box.x1, p.box.y1}, Vec2i{p.box.x2, p.box.y2}},
                            p.box.color,
                            p.box.fill != 0);
                    break;
                case RenderPacketType::DrawCircle:
                    DrawCircle(Vec2i{p.circle.center_x, p.circle.center_y},
                               p.circle.radius,
                               p.circle.color,
                               p.circle.fill != 0);
                    break;
                case RenderPacketType::DrawLine:
                    DrawLine(Vec2i{p.line.x1, p.line.y1},
                             Vec2i{p.line.x2, p.line.y2},
                             p.line.color,
                             p.line.thickness);
                    break;
                case RenderPacketType::DrawGraph:
                    DrawGraph(Vec2i{p.graph.x, p.graph.y},
                              p.graph.handle,
                              p.graph.trans != 0);
                    break;
                case RenderPacketType::DrawRotaGraph:
                    DrawRotaGraph(Vec2i{p.rota_graph.center_x, p.rota_graph.center_y},
                                  static_cast<double>(p.rota_graph.scale),
                                  static_cast<double>(p.rota_graph.angle),
                                  p.rota_graph.handle,
                                  p.rota_graph.trans != 0);
                    break;
                case RenderPacketType::DrawRectRotaGraph:
                    DrawRectRotaGraph(Vec2i{p.rect_rota_graph.center_x, p.rect_rota_graph.center_y},
                                      Rect<int>{
                                          Vec2i{p.rect_rota_graph.src_x, p.rect_rota_graph.src_y},
                                          Vec2i{p.rect_rota_graph.src_x + p.rect_rota_graph.src_w,
                                                p.rect_rota_graph.src_y + p.rect_rota_graph.src_h}},
                                      static_cast<double>(p.rect_rota_graph.scale),
                                      static_cast<double>(p.rect_rota_graph.angle),
                                      p.rect_rota_graph.handle,
                                      p.rect_rota_graph.trans != 0,
                                      p.rect_rota_graph.turn_x != 0);
                    break;
                case RenderPacketType::DrawString:
                    if (text_buffer != nullptr && p.string_data.text_offset >= 0 &&
                        p.string_data.text_offset < text_length) {
                        const wchar_t* str = text_buffer + p.string_data.text_offset;
                        DrawString(Vec2i{p.string_data.x, p.string_data.y},
                                   str,
                                   p.string_data.color);
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
        Vec2i GetGraphSize(int handle) const override {
            Vec2i size{0, 0};
            if (handle >= 0) {
                ::GetGraphSize(handle, &size.x, &size.y);
            }
            return size;
        }
        void GetGraphSize(int handle, Vec2i* out_size) const override {
            if (handle >= 0 && out_size != nullptr) {
                ::GetGraphSize(handle, &out_size->x, &out_size->y);
            }
        }

        // --- 2D プリミティブ・テキスト描画 ---
        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) override {
            Vec2i p1 = ApplyCamera(r.p1);
            Vec2i p2 = ApplyCamera(r.p2);
            ::DrawBox(p1.x, p1.y, p2.x, p2.y, color, fill_flag ? TRUE : FALSE);
        }
        void DrawBox(Vec2i p1, Vec2i p2, uint32_t color, bool fill_flag) override {
            Vec2i dp1 = ApplyCamera(p1);
            Vec2i dp2 = ApplyCamera(p2);
            ::DrawBox(dp1.x, dp1.y, dp2.x, dp2.y, color, fill_flag ? TRUE : FALSE);
        }
        void DrawCircle(Vec2i center, int radius, uint32_t color, bool fill_flag) override {
            Vec2i p = ApplyCamera(center);
            int r = camera_enabled_ ? static_cast<int>(radius * camera_scale_) : radius;
            ::DrawCircle(p.x, p.y, r, color, fill_flag ? TRUE : FALSE);
        }
        void DrawLine(Vec2i p1, Vec2i p2, uint32_t color, int thickness) override {
            Vec2i dp1 = ApplyCamera(p1);
            Vec2i dp2 = ApplyCamera(p2);
            int th = thickness;
            if (camera_enabled_) {
                th = static_cast<int>(thickness * camera_scale_);
                if (th < 1 && thickness > 0) th = 1;
            }
            ::DrawLine(dp1.x, dp1.y, dp2.x, dp2.y, color, th);
        }
        void DrawString(Vec2i pos, const wchar_t* text, uint32_t color) override {
            if (!text) return;
            Vec2i p = ApplyCamera(pos);
            ::DrawString(p.x, p.y, text, color);
        }
        void DrawGraph(Vec2i pos, int handle, bool trans_flag) override {
            Vec2i p = ApplyCamera(pos);
            ::DrawGraph(p.x, p.y, handle, trans_flag ? TRUE : FALSE);
        }
        void DrawRotaGraph(Vec2i center, double ext_rate, double angle, int graph_handle, bool trans_flag) override {
            Vec2i p = ApplyCamera(center);
            double scale = camera_enabled_ ? (ext_rate * camera_scale_) : ext_rate;
            ::DrawRotaGraph(p.x, p.y, scale, angle, graph_handle, trans_flag ? TRUE : FALSE);
        }
        void DrawRectRotaGraph(Vec2i center, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) override {
            Vec2i p = ApplyCamera(center);
            double scale = camera_enabled_ ? (ext_rate * camera_scale_) : ext_rate;
            int src_w = src.Width();
            int src_h = src.Height();
            ::DrawRectRotaGraph(p.x, p.y, src.p1.x, src.p1.y, src_w, src_h,
                                scale, angle, graph_handle,
                                trans_flag ? TRUE : FALSE, turn_flag ? TRUE : FALSE);
        }

        // --- カメラ・状態設定 ---
        void SetCameraEnabled(bool enabled) override { camera_enabled_ = enabled; }
        void SetCameraState(float x, float y, float scale) override { camera_x_ = x; camera_y_ = y; camera_scale_ = scale; }
        void SetDrawBlendMode(BlendMode mode, int pal) override {
            int dx_mode = DX_BLENDMODE_NOBLEND;
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
