module;

#include <cstdint>
#include <memory>
#include <RenderUtils.h>

export module Camera;

namespace ab {
    export class Camera {
    public:
        Camera();

        void Reset(float targetX, float targetY, float scale);
        void Update(float dt, const Vec2<float>* targets, int targetCount, float viewportWidth, float viewportHeight);

        float GetX() const { return m_currentX; }
        float GetY() const { return m_currentY; }
        float GetScale() const { return m_currentScale; }

        void SetMinScale(float scale) { m_minScale = scale; }
        void SetMaxScale(float scale) { m_maxScale = scale; }
        void SetLerpSpeed(float speed) { m_lerpSpeed = speed; }
        void SetPadding(float padX, float padY) { m_paddingX = padX; m_paddingY = padY; }

    private:
        float m_currentX = 640.0f;
        float m_currentY = 360.0f;
        float m_currentScale = 1.0f;

        float m_targetX = 640.0f;
        float m_targetY = 360.0f;
        float m_targetScale = 1.0f;

        float m_minScale = 0.45f;
        float m_maxScale = 1.0f;
        float m_lerpSpeed = 4.0f;
        float m_paddingX = 250.0f;
        float m_paddingY = 180.0f;
    };
}
