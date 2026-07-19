module;

#include <cstdint>
#include <memory>
#include <RenderUtils.h>

export module RenderService;

namespace ab {
    /// @brief SetDrawBlendMode に渡すブレンドモード（DxLib の DX_BLENDMODE_* に対応）
    export enum class BlendMode
    {
        NoBlend, ///< ブレンドなし（DX_BLENDMODE_NOBLEND）
        Alpha, ///< アルファブレンド（DX_BLENDMODE_ALPHA）
    };

    /// @brief 各種描画処理（矩形、文字列、スプライト、ブレンドモードなど）を提供するレンダリングサービスインターフェース
    export class IRenderService
    {
    public:
        virtual ~IRenderService() = default;

        /// @brief 現在設定されているフォントサイズを取得する
        /// @return フォントサイズ
        virtual int GetFontSize() = 0;

        /// @brief 指定した座標に文字列を描画する
        /// @param x 描画先X座標
        /// @param y 描画先Y座標
        /// @param text 描画する文字列
        /// @param color 描画色
        virtual void DrawString(int x, int y, const wchar_t* text, uint32_t color) = 0;

        /// @brief 矩形を描画する（アンチエイリアス対応）
        /// @param x1 左上X座標
        /// @param y1 左上Y座標
        /// @param x2 右下X座標
        /// @param y2 右下Y座標
        /// @param color 描画色
        /// @param fillFlag 塗りつぶしフラグ（trueで塗りつぶし）
        virtual void DrawBoxAA(float x1, float y1, float x2, float y2, uint32_t color, bool fillFlag) = 0;

        /// @brief 指定した文字列を描画した際のピクセル幅を取得する
        /// @param text 文字列
        /// @return 文字列の描画幅（ピクセル）
        virtual int GetDrawStringWidth(const wchar_t* text) = 0;

        /// @brief 画像ハンドルを指定してグラフィックを描画する
        /// @param x 描画先X座標
        /// @param y 描画先Y座標
        /// @param handle 画像ハンドル
        /// @param transFlag 透過処理フラグ（trueで透過有効）
        virtual void DrawGraph(int x, int y, int handle, bool transFlag) = 0;

        /// @brief 画像ハンドルを指定してグラフィックを回転・拡大縮小描画する（座標はfloat）
        /// @param x 描画中心X座標
        /// @param y 描画中心Y座標
        /// @param extRate 拡大率
        /// @param angle 回転角度（ラジアン）
        /// @param graphHandle 画像ハンドル
        /// @param transFlag 透過処理フラグ
        virtual void DrawRotaGraphF(float x, float y, double extRate, double angle,
                                    int graphHandle, bool transFlag) = 0;

        /// @brief 切り出し領域を指定してグラフィックを回転・拡大縮小・左右反転描画する
        /// @param x 描画中心X座標
        /// @param y 描画中心Y座標
        /// @param srcX 切り出し元左上X座標
        /// @param srcY 切り出し元左上Y座標
        /// @param width 切り出し幅
        /// @param height 切り出し高さ
        /// @param extRate 拡大率
        /// @param angle 回転角度（ラジアン）
        /// @param graphHandle 画像ハンドル
        /// @param transFlag 透過処理フラグ
        /// @param turnFlag 左右反転フラグ（trueで反転）
        virtual void DrawRectRotaGraph(int x, int y, int srcX, int srcY, int width, int height, double extRate,
                                       double angle, int graphHandle, bool transFlag, bool turnFlag) = 0;

        /// @brief 描画時のブレンドモードとブレンドパラメータを設定する
        /// @param mode ブレンドモード
        /// @param pal 不透明度（0〜255）
        virtual void SetDrawBlendMode(BlendMode mode, int pal) = 0;

        /// @brief 描画時の輝度を設定する
        /// @param r 赤成分（0〜255）
        /// @param g 緑成分（0〜255）
        /// @param b 青成分（0〜255）
        virtual void SetDrawBright(int r, int g, int b) = 0;

