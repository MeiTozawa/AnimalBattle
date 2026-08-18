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

namespace ab {
namespace {

std::unique_ptr<DxLibRenderService> g_render_service = nullptr;
std::unique_ptr<DxLibInputService> g_input_service = nullptr;
HWND g_hwnd = nullptr;
HDC g_hdc = nullptr;

}  // namespace

bool SetupDxLibBackend(void* hwnd, int width, int height) noexcept {
    try {
        g_hwnd = static_cast<HWND>(hwnd);
        g_render_service = std::make_unique<DxLibRenderService>(width, height);
        g_input_service = std::make_unique<DxLibInputService>();

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

        // AnimalBattleCore へ具象サービスポインタを登録
        SetRenderService(g_render_service.get());
        SetInputService(g_input_service.get());
        return true;
    } catch (...) {
        return false;
    }
}

void ShutdownDxLibBackend() noexcept {
    try {
        SetRenderService(nullptr);
        SetInputService(nullptr);

        if (g_hwnd != nullptr && g_hdc != nullptr) {
            ::ReleaseDC(g_hwnd, g_hdc);
            g_hdc = nullptr;
        }

        g_input_service.reset();
        g_render_service.reset();
        ::DxLib_End();
    } catch (...) {
    }
}

bool ProcessDxLibMessages() noexcept {
    try {
        if (g_input_service != nullptr) {
            g_input_service->Update();
        }
        return ::ProcessMessage() == 0;
    } catch (...) {
        return false;
    }
}

}  // namespace ab
