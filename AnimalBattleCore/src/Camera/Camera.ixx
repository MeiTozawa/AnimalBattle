module;

#include <cstdint>
#include <memory>
#include <RenderUtils.h>

export module Camera;

namespace ab {
    export class Camera {
    public:
        Camera(float x, float y, float scale, float min_scale, float max_scale, float lerp_speed, float padding_x, float padding_y);

        void Reset(float target_x, float target_y, float scale);
        void Update(float dt, const Vec2<float>* targets, int target_count, float viewport_width, float viewport_height);

        float GetX() const { return current_x_; }
        float GetY() const { return current_y_; }
        float GetScale() const { return current_scale_; }

        void SetMinScale(float scale) { min_scale_ = scale; }
        void SetMaxScale(float scale) { max_scale_ = scale; }
        void SetLerpSpeed(float speed) { lerp_speed_ = speed; }
        void SetPadding(float pad_x, float pad_y) { padding_x_ = pad_x; padding_y_ = pad_y; }

    private:
        float current_x_ = 0.0f;
        float current_y_ = 0.0f;
        float current_scale_ = 1.0f;

        float target_x_ = 0.0f;
        float target_y_ = 0.0f;
        float target_scale_ = 1.0f;

        float min_scale_ = 0.0f;
        float max_scale_ = 1.0f;
        float lerp_speed_ = 0.0f;
        float padding_x_ = 0.0f;
        float padding_y_ = 0.0f;
    };
}  // namespace ab
