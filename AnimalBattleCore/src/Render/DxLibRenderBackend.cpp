module;

#include <cstdint>
#include <DxLib.h>

module DxLibRenderBackend;

extern "C" bool g_is_exit_requested;

namespace ab {
    int DxLibRenderBackend::GetFontSize() const {
        return ::GetFontSize();
    }

    int DxLibRenderBackend::GetDrawStringWidth(const wchar_t* text) const {
        return ::GetDrawStringWidth(text, -1);
    }

    void DxLibRenderBackend::DrawString(Point<int> p, const wchar_t* text, uint32_t color) const {
        ::DrawString(p.x, p.y, text, color);
    }

    void DxLibRenderBackend::DrawBox(Rect<int> r, uint32_t color, bool fill_flag) const {
        ::DrawBox(r.x1, r.y1, r.x2, r.y2, color, fill_flag ? TRUE : FALSE);
    }

    void DxLibRenderBackend::DrawRoundRect(Rect<int> r, int rx, int ry, uint32_t color, bool fill_flag) const {
        ::DrawRoundRect(r.x1, r.y1, r.x2, r.y2, rx, ry, color, fill_flag ? TRUE : FALSE);
    }

    void DxLibRenderBackend::DrawGraph(Point<int> p, int handle, bool trans_flag) const {
        ::DrawGraph(p.x, p.y, handle, trans_flag ? TRUE : FALSE);
    }

    void DxLibRenderBackend::DrawRotaGraph(Point<int> p, double ext_rate, double angle,
                                           int graph_handle, bool trans_flag) const {
        ::DrawRotaGraph(p.x, p.y, ext_rate, angle, graph_handle, trans_flag);
    }

    void DxLibRenderBackend::DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle,
                                              int graph_handle, bool trans_flag, bool turn_flag) const {
        int src_width = src.x2 - src.x1;
        int src_height = src.y2 - src.y1;
        ::DrawRectRotaGraph(p.x, p.y, src.x1, src.y1, src_width, src_height, ext_rate, angle,
                            graph_handle, trans_flag ? TRUE : FALSE, turn_flag ? TRUE : FALSE);
    }

    void DxLibRenderBackend::SetDrawBlendMode(BlendMode mode, int pal) {
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

    void DxLibRenderBackend::SetDrawBright(int r, int g, int b) {
        ::SetDrawBright(r, g, b);
    }

    void DxLibRenderBackend::SetFontTypeNormal() {
        ::ChangeFontType(DX_FONTTYPE_NORMAL);
    }

    void DxLibRenderBackend::ChangeFont(const wchar_t* font_name) {
        ::ChangeFont(font_name);
    }

    void DxLibRenderBackend::SetFontSize(int size) {
        ::SetFontSize(size);
    }

    void DxLibRenderBackend::SetFontThickness(int thickness) {
        ::SetFontThickness(thickness);
    }

    void DxLibRenderBackend::SetBackgroundColor(int r, int g, int b) {
        ::SetBackgroundColor(r, g, b);
    }

    void DxLibRenderBackend::ExitApplication() {
        g_is_exit_requested = true;
    }
} // namespace ab
