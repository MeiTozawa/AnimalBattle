using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    [LibraryImport(DllName, EntryPoint = "Camera_Create")]
    public static partial IntPtr CameraCreate();

    [LibraryImport(DllName, EntryPoint = "Camera_Destroy")]
    public static partial void CameraDestroy(IntPtr camera);

    [LibraryImport(DllName, EntryPoint = "Camera_Reset")]
    [SuppressGCTransition]
    public static partial void CameraReset(IntPtr camera, float x, float y, float scale);

    [LibraryImport(DllName, EntryPoint = "Camera_Update")]
    [SuppressGCTransition]
    public static unsafe partial void CameraUpdate(
        IntPtr camera,
        float dt,
        Point<float>* targets,
        int targetCount,
        float viewportWidth,
        float viewportHeight);

    [LibraryImport(DllName, EntryPoint = "Camera_GetState")]
    [SuppressGCTransition]
    public static unsafe partial void CameraGetState(IntPtr camera, float* outX, float* outY, float* outScale);

    [LibraryImport(DllName, EntryPoint = "Camera_SetConfig")]
    [SuppressGCTransition]
    public static partial void CameraSetConfig(
        IntPtr camera,
        float minScale,
        float maxScale,
        float lerpSpeed,
        float paddingX,
        float paddingY);
}
