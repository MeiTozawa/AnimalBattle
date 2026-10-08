#include "Core/EngineCore.h"
#include "Render/RenderPacket.h"

#include <cstdint>
#include <string>
#include <vector>

import RenderService;
import InputService;
import PhysicsService;
import DefaultPhysicsService;

namespace ab {

namespace {
IRenderService* g_render_service = nullptr;
IInputService* g_input_service = nullptr;
IPhysicsService* g_physics_service = nullptr;
DefaultPhysicsService g_fallback_physics{};
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

void SetPhysicsService(IPhysicsService* service) noexcept {
    g_physics_service = service;
}

IPhysicsService* GetPhysicsService() noexcept {
    return g_physics_service != nullptr ? g_physics_service : &g_fallback_physics;
}

}  // namespace ab

#pragma warning(disable: 4190)

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

__declspec(dllexport) void Engine_SubmitRenderCommands(
    const ab::RenderPacket* packets,
    int packet_count,
    const wchar_t* text_buffer,
    int text_length,
    const ab::CompactCirclePacket* circles = nullptr,
    int circle_count = 0) noexcept {
    if (packets == nullptr || packet_count <= 0) {
        return;
    }
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service == nullptr) {
            return;
        }

        render_service->SubmitPackets(packets, packet_count, text_buffer, text_length, circles, circle_count);
    } catch (...) {
    }
}

__declspec(dllexport) void Engine_SubmitCompactCircles(
    const ab::CompactCirclePacket* circles,
    int circle_count) noexcept {
    if (circles == nullptr || circle_count <= 0) {
        return;
    }
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service == nullptr) {
            return;
        }

        render_service->SubmitCompactCircles(circles, circle_count);
    } catch (...) {
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

__declspec(dllexport) void Input_Snapshot() noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            input_service->Snapshot();
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

__declspec(dllexport) void Input_GetMousePosition(ab::Vec2i* out_pos) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && out_pos != nullptr) {
            *out_pos = input_service->GetMousePosition();
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Input_GetMouseDelta(ab::Vec2i* out_delta) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && out_delta != nullptr) {
            *out_delta = input_service->GetMouseDelta();
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

__declspec(dllexport) bool Input_IsMouseOver(ab::Vec2i p1, ab::Vec2i p2) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr) {
            return input_service->IsMouseOver(p1, p2);
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

__declspec(dllexport) void Engine_GetMousePoint(ab::Vec2i* out_pos) noexcept {
    try {
        auto* input_service = ab::GetInputService();
        if (input_service != nullptr && out_pos != nullptr) {
            input_service->GetMousePoint(out_pos);
        }
    } catch (...) {
    }
}

// --- Graph / Texture APIs ---

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

__declspec(dllexport) void Engine_GetGraphSize(int handle, ab::Vec2i* out_size) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0 && out_size != nullptr) {
            render_service->GetGraphSize(handle, out_size);
        }
    } catch (...) {
    }
}

// --- Physics & Mathematics APIs ---

__declspec(dllexport) bool Physics_CheckPlatformGround(float prev_y, float next_y, ab::Box2D player_box, ab::Box2D platform_box,
                                                       float* out_ground_y, float* out_vy) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            auto result = physics_service->CheckPlatformGround(prev_y, next_y, player_box, platform_box);
            if (out_ground_y != nullptr) {
                *out_ground_y = result.ground_y;
            }
            if (out_vy != nullptr) {
                *out_vy = result.adjusted_vy;
            }
            return result.is_grounded;
        }
        return false;
    } catch (...) {
        return false;
    }
}

}

