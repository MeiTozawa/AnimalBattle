module;

#include <utility>

export module OSService;

import OSBackendConcept;

namespace ab {
    /// @brief バックエンドを合成して OS レベル操作を提供する静的多態 OSService テンプレートクラス
    /// @tparam Backend OS 操作バックエンド型
    export template <OSBackendConcept Backend>
    class OSService {
    public:
        /// @brief デフォルトコンストラクタ
        OSService() = default;

        /// @brief バックエンドを明示指定して生成するコンストラクタ
        /// @param backend 使用するバックエンドインスタンス
        explicit OSService(Backend backend) : backend_(std::move(backend)) {}

        /// @brief マウスカーソルをクリック可能を示す手のポインタに変更する
        void SetCursorPointer() {
            backend_.SetCursorPointer();
        }

        /// @brief マウスカーソルをデフォルトの矢印に戻す
        void SetCursorArrow() {
            backend_.SetCursorArrow();
        }

    private:
        Backend backend_{};
    };
} // namespace ab
