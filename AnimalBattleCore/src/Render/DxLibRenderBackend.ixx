module;

#include <cstdint>

export module DxLibRenderBackend;

export import RenderTypes;
import RenderConcept;

namespace ab {
    /// @brief DxLib を使用した具象レンダリングバックエンド（原子描画プリミティブのみを提供）
    export class DxLibRenderBackend {
    public:
        DxLibRenderBackend() = default;

        /// @brief フォントサイズを取得する
        int GetFontSize() const;

        /// @brief 文字列描画時の幅を取得する
        int GetDrawStringWidth(const wchar_t* text) const;

        /// @brief 文字列を描画する
        void DrawString(Point<int> p, const wchar_t* text, uint32_t color) const;

        /// @brief 矩形を描画する
        void DrawBox(Rect<int> r, uint32_t color, bool fill_flag) const;

        /// @brief 角丸矩形を描画する
        void DrawRoundRect(Rect<int> r, int rx, int ry, uint32_t color, bool fill_flag) const;

        /// @brief 画像を描画する
        void DrawGraph(Point<int> p, int handle, bool trans_flag) const;

        /// @brief 拡大/縮小/回転画像を描画する
        void DrawRotaGraph(Point<int> p, double ext_rate, double angle, int graph_handle, bool trans_flag) const;

        /// @brief 矩形切り抜き拡大/縮小/回転画像を描画する
        void DrawRectRotaGraph(Point<int> p, Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) const;

        /// @brief 描画ブレンドモードを設定する
        void SetDrawBlendMode(BlendMode mode, int pal);

        /// @brief 描画輝度を設定する
        void SetDrawBright(int r, int g, int b);

        /// @brief フォントタイプを標準に戻す
        void SetFontTypeNormal();

        /// @brief フォントを変更する
        void ChangeFont(const wchar_t* font_name);

        /// @brief フォントサイズを設定する
        void SetFontSize(int size);

        /// @brief フォントの太さを設定する
        void SetFontThickness(int thickness);

        /// @brief 背景色を設定する
        void SetBackgroundColor(int r, int g, int b);

        /// @brief アプリケーション終了フラグを立てる
        void ExitApplication();
    };

    static_assert(RenderBackendConcept<DxLibRenderBackend>, "DxLibRenderBackend must satisfy RenderBackendConcept");
} // namespace ab
