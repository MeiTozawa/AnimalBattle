#include "Core/EngineCore.h"

#include <cstdint>
#include <memory>
#include <Windows.h>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

import DxLibRenderService;
import DxLibInputService;
import DefaultPhysicsService;
import DxLibSoundService;

namespace ab {
namespace {

std::unique_ptr<DxLibRenderService> g_render_service = nullptr;
std::unique_ptr<DxLibInputService> g_input_service = nullptr;
std::unique_ptr<DefaultPhysicsService> g_physics_service = nullptr;
std::unique_ptr<DxLibSoundService> g_sound_service = nullptr;
HWND g_hwnd = nullptr;
HDC g_hdc = nullptr;

}  // namespace

bool SetupDxLibBackend(void* hwnd, int width, int height) noexcept {
    try {
        g_hwnd = static_cast<HWND>(hwnd);
        g_render_service = std::make_unique<DxLibRenderService>(width, height);
        g_input_service = std::make_unique<DxLibInputService>();
        g_physics_service = std::make_unique<DefaultPhysicsService>();
        g_sound_service = std::make_unique<DxLibSoundService>();

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
            ::OutputDebugStringW(L"[DxLibMain エラー] DxLib_Init に失敗しました (-1)\n");
            return false;
        }
        ::SetDrawScreen(DX_SCREEN_BACK);
        if (g_hwnd != nullptr) {
            g_hdc = ::GetDC(g_hwnd);
        }

        // 各種具象サービスインスタンスのポインタをグローバルコンテキストへ登録します
        SetRenderService(g_render_service.get());
        SetInputService(g_input_service.get());
        SetPhysicsService(g_physics_service.get());
        SetSoundService(g_sound_service.get());
        return true;
    } catch (const std::exception& e) {
        ::OutputDebugStringW(L"[DxLibMain 例外] SetupDxLibBackend で例外が発生しました\n");
        return false;
    } catch (...) {
        ::OutputDebugStringW(L"[DxLibMain 例外] SetupDxLibBackend で不明な例外が発生しました\n");
        return false;
    }
}

void ShutdownDxLibBackend() noexcept {
    try {
        SetRenderService(nullptr);
        SetInputService(nullptr);
        SetPhysicsService(nullptr);
        SetSoundService(nullptr);

        if (g_hwnd != nullptr && g_hdc != nullptr) {
            ::ReleaseDC(g_hwnd, g_hdc);
            g_hdc = nullptr;
        }

        g_sound_service.reset();
        g_physics_service.reset();
        g_input_service.reset();
        g_render_service.reset();
        ::SetMouseDispFlag(TRUE);
        ::DxLib_End();
    } catch (...) {
        ::OutputDebugStringW(L"[DxLibMain 警告] ShutdownDxLibBackend 中に例外が発生しました\n");
    }
}

bool ProcessDxLibMessages() noexcept {
    bool success = (::ProcessMessage() == 0);
    if (g_input_service != nullptr) {
        g_input_service->Update();
    }
    return success;
}

void SetMouseDispFlag(bool visible) noexcept {
    ::SetMouseDispFlag(visible ? TRUE : FALSE);
}

}  // namespace ab
