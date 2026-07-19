using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    public enum PlatformType : int
    {
        Solid = 0,
        SemiSolid = 1
    }

    public const int COLLISION_NONE = 0;
    public const int COLLISION_FLOOR = 1 << 0;
    public const int COLLISION_CEILING = 1 << 1;
    public const int COLLISION_WALL_LEFT = 1 << 2;
    public const int COLLISION_WALL_RIGHT = 1 << 3;

    [StructLayout(LayoutKind.Sequential)]
    public struct NativePhysicsObject
    {
        public int Id;
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float Kbx;
        public float Kby;
        public float Width;
        public float Height;
        public float Weight;
        public float DamagePercent;
        public float HitstunTimer;
        public float DropThroughTimer;
        public int CollisionFlags;
    }

    [LibraryImport(DllName, EntryPoint = "Physics_Create")]
    public static partial IntPtr PhysicsCreate();

    [LibraryImport(DllName, EntryPoint = "Physics_Destroy")]
    public static partial void PhysicsDestroy(IntPtr engine);

    [LibraryImport(DllName, EntryPoint = "Physics_GetConfigValue", StringMarshalling = StringMarshalling.Utf8)]
    public static partial float PhysicsGetConfigValue(IntPtr engine, string key);

    [LibraryImport(DllName, EntryPoint = "Physics_AddObject")]
    public static partial int PhysicsAddObject(IntPtr engine, float x, float y, float width, float height, float weight);

    [LibraryImport(DllName, EntryPoint = "Physics_RemoveObject")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool PhysicsRemoveObject(IntPtr engine, int id);

    [LibraryImport(DllName, EntryPoint = "Physics_GetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe partial bool PhysicsGetObjectState(IntPtr engine, int id, NativePhysicsObject* outState);

    [LibraryImport(DllName, EntryPoint = "Physics_SetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe partial bool PhysicsSetObjectState(IntPtr engine, int id, NativePhysicsObject* inState);

    [LibraryImport(DllName, EntryPoint = "Physics_AddPlatform")]
    public static partial int PhysicsAddPlatform(IntPtr engine, float x1, float y1, float x2, float y2, int type);

    [LibraryImport(DllName, EntryPoint = "Physics_RemovePlatform")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool PhysicsRemovePlatform(IntPtr engine, int id);

    [LibraryImport(DllName, EntryPoint = "Physics_ApplyKnockback")]
    [SuppressGCTransition]
    public static partial void PhysicsApplyKnockback(IntPtr engine, int id, float damage, float angleRad, float baseKb, float scaling);

    [LibraryImport(DllName, EntryPoint = "Physics_Step")]
    [SuppressGCTransition]
    public static partial void PhysicsStep(IntPtr engine, float dt);
}
