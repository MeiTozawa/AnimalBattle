module;

export module DxLibInputBackend;

export import InputTypes;
import InputConcept;

namespace ab {
    /// @brief DxLib の入力 API をカプセル化する具象入力バックエンドクラス
    export class DxLibInputBackend {
    public:
        DxLibInputBackend() = default;

        bool CheckHitKey(int key_code) const;
        bool GetHitKeyStateAll(char* key_state_buf) const;

        int GetJoypadInputState(int input_type) const;
        int GetJoypadNum() const;

        void GetMousePoint(int* out_x, int* out_y) const;
        int GetMouseInput() const;
        int GetMouseWheelRotVol() const;
        void SetMouseDispFlag(bool disp_flag);
    };

    static_assert(InputBackendConcept<DxLibInputBackend>, "DxLibInputBackend must satisfy InputBackendConcept");
} // namespace ab
