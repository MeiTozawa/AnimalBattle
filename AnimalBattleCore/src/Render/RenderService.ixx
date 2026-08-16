module;

#include <cstdint>
#include <utility>
#include <variant>
#include <vector>
#include <mutex>

export module RenderService;

export import RenderTypes;
import RenderConcept;

namespace ab {
    /// @brief バックエンドを合成してカメラ変換やコマンド実行・スレッドセーフなダブルバッファキューイングを提供する静的多態 RenderService テンプレートクラス
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

        // スレッドセーフなダブルバッファ構造 (Logic Thread ⇄ Render Thread)
        std::vector<RenderCommand> write_queue_;  ///< C# Logic スレッドが書き込むバッファ
        std::vector<RenderCommand> render_queue_; ///< C++ Render スレッドが消費・描画するバッファ
        mutable std::mutex queue_mutex_;

    public:
        RenderService() = default;
        RenderService(int width, int height) : viewport_width_(width), viewport_height_(height) {}
        explicit RenderService(int width, int height, Backend backend)
            : viewport_width_(width), viewport_height_(height), backend_(std::move(backend)) {}

        Backend& GetBackend() { return backend_; }

        /// @brief スレッドセーフに描画コマンドを書き込みバッファに積む (C# 逻辑スレッドから非同期呼び出し可能)
        void PushCommand(RenderCommand cmd) {
            std::lock_guard<std::mutex> lock(queue_mutex_);
            write_queue_.push_back(std::move(cmd));
        }

        /// @brief 書き込みバッファをクリアする
        void ClearCommands() {
            std::lock_guard<std::mutex> lock(queue_mutex_);
            write_queue_.clear();
        }

        /// @brief 書き込みバッファと描画バッファを高速スワップし、描画スレッドで一括実行する (非ブロック・ダブルバッファリング)
        void FlushCommands() {
            {
                std::lock_guard<std::mutex> lock(queue_mutex_);
                render_queue_.swap(write_queue_);
            }

            // 描画スレッドが render_queue_ を処理中も、Logic スレッドは lock 解除済みの write_queue_ に自由に追加可能
            for (const auto& cmd : render_queue_) {
                ExecuteCommand(cmd);
            }
            render_queue_.clear();
        }

