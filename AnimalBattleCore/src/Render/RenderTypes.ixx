module;

#include <cstdint>
#include <concepts>
#include <string>
#include <variant>

export module RenderTypes;

namespace ab {
    // --- Vector & Geometry Primitives ---

    export template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Rect;

    export template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Point {
        T x;
        T y;

        constexpr bool In(const Rect<T>& r) const;
    };

    export template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Vec2 {
        T x;
        T y;
    };

    export template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Rect {
        T x1;
        T y1;
        T x2;
        T y2;

        constexpr Rect<T> operator+(const Point<T>& p) const {
            return Rect<T>{x1 + p.x, y1 + p.y, x2 + p.x, y2 + p.y};
        }

        constexpr Rect<T> Expand(T amount) const {
            return Rect<T>{x1 - amount, y1 - amount, x2 + amount, y2 + amount};
        }

        constexpr bool Intersects(const Rect<T>& other) const {
            return x1 < other.x2 && x2 > other.x1 && y1 < other.y2 && y2 > other.y1;
        }

        constexpr bool Contains(T x, T y) const {
            return x >= x1 && x <= x2 && y >= y1 && y <= y2;
        }

        constexpr bool Contains(const Point<T>& p) const {
            return p.x >= x1 && p.x <= x2 && p.y >= y1 && p.y <= y2;
        }
    };

    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    constexpr bool Point<T>::In(const Rect<T>& r) const {
        return x >= r.x1 && x <= r.x2 && y >= r.y1 && y <= r.y2;
    }

    export struct Sprite {
        int handle;
        Vec2<int> size;
    };

    // --- Color Constants ---
    export constexpr uint32_t COLOR_WHITE = 0xFFFFFF;
    export constexpr uint32_t COLOR_BLACK = 0x000000;
    export constexpr uint32_t COLOR_RED = 0xFF0000;
    export constexpr uint32_t COLOR_GREEN = 0x00FF00;
    export constexpr uint32_t COLOR_BLUE = 0x0000FF;
    export constexpr uint32_t COLOR_YELLOW = 0xFFFF00;
    export constexpr uint32_t COLOR_GRAY = 0x808080;
    export constexpr uint32_t COLOR_BG = COLOR_WHITE;

    export constexpr int FONT_SIZE = 48;

    /// @brief SetDrawBlendMode に渡すブレンドモード（DxLib の DX_BLENDMODE_* に対応）
    export enum class BlendMode {
        NoBlend,  ///< ブレンドなし（DX_BLENDMODE_NOBLEND）
        Alpha,    ///< アルファブレンド（DX_BLENDMODE_ALPHA）
    };

    export enum class BorderShape {
        Null,
        Rectangle,
        DoubleRectangle,
        Pill,
        Underline,
    };

    // --- Render Commands ---

    export struct DrawStringCmd {
        Point<int> p;
        std::wstring text;
        uint32_t color;
    };

    export struct DrawBoxCmd {
        Rect<int> r;
        uint32_t color;
        bool fill_flag;
    };

    export struct DrawCircleCmd {
        Point<int> p;
        int radius;
        uint32_t color;
        bool fill_flag;
    };

    export struct DrawLineCmd {
        Point<int> p1;
        Point<int> p2;
        uint32_t color;
        int thickness;
    };

    export struct DrawHollowBoxCmd {
        Rect<int> r;
        int thickness;
        uint32_t color;
    };

    export struct DrawRoundRectFrameCmd {
        Rect<int> r;
        int corner_radius;
        int thickness;
        uint32_t color;
    };

    export struct DrawStringWithBorderCmd {
        Rect<int> r;
        std::wstring text;
        BorderShape shape;
        uint32_t text_color;
        uint32_t border_color;
    };

    export struct DrawGraphCmd {
        Point<int> p;
        int handle;
        bool trans_flag;
    };

    export struct DrawRotaGraphCmd {
        Point<int> p;
        double ext_rate;
        double angle;
        int graph_handle;
        bool trans_flag;
    };

    export struct DrawRectRotaGraphCmd {
        Point<int> p;
        Rect<int> src;
        double ext_rate;
        double angle;
        int graph_handle;
        bool trans_flag;
        bool turn_flag;
    };

    export struct SetDrawBlendModeCmd {
        BlendMode mode;
        int pal;
    };

    export struct SetDrawBrightCmd {
        int r;
        int g;
        int b;
    };

    export struct SetFontTypeNormalCmd {};

    export struct ChangeFontCmd {
        const wchar_t* font_name;
    };

    export struct SetFontSizeCmd {
        int size;
    };

    export struct SetFontThicknessCmd {
        int thickness;
    };

    export struct SetBackgroundColorCmd {
        int r;
        int g;
        int b;
    };

    export struct ExitApplicationCmd {};

    export struct SetCameraEnabledCmd {
        bool enabled;
    };

    export struct SetCameraStateCmd {
        float x;
        float y;
        float scale;
    };

    /// @brief すべての描画コマンドを包含する std::variant
    export using RenderCommand = std::variant<
        DrawStringCmd,
        DrawBoxCmd,
        DrawCircleCmd,
        DrawLineCmd,
        DrawHollowBoxCmd,
        DrawRoundRectFrameCmd,
        DrawStringWithBorderCmd,
        DrawGraphCmd,
        DrawRotaGraphCmd,
        DrawRectRotaGraphCmd,
        SetDrawBlendModeCmd,
        SetDrawBrightCmd,
        SetFontTypeNormalCmd,
        ChangeFontCmd,
        SetFontSizeCmd,
        SetFontThicknessCmd,
        SetBackgroundColorCmd,
        ExitApplicationCmd,
        SetCameraEnabledCmd,
        SetCameraStateCmd
    >;

    /// @brief std::visit 用ラムダオーバーロードヘルパー構造体
    export template <class... Ts>
    struct Overloaded : Ts... {
        using Ts::operator()...;
    };
} // namespace ab
