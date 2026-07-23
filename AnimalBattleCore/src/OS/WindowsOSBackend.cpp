module;

#include <windows.h>

module WindowsOSBackend;

namespace ab {
    void WindowsOSBackend::SetCursorPointer() {
        ::SetCursor(::LoadCursor(nullptr, IDC_HAND));
    }

    void WindowsOSBackend::SetCursorArrow() {
        ::SetCursor(::LoadCursor(nullptr, IDC_ARROW));
    }
} // namespace ab
