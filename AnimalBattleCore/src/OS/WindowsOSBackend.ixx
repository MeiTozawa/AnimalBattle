module;

export module WindowsOSBackend;

import OSBackendConcept;

namespace ab {
    /// @brief Windows プラットフォーム向けの具体的な OS 操作バックエンド
    export struct WindowsOSBackend {
        /// @brief マウスカーソルをクリック可能を示す手のポインタに変更する
        void SetCursorPointer();

        /// @brief マウスカーソルをデフォルトの矢印に戻す
        void SetCursorArrow();
    };

    static_assert(OSBackendConcept<WindowsOSBackend>, "WindowsOSBackend must satisfy OSBackendConcept");
} // namespace ab
