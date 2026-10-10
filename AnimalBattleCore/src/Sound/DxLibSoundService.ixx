module;

#include <algorithm>
#include <cstdint>
#include <DxLib.h>

#ifdef GetObject
#undef GetObject
#endif

export module DxLibSoundService;

export import SoundService;

namespace ab {

/// @brief DxLib を使用した具象サウンドサービス
export class DxLibSoundService : public ISoundService {
public:
    DxLibSoundService() = default;
    ~DxLibSoundService() override = default;

    int LoadSound(const wchar_t* file_path) override {
        if (!file_path) return -1;
        return ::LoadSoundMem(file_path);
    }

    bool Play(int sound_handle, SoundPlayType play_type, bool top_position_flag) override {
        if (sound_handle < 0) return false;
        int dx_play_type = DX_PLAYTYPE_BACK;
        switch (play_type) {
            case SoundPlayType::Normal:
                dx_play_type = DX_PLAYTYPE_NORMAL;
                break;
            case SoundPlayType::Back:
                dx_play_type = DX_PLAYTYPE_BACK;
                break;
            case SoundPlayType::Loop:
                dx_play_type = DX_PLAYTYPE_LOOP;
                break;
        }
        return ::PlaySoundMem(sound_handle, dx_play_type, top_position_flag ? TRUE : FALSE) == 0;
    }

    bool Stop(int sound_handle) override {
        if (sound_handle < 0) return false;
        return ::StopSoundMem(sound_handle) == 0;
    }

    bool SetVolume(int sound_handle, int volume_ratio) override {
        if (sound_handle < 0) return false;
        int clamped_vol = std::clamp(volume_ratio, 0, 255);
        return ::ChangeVolumeSoundMem(clamped_vol, sound_handle) == 0;
    }

    bool Delete(int sound_handle) override {
        if (sound_handle < 0) return false;
        return ::DeleteSoundMem(sound_handle) == 0;
    }

    bool IsPlaying(int sound_handle) override {
        if (sound_handle < 0) return false;
        return ::CheckSoundMem(sound_handle) == 1;
    }
};

}  // namespace ab
