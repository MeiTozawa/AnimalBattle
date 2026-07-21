module;

#include <algorithm>
#include <cmath>
#include <RenderUtils.h>

module Camera;

namespace ab {
    Camera::Camera() = default;

    void Camera::Reset(float target_x, float target_y, float scale) {
        current_x_ = target_x;
        current_y_ = target_y;
        current_scale_ = scale;
        target_x_ = target_x;
        target_y_ = target_y;
        target_scale_ = scale;
    }

    void Camera::Update(float dt, const Vec2<float>* targets, int target_count, float viewport_width, float viewport_height) {
        if (target_count <= 0) {
            return;
        }

        if (target_count == 1) {
            target_x_ = targets[0].x;
            target_y_ = targets[0].y;
            target_scale_ = max_scale_;
        } else {
            float min_x = targets[0].x;
            float max_x = targets[0].x;
            float min_y = targets[0].y;
            float max_y = targets[0].y;

            for (int i = 1; i < target_count; ++i) {
                if (targets[i].x < min_x) min_x = targets[i].x;
                if (targets[i].x > max_x) max_x = targets[i].x;
                if (targets[i].y < min_y) min_y = targets[i].y;
                if (targets[i].y > max_y) max_y = targets[i].y;
            }

            target_x_ = (min_x + max_x) / 2.0f;
            target_y_ = (min_y + max_y) / 2.0f;

            float dist_x = max_x - min_x;
            float dist_y = max_y - min_y;

            float needed_width = std::max(dist_x + 2.0f * padding_x_, 400.0f);
            float needed_height = std::max(dist_y + 2.0f * padding_y_, 300.0f);

            float scale_x = viewport_width / needed_width;
            float scale_y = viewport_height / needed_height;

            target_scale_ = std::min(scale_x, scale_y);
            target_scale_ = std::clamp(target_scale_, min_scale_, max_scale_);
        }

        // Frame-rate independent exponential interpolation
        float t = 1.0f - std::exp(-lerp_speed_ * dt);
        t = std::clamp(t, 0.0f, 1.0f);

        current_x_ += (target_x_ - current_x_) * t;
        current_y_ += (target_y_ - current_y_) * t;
        current_scale_ += (target_scale_ - current_scale_) * t;
    }
}  // namespace ab