        /// @brief 角丸矩形の枠線を描画する
        /// @param x1 左上X座標
        /// @param y1 左上Y座標
        /// @param x2 右下X座標
        /// @param y2 右下Y座標
        /// @param cornerRadius 角の半径
        /// @param thickness 枠線の太さ
        /// @param color 描画色
        virtual void DrawRoundRectFrame(int x1, int y1, int x2, int y2,
                                        int cornerRadius, int thickness, uint32_t color) = 0;

        /// @brief 塗り潰し三角形を描画する
        /// @param x1 頂点1 X座標
        /// @param y1 頂点1 Y座標
        /// @param x2 頂点2 X座標
        /// @param y2 頂点2 Y座標
        /// @param x3 頂点3 X座標
        /// @param y3 頂点3 Y座標
        /// @param color 描画色
        virtual void DrawFilledTriangle(int x1, int y1, int x2, int y2,
                                        int x3, int y3, uint32_t color) = 0;

        /// @brief 文字列を中央揃えで描画する（x, y は中心座標）
        /// @param x 中心X座標
        /// @param y 中心Y座標
        /// @param text 描画する文字列
        /// @param color 描画色
        virtual void DrawCenterString(int x, int y, const wchar_t* text, uint32_t color) = 0;

        /// @brief 文字列を左揃えで描画する（x は左端、y は中心Y座標）
        /// @param x 左端X座標
        /// @param y 中心Y座標
        /// @param text 描画する文字列
        /// @param color 描画色
        virtual void DrawLeftString(int x, int y, const wchar_t* text, uint32_t color) = 0;

        /// @brief 文字列を右揃えで描画する（x は右端、y は中心Y座標）
        /// @param x 右端X座標
        /// @param y 中心Y座標
        /// @param text 描画する文字列
        /// @param color 描画色
        virtual void DrawRightString(int x, int y, const wchar_t* text, uint32_t color) = 0;

        /// @brief 中空の矩形（枠線）を太さを指定して描画する
        /// @param x1 左上X座標
        /// @param y1 左上Y座標
        /// @param x2 右下X座標
        /// @param y2 右下Y座標
        /// @param thickness 線の太さ
        /// @param color 描画色
        virtual void DrawHollowBox(int x1, int y1, int x2, int y2, int thickness, uint32_t color) = 0;

        /// @brief ボタンを描画する（枠線と中央揃えテキスト付き）
        /// @param x1 左上X座標
        /// @param y1 左上Y座標
        /// @param x2 右下X座標
        /// @param y2 右下Y座標
        /// @param text ボタンのテキスト
        /// @param isFocus フォーカス中の場合は内側にもう一重枠を描画する（デフォルト: false）
        /// @param bgColor 背景色 (デフォルト: COLOR_BG)
        /// @param fgColor 文字と枠線の色 (デフォルト: COLOR_WHITE)
        virtual void DrawButton(int x1, int y1, int x2, int y2, const wchar_t* text,
                                bool isFocus = false,
                                uint32_t bgColor = COLOR_BG, uint32_t fgColor = COLOR_WHITE) = 0;

        /// @brief フォントタイプを通常モードに設定する（DX_FONTTYPE_NORMAL）
        virtual void SetFontTypeNormal() = 0;

        /// @brief 描画に使用するフォントを変更する
        /// @param fontName フォント名
        virtual void ChangeFont(const wchar_t* fontName) = 0;

        /// @brief 描画に使用するフォントのサイズを設定する
        /// @param size フォントサイズ
        virtual void SetFontSize(int size) = 0;

        /// @brief 描画に使用するフォントの太さを設定する
        /// @param thickness 太さ
        virtual void SetFontThickness(int thickness) = 0;

        /// @brief 画面の背景色を設定する
        /// @param r 赤成分（0〜255）
        /// @param g 緑成分（0〜255）
        /// @param b 青成分（0〜255）
        virtual void SetBackgroundColor(int r, int g, int b) = 0;

