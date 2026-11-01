module;

#include <cstdint>
#include <concepts>

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

        constexpr bool Contains(const Vector2D<T>& p) const {
            return p.x >= p1.x && p.x <= p2.x && p.y >= p1.y && p.y <= p2.y;
        }
    };

    export using Recti = Rect<int>;
    export using Rectf = Rect<float>;

    /// @brief SetDrawBlendMode に渡すブレンドモード（DxLib の DX_BLENDMODE_* に対応）
    export enum class BlendMode {
        NoBlend,  ///< ブレンドなし（DX_BLENDMODE_NOBLEND）
        Alpha,    ///< アルファブレンド（DX_BLENDMODE_ALPHA）
        Add,      ///< 加算ブレンド（DX_BLENDMODE_ADD）
        Sub,      ///< 減算ブレンド（DX_BLENDMODE_SUB）
        Mul,      ///< 乗算ブレンド（DX_BLENDMODE_MULA）
    };
} // namespace ab
