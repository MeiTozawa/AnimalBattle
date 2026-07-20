module;

#include <cstdint>
#include <memory>
#include <DxLib.h>
#include <RenderUtils.h>

module RenderService;
namespace ab {
    extern bool g_IsExitRequested;

    class DxLibRenderService : public IRenderService
    {
    private:
        int m_viewportWidth = 1280;
        int m_viewportHeight = 720;
        bool m_cameraEnabled = false;
        float m_cameraX = 640.0f;
        float m_cameraY = 360.0f;
        float m_cameraScale = 1.0f;

    public:
        DxLibRenderService(int width, int height)
            : m_viewportWidth(width), m_viewportHeight(height)
        {
        }

        void SetCameraEnabled(bool enabled) override
        {
            m_cameraEnabled = enabled;
        }

        void SetCameraState(float x, float y, float scale) override
        {
            m_cameraX = x;
            m_cameraY = y;
            m_cameraScale = scale;
        }

        int GetFontSize() override
        {
            return ::GetFontSize();
        }

        int GetDrawStringWidth(const wchar_t* text) override
        {
            return ::GetDrawStringWidth(text, -1);
        }

        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) override
        {
            Point<int> p_draw = p;
            if (m_cameraEnabled)
            {
                p_draw.x = static_cast<int>((p.x - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                p_draw.y = static_cast<int>((p.y - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
            }
            ::DrawString(p_draw.x, p_draw.y, text, color);
        }

        void DrawBox(Rect<int> r, uint32_t color, bool fillFlag) override
        {
            Rect<int> r_draw = r;
            if (m_cameraEnabled)
            {
                r_draw.x1 = static_cast<int>((r.x1 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
            }
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y2, color, fillFlag ? TRUE : FALSE);
        }

        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) override
        {
            Rect<int> r_draw = r;
            int thickness_draw = thickness;
            if (m_cameraEnabled)
            {
                r_draw.x1 = static_cast<int>((r.x1 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                thickness_draw = static_cast<int>(thickness * m_cameraScale);
                if (thickness_draw < 1 && thickness > 0) thickness_draw = 1;
            }
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, color, TRUE);
            ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, color, TRUE);
            ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, color, TRUE);
            ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, color, TRUE);
        }

        void DrawRoundRectFrame(Rect<int> r, int cornerRadius, int thickness, uint32_t color) override
        {
            Rect<int> r_draw = r;
            int thickness_draw = thickness;
            int radius_draw = cornerRadius;
            if (m_cameraEnabled)
            {
                r_draw.x1 = static_cast<int>((r.x1 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                thickness_draw = static_cast<int>(thickness * m_cameraScale);
                if (thickness_draw < 1 && thickness > 0) thickness_draw = 1;
                radius_draw = static_cast<int>(cornerRadius * m_cameraScale);
            }
            for (int i = 0; i < thickness_draw; ++i)
                ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                radius_draw, radius_draw, color, FALSE);
        }

        void DrawStringWithBorder(Rect<int> r, const wchar_t* text,
                                  BorderShape shape, uint32_t textColor, uint32_t borderColor) override
        {
            Rect<int> r_draw = r;
            int thickness_draw = 2;
            if (m_cameraEnabled)
            {
                r_draw.x1 = static_cast<int>((r.x1 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y1 = static_cast<int>((r.y1 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                r_draw.x2 = static_cast<int>((r.x2 - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                r_draw.y2 = static_cast<int>((r.y2 - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                thickness_draw = static_cast<int>(2 * m_cameraScale);
                if (thickness_draw < 1) thickness_draw = 1;
            }

            switch (shape)
            {
            case BorderShape::Rectangle:
                ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, borderColor, TRUE);
                ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, borderColor, TRUE);
                ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, borderColor, TRUE);
                ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, borderColor, TRUE);
                break;
            case BorderShape::DoubleRectangle:
                {
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x2, r_draw.y1 + thickness_draw, borderColor, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, borderColor, TRUE);
                    ::DrawBox(r_draw.x1, r_draw.y1, r_draw.x1 + thickness_draw, r_draw.y2, borderColor, TRUE);
                    ::DrawBox(r_draw.x2 - thickness_draw, r_draw.y1, r_draw.x2, r_draw.y2, borderColor, TRUE);

                    Rect<int> r_inner = r_draw.Expand(-2 * thickness_draw);
                    ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x2, r_inner.y1 + thickness_draw, borderColor, TRUE);
                    ::DrawBox(r_inner.x1, r_inner.y2 - thickness_draw, r_inner.x2, r_inner.y2, borderColor, TRUE);
                    ::DrawBox(r_inner.x1, r_inner.y1, r_inner.x1 + thickness_draw, r_inner.y2, borderColor, TRUE);
                    ::DrawBox(r_inner.x2 - thickness_draw, r_inner.y1, r_inner.x2, r_inner.y2, borderColor, TRUE);
                }
                break;
            case BorderShape::Pill:
                {
                    int radius = (r_draw.y2 - r_draw.y1) / 2;
                    for (int i = 0; i < thickness_draw; ++i)
                        ::DrawRoundRect(r_draw.x1 + i, r_draw.y1 + i, r_draw.x2 - i, r_draw.y2 - i,
                                        radius, radius, borderColor, FALSE);
                }
                break;
            case BorderShape::Underline:
                ::DrawBox(r_draw.x1, r_draw.y2 - thickness_draw, r_draw.x2, r_draw.y2, borderColor, TRUE);
                break;
            case BorderShape::Null:
            default:
                break;
            }

            const int drawX = (r_draw.x1 + r_draw.x2) / 2 - GetDrawStringWidth(text) / 2;
            const int drawY = (r_draw.y1 + r_draw.y2) / 2 - GetFontSize() / 2;
            ::DrawString(drawX, drawY, text, textColor);
        }

        void DrawGraph(Point<int> p, int handle, bool transFlag) override
        {
            if (m_cameraEnabled)
            {
                int w = 0, h = 0;
                ::GetGraphSize(handle, &w, &h);
                int x1 = static_cast<int>((p.x - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                int y1 = static_cast<int>((p.y - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                int x2 = x1 + static_cast<int>(w * m_cameraScale);
                int y2 = y1 + static_cast<int>(h * m_cameraScale);
                ::DrawExtendGraph(x1, y1, x2, y2, handle, transFlag ? TRUE : FALSE);
            }
            else
            {
                ::DrawGraph(p.x, p.y, handle, transFlag ? TRUE : FALSE);
            }
        }

        void DrawRotaGraphF(Point<float> p, double extRate, double angle,
                            int graphHandle, bool transFlag) override
        {
            Point<float> p_draw = p;
            double scale_draw = extRate;
            if (m_cameraEnabled)
            {
                p_draw.x = static_cast<float>((p.x - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                p_draw.y = static_cast<float>((p.y - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                scale_draw = extRate * m_cameraScale;
            }
            ::DrawRotaGraphF(p_draw.x, p_draw.y, scale_draw, angle, graphHandle, transFlag ? TRUE : FALSE);
        }

        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double extRate, double angle,
                               int graphHandle, bool transFlag, bool turnFlag) override
        {
            Point<int> p_draw = p;
            double scale_draw = extRate;
            if (m_cameraEnabled)
            {
                p_draw.x = static_cast<int>((p.x - m_cameraX) * m_cameraScale + m_viewportWidth / 2.0f);
                p_draw.y = static_cast<int>((p.y - m_cameraY) * m_cameraScale + m_viewportHeight / 2.0f);
                scale_draw = extRate * m_cameraScale;
            }
            int srcWidth = src.x2 - src.x1;
            int srcHeight = src.y2 - src.y1;
            ::DrawRectRotaGraph(p_draw.x, p_draw.y, src.x1, src.y1, srcWidth, srcHeight, scale_draw, angle,
                                graphHandle, transFlag ? TRUE : FALSE, turnFlag ? TRUE : FALSE);
        }

        void SetDrawBlendMode(BlendMode mode, int pal) override
        {
            int dxMode = DX_BLENDMODE_NOBLEND;
            switch (mode)
            {
            case BlendMode::Alpha: dxMode = DX_BLENDMODE_ALPHA;
                break;
            case BlendMode::NoBlend: dxMode = DX_BLENDMODE_NOBLEND;
                break;
            }
            ::SetDrawBlendMode(dxMode, pal);
        }

        void SetDrawBright(int r, int g, int b) override
        {
            ::SetDrawBright(r, g, b);
        }

        void SetFontTypeNormal() override
        {
            ::ChangeFontType(DX_FONTTYPE_NORMAL);
        }

        void ChangeFont(const wchar_t* fontName) override
        {
            ::ChangeFont(fontName);
        }

        void SetFontSize(int size) override
        {
            ::SetFontSize(size);
        }

        void SetFontThickness(int thickness) override
        {
            ::SetFontThickness(thickness);
        }

        void SetBackgroundColor(int r, int g, int b) override
        {
            ::SetBackgroundColor(r, g, b);
        }

        void ExitApplication() override
        {
            g_IsExitRequested = true;
        }
    };

    std::unique_ptr<IRenderService> CreateRenderService(int width, int height)
    {
        return std::unique_ptr<IRenderService>(new DxLibRenderService(width, height));
    }
} // namespace ab
