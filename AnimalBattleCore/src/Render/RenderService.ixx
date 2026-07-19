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

    export enum class BorderShape
    {
        Null,
        Rectangle,
        DoubleRectangle,
        Pill,
        Underline,
    };

    /// @brief 各種描画処理（矩形、文字列、スプライト、ブレンドモードなど）を提供するレンダリングサービスインターフェース
    export class IRenderService
    {
    public:
        virtual ~IRenderService() = default;

        /// @brief 現在設定されているフォントサイズを取得する
        /// @return フォントサイズ
        virtual int GetFontSize() = 0;

        /// @brief 指定した文字列を描画した際のピクセル幅を取得する
        /// @param text 文字列
        /// @return 文字列の描画幅（ピクセル）
        virtual int GetDrawStringWidth(const wchar_t* text) = 0;

        /// @brief 指定した座標（Point）に文字列を描画する
        virtual void DrawString(Point<int> p, const wchar_t* text, uint32_t color) = 0;
        
        /// @brief 矩形（Rect）を描画する（int）
        virtual void DrawBox(Rect<int> r, uint32_t color, bool fillFlag) = 0;

        /// @brief 中空の矩形（枠線）を太さを指定して描画する
        virtual void DrawHollowBox(Rect<int> r, int thickness, uint32_t color) = 0;

        /// @brief 角丸矩形の枠線を描画する
        virtual void DrawRoundRectFrame(Rect<int> r, int cornerRadius, int thickness, uint32_t color) = 0;

        /// @brief ボーダーと文字を描画する
        virtual void DrawStringWithBorder(Rect<int> r, const wchar_t* text, BorderShape shape = BorderShape::Null,
                                uint32_t textColor = COLOR_WHITE, uint32_t borderColor = COLOR_WHITE) = 0;


        /// @brief 画像ハンドルを指定してグラフィックを描画する（Point）
        virtual void DrawGraph(Point<int> p, int handle, bool transFlag) = 0;

        /// @brief 画像ハンドルを指定してグラフィックを回転・拡大縮小描画する（Point）
        virtual void DrawRotaGraphF(Point<float> p, double extRate, double angle, int graphHandle, bool transFlag) = 0;

        /// @brief 切り出し領域を指定してグラフィックを回転・拡大縮小・左右反転描画する
        virtual void DrawRectRotaGraph(Point<int> p, Rect<int> src, double extRate, double angle, int graphHandle, bool transFlag, bool turnFlag) = 0;

        /// @brief 描画時のブレンドモードとブレンドパラメータを設定する
        /// @param mode ブレンドモード
        /// @param pal 不透明度（0〜255）
        virtual void SetDrawBlendMode(BlendMode mode, int pal) = 0;

        /// @brief 描画時の輝度を設定する
        /// @param r 赤成分（0〜255）
        /// @param g 緑成分（0〜255）
        /// @param b 青成分（0〜255）
        virtual void SetDrawBright(int r, int g, int b) = 0;

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
    };

    export std::unique_ptr<IRenderService> CreateRenderService();
} // namespace ab
