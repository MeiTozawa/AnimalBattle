module;

#include <cstdint>
#include <mutex>
#include <string>
#include <utility>
#include <variant>
#include <vector>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

export module DxLibRenderService;

export import RenderService;

extern "C" bool g_is_exit_requested;

namespace ab {
    /// @brief DxLib を使用して描画・カメラ変換・ダブルバッファリングを提供する具象レンダリングサービス
    export class DxLibRenderService : public IRenderService {
    private:
        int viewport_width_ = 0;
        int viewport_height_ = 0;
        bool camera_enabled_ = false;
        float camera_x_ = 640.0f;
        float camera_y_ = 360.0f;
        float camera_scale_ = 1.0f;

        std::vector<RenderCommand> write_queue_;
        std::vector<RenderCommand> render_queue_;
        mutable std::mutex queue_mutex_;

        void ExecuteCommand(const RenderCommand& cmd) {
            std::visit(Overloaded{
                [this](const DrawStringCmd& c) {
                    Point<int> p_draw = c.p;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    ::DrawString(p_draw.x, p_draw.y, c.text.c_str(), c.color);
                },
                [this](const DrawBoxCmd& c) {
                    Rect<int> r_draw = c.r;
                    if (camera_enabled_) {
                        r_draw.x1 = static_cast<int>((c.r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y1 = static_cast<int>((c.r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        r_draw.x2 = static_cast<int>((c.r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y2 = static_cast<int>((c.r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y2, c.color, c.fill_flag ? TRUE : FALSE);
                },
                [this](const DrawCircleCmd& c) {
                    Point<int> p_draw = c.p;
                    int radius_draw = c.radius;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        radius_draw = static_cast<int>(c.radius * camera_scale_);
                    }
                    ::DrawCircle(p_draw.x, p_draw.y, radius_draw, c.color, c.fill_flag ? TRUE : FALSE);
                },
                [this](const DrawLineCmd& c) {
                    Point<int> p1_draw = c.p1;
                    Point<int> p2_draw = c.p2;
                    int thickness_draw = c.thickness;
                    if (camera_enabled_) {
                        p1_draw.x = static_cast<int>((c.p1.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p1_draw.y = static_cast<int>((c.p1.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        p2_draw.x = static_cast<int>((c.p2.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p2_draw.y = static_cast<int>((c.p2.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        thickness_draw = static_cast<int>(c.thickness * camera_scale_);
                        if (thickness_draw < 1 && c.thickness > 0) thickness_draw = 1;
                    }
                    ::DrawLine(p1_draw.x, p1_draw.y, p2_draw.x, p2_draw.y, c.color, thickness_draw);
                },
                [this](const DrawHollowBoxCmd& c) {
                    Rect<int> r_draw = c.r;
                    int thickness_draw = c.thickness;
                    if (camera_enabled_) {
                        r_draw.x1 = static_cast<int>((c.r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y1 = static_cast<int>((c.r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        r_draw.x2 = static_cast<int>((c.r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y2 = static_cast<int>((c.r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        thickness_draw = static_cast<int>(c.thickness * camera_scale_);
                        if (thickness_draw < 1 && c.thickness > 0) thickness_draw = 1;
                    }
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, c.color, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, c.color, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, c.color, TRUE);
                    ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, c.color, TRUE);
                },
                [this](const DrawRoundRectFrameCmd& c) {
                    Rect<int> r_draw = c.r;
                    int thickness_draw = c.thickness;
                    int radius_draw = c.corner_radius;
                    if (camera_enabled_) {
                        r_draw.x1 = static_cast<int>((c.r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y1 = static_cast<int>((c.r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        r_draw.x2 = static_cast<int>((c.r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y2 = static_cast<int>((c.r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        thickness_draw = static_cast<int>(c.thickness * camera_scale_);
                        if (thickness_draw < 1 && c.thickness > 0) thickness_draw = 1;
                        radius_draw = static_cast<int>(c.corner_radius * camera_scale_);
                    }
                    for (int i = 0; i < thickness_draw; ++i) {
                        ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                        radius_draw, radius_draw, c.color, FALSE);
                    }
                },
                [this](const DrawStringWithBorderCmd& c) {
                    Rect<int> r_draw = c.r;
                    int thickness_draw = 2;
                    if (camera_enabled_) {
                        r_draw.x1 = static_cast<int>((c.r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y1 = static_cast<int>((c.r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        r_draw.x2 = static_cast<int>((c.r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y2 = static_cast<int>((c.r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        thickness_draw = static_cast<int>(2 * camera_scale_);
                        if (thickness_draw < 1) thickness_draw = 1;
                    }

                    switch (c.shape) {
                    case BorderShape::Rectangle:
                        ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, c.border_color, TRUE);
                        ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, c.border_color, TRUE);
                        ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, c.border_color, TRUE);
                        ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, c.border_color, TRUE);
                        break;
                    case BorderShape::DoubleRectangle: {
                        ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, c.border_color, TRUE);
                        ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, c.border_color, TRUE);
                        ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, c.border_color, TRUE);
                        ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, c.border_color, TRUE);

                        Rect<int> r_inner = r_draw.Expand(-2 * thickness_draw);
                        ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x2, r_inner.y1 + thickness_draw, c.border_color, TRUE);
                        ::DrawBox(r_inner.x1, r_inner.y2 - thickness_draw, r_inner.x2, r_inner.y2, c.border_color, TRUE);
                        ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x1 + thickness_draw, r_inner.y2, c.border_color, TRUE);
                        ::DrawBox(r_inner.x2 - thickness_draw, r_inner.y1, r_inner.x2, r_inner.y2, c.border_color, TRUE);
                    }
                    break;
                    case BorderShape::Pill: {
                        int radius = (r_draw.y2 - r_draw.y1) / 2;
                        for (int i = 0; i < thickness_draw; ++i) {
                            ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                            radius, radius, c.border_color, FALSE);
                        }
                    }
                    break;
                    case BorderShape::Underline:
                        ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, c.border_color, TRUE);
                        break;
                    case BorderShape::Null:
                    default:
                        break;
                    }

                    const int draw_x = (r_draw.x1 + r_draw.x2) / 2 - ::GetDrawStringWidth(c.text.c_str(), -1) / 2;
                    const int draw_y = (r_draw.y1 + r_draw.y2) / 2 - ::GetFontSize() / 2;
                    ::DrawString(draw_x, draw_y, c.text.c_str(), c.text_color);
                },
                [this](const DrawGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    ::DrawGraph(p_draw.x, p_draw.y, c.handle, c.trans_flag ? TRUE : FALSE);
                },
                [this](const DrawRotaGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    double scale_draw = c.ext_rate;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        scale_draw = c.ext_rate * camera_scale_;
                    }
                    ::DrawRotaGraph(p_draw.x, p_draw.y, scale_draw, c.angle, c.graph_handle, c.trans_flag ? TRUE : FALSE);
                },
                [this](const DrawRectRotaGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    double scale_draw = c.ext_rate;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        scale_draw = c.ext_rate * camera_scale_;
                    }
                    int src_w = c.src.x2 - c.src.x1;
                    int src_h = c.src.y2 - c.src.y1;
                    ::DrawRectRotaGraph(p_draw.x, p_draw.y, c.src.x1, c.src.y1, src_w, src_h,
                                        scale_draw, c.angle, c.graph_handle,
                                        c.trans_flag ? TRUE : FALSE, c.turn_flag ? TRUE : FALSE);
                },
                [](const SetDrawBlendModeCmd& c) {
                    int dx_mode = (c.mode == BlendMode::Alpha) ? DX_BLENDMODE_ALPHA : DX_BLENDMODE_NOBLEND;
                    ::SetDrawBlendMode(dx_mode, c.pal);
                },
                [](const SetDrawBrightCmd& c) { ::SetDrawBright(c.r, c.g, c.b); },
                [](const SetFontTypeNormalCmd&) { ::ChangeFontType(DX_FONTTYPE_NORMAL); },
                [](const ChangeFontCmd& c) { ::ChangeFont(c.font_name); },
                [](const SetFontSizeCmd& c) { ::SetFontSize(c.size); },
                [](const SetFontThicknessCmd& c) { ::SetFontThickness(c.thickness); },
                [](const SetBackgroundColorCmd& c) { ::SetBackgroundColor(c.r, c.g, c.b); },
                [](const ExitApplicationCmd&) { g_is_exit_requested = true; },
                [this](const SetCameraEnabledCmd& c) { camera_enabled_ = c.enabled; },
                [this](const SetCameraStateCmd& c) { camera_x_ = c.x; camera_y_ = c.y; camera_scale_ = c.scale; }
            }, cmd);
        }

    public:
        DxLibRenderService() = default;
        DxLibRenderService(int width, int height) : viewport_width_(width), viewport_height_(height) {}

        // --- 画面・フレーム制御 ---
        bool ClearScreen() override { return ::ClearDrawScreen() == 0; }
        bool ScreenFlip() override { return ::ScreenFlip() == 0; }
        void ClearCommands() override {
            std::lock_guard<std::mutex> lock(queue_mutex_);
            write_queue_.clear();
        }
        void FlushCommands() override {
            {
                std::lock_guard<std::mutex> lock(queue_mutex_);
                render_queue_.swap(write_queue_);
            }
            for (const auto& cmd : render_queue_) {
                ExecuteCommand(cmd);
            }
            render_queue_.clear();
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
        void GetGraphSize(int handle, int* out_width, int* out_height) const override {
            if (handle >= 0 && out_width && out_height) {
                ::GetGraphSize(handle, out_width, out_height);
            }
        }

        // --- 2D プリミティブ・テキスト描画 ---
        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) override {
            PushCommand(DrawBoxCmd{r, color, fill_flag});
        }
        void DrawCircle(Point<int> p, int radius, uint32_t color, bool fill_flag) override {
            PushCommand(DrawCircleCmd{p, radius, color, fill_flag});
        }
        void DrawLine(Point<int> p1, Point<int> p2, uint32_t color, int thickness) override {
            PushCommand(DrawLineCmd{p1, p2, color, thickness});
        }
        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) override {
            PushCommand(DrawStringCmd{p, text, color});
        }
        void DrawGraph(Point<int> p, int handle, bool trans_flag) override {
            PushCommand(DrawGraphCmd{p, handle, trans_flag});
        }
        void DrawRotaGraph(Point<int> p, double ext_rate, double angle, int graph_handle, bool trans_flag) override {
            PushCommand(DrawRotaGraphCmd{p, ext_rate, angle, graph_handle, trans_flag});
        }
        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) override {
            PushCommand(DrawRectRotaGraphCmd{p, src, ext_rate, angle, graph_handle, trans_flag, turn_flag});
        }
        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) override {
            PushCommand(DrawHollowBoxCmd{r, thickness, color});
        }
        void DrawRoundRectFrame(Rect<int> r, int corner_radius, int thickness, uint32_t color) override {
            PushCommand(DrawRoundRectFrameCmd{r, corner_radius, thickness, color});
        }
        void DrawStringWithBorder(Rect<int> r, const wchar_t* text, BorderShape shape = BorderShape::Null,
                                  uint32_t text_color = COLOR_WHITE, uint32_t border_color = COLOR_WHITE) override {
            PushCommand(DrawStringWithBorderCmd{r, text, shape, text_color, border_color});
        }

        // --- カメラ・状態設定 ---
        void SetCameraEnabled(bool enabled) override { PushCommand(SetCameraEnabledCmd{enabled}); }
        void SetCameraState(float x, float y, float scale) override { PushCommand(SetCameraStateCmd{x, y, scale}); }
        void SetDrawBlendMode(BlendMode mode, int pal) override { PushCommand(SetDrawBlendModeCmd{mode, pal}); }
        void SetDrawBright(int r, int g, int b) override { PushCommand(SetDrawBrightCmd{r, g, b}); }
        void SetFontTypeNormal() override { PushCommand(SetFontTypeNormalCmd{}); }
        void ChangeFont(const wchar_t* font_name) override { PushCommand(ChangeFontCmd{font_name}); }
        void SetFontSize(int size) override { PushCommand(SetFontSizeCmd{size}); }
        void SetFontThickness(int thickness) override { PushCommand(SetFontThicknessCmd{thickness}); }
        void SetBackgroundColor(int r, int g, int b) override { PushCommand(SetBackgroundColorCmd{r, g, b}); }
        void ExitApplication() override { PushCommand(ExitApplicationCmd{}); }

        // --- 情報取得 ---
        int GetFontSize() const override { return ::GetFontSize(); }
        int GetDrawStringWidth(const wchar_t* text) const override { return ::GetDrawStringWidth(text, -1); }

        // --- コマンドキューイング ---
        void PushCommand(RenderCommand cmd) {
            std::lock_guard<std::mutex> lock(queue_mutex_);
            write_queue_.push_back(std::move(cmd));
        }
    };
} // namespace ab
