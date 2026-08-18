module;

#include <cstdint>

export module RenderService;

export import RenderTypes;

namespace ab {
    /// @brief バックエンド非依存のレンダリングサービス抽象インターフェース
    export class IRenderService {
    public:
        virtual ~IRenderService() = default;

        // --- 画面・フレーム制御 ---
        virtual bool ClearScreen() = 0;
        virtual bool ScreenFlip() = 0;
        virtual void ClearCommands() = 0;
        virtual void FlushCommands() = 0;

        // --- テクスチャ・アセット管理 ---
        virtual int LoadGraph(const wchar_t* file_path) = 0;
        virtual bool DeleteGraph(int handle) = 0;
        virtual void GetGraphSize(int handle, int* out_width, int* out_height) const = 0;

        // --- 2D プリミティブ・テキスト描画 ---
        virtual void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) = 0;
        virtual void DrawCircle(Point<int> p, int radius, uint32_t color, bool fill_flag) = 0;
        virtual void DrawLine(Point<int> p1, Point<int> p2, uint32_t color, int thickness) = 0;
        virtual void DrawString(Point<int> p, const wchar_t* text, uint32_t color) = 0;
        virtual void DrawGraph(Point<int> p, int handle, bool trans_flag) = 0;
        virtual void DrawRotaGraph(Point<int> p, double ext_rate, double angle, int graph_handle, bool trans_flag) = 0;
        virtual void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) = 0;
        virtual void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) = 0;
        virtual void DrawRoundRectFrame(Rect<int> r, int corner_radius, int thickness, uint32_t color) = 0;
        virtual void DrawStringWithBorder(Rect<int> r, const wchar_t* text, BorderShape shape = BorderShape::Null,
                                          uint32_t text_color = COLOR_WHITE, uint32_t border_color = COLOR_WHITE) = 0;

        // --- カメラ・状態設定 ---
        virtual void SetCameraEnabled(bool enabled) = 0;
        virtual void SetCameraState(float x, float y, float scale) = 0;
        virtual void SetDrawBlendMode(BlendMode mode, int pal) = 0;
        virtual void SetDrawBright(int r, int g, int b) = 0;
        virtual void SetFontTypeNormal() = 0;
        virtual void ChangeFont(const wchar_t* font_name) = 0;
        virtual void SetFontSize(int size) = 0;
        virtual void SetFontThickness(int thickness) = 0;
        virtual void SetBackgroundColor(int r, int g, int b) = 0;
        virtual void ExitApplication() = 0;

        // --- 情報取得 ---
        virtual int GetFontSize() const = 0;
        virtual int GetDrawStringWidth(const wchar_t* text) const = 0;
    };
} // namespace ab
