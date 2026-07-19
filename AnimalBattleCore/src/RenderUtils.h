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

    constexpr uint32_t COLOR_BG = 0x071F38;

    constexpr uint32_t COLOR_WHITE = 0xFFFFFF;
    constexpr uint32_t COLOR_BLACK = 0x000000;
    constexpr uint32_t COLOR_RED = 0xFF0000;
    constexpr uint32_t COLOR_GREEN = 0x00FF00;
    constexpr uint32_t COLOR_BLUE = 0x0000FF;
    constexpr uint32_t COLOR_YELLOW = 0xFFFF00;
    constexpr uint32_t COLOR_GRAY = 0x808080;

    constexpr uint32_t COLOR_TEXT_NORMAL = 0xC8C8C8;
    constexpr uint32_t COLOR_TEXT_BLUE = 0x96C8FF;
    constexpr uint32_t COLOR_TEXT_RED = 0xFF9696;
    constexpr uint32_t COLOR_TEXT_GREEN = 0xC8FFC8;

    constexpr uint32_t COLOR_CARD_DEFAULT = 0x79D5EE;
    constexpr uint32_t COLOR_CARD_ROCK = 0x555555;
    constexpr uint32_t COLOR_CARD_PAPER = 0xF5F5DC;
    constexpr uint32_t COLOR_CARD_SCISSORS = 0xB0C4DE;
    constexpr uint32_t COLOR_CARD_MAGIC = 0x9370DB;
    
    // MenuScene 
    constexpr int MENU_BOX_MARGIN_X = 100;
    constexpr int MENU_BOX_MARGIN_Y_UP = 75;
    constexpr int MENU_BOX_MARGIN_Y_DOWN = 250;
    constexpr int MENU_CONTENT_START_OFFSET_Y = 50;
    constexpr uint32_t MENU_COLOR_BOX_BG = 0x1E1E28;
    constexpr int MENU_BOX_X1 = MENU_BOX_MARGIN_X;
    constexpr int MENU_TEXT_Y = MENU_BOX_MARGIN_Y_UP + MENU_CONTENT_START_OFFSET_Y;

    constexpr int FONT_SIZE = 48;  
    
    constexpr int CARD_HEIGHT = 300;
    constexpr int CARD_WIDTH = 200;
    
    constexpr int ICON_SIZE_X = 128; 
    constexpr int ICON_SIZE_Y = 128; 

    constexpr int ACTION_MENU_X = 400;  ///< 行動ボタン左端 X
    constexpr int ACTION_MENU_Y = 200;  ///< 行動ボタン最上段 Y
    constexpr int ACTION_MENU_W = 300;  ///< 行動ボタン幅
    constexpr int ACTION_MENU_H = 100;  ///< 行動ボタン高さ
    constexpr int ACTION_MENU_STEP_Y = 120;  ///< 行動ボタン間の Y オフセット

    constexpr int DRAW_PILE_X1 = 50;   ///< 山札領域 左端 X
    constexpr int DRAW_PILE_Y1 = 400;  ///< 山札領域 上端 Y
    constexpr int DRAW_PILE_X2 = DRAW_PILE_X1 + CARD_WIDTH;  ///< 山札領域 右端 X  
    constexpr int DRAW_PILE_Y2 = DRAW_PILE_Y1 + CARD_HEIGHT;  ///< 山札領域 下端 Y  
    
    constexpr int MENU_ICON_X = WINDOW_WIDTH - 60;   ///< 右端からのオフセット
    constexpr int MENU_ICON_Y = 60;   ///< ボタン中心 Y
    constexpr int MENU_ICON_HALF_W = 40;   ///< 判定半幅 X
    constexpr int MENU_ICON_HALF_H = 28;   ///< 判定半幅 Y

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

    template <typename T>
    requires std::integral<T> || std::floating_point<T>
    constexpr bool Point<T>::In(const Rect<T>& r) const
    {
        return x >= r.x1 && x <= r.x2 && y >= r.y1 && y <= r.y2;
    }


    
    constexpr Rect<int> DRAW_PILE_RECT = {
        DRAW_PILE_X1, DRAW_PILE_Y1,
        DRAW_PILE_X2, DRAW_PILE_Y2
    };
    
    constexpr Rect<int> MENU_ICON_RECT = {
        MENU_ICON_X - MENU_ICON_HALF_W, MENU_ICON_Y - MENU_ICON_HALF_H,
        MENU_ICON_X + MENU_ICON_HALF_W, MENU_ICON_Y + MENU_ICON_HALF_H
    };
    
    constexpr Rect<int> MENU_BOX_RECT = {
        MENU_BOX_MARGIN_X, MENU_BOX_MARGIN_Y_UP,
        WINDOW_WIDTH - MENU_BOX_MARGIN_X, WINDOW_HEIGHT - MENU_BOX_MARGIN_Y_DOWN
    };
}
