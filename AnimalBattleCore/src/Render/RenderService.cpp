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
    public:
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
            ::DrawString(p.x, p.y, text, color);
        }

        void DrawBox(Rect<int> r, uint32_t color, bool fillFlag) override
        {
            ::DrawBox(r.x1, r.y1, r.x2, r.y2, color, fillFlag ? TRUE : FALSE);
        }


        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) override
        {
            ::DrawBox(r.x1, r.y1, r.x2, r.y1 + thickness, color, TRUE);
            ::DrawBox(r.x1, r.y2 - thickness, r.x2, r.y2, color, TRUE);
            ::DrawBox(r.x1, r.y1, r.x1 + thickness, r.y2, color, TRUE);
            ::DrawBox(r.x2 - thickness, r.y1, r.x2, r.y2, color, TRUE);
        }

        void DrawRoundRectFrame(Rect<int> r, int cornerRadius, int thickness, uint32_t color) override
        {
            for (int i = 0; i < thickness; ++i)
                ::DrawRoundRect(r.x1 + i, r.y1 + i, r.x2 - i, r.y2 - i,
                                cornerRadius, cornerRadius, color, FALSE);
        }


        void DrawStringWithBorder(Rect<int> r, const wchar_t* text,
                        BorderShape shape, uint32_t textColor, uint32_t borderColor) override
        {
            constexpr int THICKNESS = 2;
            
            // 1. Draw border based on shape
            switch (shape)
            {
            case BorderShape::Rectangle:
                DrawHollowBox(r, THICKNESS, borderColor);
                break;
            case BorderShape::DoubleRectangle:
                DrawHollowBox(r, THICKNESS, borderColor);
                DrawHollowBox(r.Expand(-2 * THICKNESS), THICKNESS, borderColor);
                break;
            case BorderShape::Pill:
                {
                    int radius = (r.y2 - r.y1) / 2;
                    DrawRoundRectFrame(r, radius, THICKNESS, borderColor);
                }
                break;
            case BorderShape::Underline:
                ::DrawBox(r.x1, r.y2 - THICKNESS, r.x2, r.y2, borderColor, TRUE);
                break;
            case BorderShape::Null:
            default:
                break;
            }
            
            // 2. Draw text in the center
            const int drawX = (r.x1 + r.x2) / 2 - GetDrawStringWidth(text) / 2;
            const int drawY = (r.y1 + r.y2) / 2 - GetFontSize() / 2;
            ::DrawString(drawX, drawY, text, textColor);
        }



        void DrawGraph(Point<int> p, int handle, bool transFlag) override
        {
            ::DrawGraph(p.x, p.y, handle, transFlag ? TRUE : FALSE);
        }

        void DrawRotaGraphF(Point<float> p, double extRate, double angle,
                            int graphHandle, bool transFlag) override
        {
            ::DrawRotaGraphF(p.x, p.y, extRate, angle, graphHandle, transFlag ? TRUE : FALSE);
        }

        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double extRate, double angle,
                               int graphHandle, bool transFlag, bool turnFlag) override
        {
            int srcWidth = src.x2 - src.x1;
            int srcHeight = src.y2 - src.y1;
            ::DrawRectRotaGraph(p.x, p.y, src.x1, src.y1, srcWidth, srcHeight, extRate, angle,
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

    std::unique_ptr<IRenderService> CreateRenderService()
    {
        return std::unique_ptr<IRenderService>(new DxLibRenderService());
    }
} // namespace ab
