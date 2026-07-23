module;

#include <cstdint>
#include <utility>
#include <variant>
#include <type_traits>

export module RenderService;

export import RenderTypes;
import RenderConcept;

namespace ab {
    /// @brief バックエンドを合成してカメラ変換やコマンド実行を提供する静的多態 RenderService テンプレートクラス
    /// @tparam Backend レンダリングバックエンド型
    export template <RenderBackendConcept Backend>
    class RenderService {
    private:
        int viewport_width_ = 0;
        int viewport_height_ = 0;
        bool camera_enabled_ = false;
        float camera_x_ = 640.0f;
        float camera_y_ = 360.0f;
        float camera_scale_ = 1.0f;
        Backend backend_{};

    public:
        RenderService() = default;
        RenderService(int width, int height) : viewport_width_(width), viewport_height_(height) {}
        explicit RenderService(int width, int height, Backend backend)
            : viewport_width_(width), viewport_height_(height), backend_(std::move(backend)) {}

        /// @brief std::variant でカプセル化された描画コマンドを実行する
        /// @param cmd 実行する描画コマンド variant
        void ExecuteCommand(const RenderCommand& cmd) {
            std::visit(Overloaded{
                [this](const DrawStringCmd& c) { DrawString(c.p, c.text, c.color); },
                [this](const DrawBoxCmd& c) { DrawBox(c.r, c.color, c.fill_flag); },
                [this](const DrawHollowBoxCmd& c) { DrawHollowBox(c.r, c.thickness, c.color); },
                [this](const DrawRoundRectFrameCmd& c) { DrawRoundRectFrame(c.r, c.corner_radius, c.thickness, c.color); },
                [this](const DrawStringWithBorderCmd& c) { DrawStringWithBorder(c.r, c.text, c.shape, c.text_color, c.border_color); },
                [this](const DrawGraphCmd& c) { DrawGraph(c.p, c.handle, c.trans_flag); },
                [this](const DrawRotaGraphCmd& c) { DrawRotaGraph(c.p, c.ext_rate, c.angle, c.graph_handle, c.trans_flag); },
                [this](const DrawRectRotaGraphCmd& c) { DrawRectRotaGraph(c.p, c.src, c.ext_rate, c.angle, c.graph_handle, c.trans_flag, c.turn_flag); },
                [this](const SetDrawBlendModeCmd& c) { SetDrawBlendMode(c.mode, c.pal); },
                [this](const SetDrawBrightCmd& c) { SetDrawBright(c.r, c.g, c.b); },
                [this](const SetFontTypeNormalCmd&) { SetFontTypeNormal(); },
                [this](const ChangeFontCmd& c) { ChangeFont(c.font_name); },
                [this](const SetFontSizeCmd& c) { SetFontSize(c.size); },
                [this](const SetFontThicknessCmd& c) { SetFontThickness(c.thickness); },
                [this](const SetBackgroundColorCmd& c) { SetBackgroundColor(c.r, c.g, c.b); },
                [this](const ExitApplicationCmd&) { ExitApplication(); }
            }, cmd);
        }

        void SetCameraEnabled(bool enabled) { camera_enabled_ = enabled; }
        void SetCameraState(float x, float y, float scale) {
            camera_x_ = x;
            camera_y_ = y;
            camera_scale_ = scale;
        }

