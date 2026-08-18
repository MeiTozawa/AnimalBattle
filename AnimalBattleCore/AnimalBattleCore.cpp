#include "Core/EngineCore.h"

#include <cstdint>
#include <string>
#include <vector>

import RenderService;
import InputService;
import PhysicsService;

namespace ab {

namespace {
IRenderService* g_render_service = nullptr;
IInputService* g_input_service = nullptr;
IPhysicsService* g_physics_service = nullptr;
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
    return g_physics_service;
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

__declspec(dllexport) void Graph_GetSize(int handle, ab::Vec2i* out_size) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0 && out_size != nullptr) {
            render_service->GetGraphSize(handle, out_size);
        }
    } catch (...) {
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

// --- 2D Drawing APIs ---

__declspec(dllexport) void Draw_Box(ab::Vec2i p1, ab::Vec2i p2, unsigned int color, bool fill) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawBox(p1, p2, color, fill);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Circle(ab::Vec2i center, int radius, unsigned int color, bool fill) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawCircle(center, radius, color, fill);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Line(ab::Vec2i p1, ab::Vec2i p2, unsigned int color, int thickness) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr) {
            render_service->DrawLine(p1, p2, color, thickness);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_String(ab::Vec2i pos, const wchar_t* text, unsigned int color) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && text != nullptr) {
            render_service->DrawString(pos, text, color);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_Graph(ab::Vec2i pos, int handle, bool trans) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawGraph(pos, handle, trans);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_RectGraph(ab::Vec2i dest_pos, ab::Vec2i src_pos, ab::Vec2i src_size, int handle, bool trans) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawRectRotaGraph(
                dest_pos,
                ab::Rect<int>{src_pos, src_pos + src_size},
                1.0,
                0.0,
                handle,
                trans,
                false);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_RectRotaGraph(ab::Vec2i center, ab::Vec2i src_pos, ab::Vec2i src_size, double scale, double angle_rad, int handle, bool trans, bool turn_x) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawRectRotaGraph(
                center,
                ab::Rect<int>{src_pos, src_pos + src_size},
                scale,
                angle_rad,
                handle,
                trans,
                turn_x);
        }
    } catch (...) {
    }
}

__declspec(dllexport) void Draw_RotaGraph(ab::Vec2i center, double scale, double angle_rad, int handle, bool trans) noexcept {
    try {
        auto* render_service = ab::GetRenderService();
        if (render_service != nullptr && handle >= 0) {
            render_service->DrawRotaGraph(center, scale, angle_rad, handle, trans);
        }
    } catch (...) {
    }
}

// --- Physics & Mathematics APIs ---

__declspec(dllexport) bool Physics_IntersectsCircleCircle(ab::Circle2D c1, ab::Circle2D c2) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntersectsCircleCircle(c1, c2);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Physics_IntersectsBoxBox(ab::Box2D b1, ab::Box2D b2) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntersectsBoxBox(b1, b2);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Physics_IntersectsCircleBox(ab::Circle2D circle, ab::Box2D box) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntersectsCircleBox(circle, box);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Physics_IntersectsPointCircle(ab::Vec2f point, ab::Circle2D circle) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntersectsPointCircle(point, circle);
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) bool Physics_IntersectsPointBox(ab::Vec2f point, ab::Box2D box) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntersectsPointBox(point, box);
        }
        return false;
    } catch (...) {
        return false;
    }
}

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

__declspec(dllexport) bool Physics_CheckPenetrationBoxBox(ab::Box2D b1, ab::Box2D b2,
                                                          ab::Vec2f* out_normal, float* out_depth) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            auto result = physics_service->CheckPenetrationBoxBox(b1, b2);
            if (out_normal != nullptr) *out_normal = result.normal;
            if (out_depth != nullptr) *out_depth = result.depth;
            return result.is_penetrating;
        }
        return false;
    } catch (...) {
        return false;
    }
}

__declspec(dllexport) ab::Vec2f Physics_IntegrateVelocity(ab::Vec2f position, ab::Vec2f velocity, float delta_time) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->IntegrateVelocity(position, velocity, delta_time);
        }
        return position;
    } catch (...) {
        return position;
    }
}

__declspec(dllexport) void Physics_ApplyGravity(float* in_out_vy, float gravity, float max_fall_speed, float delta_time) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr && in_out_vy != nullptr) {
            physics_service->ApplyGravity(*in_out_vy, gravity, max_fall_speed, delta_time);
        }
    } catch (...) {
    }
}

__declspec(dllexport) float Physics_CalculateDistance(ab::Vec2f a, ab::Vec2f b) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->CalculateDistance(a, b);
        }
        return 0.0f;
    } catch (...) {
        return 0.0f;
    }
}

__declspec(dllexport) float Physics_CalculateAngle(ab::Vec2f from, ab::Vec2f to) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->CalculateAngle(from, to);
        }
        return 0.0f;
    } catch (...) {
        return 0.0f;
    }
}

__declspec(dllexport) ab::Vec2f Physics_DirectionToVector(float angle_rad, float speed) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->DirectionToVector(angle_rad, speed);
        }
        return ab::Vec2f{0, 0};
    } catch (...) {
        return ab::Vec2f{0, 0};
    }
}

__declspec(dllexport) float Physics_Clamp(float value, float min_val, float max_val) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->Clamp(value, min_val, max_val);
        }
        return value;
    } catch (...) {
        return value;
    }
}

__declspec(dllexport) float Physics_Lerp(float a, float b, float t) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            return physics_service->Lerp(a, b, t);
        }
        return a;
    } catch (...) {
        return a;
    }
}

// --- Batch SIMD Physics APIs ---

__declspec(dllexport) void Physics_BatchBulletMovement(
    ab::Transform* transforms,
    const ab::Velocity* velocities,
    int count,
    float delta_time,
    float min_x, float max_x, float min_y, float max_y,
    uint8_t* out_despawn_flags) noexcept {
    try {
        auto* physics_service = ab::GetPhysicsService();
        if (physics_service != nullptr) {
            physics_service->BatchBulletMovement(
                transforms, velocities, count, delta_time,
                min_x, max_x, min_y, max_y, out_despawn_flags);
        }
    } catch (...) {
    }
}

}
