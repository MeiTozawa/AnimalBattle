module;

#include <algorithm>
#include <cmath>

module Camera;

namespace ab {
    Camera::Camera(float x, float y, float scale, float min_scale, float max_scale, float lerp_speed, float padding_x, float padding_y)
        : current_x_(x), current_y_(y), current_scale_(scale),
          target_x_(x), target_y_(y), target_scale_(scale),
          min_scale_(min_scale), max_scale_(max_scale),
          lerp_speed_(lerp_speed), padding_x_(padding_x), padding_y_(padding_y) {}

    void Camera::Reset(float target_x, float target_y, float scale) {
        current_x_ = target_x;
        current_y_ = target_y;
        current_scale_ = scale;
        target_x_ = target_x;
        target_y_ = target_y;
        target_scale_ = scale;
    }

    void Camera::Update(float delta_time, const Vec2<float>* targets, int target_count, float viewport_width, float viewport_height) {
        if (targets == nullptr || target_count <= 0) return;

        // 1. Calculate Target Center (Average of all targets)
        float sum_x = 0.0f;
        float sum_y = 0.0f;
        for (int i = 0; i < target_count; ++i) {
            sum_x += targets[i].x;
            sum_y += targets[i].y;
        }
        target_x_ = sum_x / static_cast<float>(target_count);
        target_y_ = sum_y / static_cast<float>(target_count);

        // 2. Calculate Required Scale based on Target Bounding Box
        float min_x = targets[0].x;
        float max_x = targets[0].x;
        float min_y = targets[0].y;
        float max_y = targets[0].y;
        for (int i = 1; i < target_count; ++i) {
            min_x = std::min(min_x, targets[i].x);
            max_x = std::max(max_x, targets[i].x);
            min_y = std::min(min_y, targets[i].y);
            max_y = std::max(max_y, targets[i].y);
        }

        float bbox_width = (max_x - min_x) + padding_x_ * 2.0f;
        float bbox_height = (max_y - min_y) + padding_y_ * 2.0f;

        float scale_x = viewport_width / (bbox_width > 1.0f ? bbox_width : 1.0f);
        float scale_y = viewport_height / (bbox_height > 1.0f ? bbox_height : 1.0f);

        float desired_scale = std::min(scale_x, scale_y);
        desired_scale = std::clamp(desired_scale, min_scale_, max_scale_);
        target_scale_ = desired_scale;

        // 3. Smooth Exponential Interpolation (Framerate-independent)
        float t = 1.0f - std::exp(-lerp_speed_ * delta_time);
        t = std::clamp(t, 0.0f, 1.0f);

        current_x_ += (target_x_ - current_x_) * t;
        current_y_ += (target_y_ - current_y_) * t;
        current_scale_ += (target_scale_ - current_scale_) * t;
    }
}  // namespace ab