        int GetFontSize() const { return backend_.GetFontSize(); }
        int GetDrawStringWidth(const wchar_t* text) const { return backend_.GetDrawStringWidth(text); }

        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) const {
            Point<int> p_draw = p;
            if (camera_enabled_) {
                p_draw.x = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
            }
            backend_.DrawString(p_draw, text, color);
        }

        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) const {
            Rect<int> r_draw = r;
            if (camera_enabled_) {
                r_draw.x1 = static_cast<int>((r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
            }
            backend_.DrawBox(r_draw, color, fill_flag);
        }

        /// @brief 枠線のみの空心矩形を描画する（原子 DrawBox を組み合わせて実装）
        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) const {
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
            backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, color, true);
            backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, color, true);
            backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, color, true);
            backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, color, true);
        }

        /// @brief 角丸矩形フレームを描画する（原子 DrawRoundRect を組み合わせて実装）
        void DrawRoundRectFrame(Rect<int> r, int corner_radius, int thickness, uint32_t color) const {
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
            for (int i = 0; i < thickness_draw; ++i) {
                backend_.DrawRoundRect(Rect<int>{r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i},
                                       radius_draw, radius_draw, color, false);
            }
        }

        /// @brief 枠付き文字列を描画する（原子 DrawBox, DrawRoundRect, DrawString を組み合わせて実装）
        void DrawStringWithBorder(Rect<int> r, const wchar_t* text, BorderShape shape = BorderShape::Null,
                                  uint32_t text_color = COLOR_WHITE, uint32_t border_color = COLOR_WHITE) const {
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
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, border_color, true);
                break;
            case BorderShape::DoubleRectangle: {
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, border_color, true);

                Rect<int> r_inner = r_draw.Expand(-2 * thickness_draw);
                backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y1, r_inner.x2, r_inner.y1 + thickness_draw}, border_color, true);
                backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y2 - thickness_draw, r_inner.x2, r_inner.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y1, r_inner.x1 + thickness_draw, r_inner.y2}, border_color, true);
                backend_.DrawBox(Rect<int>{r_inner.x2 - thickness_draw, r_inner.y1, r_inner.x2, r_inner.y2}, border_color, true);
            }
            break;
            case BorderShape::Pill: {
                int radius = (r_draw.y2 - r_draw.y1) / 2;
                for (int i = 0; i < thickness_draw; ++i) {
                    backend_.DrawRoundRect(Rect<int>{r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i},
                                           radius, radius, border_color, false);
                }
            }
            break;
            case BorderShape::Underline:
                backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, border_color, true);
                break;
            case BorderShape::Null:
            default:
                break;
            }

            const int draw_x = (r_draw.x1 + r_draw.x2) / 2 - backend_.GetDrawStringWidth(text) / 2;
            const int draw_y = (r_draw.y1 + r_draw.y2) / 2 - backend_.GetFontSize() / 2;
            backend_.DrawString(Point<int>{draw_x, draw_y}, text, text_color);
        }

        void DrawGraph(Point<int> p, int handle, bool trans_flag) const {
            if (camera_enabled_) {
                Point<int> p_draw;
                p_draw.x = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                backend_.DrawGraph(p_draw, handle, trans_flag);
            } else {
                backend_.DrawGraph(p, handle, trans_flag);
            }
        }

        void DrawRotaGraph(Point<int> p, double ext_rate, double angle, int graph_handle, bool trans_flag) const {
            Point<int> p_draw = p;
            double scale_draw = ext_rate;
            if (camera_enabled_) {
                p_draw.x = (p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f;
                p_draw.y = (p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f;
                scale_draw = ext_rate * camera_scale_;
            }
            backend_.DrawRotaGraph(p_draw, scale_draw, angle, graph_handle, trans_flag);
        }

        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) const {
            Point<int> p_draw = p;
            double scale_draw = ext_rate;
            if (camera_enabled_) {
                p_draw.x = static_cast<int>((p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                p_draw.y = static_cast<int>((p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                scale_draw = ext_rate * camera_scale_;
            }
            backend_.DrawRectRotaGraph(p_draw, src, scale_draw, angle, graph_handle, trans_flag, turn_flag);
        }

        void SetDrawBlendMode(BlendMode mode, int pal) { backend_.SetDrawBlendMode(mode, pal); }
        void SetDrawBright(int r, int g, int b) { backend_.SetDrawBright(r, g, b); }
        void SetFontTypeNormal() { backend_.SetFontTypeNormal(); }
        void ChangeFont(const wchar_t* font_name) { backend_.ChangeFont(font_name); }
        void SetFontSize(int size) { backend_.SetFontSize(size); }
        void SetFontThickness(int thickness) { backend_.SetFontThickness(thickness); }
        void SetBackgroundColor(int r, int g, int b) { backend_.SetBackgroundColor(r, g, b); }
        void ExitApplication() { backend_.ExitApplication(); }
        
    };
} // namespace ab
