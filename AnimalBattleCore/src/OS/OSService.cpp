module;

#include <memory>
#include <windows.h>

module OSService;

namespace ab {
    class WindowsService : public IOSService {
    public:
        void SetCursorPointer() override {
            ::SetCursor(::LoadCursor(nullptr, IDC_HAND));
        }

        void SetCursorArrow() override {
            ::SetCursor(::LoadCursor(nullptr, IDC_ARROW));
        }
    };

    std::unique_ptr<IOSService> CreateWindowService() {
        return std::make_unique<WindowsService>();
    }
}  // namespace ab
