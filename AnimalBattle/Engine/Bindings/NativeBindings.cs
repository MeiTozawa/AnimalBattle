using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

/// <summary>
///     Native C++ コアライブラリ (AnimalBattleCore.dll) との P/Invoke バインディングを提供する静的クラス。
/// </summary>
internal static unsafe partial class NativeBindings {
    private const string _dllName = "AnimalBattleCore.dll";

    #region Physics & Mathematics

    [LibraryImport(_dllName, EntryPoint = "Physics_CheckPlatformGround")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_CheckPlatformGround(float prevY, float nextY, Box2D playerBox, Box2D platformBox,
        float* outGroundY, float* outVy);

    #endregion

    #region Engine Control

    [LibraryImport(_dllName, EntryPoint = "Engine_Init")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_Init(IntPtr hwnd, int width, int height);

    [LibraryImport(_dllName, EntryPoint = "Engine_Shutdown")]
    public static partial void Engine_Shutdown();

    [LibraryImport(_dllName, EntryPoint = "Engine_ProcessMessages")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_ProcessMessages();

    [LibraryImport(_dllName, EntryPoint = "Engine_BeginDraw")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_BeginDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_EndDraw")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_EndDraw();

    [LibraryImport(_dllName, EntryPoint = "Engine_SubmitRenderCommands")]
    public static partial void Engine_SubmitRenderCommands(
        RenderPacket* packets,
        int packetCount,
        char* textBuffer,
        int textLength,
        CompactCirclePacket* circles,
        int circleCount);

    [LibraryImport(_dllName, EntryPoint = "Engine_SubmitCompactCircles")]
    public static partial void Engine_SubmitCompactCircles(
        CompactCirclePacket* circles,
        int circleCount);

    [LibraryImport(_dllName, EntryPoint = "Engine_RequestExit")]
    [SuppressGCTransition]
    public static partial void Engine_RequestExit();

    [LibraryImport(_dllName, EntryPoint = "Engine_IsExitRequested")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_IsExitRequested();

    [LibraryImport(_dllName, EntryPoint = "Engine_SetMouseDispFlag")]
    [SuppressGCTransition]
    public static partial void Engine_SetMouseDispFlag([MarshalAs(UnmanagedType.U1)] bool visible);

    #endregion

    #region InputService High-Level APIs

    [LibraryImport(_dllName, EntryPoint = "Input_Update")]
    [SuppressGCTransition]
    public static partial void Input_Update();

    [LibraryImport(_dllName, EntryPoint = "Input_Snapshot")]
    [SuppressGCTransition]
    public static partial void Input_Snapshot();

    [LibraryImport(_dllName, EntryPoint = "Input_IsPressed")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsPressed(int action);

    [LibraryImport(_dllName, EntryPoint = "Input_IsHolding")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsHolding(int action);

    [LibraryImport(_dllName, EntryPoint = "Input_IsReleased")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsReleased(int action);

    [LibraryImport(_dllName, EntryPoint = "Input_IsButtonPressed")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsButtonPressed(int button);

    [LibraryImport(_dllName, EntryPoint = "Input_IsButtonHolding")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsButtonHolding(int button);

    [LibraryImport(_dllName, EntryPoint = "Input_IsButtonReleased")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsButtonReleased(int button);

    [LibraryImport(_dllName, EntryPoint = "Input_PushContext")]
    [SuppressGCTransition]
    public static partial void Input_PushContext(int context);

    [LibraryImport(_dllName, EntryPoint = "Input_PopContext")]
    [SuppressGCTransition]
    public static partial void Input_PopContext();

    [LibraryImport(_dllName, EntryPoint = "Input_ClearAndSetContext")]
    [SuppressGCTransition]
    public static partial void Input_ClearAndSetContext(int context);

    [LibraryImport(_dllName, EntryPoint = "Input_BindAction")]
    [SuppressGCTransition]
    public static partial void Input_BindAction(int context, int action, int* buttons, int buttonCount);

    [LibraryImport(_dllName, EntryPoint = "Input_AddActionButton")]
    [SuppressGCTransition]
    public static partial void Input_AddActionButton(int context, int action, int button);

    [LibraryImport(_dllName, EntryPoint = "Input_ClearActions")]
    [SuppressGCTransition]
    public static partial void Input_ClearActions(int context);

    [LibraryImport(_dllName, EntryPoint = "Input_GetCurrentContext")]
    [SuppressGCTransition]
    public static partial int Input_GetCurrentContext();

    [LibraryImport(_dllName, EntryPoint = "Input_GetActiveDevice")]
    [SuppressGCTransition]
    public static partial int Input_GetActiveDevice();

    [LibraryImport(_dllName, EntryPoint = "Input_GetMousePosition")]
    [SuppressGCTransition]
    public static partial void Input_GetMousePosition(Vec2i* outPos);

    [LibraryImport(_dllName, EntryPoint = "Input_GetMouseDelta")]
    [SuppressGCTransition]
    public static partial void Input_GetMouseDelta(Vec2i* outDelta);

    [LibraryImport(_dllName, EntryPoint = "Input_GetMouseWheelDelta")]
    [SuppressGCTransition]
    public static partial int Input_GetMouseWheelDelta();

    [LibraryImport(_dllName, EntryPoint = "Input_IsMouseOver")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsMouseOver(Vec2i p1, Vec2i p2);

    #endregion

    #region Input Legacy & Low-level APIs

    [LibraryImport(_dllName, EntryPoint = "Engine_CheckHitKey")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_CheckHitKey(int keyCode);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadInputState")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadInputState(int inputType);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetJoypadNum")]
    [SuppressGCTransition]
    public static partial int Engine_GetJoypadNum();

    [LibraryImport(_dllName, EntryPoint = "Engine_GetMousePoint")]
    [SuppressGCTransition]
    public static partial void Engine_GetMousePoint(Vec2i* outPos);

    #endregion

    #region Assets

    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int Engine_LoadGraph(string path);

    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_DeleteGraph(int handle);

    [LibraryImport(_dllName, EntryPoint = "Engine_GetGraphSize")]
    [SuppressGCTransition]
    public static partial void Engine_GetGraphSize(int handle, Vec2i* outSize);

    #endregion

    #region Sound

    [LibraryImport(_dllName, EntryPoint = "Sound_Load", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int Sound_Load(string filePath);

    [LibraryImport(_dllName, EntryPoint = "Sound_Play")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Sound_Play(int soundHandle, int playType, [MarshalAs(UnmanagedType.U1)] bool topPositionFlag);

    [LibraryImport(_dllName, EntryPoint = "Sound_Stop")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Sound_Stop(int soundHandle);

    [LibraryImport(_dllName, EntryPoint = "Sound_SetVolume")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Sound_SetVolume(int soundHandle, int volumeRatio);

    [LibraryImport(_dllName, EntryPoint = "Sound_Delete")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Sound_Delete(int soundHandle);

    [LibraryImport(_dllName, EntryPoint = "Sound_IsPlaying")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Sound_IsPlaying(int soundHandle);

    #endregion
}