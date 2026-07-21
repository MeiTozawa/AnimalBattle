module;

#include <cstdint>
#include <memory>
#include <DxLib.h>
#include <RenderUtils.h>

module RenderService;

namespace ab {
    extern bool g_is_exit_requested;

    class DxLibRenderService : public IRenderService {
    private:
        int viewport_width_ = 1280;
        int viewport_height_ = 720;
        bool camera_enabled_ = false;
        float camera_x_ = 640.0f;
        float camera_y_ = 360.0f;
        float camera_scale_ = 1.0f;

    public:
        DxLibRenderService(int width, int height)
            : viewport_width_(width), viewport_height_(height) {
        }

        void SetCameraEnabled(bool enabled) override {
            camera_enabled_ = enabled;
        }

        void SetCameraState(float x, float y, float scale) override {
            camera_x_ = x;
            camera_y_ = y;
            camera_scale_ = scale;
        }

        int GetFontSize() const override {
            return ::GetFontSize();
        }

        int GetDrawStringWidth(const wchar_t* text) const override {
            return ::GetDrawStringWidth(text, -1);
        }

        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) override {
            Point<int> p_draw = p;
            if (camera_enabled_) {
                p_draw.x = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
            }
            ::DrawString(p_draw.x, p_draw.y, text, color);
        }

        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) override {
            Rect<int> r_draw = r;
            if (camera_enabled_) {
                r_draw.x1 = static_cast<int>((r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
            }
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y2, color, fill_flag ? TRUE : FALSE);
        }

        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) override {
            Rect<int> r_draw = r;
            int thickness_draw = thickness;
            if (camera_enabled_) {
                r_draw.x1 = static_cast<int>((r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                thickness_draw = static_cast<int>(thickness * camera_scale_);
                if (thickness_draw < 1 && thickness > 0) thickness_draw = 1;
            }
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, color, TRUE);
            ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, color, TRUE);
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, color, TRUE);
            ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, color, TRUE);
        }

        void DrawRoundRectFrame(Rect<int> r, int corner_radius, int thickness, uint32_t color) override {
            Rect<int> r_draw = r;
            int thickness_draw = thickness;
            int radius_draw = corner_radius;
            if (camera_enabled_) {
                r_draw.x1 = static_cast<int>((r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                thickness_draw = static_cast<int>(thickness * camera_scale_);
                if (thickness_draw < 1 && thickness > 0) thickness_draw = 1;
                radius_draw = static_cast<int>(corner_radius * camera_scale_);
            }
            for (int i = 0; i < thickness_draw; ++i)
                ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                radius_draw, radius_draw, color, FALSE);
        }

        void DrawStringWithBorder(Rect<int> r, const wchar_t* text,
                                  BorderShape shape, uint32_t text_color, uint32_t border_color) override {
            Rect<int> r_draw = r;
            int thickness_draw = 2;
            if (camera_enabled_) {
                r_draw.x1 = static_cast<int>((r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                thickness_draw = static_cast<int>(2 * camera_scale_);
                if (thickness_draw < 1) thickness_draw = 1;
            }

            switch (shape) {
            case BorderShape::Rectangle:
                ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, border_color, TRUE);
                ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, border_color, TRUE);
                ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, border_color, TRUE);
                ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, border_color, TRUE);
                break;
            case BorderShape::DoubleRectangle:
                {
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, border_color, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, border_color, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, border_color, TRUE);
                    ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, border_color, TRUE);

                    Rect<int> r_inner = r_draw.Expand(-2 * thickness_draw);
                    ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x2, r_inner.y1 + thickness_draw, border_color, TRUE);
                    ::DrawBox(r_inner.x1, r_inner.y2 - thickness_draw, r_inner.x2, r_inner.y2, border_color, TRUE);
                    ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x1 + thickness_draw, r_inner.y2, border_color, TRUE);
                    ::DrawBox(r_inner.x2 - thickness_draw, r_inner.y1, r_inner.x2, r_inner.y2, border_color, TRUE);
                }
                break;
            case BorderShape::Pill:
                {
                    int radius = (r_draw.y2 - r_draw.y1) / 2;
                    for (int i = 0; i < thickness_draw; ++i)
                        ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                        radius, radius, border_color, FALSE);
                }
                break;
            case BorderShape::Underline:
                ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, border_color, TRUE);
                break;
            case BorderShape::Null:
            default:
                break;
            }

            const int draw_x = (r_draw.x1 + r_draw.x2) / 2 - GetDrawStringWidth(text) / 2;
            const int draw_y = (r_draw.y1 + r_draw.y2) / 2 - GetFontSize() / 2;
            ::DrawString(draw_x, draw_y, text, text_color);
        }

        void DrawGraph(Point<int> p, int handle, bool trans_flag) override {
            if (camera_enabled_) {
                int w = 0;
                int h = 0;
                ::GetGraphSize(handle, &w, &h);
                int x1 = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                int y1 = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                int x2 = x1 + static_cast<int>(w * camera_scale_);
                int y2 = y1 + static_cast<int>(h * camera_scale_);
                ::DrawExtendGraph(x1, y1, x2, y2, handle, trans_flag ? TRUE : FALSE);
            } else {
                ::DrawGraph(p.x, p.y, handle, trans_flag ? TRUE : FALSE);
            }
        }

        void DrawRotaGraphF(Point<float> p, double ext_rate, double angle,
                            int graph_handle, bool trans_flag) override {
            Point<float> p_draw = p;
            double scale_draw = ext_rate;
            if (camera_enabled_) {
                p_draw.x = static_cast<float>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<float>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                scale_draw = ext_rate * camera_scale_;
            }
            ::DrawRotaGraphF(p_draw.x, p_draw.y, scale_draw, angle, graph_handle, trans_flag ? TRUE : FALSE);
        }

        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle,
                               int graph_handle, bool trans_flag, bool turn_flag) override {
            Point<int> p_draw = p;
            double scale_draw = ext_rate;
            if (camera_enabled_) {
                p_draw.x = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                scale_draw = ext_rate * camera_scale_;
            }
            int src_width = src.x2 - src.x1;
            int src_height = src.y2 - src.y1;
            ::DrawRectRotaGraph(p_draw.x, p_draw.y, src.x1, src.y1, src_width, src_height, scale_draw, angle,
                                graph_handle, trans_flag ? TRUE : FALSE, turn_flag ? TRUE : FALSE);
        }

        void SetDrawBlendMode(BlendMode mode, int pal) override {
            int dx_mode = DX_BLENDMODE_NOBLEND;
            switch (mode) {
            case BlendMode::Alpha:
                dx_mode = DX_BLENDMODE_ALPHA;
                break;
            case BlendMode::NoBlend:
                dx_mode = DX_BLENDMODE_NOBLEND;
                break;
            }
            ::SetDrawBlendMode(dx_mode, pal);
        }

        void SetDrawBright(int r, int g, int b) override {
            ::SetDrawBright(r, g, b);
        }

        void SetFontTypeNormal() override {
            ::ChangeFontType(DX_FONTTYPE_NORMAL);
        }

        void ChangeFont(const wchar_t* font_name) override {
            ::ChangeFont(font_name);
        }

        void SetFontSize(int size) override {
            ::SetFontSize(size);
        }

        void SetFontThickness(int thickness) override {
            ::SetFontThickness(thickness);
        }

        void SetBackgroundColor(int r, int g, int b) override {
            ::SetBackgroundColor(r, g, b);
        }

        void ExitApplication() override {
            g_is_exit_requested = true;
        }
    };

    std::unique_ptr<IRenderService> CreateRenderService(int width, int height) {
        return std::make_unique<DxLibRenderService>(width, height);
    }
}  // namespace ab
