module;

#include <algorithm>
#include <cmath>
#include <numbers>

export module Easing;

export import PhysicsTypes;

namespace {

constexpr float PI = std::numbers::pi_v<float>;

/// @brief OutBounce 補間係数を計算します。
constexpr float EvaluateOutBounce(float x) noexcept {
    constexpr float n1 = 7.5625f;
    constexpr float d1 = 2.75f;
    if (x < 1.0f / d1) {
        return n1 * x * x;
    }
    if (x < 2.0f / d1) {
        x -= 1.5f / d1;
        return n1 * x * x + 0.75f;
    }
    if (x < 2.5f / d1) {
        x -= 2.25f / d1;
        return n1 * x * x + 0.9375f;
    }
    x -= 2.625f / d1;
    return n1 * x * x + 0.984375f;
}

/// @brief OutElastic 補間係数を計算します。
float EvaluateOutElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c4 = (2.0f * PI) / 3.0f;
    return std::pow(2.0f, -10.0f * t) * std::sin((t * 10.0f - 0.75f) * c4) + 1.0f;
}

/// @brief InElastic 補間係数を計算します。
float EvaluateInElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c4 = (2.0f * PI) / 3.0f;
    return -std::pow(2.0f, 10.0f * t - 10.0f) * std::sin((t * 10.0f - 10.75f) * c4);
}

/// @brief InOutElastic 補間係数を計算します。
float EvaluateInOutElastic(float t) noexcept {
    if (t <= 0.0f) return 0.0f;
    if (t >= 1.0f) return 1.0f;
    constexpr float c5 = (2.0f * PI) / 4.5f;
    return t < 0.5f
        ? -(std::pow(2.0f, 20.0f * t - 10.0f) * std::sin((20.0f * t - 11.125f) * c5)) * 0.5f
        : (std::pow(2.0f, -20.0f * t + 10.0f) * std::sin((20.0f * t - 11.125f) * c5)) * 0.5f + 1.0f;
}

}  // namespace

export namespace ab::extension {

/// @brief 指定されたイージング型および進捗割合から補間値を計算します。
float EvaluateEase(ab::EaseType ease_type, float progress) noexcept {
    float p = std::clamp(progress, 0.0f, 1.0f);

    switch (ease_type) {
        case ab::EaseType::Linear:
            return p;
        case ab::EaseType::InSine:
            return 1.0f - std::cos(p * PI * 0.5f);
        case ab::EaseType::OutSine:
            return std::sin(p * PI * 0.5f);
        case ab::EaseType::InOutSine:
            return -(std::cos(PI * p) - 1.0f) * 0.5f;
        case ab::EaseType::InQuad:
            return p * p;
        case ab::EaseType::OutQuad:
            return 1.0f - (1.0f - p) * (1.0f - p);
        case ab::EaseType::InOutQuad:
            return p < 0.5f ? 2.0f * p * p : 1.0f - std::pow(-2.0f * p + 2.0f, 2.0f) * 0.5f;
        case ab::EaseType::InCubic:
            return p * p * p;
        case ab::EaseType::OutCubic:
            return 1.0f - std::pow(1.0f - p, 3.0f);
        case ab::EaseType::InOutCubic:
            return p < 0.5f ? 4.0f * p * p * p : 1.0f - std::pow(-2.0f * p + 2.0f, 3.0f) * 0.5f;
        case ab::EaseType::InExpo:
            return p <= 0.0f ? 0.0f : std::pow(2.0f, 10.0f * p - 10.0f);
        case ab::EaseType::OutExpo:
            return p >= 1.0f ? 1.0f : 1.0f - std::pow(2.0f, -10.0f * p);
        case ab::EaseType::InOutExpo:
            if (p <= 0.0f) return 0.0f;
            if (p >= 1.0f) return 1.0f;
            return p < 0.5f ? std::pow(2.0f, 20.0f * p - 10.0f) * 0.5f : (2.0f - std::pow(2.0f, -20.0f * p + 10.0f)) * 0.5f;
        case ab::EaseType::InElastic:
            return EvaluateInElastic(p);
        case ab::EaseType::OutElastic:
            return EvaluateOutElastic(p);
        case ab::EaseType::InOutElastic:
            return EvaluateInOutElastic(p);
        case ab::EaseType::InBounce:
            return 1.0f - EvaluateOutBounce(1.0f - p);
        case ab::EaseType::OutBounce:
            return EvaluateOutBounce(p);
        case ab::EaseType::InOutBounce:
            return p < 0.5f
                ? (1.0f - EvaluateOutBounce(1.0f - 2.0f * p)) * 0.5f
                : (1.0f + EvaluateOutBounce(2.0f * p - 1.0f)) * 0.5f;
        default:
            return p * p;
    }
}

}  // namespace ab::extension
