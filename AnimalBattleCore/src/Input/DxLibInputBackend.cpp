module;

#include <DxLib.h>

module DxLibInputBackend;

namespace ab {
    bool DxLibInputBackend::CheckHitKey(int key_code) const {
        return ::CheckHitKey(key_code) == 1;
    }

    bool DxLibInputBackend::GetHitKeyStateAll(char* key_state_buf) const {
        return ::GetHitKeyStateAll(key_state_buf) == 0;
    }

    int DxLibInputBackend::GetJoypadInputState(int input_type) const {
        return ::GetJoypadInputState(input_type);
    }

    int DxLibInputBackend::GetJoypadNum() const {
        return ::GetJoypadNum();
    }

    void DxLibInputBackend::GetMousePoint(int* out_x, int* out_y) const {
        ::GetMousePoint(out_x, out_y);
    }

    int DxLibInputBackend::GetMouseInput() const {
        return ::GetMouseInput();
    }

    int DxLibInputBackend::GetMouseWheelRotVol() const {
        return ::GetMouseWheelRotVol();
    }

    void DxLibInputBackend::SetMouseDispFlag(bool disp_flag) {
        ::SetMouseDispFlag(disp_flag ? TRUE : FALSE);
    }
} // namespace ab
