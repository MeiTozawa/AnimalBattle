#include <Windows.h>
#include <cstdint>
#include <memory>
#include <DxLib.h>
#include "program/RenderUtils.h"
import RenderService;

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

    __declspec(dllexport) void Render_DrawString(ab::IRenderService* service, int x, int y, const wchar_t* text, uint32_t color) {
        if (service) service->DrawString(x, y, text, color);
    }

    __declspec(dllexport) void Render_DrawBoxAA(ab::IRenderService* service, float x1, float y1, float x2, float y2, uint32_t color, bool fillFlag) {
        if (service) service->DrawBoxAA(x1, y1, x2, y2, color, fillFlag);
    }

    __declspec(dllexport) void Render_DrawGraph(ab::IRenderService* service, int x, int y, int handle, bool transFlag) {
        if (service) service->DrawGraph(x, y, handle, transFlag);
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
}
