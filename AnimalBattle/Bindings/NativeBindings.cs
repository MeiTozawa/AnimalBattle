using System;
using System.Runtime.InteropServices;
using AnimalBattle.Core.Physics;

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

    #region InputService High-Level APIs

    [LibraryImport(_dllName, EntryPoint = "Input_Update")]
    [SuppressGCTransition]
    public static partial void Input_Update();

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
    public static partial void Input_GetMousePosition(int* outX, int* outY);

    [LibraryImport(_dllName, EntryPoint = "Input_GetMouseDelta")]
    [SuppressGCTransition]
    public static partial void Input_GetMouseDelta(int* outDx, int* outDy);

    [LibraryImport(_dllName, EntryPoint = "Input_GetMouseWheelDelta")]
    [SuppressGCTransition]
    public static partial int Input_GetMouseWheelDelta();

    [LibraryImport(_dllName, EntryPoint = "Input_IsMouseOver")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Input_IsMouseOver(int x1, int y1, int x2, int y2);

    #endregion

    #region Input Legacy & Low-level APIs

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
    public static partial void Engine_GetMousePoint(int* x, int* y);

    #endregion

    #region Assets

    [LibraryImport(_dllName, EntryPoint = "Engine_LoadGraph", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int Engine_LoadGraph(string path);

    [LibraryImport(_dllName, EntryPoint = "Engine_DeleteGraph")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Engine_DeleteGraph(int handle);

    #endregion

    #region Drawing

    [LibraryImport(_dllName, EntryPoint = "Draw_Circle")]
    [SuppressGCTransition]
    public static partial void Draw_Circle(int x, int y, int radius, uint color, [MarshalAs(UnmanagedType.U1)] bool fill);

    [LibraryImport(_dllName, EntryPoint = "Draw_Box")]
    [SuppressGCTransition]
    public static partial void Draw_Box(int x1, int y1, int x2, int y2, uint color, [MarshalAs(UnmanagedType.U1)] bool fill);

    [LibraryImport(_dllName, EntryPoint = "Draw_String", StringMarshalling = StringMarshalling.Utf16)]
    public static partial void Draw_String(int x, int y, string text, uint color);

    [LibraryImport(_dllName, EntryPoint = "Draw_Graph")]
    [SuppressGCTransition]
    public static partial void Draw_Graph(int x, int y, int handle, [MarshalAs(UnmanagedType.U1)] bool trans);

    [LibraryImport(_dllName, EntryPoint = "Draw_RectRotaGraph")]
    [SuppressGCTransition]
    public static partial void Draw_RectRotaGraph(int x, int y, int srcX, int srcY, int width, int height, double scale, double angle, int handle, [MarshalAs(UnmanagedType.U1)] bool trans, [MarshalAs(UnmanagedType.U1)] bool turnX);

    #endregion

    #region Physics & Mathematics

    [LibraryImport(_dllName, EntryPoint = "Physics_IntersectsCircleCircle")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_IntersectsCircleCircle(Circle2D c1, Circle2D c2);

    [LibraryImport(_dllName, EntryPoint = "Physics_IntersectsBoxBox")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_IntersectsBoxBox(Box2D b1, Box2D b2);

    [LibraryImport(_dllName, EntryPoint = "Physics_IntersectsCircleBox")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_IntersectsCircleBox(Circle2D circle, Box2D box);

    [LibraryImport(_dllName, EntryPoint = "Physics_IntersectsPointCircle")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_IntersectsPointCircle(Vector2D point, Circle2D circle);

    [LibraryImport(_dllName, EntryPoint = "Physics_IntersectsPointBox")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_IntersectsPointBox(Vector2D point, Box2D box);

    [LibraryImport(_dllName, EntryPoint = "Physics_CheckPlatformGround")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_CheckPlatformGround(float prevY, float nextY, Box2D playerBox, Box2D platformBox, float* outGroundY, float* outVy);

    [LibraryImport(_dllName, EntryPoint = "Physics_CheckPenetrationBoxBox")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool Physics_CheckPenetrationBoxBox(Box2D b1, Box2D b2, Vector2D* outNormal, float* outDepth);

    [LibraryImport(_dllName, EntryPoint = "Physics_IntegrateVelocity")]
    [SuppressGCTransition]
    public static partial Vector2D Physics_IntegrateVelocity(Vector2D position, Vector2D velocity, float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "Physics_ApplyGravity")]
    [SuppressGCTransition]
    public static partial void Physics_ApplyGravity(float* inOutVy, float gravity, float maxFallSpeed, float deltaTime);

    [LibraryImport(_dllName, EntryPoint = "Physics_CalculateDistance")]
    [SuppressGCTransition]
    public static partial float Physics_CalculateDistance(Vector2D a, Vector2D b);

    [LibraryImport(_dllName, EntryPoint = "Physics_CalculateAngle")]
    [SuppressGCTransition]
    public static partial float Physics_CalculateAngle(Vector2D from, Vector2D to);

    [LibraryImport(_dllName, EntryPoint = "Physics_DirectionToVector")]
    [SuppressGCTransition]
    public static partial Vector2D Physics_DirectionToVector(float angleRad, float speed);

    [LibraryImport(_dllName, EntryPoint = "Physics_Clamp")]
    [SuppressGCTransition]
    public static partial float Physics_Clamp(float value, float minVal, float maxVal);

    [LibraryImport(_dllName, EntryPoint = "Physics_Lerp")]
    [SuppressGCTransition]
    public static partial float Physics_Lerp(float a, float b, float t);

    #endregion
}
