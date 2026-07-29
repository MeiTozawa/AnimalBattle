#include <cstdint>
#include <memory>
#include <Windows.h>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

import RenderService;
import DxLibRenderBackend;
import PhysicsEngine;
import Camera;
import AnimationPlayer;

using RenderService = ab::RenderService<ab::DxLibRenderBackend>;

extern "C" {
    bool g_is_exit_requested = false;
}

static std::unique_ptr<RenderService> g_render_service = nullptr;
static HWND g_hwnd = nullptr;
static HDC g_hdc = nullptr;

extern "C" {
    // --- Engine Control APIs ---

    __declspec(dllexport) int Engine_Initialize(int width, int height, bool window_mode) noexcept {
        try {
            SetUseDirect3DVersion(DX_DIRECT3D_11);
            SetOutApplicationLogValidFlag(FALSE);
            SetAlwaysRunFlag(TRUE);
            SetWindowPosition(0, 0);

            SetChangeScreenModeGraphicsSystemResetFlag(FALSE);
            ChangeWindowMode(window_mode ? TRUE : FALSE);
            SetGraphMode(width, height, 32);
            SetWaitVSyncFlag(TRUE);
            SetZBufferBitDepth(32);
            SetCreateDrawValidGraphZBufferBitDepth(32);
            SetDoubleStartValidFlag(TRUE);
            SetUseCharCodeFormat(DX_CHARCODEFORMAT_UTF8);

            if (DxLib_Init() == -1) return -1;
            SetDrawScreen(DX_SCREEN_BACK);
            SetMouseDispFlag(TRUE);

            g_hwnd = GetMainWindowHandle();
            g_hdc = GetDC(g_hwnd);

            // Instantiate rendering service directly
            g_render_service = std::make_unique<RenderService>(width, height);
            g_is_exit_requested = false;

            return 0;
        } catch (...) {
            return -1;
        }
    }

    __declspec(dllexport) int Engine_ProcessMessage() noexcept {
        return ProcessMessage();
    }

    __declspec(dllexport) void Engine_BeginFrame() noexcept {
        if (g_render_service) {
            g_render_service->ClearCommands();
        }
        ClearDrawScreen();
    }

    __declspec(dllexport) void Engine_EndFrame(float delta_time) noexcept {
        if (g_render_service) {
            g_render_service->FlushCommands();
        }
        ScreenFlip();
    }

    __declspec(dllexport) bool Engine_IsExitRequested() noexcept {
        return g_is_exit_requested;
    }

    __declspec(dllexport) void Engine_Shutdown() noexcept {
        try {
            g_render_service.reset();
            DxLib_End();
            if (g_hwnd && g_hdc) {
                ReleaseDC(g_hwnd, g_hdc);
            }
        } catch (...) {}
    }

    __declspec(dllexport) RenderService* Engine_GetRenderService() noexcept {
        return g_render_service.get();
    }

    // --- Asset Management APIs ---

    __declspec(dllexport) int Engine_LoadGraph(const wchar_t* path) noexcept {
        return LoadGraph(path);
    }

    __declspec(dllexport) int Engine_DeleteGraph(int handle) noexcept {
        return DeleteGraph(handle);
    }

    // --- RenderService Direct P/Invoke Wrapper APIs ---

    __declspec(dllexport) void Render_ExecuteCommand(RenderService* service, const ab::RenderCommand* cmd) noexcept {
        if (service && cmd) service->ExecuteCommand(*cmd);
    }

    __declspec(dllexport) void Render_FlushCommands(RenderService* service) noexcept {
        if (service) service->FlushCommands();
    }

    __declspec(dllexport) void Render_DrawString(RenderService* service, ab::Point<int> p, const wchar_t* text, uint32_t color) noexcept {
        if (service) service->DrawString(p, text, color);
    }

    __declspec(dllexport) void Render_DrawBox(RenderService* service, ab::Rect<int> r, uint32_t color, bool fill_flag) noexcept {
        if (service) service->DrawBox(r, color, fill_flag);
    }

    __declspec(dllexport) void Render_DrawStringWithBorder(RenderService* service, ab::Rect<int> r, const wchar_t* text, ab::BorderShape shape, uint32_t text_color, uint32_t border_color) noexcept {
        if (service) service->DrawStringWithBorder(r, text, shape, text_color, border_color);
    }

    __declspec(dllexport) void Render_DrawGraph(RenderService* service, ab::Point<int> p, int handle, bool trans_flag) noexcept {
        if (service) service->DrawGraph(p, handle, trans_flag);
    }

    __declspec(dllexport) void Render_SetCameraEnabled(RenderService* service, bool enabled) noexcept {
        if (service) service->SetCameraEnabled(enabled);
    }

    __declspec(dllexport) void Render_SetCameraState(RenderService* service, float x, float y, float scale) noexcept {
        if (service) service->SetCameraState(x, y, scale);
    }

    // --- Camera APIs ---

    __declspec(dllexport) ab::Camera* Camera_Create(
        float x, float y, float scale,
        float min_scale, float max_scale,
        float lerp_speed, float padding_x, float padding_y) noexcept {
        try {
            return new ab::Camera(x, y, scale, min_scale, max_scale, lerp_speed, padding_x, padding_y);
        } catch (...) {
            return nullptr;
        }
    }

    __declspec(dllexport) void Camera_Destroy(ab::Camera* camera) noexcept {
        delete camera;
    }

    __declspec(dllexport) void Camera_Reset(ab::Camera* camera, float x, float y, float scale) noexcept {
        if (camera) camera->Reset(x, y, scale);
    }

    __declspec(dllexport) void Camera_Update(ab::Camera* camera, float delta_time, const ab::Point<float>* targets, int target_count, float viewport_width, float viewport_height) noexcept {
        if (camera) camera->Update(delta_time, targets, target_count, viewport_width, viewport_height);
    }

    __declspec(dllexport) void Camera_GetState(ab::Camera* camera, float* out_x, float* out_y, float* out_scale) noexcept {
        if (camera && out_x && out_y && out_scale) {
            *out_x = camera->GetX();
            *out_y = camera->GetY();
            *out_scale = camera->GetScale();
        }
    }

    __declspec(dllexport) void Camera_SetConfig(ab::Camera* camera, float min_scale, float max_scale, float lerp_speed, float padding_x, float padding_y) noexcept {
        if (camera) {
            camera->SetMinScale(min_scale);
            camera->SetMaxScale(max_scale);
            camera->SetLerpSpeed(lerp_speed);
            camera->SetPadding(padding_x, padding_y);
        }
    }

    // --- OSService / Cursor Control ---
    __declspec(dllexport) void Engine_SetCursorPointer() noexcept {
        SetCursor(::LoadCursor(nullptr, IDC_HAND));
    }

    __declspec(dllexport) void Engine_SetCursorArrow() noexcept {
        SetCursor(::LoadCursor(nullptr, IDC_ARROW));
    }

    // --- Generic Audio (DxLib Sound wrappers) ---
    __declspec(dllexport) int Engine_PlaySoundMem(int handle, int play_type, bool top_position_flag) noexcept {
        return PlaySoundMem(handle, play_type, top_position_flag ? TRUE : FALSE);
    }

    __declspec(dllexport) int Engine_StopSoundMem(int handle) noexcept {
        return StopSoundMem(handle);
    }

    __declspec(dllexport) int Engine_ChangeVolumeSoundMem(int volume_percent, int handle) noexcept {
        int vol_val = (volume_percent * 255) / 100;
        return ChangeVolumeSoundMem(vol_val, handle);
    }

    // --- Generic Input (DxLib Keyboard & Mouse wrappers) ---
    __declspec(dllexport) int Engine_CheckHitKey(int key_code) noexcept {
        return CheckHitKey(key_code);
    }

    __declspec(dllexport) void Engine_GetMousePoint(int* x, int* y) noexcept {
        GetMousePoint(x, y);
    }

    __declspec(dllexport) int Engine_GetMouseInput() noexcept {
        return GetMouseInput();
    }

    // --- Physics Engine APIs ---

    struct NativePhysicsObject {
        int id;
        float x;
        float y;
        float vx;
        float vy;
        float knockback_x;
        float knockback_y;
        float width;
        float height;
        float weight;
        float damage_percent;
        float hitstun_timer;
        float drop_through_timer;
        int collision_flags;
    };

    __declspec(dllexport) ab::PhysicsEngine* Physics_Create() noexcept {
        try {
            return new ab::PhysicsEngine();
        } catch (...) {
            return nullptr;
        }
    }

    __declspec(dllexport) void Physics_Destroy(ab::PhysicsEngine* engine) noexcept {
        delete engine;
    }

    __declspec(dllexport) int Physics_AddObject(ab::PhysicsEngine* engine, float x, float y, float width, float height, float weight) noexcept {
        if (engine) return engine->AddObject(x, y, width, height, weight);
        return -1;
    }

    __declspec(dllexport) bool Physics_RemoveObject(ab::PhysicsEngine* engine, int id) noexcept {
        if (engine) return engine->RemoveObject(id);
        return false;
    }

    __declspec(dllexport) bool Physics_GetObjectState(ab::PhysicsEngine* engine, int id, NativePhysicsObject* out_state) noexcept {
        if (!engine || !out_state) return false;
        auto* obj = engine->GetObject(id);
        if (!obj) return false;

        out_state->id = obj->id;
        out_state->x = obj->x;
        out_state->y = obj->y;
        out_state->vx = obj->vx;
        out_state->vy = obj->vy;
        out_state->knockback_x = obj->knockback_x;
        out_state->knockback_y = obj->knockback_y;
        out_state->width = obj->width;
        out_state->height = obj->height;
        out_state->weight = obj->weight;
        out_state->damage_percent = obj->damage_percent;
        out_state->hitstun_timer = obj->hitstun_timer;
        out_state->drop_through_timer = obj->drop_through_timer;
        out_state->collision_flags = static_cast<int>(obj->collision_flags);
        return true;
    }

    __declspec(dllexport) bool Physics_SetObjectState(ab::PhysicsEngine* engine, int id, const NativePhysicsObject* in_state) noexcept {
        if (!engine || !in_state) return false;
        auto* obj = engine->GetObject(id);
        if (!obj) return false;

        obj->x = in_state->x;
        obj->y = in_state->y;
        obj->vx = in_state->vx;
        obj->vy = in_state->vy;
        obj->knockback_x = in_state->knockback_x;
        obj->knockback_y = in_state->knockback_y;
        obj->width = in_state->width;
        obj->height = in_state->height;
        obj->weight = in_state->weight;
        obj->damage_percent = in_state->damage_percent;
        obj->hitstun_timer = in_state->hitstun_timer;
        obj->drop_through_timer = in_state->drop_through_timer;
        obj->collision_flags = static_cast<ab::CollisionFlags>(in_state->collision_flags);
        return true;
    }

    __declspec(dllexport) int Physics_AddPlatform(ab::PhysicsEngine* engine, float x1, float y1, float x2, float y2, int type) noexcept {
        if (!engine) return -1;
        return engine->AddPlatform(x1, y1, x2, y2, static_cast<ab::PlatformType>(type));
    }

    __declspec(dllexport) bool Physics_RemovePlatform(ab::PhysicsEngine* engine, int id) noexcept {
        if (engine) return engine->RemovePlatform(id);
        return false;
    }

    __declspec(dllexport) void Physics_ApplyKnockback(ab::PhysicsEngine* engine, int id, float damage, float angle_rad, float base_knockback, float scaling) noexcept {
        if (engine) engine->ApplyKnockback(id, damage, angle_rad, base_knockback, scaling);
    }

    __declspec(dllexport) void Physics_Step(ab::PhysicsEngine* engine, float delta_time) noexcept {
        if (engine) engine->Step(delta_time);
    }

    __declspec(dllexport) void Render_DrawRectRotaGraph(RenderService* service, ab::Point<int> p, ab::Rect<int> src, double ext_rate, double angle, int graph_handle, bool trans_flag, bool turn_flag) noexcept {
        if (service) service->DrawRectRotaGraph(p, src, ext_rate, angle, graph_handle, trans_flag, turn_flag);
    }

    // --- AnimationPlayer APIs ---

    __declspec(dllexport) ab::AnimationPlayer* AnimationPlayer_Create() noexcept {
        try {
            return new ab::AnimationPlayer();
        } catch (...) {
            return nullptr;
        }
    }

    __declspec(dllexport) void AnimationPlayer_Destroy(ab::AnimationPlayer* player) noexcept {
        delete player;
    }

    __declspec(dllexport) void AnimationPlayer_AddGridClip(
        ab::AnimationPlayer* player, const wchar_t* name, int row,
        int frame_width, int frame_height, int frame_count,
        float fps, bool is_looping) noexcept {
        if (player && name) {
            player->AddGridClip(name, row, frame_width, frame_height, frame_count, fps, is_looping);
        }
    }

    __declspec(dllexport) bool AnimationPlayer_Play(ab::AnimationPlayer* player, const wchar_t* name, bool restart_if_same) noexcept {
        if (player && name) {
            return player->Play(name, restart_if_same);
        }
        return false;
    }

    __declspec(dllexport) void AnimationPlayer_Pause(ab::AnimationPlayer* player) noexcept {
        if (player) player->Pause();
    }

    __declspec(dllexport) void AnimationPlayer_Resume(ab::AnimationPlayer* player) noexcept {
        if (player) player->Resume();
    }

    __declspec(dllexport) void AnimationPlayer_Stop(ab::AnimationPlayer* player) noexcept {
        if (player) player->Stop();
    }

    __declspec(dllexport) void AnimationPlayer_Update(ab::AnimationPlayer* player, float delta_time) noexcept {
        if (player) player->Update(delta_time);
    }

    __declspec(dllexport) void AnimationPlayer_Draw(
        ab::AnimationPlayer* player, RenderService* service,
        int pos_x, int pos_y, int graph_handle,
        bool flip_x, float scale, double angle) noexcept {
        if (player && service) {
            player->Draw(service, ab::Point<int>{pos_x, pos_y}, graph_handle, flip_x, scale, angle);
        }
    }

    __declspec(dllexport) bool AnimationPlayer_IsFinished(ab::AnimationPlayer* player) noexcept {
        if (player) return player->IsFinished();
        return false;
    }

    __declspec(dllexport) int AnimationPlayer_GetCurrentFrameIndex(ab::AnimationPlayer* player) noexcept {
        if (player) return player->GetCurrentFrameIndex();
        return 0;
    }

    // --- Joypad / Gamepad APIs ---

    __declspec(dllexport) int Engine_GetJoypadInputState(int input_type) noexcept {
        return GetJoypadInputState(input_type);
    }

    __declspec(dllexport) int Engine_GetJoypadNum() noexcept {
        return GetJoypadNum();
    }
}