        /// @brief std::variant でカプセル化された単一描画コマンドを実行する（カメラ変換を適用してバックエンドへ転送）
        /// @param cmd 実行する描画コマンド variant
        void ExecuteCommand(const RenderCommand& cmd) {
            std::visit(Overloaded{
                [this](const DrawStringCmd& c) {
                    Point<int> p_draw = c.p;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    backend_.DrawString(p_draw, c.text.c_str(), c.color);
                },
                [this](const DrawBoxCmd& c) {
                    Rect<int> r_draw = c.r;
                    if (camera_enabled_) {
                        r_draw.x1 = static_cast<int>((c.r.x1 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y1 = static_cast<int>((c.r.y1 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        r_draw.x2 = static_cast<int>((c.r.x2 - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        r_draw.y2 = static_cast<int>((c.r.y2 - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    backend_.DrawBox(r_draw, c.color, c.fill_flag);
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
                    backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, c.color, true);
                    backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, c.color, true);
                    backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, c.color, true);
                    backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, c.color, true);
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
                        backend_.DrawRoundRect(Rect<int>{r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i},
                                               radius_draw, radius_draw, c.color, false);
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
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, c.border_color, true);
                        break;
                    case BorderShape::DoubleRectangle: {
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2}, c.border_color, true);

                        Rect<int> r_inner = r_draw.Expand(-2 * thickness_draw);
                        backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y1, r_inner.x2, r_inner.y1 + thickness_draw}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y2 - thickness_draw, r_inner.x2, r_inner.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_inner.x1, r_inner.y1, r_inner.x1 + thickness_draw, r_inner.y2}, c.border_color, true);
                        backend_.DrawBox(Rect<int>{r_inner.x2 - thickness_draw, r_inner.y1, r_inner.x2, r_inner.y2}, c.border_color, true);
                    }
                    break;
                    case BorderShape::Pill: {
                        int radius = (r_draw.y2 - r_draw.y1) / 2;
                        for (int i = 0; i < thickness_draw; ++i) {
                            backend_.DrawRoundRect(Rect<int>{r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i},
                                                   radius, radius, c.border_color, false);
                        }
                    }
                    break;
                    case BorderShape::Underline:
                        backend_.DrawBox(Rect<int>{r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2}, c.border_color, true);
                        break;
                    case BorderShape::Null:
                    default:
                        break;
                    }

                    const int draw_x = (r_draw.x1 + r_draw.x2) / 2 - backend_.GetDrawStringWidth(c.text.c_str()) / 2;
                    const int draw_y = (r_draw.y1 + r_draw.y2) / 2 - backend_.GetFontSize() / 2;
                    backend_.DrawString(Point<int>{draw_x, draw_y}, c.text.c_str(), c.text_color);
                },
                [this](const DrawGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                    }
                    backend_.DrawGraph(p_draw, c.handle, c.trans_flag);
                },
                [this](const DrawRotaGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    double scale_draw = c.ext_rate;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        scale_draw = c.ext_rate * camera_scale_;
                    }
                    backend_.DrawRotaGraph(p_draw, scale_draw, c.angle, c.graph_handle, c.trans_flag);
                },
                [this](const DrawRectRotaGraphCmd& c) {
                    Point<int> p_draw = c.p;
                    double scale_draw = c.ext_rate;
                    if (camera_enabled_) {
                        p_draw.x = static_cast<int>((c.p.x - camera_x_) * camera_scale_ + viewport_width_ / 2.0f);
                        p_draw.y = static_cast<int>((c.p.y - camera_y_) * camera_scale_ + viewport_height_ / 2.0f);
                        scale_draw = c.ext_rate * camera_scale_;
                    }
                    backend_.DrawRectRotaGraph(p_draw, c.src, scale_draw, c.angle, c.graph_handle, c.trans_flag, c.turn_flag);
                },
                [this](const SetDrawBlendModeCmd& c) { backend_.SetDrawBlendMode(c.mode, c.pal); },
                [this](const SetDrawBrightCmd& c) { backend_.SetDrawBright(c.r, c.g, c.b); },
                [this](const SetFontTypeNormalCmd&) { backend_.SetFontTypeNormal(); },
                [this](const ChangeFontCmd& c) { backend_.ChangeFont(c.font_name); },
                [this](const SetFontSizeCmd& c) { backend_.SetFontSize(c.size); },
                [this](const SetFontThicknessCmd& c) { backend_.SetFontThickness(c.thickness); },
                [this](const SetBackgroundColorCmd& c) { backend_.SetBackgroundColor(c.r, c.g, c.b); },
                [this](const ExitApplicationCmd&) { backend_.ExitApplication(); },
                [this](const SetCameraEnabledCmd& c) { camera_enabled_ = c.enabled; },
                [this](const SetCameraStateCmd& c) { camera_x_ = c.x; camera_y_ = c.y; camera_scale_ = c.scale; }
            }, cmd);
        }

        void SetCameraEnabled(bool enabled) { PushCommand(SetCameraEnabledCmd{enabled}); }
        void SetCameraState(float x, float y, float scale) { PushCommand(SetCameraStateCmd{x, y, scale}); }

        int GetFontSize() const { return backend_.GetFontSize(); }
        int GetDrawStringWidth(const wchar_t* text) const { return backend_.GetDrawStringWidth(text); }

        // --- シリアライズ描画 API (RenderCommand 構造体としてキューへ積む) ---

        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) {
            PushCommand(DrawStringCmd{p, text, color});
        }

        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) {
            PushCommand(DrawBoxCmd{r, color, fill_flag});
        }

        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) {
            PushCommand(DrawHollowBoxCmd{r, thickness, color});
        }

        void DrawRoundRectFrame(Rect<int> r, int corner_radius, int thickness, uint32_t color) {
            PushCommand(DrawRoundRectFrameCmd{r, corner_radius, thickness, color});
        }

        void DrawStringWithBorder(Rect<int> r, const wchar_t* text, BorderShape shape = BorderShape::Null,
                                  uint32_t text_color = COLOR_WHITE, uint32_t border_color = COLOR_WHITE) {
            PushCommand(DrawStringWithBorderCmd{r, text, shape, text_color, border_color});
        }

        void DrawGraph(Point<int> p, int handle, bool trans_flag) {
            PushCommand(DrawGraphCmd{p, handle, trans_flag});
        }

        void DrawRotaGraph(Point<int> p, double ext_rate, double angle, int graph_handle, bool trans_flag) {
            PushCommand(DrawRotaGraphCmd{p, ext_rate, angle, graph_handle, trans_flag});
        }

        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) {
            PushCommand(DrawRectRotaGraphCmd{p, src, ext_rate, angle, graph_handle, trans_flag, turn_flag});
        }

        void SetDrawBlendMode(BlendMode mode, int pal) { PushCommand(SetDrawBlendModeCmd{mode, pal}); }
        void SetDrawBright(int r, int g, int b) { PushCommand(SetDrawBrightCmd{r, g, b}); }
        void SetFontTypeNormal() { PushCommand(SetFontTypeNormalCmd{}); }
        void ChangeFont(const wchar_t* font_name) { PushCommand(ChangeFontCmd{font_name}); }
        void SetFontSize(int size) { PushCommand(SetFontSizeCmd{size}); }
        void SetFontThickness(int thickness) { PushCommand(SetFontThicknessCmd{thickness}); }
        void SetBackgroundColor(int r, int g, int b) { PushCommand(SetBackgroundColorCmd{r, g, b}); }
        void ExitApplication() { PushCommand(ExitApplicationCmd{}); }
    };
} // namespace ab
