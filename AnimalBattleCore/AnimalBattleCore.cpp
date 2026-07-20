#include <Windows.h>
#include <cstdint>
#include <memory>
#include <DxLib.h>
#include "src/RenderUtils.h"
import RenderService;
import PhysicsEngine;

namespace ab {
    bool g_IsExitRequested = false;
}

static std::unique_ptr<ab::IRenderService> g_RenderService = nullptr;
static HWND g_hWnd = nullptr;
static HDC g_hdc = nullptr;

extern "C" {
    // --- Engine Control APIs ---

    __declspec(dllexport) int Engine_Initialize(int width, int height, bool windowMode) {
        SetUseDirect3DVersion(DX_DIRECT3D_11);
        SetOutApplicationLogValidFlag(FALSE);
        SetAlwaysRunFlag(TRUE);
        SetWindowPosition(0, 0);

        SetChangeScreenModeGraphicsSystemResetFlag(FALSE);
        ChangeWindowMode(windowMode ? TRUE : FALSE);
        SetGraphMode(width, height, 32);
        SetWaitVSyncFlag(TRUE);
        SetZBufferBitDepth(32);
        SetCreateDrawValidGraphZBufferBitDepth(32);
        SetDoubleStartValidFlag(TRUE);
        SetUseCharCodeFormat(DX_CHARCODEFORMAT_UTF8);

        if (DxLib_Init() == -1) return -1;
        SetDrawScreen(DX_SCREEN_BACK);
        SetMouseDispFlag(TRUE);

        g_hWnd = GetMainWindowHandle();
        g_hdc = GetDC(g_hWnd);

        // Instantiate rendering service
        g_RenderService = ab::CreateRenderService();
        ab::g_IsExitRequested = false;

        return 0;
    }

    __declspec(dllexport) int Engine_ProcessMessage() {
        return ProcessMessage();
    }

    __declspec(dllexport) void Engine_BeginFrame() {
        ClearDrawScreen();
    }

    __declspec(dllexport) void Engine_EndFrame(float deltaTime) {
        ScreenFlip();
    }

    __declspec(dllexport) bool Engine_IsExitRequested() {
        return ab::g_IsExitRequested;
    }

    __declspec(dllexport) void Engine_Shutdown() {
        g_RenderService.reset();
        DxLib_End();
        if (g_hWnd && g_hdc) {
            ReleaseDC(g_hWnd, g_hdc);
        }
    }

    __declspec(dllexport) ab::IRenderService* Engine_GetRenderService() {
        return g_RenderService.get();
    }

    // --- Asset Management APIs (Managed by C# triggering) ---

    __declspec(dllexport) int Engine_LoadGraph(const wchar_t* path) {
        return LoadGraph(path);
    }

    __declspec(dllexport) int Engine_DeleteGraph(int handle) {
        return DeleteGraph(handle);
    }

    // --- RenderService Direct P/Invoke Wrapper APIs ---

    __declspec(dllexport) void Render_DrawString(ab::IRenderService* service, ab::Point<int> p, const wchar_t* text, uint32_t color) {
        if (service) service->DrawString(p, text, color);
    }

    __declspec(dllexport) void Render_DrawBox(ab::IRenderService* service, ab::Rect<int> r, uint32_t color, bool fillFlag) {
        if (service) service->DrawBox(r, color, fillFlag);
    }

    __declspec(dllexport) void Render_DrawStringWithBorder(ab::IRenderService* service, ab::Rect<int> r, const wchar_t* text, ab::BorderShape shape, uint32_t textColor, uint32_t borderColor) {
        if (service) service->DrawStringWithBorder(r, text, shape, textColor, borderColor);
    }

    __declspec(dllexport) void Render_DrawGraph(ab::IRenderService* service, ab::Point<int> p, int handle, bool transFlag) {
        if (service) service->DrawGraph(p, handle, transFlag);
    }

    // --- OSService / Cursor Control ---
    __declspec(dllexport) void Engine_SetCursorPointer() {
        SetCursor(::LoadCursor(nullptr, IDC_HAND));
    }

    __declspec(dllexport) void Engine_SetCursorArrow() {
        SetCursor(::LoadCursor(nullptr, IDC_ARROW));
    }

    // --- Generic Audio (DxLib Sound wrappers) ---
    __declspec(dllexport) int Engine_PlaySoundMem(int handle, int playType, bool topPositionFlag) {
        return PlaySoundMem(handle, playType, topPositionFlag ? TRUE : FALSE);
    }

    __declspec(dllexport) int Engine_StopSoundMem(int handle) {
        return StopSoundMem(handle);
    }

    __declspec(dllexport) int Engine_ChangeVolumeSoundMem(int volumePercent, int handle) {
        int volVal = (volumePercent * 255) / 100;
        return ChangeVolumeSoundMem(volVal, handle);
    }

    // --- Generic Input (DxLib Keyboard & Mouse wrappers) ---
    __declspec(dllexport) int Engine_CheckHitKey(int keyCode) {
        return CheckHitKey(keyCode);
    }

    __declspec(dllexport) void Engine_GetMousePoint(int* x, int* y) {
        GetMousePoint(x, y);
    }

    __declspec(dllexport) int Engine_GetMouseInput() {
        return GetMouseInput();
    }

    // --- Physics Engine APIs ---

    struct NativePhysicsObject {
        int id;
        float x;
        float y;
        float vx;
        float vy;
        float kbx;
        float kby;
        float width;
        float height;
        float weight;
        float damagePercent;
        float hitstunTimer;
        float dropThroughTimer;
        int collisionFlags;
    };

    __declspec(dllexport) ab::PhysicsEngine* Physics_Create() {
        return new ab::PhysicsEngine();
    }

    __declspec(dllexport) void Physics_Destroy(ab::PhysicsEngine* engine) {
        delete engine;
    }

    __declspec(dllexport) int Physics_AddObject(ab::PhysicsEngine* engine, float x, float y, float width, float height, float weight) {
        if (engine) return engine->AddObject(x, y, width, height, weight);
        return -1;
    }

    __declspec(dllexport) bool Physics_RemoveObject(ab::PhysicsEngine* engine, int id) {
        if (engine) return engine->RemoveObject(id);
        return false;
    }

    __declspec(dllexport) bool Physics_GetObjectState(ab::PhysicsEngine* engine, int id, NativePhysicsObject* outState) {
        if (!engine || !outState) return false;
        auto* obj = engine->GetObject(id);
        if (!obj) return false;

        outState->id = obj->id;
        outState->x = obj->x;
        outState->y = obj->y;
        outState->vx = obj->vx;
        outState->vy = obj->vy;
        outState->kbx = obj->kbx;
        outState->kby = obj->kby;
        outState->width = obj->width;
        outState->height = obj->height;
        outState->weight = obj->weight;
        outState->damagePercent = obj->damagePercent;
        outState->hitstunTimer = obj->hitstunTimer;
        outState->dropThroughTimer = obj->dropThroughTimer;
        outState->collisionFlags = obj->collisionFlags;
        return true;
    }

    __declspec(dllexport) bool Physics_SetObjectState(ab::PhysicsEngine* engine, int id, const NativePhysicsObject* inState) {
        if (!engine || !inState) return false;
        auto* obj = engine->GetObject(id);
        if (!obj) return false;

        obj->x = inState->x;
        obj->y = inState->y;
        obj->vx = inState->vx;
        obj->vy = inState->vy;
        obj->kbx = inState->kbx;
        obj->kby = inState->kby;
        obj->width = inState->width;
        obj->height = inState->height;
        obj->weight = inState->weight;
        obj->damagePercent = inState->damagePercent;
        obj->hitstunTimer = inState->hitstunTimer;
        obj->dropThroughTimer = inState->dropThroughTimer;
        obj->collisionFlags = inState->collisionFlags;
        return true;
    }

    __declspec(dllexport) int Physics_AddPlatform(ab::PhysicsEngine* engine, float x1, float y1, float x2, float y2, int type) {
        if (!engine) return -1;
        return engine->AddPlatform(x1, y1, x2, y2, static_cast<ab::PlatformType>(type));
    }

    __declspec(dllexport) bool Physics_RemovePlatform(ab::PhysicsEngine* engine, int id) {
        if (engine) return engine->RemovePlatform(id);
        return false;
    }

    __declspec(dllexport) void Physics_ApplyKnockback(ab::PhysicsEngine* engine, int id, float damage, float angleRad, float baseKb, float scaling) {
        if (engine) engine->ApplyKnockback(id, damage, angleRad, baseKb, scaling);
    }

    __declspec(dllexport) void Physics_Step(ab::PhysicsEngine* engine, float dt) {
        if (engine) engine->Step(dt);
    }
}
