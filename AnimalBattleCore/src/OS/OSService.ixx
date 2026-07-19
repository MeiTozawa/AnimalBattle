module;

#include <memory>

export module OSService;

namespace ab {
    /// @brief OS レベルの操作（カーソル変更など）を提供するサービスインターフェース
    export class IOSService
    {
    public:
        virtual ~IOSService() = default;

        /// @brief マウスカーソルをクリック可能を示す手のポインタに変更する（Win32: IDC_HAND）
        virtual void SetCursorPointer() = 0;

        /// @brief マウスカーソルをデフォルトの矢印に戻す（Win32: IDC_ARROW）
        virtual void SetCursorArrow() = 0;
    };

    export std::unique_ptr<IOSService> CreateWindowService();
} // namespace ab
