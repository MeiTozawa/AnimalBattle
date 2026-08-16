using System;
using System.Runtime.InteropServices;
using AnimalBattle.Core;

namespace AnimalBattle.Bindings;
/// <summary>
/// Native C++ コアライブラリ (AnimalBattleCore.dll) との P/Invoke バインディングを提供する静的クラス。
/// </summary>
internal static unsafe partial class NativeBindings
{
    private const string _dllName = "AnimalBattleCore.dll";

    #region Engine Control

    [LibraryImport(_dllName, EntryPoint = "Engine_Init")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_Init(IntPtr hwnd, int width, int height);

    [LibraryImport(_dllName, EntryPoint = "Engine_Shutdown")]
    public static partial void Engine_Shutdown();

    [LibraryImport(_dllName, EntryPoint = "Engine_ProcessMessages")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_ProcessMessages();

    [LibraryImport(_dllName, EntryPoint = "Engine_BeginDraw")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_BeginDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_EndDraw")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_EndDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_RequestExit")]
    [SuppressGCTransition]
    public static partial void Engine_RequestExit();

    [LibraryImport(_dllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_IsExitRequested();

    #endregion

    #region Input

    [LibraryImport(_dllName, EntryPoint = "Input_IsKeyHit")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsKeyHit(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Input_GetJoypadState")]
    [SuppressGCTransition]
    public static partial int Input_GetJoypadState(int padIndex);

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorPointer")]
    [SuppressGCTransition]
    public static partial void Engine_SetCursorPointer();

    [LibraryImport(_dllName, EntryPoint = "Engine_SetCursorArrow")]
    [SuppressGCTransition]
    public static partial void Engine_SetCursorArrow();

    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    public static partial int Engine_CheckHitKey(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadInputState")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadInputState(int inputType);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadNum")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadNum();

    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    public static partial void Engine_GetMousePoint(int* x, int* y);

    #endregion

    #region Assets & Audio

    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int Engine_LoadGraph(string path);

    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    public static partial int Engine_DeleteGraph(int handle);

    [LibraryImport(_dllName, EntryPoint = "Engine_PlaySoundMem")]
    [SuppressGCTransition]
    public static partial int Engine_PlaySoundMem(int handle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    [LibraryImport(_dllName, EntryPoint = "Engine_StopSoundMem")]
    [SuppressGCTransition]
    public static partial void Engine_StopSoundMem(int handle);

    [LibraryImport(_dllName, EntryPoint = "Engine_ChangeVolumeSoundMem")]
    [SuppressGCTransition]
    public static partial int Engine_ChangeVolumeSoundMem(int volumePercent, int handle);

    #endregion

    #region Drawing

    [LibraryImport(_dllName, EntryPoint = "Draw_Box")]
    [SuppressGCTransition]
    public static partial void Draw_Box(int x1, int y1, int x2, int y2, uint color, [MarshalAs(UnmanagedType.U1)] bool fill);

    [LibraryImport(_dllName, EntryPoint = "Draw_String", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void Draw_String(int x, int y, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Draw_Graph")]
    [SuppressGCTransition]
    public static partial void Draw_Graph(int x, int y, int handle, [MarshalAs(UnmanagedType.U1)] bool trans);

    [LibraryImport(_dllName, EntryPoint = "Draw_RectRotaGraph")]
    [SuppressGCTransition]
    public static partial void Draw_RectRotaGraph(int x, int y, int srcX, int srcY, int width, int height, double scale, double angle, int handle, [MarshalAs(UnmanagedType.U1)] bool trans, [MarshalAs(UnmanagedType.U1)] bool turnX);

    #endregion

    #region RenderService

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraEnabled")]
    [SuppressGCTransition]
    public static partial void Render_SetCameraEnabled(IntPtr service, [MarshalAs(UnmanagedType.U1)] bool enabled);

    [LibraryImport(_dllName, EntryPoint = "Render_SetCameraState")]
    [SuppressGCTransition]
    public static partial void Render_SetCameraState(IntPtr service, float x, float y, float scale);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawBox")]
    [SuppressGCTransition]
    public static partial void Render_DrawBox(IntPtr service, Rect<int> r, uint color, [MarshalAs(UnmanagedType.U1)] bool fillFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawString", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    public static partial void Render_DrawString(IntPtr service, Point<int> p, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawStringWithBorder", StringMarshalling = StringMarshalling.Utf16)]
    [SuppressGCTransition]
    public static partial void Render_DrawStringWithBorder(IntPtr service, Rect<int> r, string text, BorderShape shape, uint textColor, uint borderColor);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawGraph")]
    [SuppressGCTransition]
    public static partial void Render_DrawGraph(IntPtr service, Point<int> p, int handle, [MarshalAs(UnmanagedType.U1)] bool transFlag);

    [LibraryImport(_dllName, EntryPoint = "Render_DrawRectRotaGraph")]
    [SuppressGCTransition]
    public static partial void Render_DrawRectRotaGraph(IntPtr service, Point<int> p, Rect<int> src, double extRate, double angle, int graphHandle, [MarshalAs(UnmanagedType.U1)] bool transFlag, [MarshalAs(UnmanagedType.U1)] bool turnFlag);

    #endregion

    #region Camera

    [LibraryImport(_dllName, EntryPoint = "Camera_Create")]
    public static partial IntPtr Camera_Create(float x, float y, float scale, float minScale, float maxScale, float lerpSpeed, float paddingX, float paddingY);

    [LibraryImport(_dllName, EntryPoint = "Camera_Destroy")]
    public static partial void Camera_Destroy(IntPtr camera);

    [LibraryImport(_dllName, EntryPoint = "Camera_Reset")]
    [SuppressGCTransition]
    public static partial void Camera_Reset(IntPtr camera, float x, float y, float scale);

    [LibraryImport(_dllName, EntryPoint = "Camera_Update")]
    [SuppressGCTransition]
    public static partial void Camera_Update(IntPtr camera, float deltaTime, Point<float>* targets, int targetCount, float viewportWidth, float viewportHeight);

    [LibraryImport(_dllName, EntryPoint = "Camera_GetState")]
    [SuppressGCTransition]
    public static partial void Camera_GetState(IntPtr camera, float* outX, float* outY, float* outScale);

    [LibraryImport(_dllName, EntryPoint = "Camera_SetConfig")]
    [SuppressGCTransition]
    public static partial void Camera_SetConfig(IntPtr camera, float minScale, float maxScale, float lerpSpeed, float paddingX, float paddingY);

    #endregion

    #region Physics

    [LibraryImport(_dllName, EntryPoint = "Physics_Create")]
    public static partial IntPtr Physics_Create();

    [LibraryImport(_dllName, EntryPoint = "Physics_Destroy")]
    public static partial void Physics_Destroy(IntPtr engine);

    [LibraryImport(_dllName, EntryPoint = "Physics_AddObject")]
    public static partial int Physics_AddObject(IntPtr engine, float x, float y, float width, float height, float weight);

    [LibraryImport(_dllName, EntryPoint = "Physics_RemoveObject")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_RemoveObject(IntPtr engine, int id);

    [LibraryImport(_dllName, EntryPoint = "Physics_GetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_GetObjectState(IntPtr engine, int id, NativePhysicsObject* outState);

    [LibraryImport(_dllName, EntryPoint = "Physics_SetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_SetObjectState(IntPtr engine, int id, NativePhysicsObject* inState);

    [LibraryImport(_dllName, EntryPoint = "Physics_AddPlatform")]
    public static partial int Physics_AddPlatform(IntPtr engine, float x1, float y1, float x2, float y2, int type);

    [LibraryImport(_dllName, EntryPoint = "Physics_RemovePlatform")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_RemovePlatform(IntPtr engine, int id);

    [LibraryImport(_dllName, EntryPoint = "Physics_ApplyKnockback")]
    [SuppressGCTransition]
    public static partial void Physics_ApplyKnockback(IntPtr engine, int id, float damage, float angleRad, float baseKnockback, float scaling);

    [LibraryImport(_dllName, EntryPoint = "Physics_Step")]
    [SuppressGCTransition]
    public static partial void Physics_Step(IntPtr engine, float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "Physics_SetKnockbackParams")]
    [SuppressGCTransition]
    public static partial void Physics_SetKnockbackParams(IntPtr engine, float weightOffset, float damageScale1, float damageScale2, float scaleMultiplier, float baseMultiplier);

    [LibraryImport(_dllName, EntryPoint = "Physics_SetHitstunParams")]
    [SuppressGCTransition]
    public static partial void Physics_SetHitstunParams(IntPtr engine, float baseSeconds, float knockbackScale, float maxSeconds);

    #endregion

    #region AnimationPlayer

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Create")]
    public static partial IntPtr AnimationPlayer_Create();

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Destroy")]
    public static partial void AnimationPlayer_Destroy(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_AddClip", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void AnimationPlayer_AddClip(
        IntPtr player,
        string name,
        int startFrame,
        int endFrame,
        int frameWidth,
        int frameHeight,
        float fps,
        [MarshalAs(UnmanagedType.U1)] bool isLooping);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Play", StringMarshalling = StringMarshalling.Utf8)]
    [SuppressGCTransition]
    public static partial void AnimationPlayer_Play(IntPtr player, string name);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Stop")]
    [SuppressGCTransition]
    public static partial void AnimationPlayer_Stop(IntPtr player);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_Update")]
    [SuppressGCTransition]
    public static partial void AnimationPlayer_Update(IntPtr player, float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "AnimationPlayer_GetCurrentSrcRect")]
    [SuppressGCTransition]
    public static partial void AnimationPlayer_GetCurrentSrcRect(
        IntPtr player,
        out int outLeft,
        out int outTop,
        out int outRight,
        out int outBottom);

    #endregion
}
