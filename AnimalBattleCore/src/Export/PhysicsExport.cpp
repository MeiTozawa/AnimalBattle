import PhysicsEngine;

extern "C" {

    // --- Physics Engine APIs ---

    __declspec(dllexport) ab::PhysicsEngine* PhysicsEngine_Create() noexcept {
        try {
            return new ab::PhysicsEngine();
        } catch (...) {
            return nullptr;
        }
    }

    __declspec(dllexport) void PhysicsEngine_Destroy(ab::PhysicsEngine* engine) noexcept {
        try {
            delete engine;
        } catch (...) {
        }
    }

    __declspec(dllexport) void PhysicsEngine_SetGravity(ab::PhysicsEngine* engine, float gx, float gy) noexcept {
        try {
            if (engine) {
                engine->SetGravity(gx, gy);
            }
        } catch (...) {
        }
    }

    __declspec(dllexport) void PhysicsEngine_Update(ab::PhysicsEngine* engine, float delta_time) noexcept {
        try {
            if (engine) engine->Step(delta_time);
        } catch (...) {
        }
    }

}
