module;

#include <string>
#include <vector>
#include <unordered_map>

export module AnimationPlayer;

import RenderTypes;
import RenderService;
import DxLibRenderBackend;

namespace ab {
    /// @brief スプライトアニメーションの 1 フレーム情報を表す構造体
    export struct AnimationFrame {
        Rect<int> src_rect; ///< スプライトシート上の切り出し矩形領域
    };

    /// @brief 一連の連続アニメーションフレーム群を定義するクリップ構造体
    export struct AnimationClip {
        std::wstring name;                   ///< アニメーションクリップ識別名
        std::vector<AnimationFrame> frames;   ///< アニメーションフレーム配列
        float fps = 10.0f;                    ///< 再生フレームレート（Frames Per Second）
        bool is_looping = true;               ///< ループ再生を行うフラグ
    };

    /// @brief 2D スプライトアニメーションの再生・更新・描画を管理する C++ ネイティブクラス
    export class AnimationPlayer {
    private:
        std::unordered_map<std::wstring, AnimationClip> clips_;
        const AnimationClip* current_clip_ = nullptr;
        int current_frame_index_ = 0;
        float frame_timer_ = 0.0f;
        bool is_playing_ = false;
        bool is_finished_ = false;

    public:
        AnimationPlayer() = default;

        /// @brief アニメーションクリップを登録します
        /// @param clip 登録するアニメーションクリップ
        void AddClip(AnimationClip clip);

        /// @brief スプライトシートの格子状レイアウトからアニメーションクリップを生成して登録します
        /// @param name アニメーション名
        /// @param row スプライトシート上の行インデックス (0 開始)
        /// @param frame_width 1 コマの幅 (ピクセル)
        /// @param frame_height 1 コマの高さ (ピクセル)
        /// @param frame_count コマ数
        /// @param fps 再生フレームレート (Frames Per Second)
        /// @param is_looping ループ再生フラグ
        void AddGridClip(const std::wstring& name, int row, int frame_width, int frame_height,
                         int frame_count, float fps, bool is_looping);

        /// @brief 指定された名前のアニメーションを開始します
        /// @param name アニメーション名
        /// @param restart_if_same 既に同名のアニメーションが再生中でも最初から再開するかどうか
        /// @return 開始に成功した場合は true
        bool Play(const std::wstring& name, bool restart_if_same = false);

        /// @brief 再生を一時停止します
        void Pause();

        /// @brief 一時停止中の再生を再開します
        void Resume();

        /// @brief 再生を停止し、最初のフレームにリセットします
        void Stop();

        /// @brief 時間の経過に応じてアニメーション状態（タイマーおよびフレームコマ）を更新します
        /// @param delta_time 前フレームからの経過時間（秒）
        void Update(float delta_time);

        /// @brief 現在のコマを指定位置・スケール・左右反転・回転で描画します
        /// @param render_service 描画サービスインスタンスポインタ
        /// @param position 描画基準座標 (X, Y)
        /// @param graph_handle 画像グラフィックハンドル
        /// @param flip_x 左右反転フラグ
        /// @param scale 拡大率 (デフォルト 1.0)
        /// @param angle 回転角度 (ラジアン, デフォルト 0.0)
        void Draw(RenderService<DxLibRenderBackend>* render_service, Point<int> position,
                  int graph_handle, bool flip_x = false, float scale = 1.0f, double angle = 0.0);

        /// @brief 非ループアニメーションが最後まで再生完了したかを取得します
        [[nodiscard]] bool IsFinished() const { return is_finished_; }

        /// @brief 現在再生中のアニメーション名を取得します
        [[nodiscard]] std::wstring GetCurrentClipName() const {
            return current_clip_ ? current_clip_->name : L"";
        }

        /// @brief 現在のフレームコマ番号を取得します
        [[nodiscard]] int GetCurrentFrameIndex() const { return current_frame_index_; }
    };
} // namespace ab
