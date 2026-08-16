module;

#include <cstdint>
#include <concepts>
#include <type_traits>

export module RenderConcept;

export import RenderTypes;

namespace ab {
    /// @brief 描画コマンド型に要求される C++20 Concept
    export template <typename T>
    concept RenderCommandConcept = std::is_object_v<T>;

    /// @brief レンダーバックエンド / レンダラーが満たすべき C++20 Concept
    /// オブジェクト指向の仮想継承 (IRenderer) を使用せず、静的多態 (Concepts / Templates) で描画能力を検証
    export template <typename T>
    concept RenderBackendConcept = requires(
        T b,
        const T const_b,
        Point<int> p_int,
        Rect<int> r_int,
        const wchar_t* text,
        uint32_t color,
        bool flag,
        int handle,
        double ext_rate,
        double angle,
        BlendMode blend_mode,
        int pal,
        int r, int g, int b_val,
        int font_size
    ) {
        // 情報取得（const）
        { const_b.GetFontSize() } -> std::same_as<int>;
        { const_b.GetDrawStringWidth(text) } -> std::same_as<int>;

        // 描画プリミティブ
        { b.DrawString(p_int, text, color) } -> std::same_as<void>;
        { b.DrawBox(r_int, color, flag) } -> std::same_as<void>;
        { b.DrawGraph(p_int, handle, flag) } -> std::same_as<void>;
        { b.DrawRotaGraph(p_int, ext_rate, angle, handle, flag) } -> std::same_as<void>;
        { b.DrawRectRotaGraph(p_int, r_int, ext_rate, angle, handle, flag, flag) } -> std::same_as<void>;

        // 状態変更
        { b.SetDrawBlendMode(blend_mode, pal) } -> std::same_as<void>;
        { b.SetDrawBright(r, g, b_val) } -> std::same_as<void>;
        { b.SetFontTypeNormal() } -> std::same_as<void>;
        { b.ChangeFont(text) } -> std::same_as<void>;
        { b.SetFontSize(font_size) } -> std::same_as<void>;
        { b.SetFontThickness(font_size) } -> std::same_as<void>;
        { b.SetBackgroundColor(r, g, b_val) } -> std::same_as<void>;
        { b.ExitApplication() } -> std::same_as<void>;
    };
} // namespace ab
