#include "Core/EngineCore.h"

#include <cstdint>
#include <string>
#include <vector>

import RenderService;
import InputService;

namespace ab {

namespace {
IRenderService* g_render_service = nullptr;
IInputService* g_input_service = nullptr;
}

void SetRenderService(IRenderService* service) noexcept {
    g_render_service = service;
}

IRenderService* GetRenderService() noexcept {
    return g_render_service;
}

void SetInputService(IInputService* service) noexcept {
    g_input_service = service;
}

IInputService* GetInputService() noexcept {
    return g_input_service;
}

}  // namespace ab

extern "C" {

bool g_is_exit_requested = false;

// --- Engine Control APIs ---

__declspec(dllexport) bool Engine_Init(void* hwnd, int width, int height) noexcept {
    try {
        return ab::SetupDxLibBackend(hwnd, width, height);
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) void Engine_Shutdown() noexcept {
    try {
        ab::ShutdownDxLibBackend();
    } catch (...) {
    }
}

__declspec(dllexport) bool Engine_ProcessMessages() noexcept {
    try {
        return ab::ProcessDxLibMessages();
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Engine_BeginDraw() noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->ClearCommands();
            return render_service->ClearScreen();
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Engine_EndDraw() noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->FlushCommands();
            return render_service->ScreenFlip();
        }
        return false;
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

// --- InputService High-Level APIs ---

__declspec(dllexport) void Input_Update() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->Update();
        }
    } catch (...) {
    }
}

__declspec(dllexport) bool Input_IsPressed(int action) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsPressed(action);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Input_IsHolding(int action) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsHolding(action);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Input_IsReleased(int action) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsReleased(action);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Input_IsButtonPressed(int button) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsButtonPressed(static_cast<ab::InputButton>(button));
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Input_IsButtonHolding(int button) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsButtonHolding(static_cast<ab::InputButton>(button));
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Input_IsButtonReleased(int button) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsButtonReleased(static_cast<ab::InputButton>(button));
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) void Input_PushContext(int context) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->PushContext(context);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_PopContext() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->PopContext();
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_ClearAndSetContext(int context) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->ClearAndSetContext(context);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_BindAction(int context, int action, const int* buttons, int button_count) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && buttons != nullptr && button_count >= 0) {
            std::vector<ab::InputButton> button_vec;
            button_vec.reserve(button_count);
            for (int i = 0; i < button_count; ++i) {
                button_vec.push_back(static_cast<ab::InputButton>(buttons[i]));
            }
            input_service->BindAction(context, action, std::move(button_vec));
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_AddActionButton(int context, int action, int button) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->AddActionButton(context, action, static_cast<ab::InputButton>(button));
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_ClearActions(int context) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->ClearActions(context);
        }
    } catch (...) {
    }
}

__declspec(dllexport) int Input_GetCurrentContext() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->GetCurrentContext();
        }
        return ab::DEFAULT_CONTEXT;
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) int Input_GetActiveDevice() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return static_cast<int>(input_service->GetActiveDevice());
        }
        return static_cast<int>(ab::InputDevice::Keyboard);
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) void Input_GetMousePosition(int* out_x, int* out_y) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && out_x != nullptr && out_y != nullptr) {
            auto pos = input_service->GetMousePosition();
            *out_x = pos.x;
            *out_y = pos.y;
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_GetMouseDelta(int* out_dx, int* out_dy) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && out_dx != nullptr && out_dy != nullptr) {
            auto delta = input_service->GetMouseDelta();
            *out_dx = delta.x;
            *out_dy = delta.y;
        }
    } catch (...) {
    }
}

__declspec(dllexport) int Input_GetMouseWheelDelta() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->GetMouseWheelDelta();
        }
        return 0;
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) bool Input_IsMouseOver(int x1, int y1, int x2, int y2) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsMouseOver(ab::Rect<int>{x1, y1, x2, y2});
        }
        return false;
    } catch (...) {
        return false;
    }
}

// --- Raw Input APIs (Legacy & Low-level) ---

__declspec(dllexport) bool Input_IsKeyHit(int key_code) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->CheckHitKey(key_code);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) int Input_GetJoypadState(int pad_index) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->GetJoypadInputState(pad_index);
        }
        return 0;
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) void Engine_SetCursorPointer() noexcept {
}

__declspec(dllexport) void Engine_SetCursorArrow() noexcept {
}

__declspec(dllexport) bool Engine_CheckHitKey(int key_code) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->CheckHitKey(key_code);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) int Engine_GetJoypadInputState(int input_type) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->GetJoypadInputState(input_type);
        }
        return 0;
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) int Engine_GetJoypadNum() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->GetJoypadNum();
        }
        return 0;
    } catch (...) {
        return 0;
    }
}

__declspec(dllexport) void Engine_GetMousePoint(int* x, int* y) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->GetMousePoint(x, y);
        }
    } catch (...) {
    }
}

// --- Graph / Texture APIs ---

__declspec(dllexport) int Graph_Load(const char* file_path) noexcept {
    if (!file_path) {
        return -1;
    }
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            std::string s(file_path);
            std::wstring w(s.begin(), s.end());
            return render_service->LoadGraph(w.c_str());
        }
        return -1;
    } catch (...) {
        return -1;
    }
}

__declspec(dllexport) int Engine_LoadGraph(const wchar_t* file_path) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            return render_service->LoadGraph(file_path);
        }
        return -1;
    } catch (...) {
        return -1;
    }
}

__declspec(dllexport) void Graph_Delete(int handle) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DeleteGraph(handle);
        }
    } catch (...) {
    }
}

__declspec(dllexport) bool Engine_DeleteGraph(int handle) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            return render_service->DeleteGraph(handle);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) void Graph_GetSize(int handle, int* out_width, int* out_height) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->GetGraphSize(handle, out_width, out_height);
        }
    } catch (...) {
    }
}

// --- 2D Drawing APIs ---

__declspec(dllexport) void Draw_Box(int x1, int y1, int x2, int y2, unsigned int color, bool fill) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawBox(ab::Rect<int>{x1, y1, x2, y2}, color, fill);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Circle(int x, int y, int radius, unsigned int color, bool fill) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawCircle(ab::Point<int>{x, y}, radius, color, fill);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Line(int x1, int y1, int x2, int y2, unsigned int color, int thickness) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawLine(ab::Point<int>{x1, y1}, ab::Point<int>{x2, y2}, color, thickness);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_String(int x, int y, const wchar_t* text, unsigned int color) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && text != nullptr) {
            render_service->DrawString(ab::Point<int>{x, y}, text, color);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Graph(int x, int y, int handle, bool trans) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawGraph(ab::Point<int>{x, y}, handle, trans);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_RectGraph(int dest_x, int dest_y, int src_x, int src_y, int width, int height, int handle, bool trans) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawRectRotaGraph(
                ab::Point<int>{dest_x, dest_y},
                ab::Rect<int>{src_x, src_y, src_x + width, src_y + height},
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
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawRectRotaGraph(
                ab::Point<int>{x, y},
                ab::Rect<int>{src_x, src_y, src_x + width, src_y + height},
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
