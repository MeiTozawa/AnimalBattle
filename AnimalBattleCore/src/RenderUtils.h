#pragma once
#include <cstdint>
#include <concepts>
#include <app_build_setting.h>

namespace ab {
    
    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Rect;

    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Vec2;
    
    constexpr uint32_t COLOR_WHITE = 0xFFFFFF;
    constexpr uint32_t COLOR_BLACK = 0x000000;
    constexpr uint32_t COLOR_RED = 0xFF0000;
    constexpr uint32_t COLOR_GREEN = 0x00FF00;
    constexpr uint32_t COLOR_BLUE = 0x0000FF;
    constexpr uint32_t COLOR_YELLOW = 0xFFFF00;
    constexpr uint32_t COLOR_GRAY = 0x808080;
    
    constexpr uint32_t COLOR_BG = COLOR_WHITE;

    constexpr int FONT_SIZE = 48;  

    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Vec2
    {
        T x, y;
        
        constexpr bool In(const Rect<T>& r) const;
    };
    
    template <typename T>
    using Point = Vec2<T>;
    
    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    struct Rect
    {
        T x1, y1, x2, y2;
        
        constexpr Rect<T> operator+(const Point<T>& p) const
        {
            return Rect<T>{x1 + p.x, y1 + p.y, x2 + p.x, y2 + p.y};
        }
        
        constexpr Rect<T> Expand(T t) const
        {
            return Rect<T>{x1 - t, y1 - t, x2 + t, y2 + t};
        }
    };

    struct Sprite
    {
        int handle;
        Vec2<int> size;
    };

    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    constexpr bool Point<T>::In(const Rect<T>& r) const
    {
        return x >= r.x1 && x <= r.x2 && y >= r.y1 && y <= r.y2;
    }

}
