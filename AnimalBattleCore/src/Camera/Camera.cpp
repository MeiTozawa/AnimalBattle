module;

#include <algorithm>
#include <cmath>
#include <RenderUtils.h>

module Camera;

namespace ab {
    Camera::Camera() {}

    void Camera::Reset(float targetX, float targetY, float scale) {
        m_currentX = targetX;
        m_currentY = targetY;
        m_currentScale = scale;
        m_targetX = targetX;
        m_targetY = targetY;
        m_targetScale = scale;
    }

    void Camera::Update(float dt, const Vec2<float>* targets, int targetCount, float viewportWidth, float viewportHeight) {
        if (targetCount <= 0) {
            return;
        }

        if (targetCount == 1) {
            m_targetX = targets[0].x;
            m_targetY = targets[0].y;
            m_targetScale = m_maxScale;
        } else {
            float minX = targets[0].x;
            float maxX = targets[0].x;
            float minY = targets[0].y;
            float maxY = targets[0].y;

            for (int i = 1; i < targetCount; ++i) {
                if (targets[i].x < minX) minX = targets[i].x;
                if (targets[i].x > maxX) maxX = targets[i].x;
                if (targets[i].y < minY) minY = targets[i].y;
                if (targets[i].y > maxY) maxY = targets[i].y;
            }

            m_targetX = (minX + maxX) / 2.0f;
            m_targetY = (minY + maxY) / 2.0f;

            float distX = maxX - minX;
            float distY = maxY - minY;

            float neededWidth = std::max(distX + 2.0f * m_paddingX, 400.0f);
            float neededHeight = std::max(distY + 2.0f * m_paddingY, 300.0f);

            float scaleX = viewportWidth / neededWidth;
            float scaleY = viewportHeight / neededHeight;

            m_targetScale = std::min(scaleX, scaleY);
            m_targetScale = std::clamp(m_targetScale, m_minScale, m_maxScale);
        }

        // Frame-rate independent exponential interpolation
        float t = 1.0f - std::exp(-m_lerpSpeed * dt);
        t = std::clamp(t, 0.0f, 1.0f);

        m_currentX += (m_targetX - m_currentX) * t;
        m_currentY += (m_targetY - m_currentY) * t;
        m_currentScale += (m_targetScale - m_currentScale) * t;
    }
}
