#include <cstdint>
#include <string>
#include <memory>
#include <Windows.h>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

import RenderService;
import DxLibRenderBackend;

extern "C" {
bool g_is_exit_requested = false;
}

namespace {

using RenderServiceType = ab::RenderService<ab::DxLibRenderBackend>;

std::unique_ptr<RenderServiceType> g_render_service = nullptr;
HWND g_hwnd = nullptr;
HDC g_hdc = nullptr;

}  // namespace

RenderServiceType* GetGlobalRenderService() noexcept {
    return g_render_service.get();
}

extern "C" {

    // --- Engine Control APIs ---

    __declspec(dllexport) bool Engine_Init(void* hwnd, int width, int height) noexcept {
        try {
            g_hwnd = static_cast<HWND>(hwnd);
            g_render_service = std::make_unique<RenderServiceType>(width, height);
            if (g_hwnd != nullptr) {
                ::SetUserWindow(g_hwnd);
            } else {
                ::ChangeWindowMode(TRUE);
                ::SetMainWindowText(L"AnimalBattle");
                ::SetAlwaysRunFlag(TRUE);
            }
            ::SetGraphMode(width, height, 32);
            ::SetBackgroundColor(24, 28, 44);
            if (::DxLib_Init() == -1) {
                return false;
            }
            ::SetDrawScreen(DX_SCREEN_BACK);
            if (g_hwnd != nullptr) {
                g_hdc = ::GetDC(g_hwnd);
            }
            return true;
        } catch (...) {
            return false;
        }
    }

    __declspec(dllexport) void Engine_Shutdown() noexcept {
        try {
            if (g_hwnd != nullptr && g_hdc != nullptr) {
                ::ReleaseDC(g_hwnd, g_hdc);
                g_hdc = nullptr;
            }

            g_render_service.reset();
            ::DxLib_End();
        } catch (...) {
        }
    }

    __declspec(dllexport) bool Engine_ProcessMessages() noexcept {
        try {
            return ::ProcessMessage() == 0;
        } catch (...) {
            return false;
        }
    }

    __declspec(dllexport) bool Engine_BeginDraw() noexcept {
        try {
            if (g_render_service != nullptr) {
                g_render_service->ClearCommands();
            }
            return ::ClearDrawScreen() == 0;
        } catch (...) {
            return false;
        }
    }

    __declspec(dllexport) bool Engine_EndDraw() noexcept {
        try {
            if (g_render_service != nullptr) {
                g_render_service->FlushCommands();
            }
            return ::ScreenFlip() == 0;
        } catch (...) {
            return false;
        }
    }

    __declspec(dllexport) void Engine_RequestExit() noexcept {
        g_is_exit_requested = true;
    }

    __declspec(dllexport) bool Engine_IsExitRequested() noexcept {
        return g_is_exit_requested;
    }

    // --- Input APIs ---

    __declspec(dllexport) bool Input_IsKeyHit(int key_code) noexcept {
        try {
            return ::CheckHitKey(key_code) == 1;
        } catch (...) {
            return false;
        }
    }

    __declspec(dllexport) int Input_GetJoypadState(int pad_index) noexcept {
        try {
            // pad_index: 1 -> DX_INPUT_PAD1 (0x0001), 2 -> DX_INPUT_PAD2 (0x0002), etc.
            int dx_pad_id = (pad_index >= 1 && pad_index <= 4) ? pad_index : 1;
            return ::GetJoypadInputState(dx_pad_id);
        } catch (...) {
            return 0;
        }
    }

    // --- Graph / Texture APIs ---

    __declspec(dllexport) int Graph_Load(const char* file_path) noexcept {
        if (!file_path) {
            return -1;
        }
        try {
            std::string s(file_path);
            std::wstring w(s.begin(), s.end());
            return ::LoadGraph(w.c_str());
        } catch (...) {
            return -1;
        }
    }

    __declspec(dllexport) int Engine_LoadGraph(const wchar_t* file_path) noexcept {
        if (!file_path) {
            return -1;
        }
        try {
            return ::LoadGraph(file_path);
        } catch (...) {
            return -1;
        }
    }

    __declspec(dllexport) void Graph_Delete(int handle) noexcept {
        try {
            if (handle >= 0) {
                ::DeleteGraph(handle);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) int Engine_DeleteGraph(int handle) noexcept {
        try {
            if (handle >= 0) {
                return ::DeleteGraph(handle);
            }
            return 0;
        } catch (...) {
            return -1;
        }
    }

    __declspec(dllexport) void Graph_GetSize(int handle, int* out_width, int* out_height) noexcept {
        try {
            if (handle >= 0 && out_width && out_height) {
                ::GetGraphSize(handle, out_width, out_height);
            }
        } catch (...) {
        }
    }

    // --- 2D Drawing APIs ---

    __declspec(dllexport) void Draw_Box(int x1, int y1, int x2, int y2, unsigned int color, bool fill) noexcept {
        try {
            if (g_render_service != nullptr) {
                g_render_service->DrawBox(
                    ab::Rect<int>{ x1, y1, x2, y2 },
                    color,
                    fill);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_Circle(int x, int y, int radius, unsigned int color, bool fill) noexcept {
        try {
            ::DrawCircle(x, y, radius, color, fill ? TRUE : FALSE);
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_Line(int x1, int y1, int x2, int y2, unsigned int color, int thickness) noexcept {
        try {
            ::DrawLine(x1, y1, x2, y2, color, thickness);
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_String(int x, int y, const char* text, unsigned int color) noexcept {
        try {
            if (g_render_service != nullptr && text != nullptr) {
                std::string stext(text);
                std::wstring wtext(stext.begin(), stext.end());
                g_render_service->DrawString(
                    ab::Point<int>{ x, y },
                    wtext.c_str(),
                    color);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_Graph(int x, int y, int handle, bool trans) noexcept {
        try {
            if (g_render_service != nullptr && handle >= 0) {
                g_render_service->DrawGraph(
                    ab::Point<int>{ x, y },
                    handle,
                    trans);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_RectGraph(int dest_x, int dest_y, int src_x, int src_y, int width, int height, int handle, bool trans) noexcept {
        try {
            if (g_render_service != nullptr && handle >= 0) {
                g_render_service->DrawRectRotaGraph(
                    ab::Point<int>{ dest_x, dest_y },
                    ab::Rect<int>{ src_x, src_y, src_x + width, src_y + height },
                    1.0,
                    0.0,
                    handle,
                    trans,
                    false);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) void Draw_RectRotaGraph(int x, int y, int src_x, int src_y, int width, int height, double scale, double angle_rad, int handle, bool trans, bool turn_x) noexcept {
        try {
            if (g_render_service != nullptr && handle >= 0) {
                g_render_service->DrawRectRotaGraph(
                    ab::Point<int>{ x, y },
                    ab::Rect<int>{ src_x, src_y, src_x + width, src_y + height },
                    scale,
                    angle_rad,
                    handle,
                    trans,
                    turn_x);
            }
        } catch (...) {
        }
    }

}
