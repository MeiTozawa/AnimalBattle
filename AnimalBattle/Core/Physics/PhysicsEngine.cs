using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;

/// <summary>
/// Native C++ 物理エンジン (PhysicsEngine) のライフサイクル、P/Invoke バインディング、および制御操作をカプセル化する C# クラス。
/// </summary>
public unsafe partial class PhysicsEngine : IDisposable
{
    private const string _dllName = "AnimalBattleCore.dll";

    private readonly IntPtr _nativePtr;
    private bool _isDisposed;

    /// <summary>
    /// 新しい Native C++ 物理エンジンインスタンスを生成して初期化します。
    /// </summary>
    public PhysicsEngine()
    {
        _nativePtr = NativePhysicsCreate();
        if (_nativePtr == IntPtr.Zero)
        {
            throw new InvalidOperationException("Native 物理エンジンの生成に失敗しました。");
        }
    }

    /// <summary>
    /// 既存の Native C++ 物理エンジンポインタを指定して初期化します。
    /// </summary>
    public PhysicsEngine(IntPtr nativePtr)
    {
        if (nativePtr == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(nativePtr), "Native 物理エンジンポインタを null にすることはできません。");
        }
        _nativePtr = nativePtr;
    }

    /// <summary>
    /// デストラクタ
    /// </summary>
    ~PhysicsEngine()
    {
        Dispose(false);
    }

    /// <summary>
    /// 内部で保持している Native C++ 物理エンジンポインタを取得します。
    /// </summary>
    public IntPtr NativePtr => _nativePtr;

    /// <summary>
    /// 物理エンジンにキャラクターオブジェクトを追加します。
    /// </summary>
    public int AddObject(float x, float y, float width, float height, float weight)
    {
        return NativePhysicsAddObject(_nativePtr, x, y, width, height, weight);
    }

    /// <summary>
    /// 物理エンジンから指定されたオブジェクトを削除します。
    /// </summary>
    public bool RemoveObject(int id)
    {
        return NativePhysicsRemoveObject(_nativePtr, id);
    }

    /// <summary>
    /// 指定されたオブジェクトの最新物理状態を取得します。
    /// </summary>
    public bool GetObjectState(int id, out NativePhysicsObject outState)
    {
        fixed (NativePhysicsObject* ptr = &outState)
        {
            return NativePhysicsGetObjectState(_nativePtr, id, ptr);
        }
    }

    /// <summary>
    /// 指定されたオブジェクトの最新物理状態をポインタ経由で取得します。
    /// </summary>
    public bool GetObjectState(int id, NativePhysicsObject* outState)
    {
        return NativePhysicsGetObjectState(_nativePtr, id, outState);
    }

    /// <summary>
    /// 指定されたオブジェクトの物理状態を更新します。
    /// </summary>
    public bool SetObjectState(int id, in NativePhysicsObject inState)
    {
        fixed (NativePhysicsObject* ptr = &inState)
        {
            return NativePhysicsSetObjectState(_nativePtr, id, ptr);
        }
    }

    /// <summary>
    /// 指定されたオブジェクトの物理状態をポインタ経由で更新します。
    /// </summary>
    public bool SetObjectState(int id, NativePhysicsObject* inState)
    {
        return NativePhysicsSetObjectState(_nativePtr, id, inState);
    }

    /// <summary>
    /// 物理エンジンに足場（プラットフォーム）を追加します。
    /// </summary>
    public int AddPlatform(float x1, float y1, float x2, float y2, PlatformType type)
    {
        return NativePhysicsAddPlatform(_nativePtr, x1, y1, x2, y2, (int)type);
    }

    /// <summary>
    /// 物理エンジンから指定された足場を削除します。
    /// </summary>
    public bool RemovePlatform(int id)
    {
        return NativePhysicsRemovePlatform(_nativePtr, id);
    }

    /// <summary>
    /// オブジェクトにダメージおよび吹っ飛び力を適用します。
    /// </summary>
    public void ApplyKnockback(int id, float damage, float angleRad, float baseKnockback, float scaling)
    {
        NativePhysicsApplyKnockback(_nativePtr, id, damage, angleRad, baseKnockback, scaling);
    }

    /// <summary>
    /// 物理エンジンの時間を 1 ステップ進めます。
    /// </summary>
    public void Step(float deltaTime)
    {
        NativePhysicsStep(_nativePtr, deltaTime);
    }

    /// <summary>
    /// アンマネージドリソースを解放します。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// リソース解放の保護仮想メソッド。
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (_nativePtr != IntPtr.Zero)
            {
                NativePhysicsDestroy(_nativePtr);
            }
            _isDisposed = true;
        }
    }

    [LibraryImport(_dllName, EntryPoint = "Physics_Create")]
    private static partial IntPtr NativePhysicsCreate();

    [LibraryImport(_dllName, EntryPoint = "Physics_Destroy")]
    private static partial void NativePhysicsDestroy(IntPtr engine);

    [LibraryImport(_dllName, EntryPoint = "Physics_AddObject")]
    private static partial int NativePhysicsAddObject(IntPtr engine, float x, float y, float width, float height, float weight);

    [LibraryImport(_dllName, EntryPoint = "Physics_RemoveObject")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativePhysicsRemoveObject(IntPtr engine, int id);

    [LibraryImport(_dllName, EntryPoint = "Physics_GetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativePhysicsGetObjectState(IntPtr engine, int id, NativePhysicsObject* outState);

    [LibraryImport(_dllName, EntryPoint = "Physics_SetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativePhysicsSetObjectState(IntPtr engine, int id, NativePhysicsObject* inState);

    [LibraryImport(_dllName, EntryPoint = "Physics_AddPlatform")]
    private static partial int NativePhysicsAddPlatform(IntPtr engine, float x1, float y1, float x2, float y2, int type);

    [LibraryImport(_dllName, EntryPoint = "Physics_RemovePlatform")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool NativePhysicsRemovePlatform(IntPtr engine, int id);

    [LibraryImport(_dllName, EntryPoint = "Physics_ApplyKnockback")]
    [SuppressGCTransition]
    private static partial void NativePhysicsApplyKnockback(IntPtr engine, int id, float damage, float angleRad, float baseKnockback, float scaling);

    [LibraryImport(_dllName, EntryPoint = "Physics_Step")]
    [SuppressGCTransition]
    private static partial void NativePhysicsStep(IntPtr engine, float deltaTime);
}