        /// @brief アプリケーションを終了する（dxe::ExitApplication() に委譲）
        virtual void ExitApplication() = 0;

        // -------------------------------------------------------------------------
        // Point / Rect を用いた直感的なオーバーロード API
        // -------------------------------------------------------------------------

        /// @brief 指定した座標（Point）に文字列を描画する
        void DrawString(Point<int> p, const wchar_t* text, uint32_t color)
        {
            DrawString(p.x, p.y, text, color);
        }

        /// @brief 矩形（Rect）を描画する（float）
        void DrawBoxAA(Rect<float> r, uint32_t color, bool fillFlag)
        {
            DrawBoxAA(r.x1, r.y1, r.x2, r.y2, color, fillFlag);
        }

        /// @brief 矩形（Rect）を描画する（int）
        void DrawBoxAA(Rect<int> r, uint32_t color, bool fillFlag)
        {
            DrawBoxAA(static_cast<float>(r.x1), static_cast<float>(r.y1),
                      static_cast<float>(r.x2), static_cast<float>(r.y2), color, fillFlag);
        }

        /// @brief 中空の矩形（枠線）を太さを指定して描画する
        void DrawHollowBox(Rect<int> r, int thickness, uint32_t color)
        {
            DrawHollowBox(r.x1, r.y1, r.x2, r.y2, thickness, color);
        }

        /// @brief 角丸矩形の枠線を描画する
        void DrawRoundRectFrame(Rect<int> r, int cornerRadius, int thickness, uint32_t color)
        {
            DrawRoundRectFrame(r.x1, r.y1, r.x2, r.y2, cornerRadius, thickness, color);
        }

        /// @brief 文字列を中央揃えで描画する（Point）
        void DrawCenterString(Point<int> p, const wchar_t* text, uint32_t color)
        {
            DrawCenterString(p.x, p.y, text, color);
        }

        /// @brief 文字列を左揃えで描画する（Point）
        void DrawLeftString(Point<int> p, const wchar_t* text, uint32_t color)
        {
            DrawLeftString(p.x, p.y, text, color);
        }

        /// @brief 文字列を右揃えで描画する（Point）
        void DrawRightString(Point<int> p, const wchar_t* text, uint32_t color)
        {
            DrawRightString(p.x, p.y, text, color);
        }

        /// @brief ボタンを描画する（Rect）
        void DrawButton(Rect<int> r, const wchar_t* text, bool isFocus = false,
                        uint32_t bgColor = COLOR_BG, uint32_t fgColor = COLOR_WHITE)
        {
            DrawButton(r.x1, r.y1, r.x2, r.y2, text, isFocus, bgColor, fgColor);
        }

        /// @brief オプションを描画する（枠線と中央揃えテキスト付き）
        /// @param r アリア
        /// @param text オプションのテキスト
        /// @param isFocus フォーカス中の場合は内側にもう一重枠を描画する（デフォルト: false）
        /// @param color 文字と枠線の色 (デフォルト: COLOR_WHITE, isFocusのときのデフォルト: COLOR_YELLOW)
        virtual void DrawOption(Rect<int> r, const wchar_t* text,
                                bool isFocus = false, uint32_t color = -1) = 0;

        /// @brief 画像ハンドルを指定してグラフィックを描画する（Point）
        void DrawGraph(Point<int> p, int handle, bool transFlag)
        {
            DrawGraph(p.x, p.y, handle, transFlag);
        }

        /// @brief 画像ハンドルを指定してグラフィックを回転・拡大縮小描画する（Point）
        void DrawRotaGraphF(Point<float> p, double extRate, double angle, int graphHandle, bool transFlag)
        {
            DrawRotaGraphF(p.x, p.y, extRate, angle, graphHandle, transFlag);
        }
    };

    export std::unique_ptr<IRenderService> CreateRenderService();
} // namespace ab
