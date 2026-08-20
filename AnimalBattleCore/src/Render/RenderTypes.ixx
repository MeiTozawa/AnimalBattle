module;

#include <cstdint>
#include <concepts>
#include <string>
#include <variant>

export module RenderTypes;

export import PhysicsTypes;

namespace ab {
    // --- Vector & Geometry Primitives ---

    export template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Rect {
        Vector2D<T> p1;
        Vector2D<T> p2;

        constexpr Rect() : p1{}, p2{} {}
        constexpr Rect(Vector2D<T> point1, Vector2D<T> point2) : p1(point1), p2(point2) {}
        constexpr Rect(T x1, T y1, T x2, T y2) : p1(x1, y1), p2(x2, y2) {}

        constexpr T Left() const noexcept { return p1.x; }
        constexpr T Top() const noexcept { return p1.y; }
        constexpr T Right() const noexcept { return p2.x; }
        constexpr T Bottom() const noexcept { return p2.y; }
        constexpr T Width() const noexcept { return p2.x - p1.x; }
        constexpr T Height() const noexcept { return p2.y - p1.y; }

        constexpr Rect<T> operator+(const Vector2D<T>& p) const {
            return Rect<T>{p1 + p, p2 + p};
        }

        constexpr Rect<T> Expand(T amount) const {
            return Rect<T>{p1.x - amount, p1.y - amount, p2.x + amount, p2.y + amount};
        }

        constexpr bool Intersects(const Rect<T>& other) const {
            return p1.x < other.p2.x && p2.x > other.p1.x && p1.y < other.p2.y && p2.y > other.p1.y;
        }

        constexpr bool Contains(T x, T y) const {
            return x >= p1.x && x <= p2.x && y >= p1.y && y <= p2.y;
        }

        constexpr bool Contains(const Vector2D<T>& p) const {
            return p.x >= p1.x && p.x <= p2.x && p.y >= p1.y && p.y <= p2.y;
        }
    };

    export using Recti = Rect<int>;
    export using Rectf = Rect<float>;

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
        Add,      ///< 加算ブレンド（DX_BLENDMODE_ADD）
        Sub,      ///< 減算ブレンド（DX_BLENDMODE_SUB）
        Mul,      ///< 乗算ブレンド（DX_BLENDMODE_MULA）
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
        Vec2i pos;
        std::wstring text;
        uint32_t color;
    };

    export struct DrawBoxCmd {
        Rect<int> r;
        uint32_t color;
        bool fill_flag;
    };

    export struct DrawCircleCmd {
        Vec2i center;
        int radius;
        uint32_t color;
        bool fill_flag;
    };

    export struct DrawLineCmd {
        Vec2i p1;
        Vec2i p2;
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
        Vec2i pos;
        int handle;
        bool trans_flag;
    };

    export struct DrawRotaGraphCmd {
        Vec2i center;
        double ext_rate;
        double angle;
        int graph_handle;
        bool trans_flag;
    };

    export struct DrawRectRotaGraphCmd {
        Vec2i center;
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
