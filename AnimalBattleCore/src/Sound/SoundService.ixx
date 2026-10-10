module;

#include <cstdint>

export module SoundService;

namespace ab {

/// @brief サウンド再生の実行モード
export enum class SoundPlayType : int {
    Normal = 0,  // 同期再生 (DX_PLAYTYPE_NORMAL)
    Back = 1,    // 非同期バックグラウンド再生 (DX_PLAYTYPE_BACK)
    Loop = 2     // ループバックグラウンド再生 (DX_PLAYTYPE_LOOP)
};

/// @brief バックエンド非依存のサウンドサービス抽象インターフェース
export class ISoundService {
public:
    virtual ~ISoundService() = default;

    /// @brief サウンドファイルをメモリにロードする
    /// @return サウンドハンドル (失敗時は -1)
    virtual int LoadSound(const wchar_t* file_path) = 0;

    /// @brief サウンドを再生する
    virtual bool Play(int sound_handle, SoundPlayType play_type, bool top_position_flag) = 0;

    /// @brief サウンドの再生を停止する
    virtual bool Stop(int sound_handle) = 0;

    /// @brief サウンドの音量を設定する (0 ~ 255)
    virtual bool SetVolume(int sound_handle, int volume_ratio) = 0;

    /// @brief サウンドメモリを破棄する
    virtual bool Delete(int sound_handle) = 0;

    /// @brief サウンドが再生中かどうか判定する
    virtual bool IsPlaying(int sound_handle) = 0;
};

}  // namespace ab
