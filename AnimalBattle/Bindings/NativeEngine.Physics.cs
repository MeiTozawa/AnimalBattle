using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Bindings;

public static partial class NativeEngine
{
    /// <summary>
    /// 足場（プラットフォーム）の属性タイプを表す列挙型。
    /// </summary>
    public enum PlatformType : int
    {
        /// <summary>
        /// 実体足場（全方向からの衝突・着地が可能）。
        /// </summary>
        Solid = 0,

        /// <summary>
        /// 半実体足場（すり抜け床：上方向からの着地のみ可能、下からのすり抜けおよび下降りが可能）。
        /// </summary>
        SemiSolid = 1
    }

    /// <summary>
    /// 衝突判定なしを表す定数。
    /// </summary>
    public const int CollisionNone = 0;

    /// <summary>
    /// 床（着地）判定を表すビットフラグ。
    /// </summary>
    public const int CollisionFloor = 1 << 0;

    /// <summary>
    /// 天井（頭ぶつけ）判定を表すビットフラグ。
    /// </summary>
    public const int CollisionCeiling = 1 << 1;

    /// <summary>
    /// 左壁判定を表すビットフラグ。
    /// </summary>
    public const int CollisionWallLeft = 1 << 2;

    /// <summary>
    /// 右壁判定を表すビットフラグ。
    /// </summary>
    public const int CollisionWallRight = 1 << 3;

    /// <summary>
    /// 半実体足場の上に乗っていることを表すビットフラグ。
    /// </summary>
    public const int CollisionOnSemiSolid = 1 << 4;

    /// <summary>
    /// Native C++ 物理オブジェクトのデータを保持する構造体。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePhysicsObject
    {
        /// <summary>オブジェクトの固有ID</summary>
        public int Id;
        /// <summary>位置 X 座標</summary>
        public float X;
        /// <summary>位置 Y 座標</summary>
        public float Y;
        /// <summary>操作移動速度 Vx</summary>
        public float Vx;
        /// <summary>操作移動速度 Vy</summary>
        public float Vy;
        /// <summary>ふっとび速度 Kbx</summary>
        public float Kbx;
        /// <summary>ふっとび速度 Kby</summary>
        public float Kby;
        /// <summary>当たり判定の幅</summary>
        public float Width;
        /// <summary>当たり判定の高さ</summary>
        public float Height;
        /// <summary>体重</summary>
        public float Weight;
        /// <summary>ダメージ蓄積率 (%)</summary>
        public float DamagePercent;
        /// <summary>ヒットスタン残り時間</summary>
        public float HitstunTimer;
        /// <summary>すり抜け無効化タイマー</summary>
        public float DropThroughTimer;
        /// <summary>現在の衝突フラグ</summary>
        public int CollisionFlags;
    }

    /// <summary>
    /// Native C++ 物理エンジンのインスタンスを生成します。
    /// </summary>
    /// <return>PhysicsEngine インスタンスポインタ</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_Create")]
    public static partial IntPtr PhysicsCreate();

    /// <summary>
    /// Native C++ 物理エンジンを破棄します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    [LibraryImport(_dllName, EntryPoint = "Physics_Destroy")]
    public static partial void PhysicsDestroy(IntPtr engine);

    /// <summary>
    /// 物理エンジンにキャラクターオブジェクトを追加します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="x">初期 X 座標</param>
    /// <param name="y">初期 Y 座標</param>
    /// <param name="width">オブジェクトの横幅</param>
    /// <param name="height">オブジェクトの高さ</param>
    /// <param name="weight">体重</param>
    /// <return>割り当てられたオブジェクトID</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_AddObject")]
    public static partial int PhysicsAddObject(IntPtr engine, float x, float y, float width, float height, float weight);

    /// <summary>
    /// 物理エンジンから指定されたオブジェクトを削除します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="id">削除対象のオブジェクトID</param>
    /// <return>成功した場合は true</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_RemoveObject")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool PhysicsRemoveObject(IntPtr engine, int id);

    /// <summary>
    /// 指定されたオブジェクトの最新物理状態を取得します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="id">オブジェクトID</param>
    /// <param name="outState">状態格納先ポインタ</param>
    /// <return>取得成功時は true</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_GetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe partial bool PhysicsGetObjectState(IntPtr engine, int id, NativePhysicsObject* outState);

    /// <summary>
    /// 指定されたオブジェクトの物理状態を書き込み・更新します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="id">オブジェクトID</param>
    /// <param name="inState">設定状態ポインタ</param>
    /// <return>更新成功時は true</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_SetObjectState")]
    [SuppressGCTransition]
    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe partial bool PhysicsSetObjectState(IntPtr engine, int id, NativePhysicsObject* inState);

    /// <summary>
    /// 物理エンジンに足場（プラットフォーム）を追加します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="x1">左端 X 座標</param>
    /// <param name="y1">上端 Y 座標</param>
    /// <param name="x2">右端 X 座標</param>
    /// <param name="y2">下端 Y 座標</param>
    /// <param name="type">足場属性 (0: Solid, 1: SemiSolid)</param>
    /// <return>割り当てられた足場ID</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_AddPlatform")]
    public static partial int PhysicsAddPlatform(IntPtr engine, float x1, float y1, float x2, float y2, int type);

    /// <summary>
    /// 物理エンジンから指定された足場を削除します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="id">足場ID</param>
    /// <return>削除成功時は true</return>
    [LibraryImport(_dllName, EntryPoint = "Physics_RemovePlatform")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool PhysicsRemovePlatform(IntPtr engine, int id);

    /// <summary>
    /// オブジェクトにダメージおよび吹っ飛び力を適用します。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="id">対象オブジェクトID</param>
    /// <param name="damage">加算ダメージ量 (%)</param>
    /// <param name="angleRad">吹っ飛び角度（ラジアン）</param>
    /// <param name="baseKb">基本吹っ飛び力</param>
    /// <param name="scaling">吹っ飛びスケーリング倍率</param>
    [LibraryImport(_dllName, EntryPoint = "Physics_ApplyKnockback")]
    [SuppressGCTransition]
    public static partial void PhysicsApplyKnockback(IntPtr engine, int id, float damage, float angleRad, float baseKb, float scaling);

    /// <summary>
    /// 物理エンジンの時間を 1 ステップ進めます。
    /// </summary>
    /// <param name="engine">PhysicsEngine インスタンスポインタ</param>
    /// <param name="dt">ステップ時間（秒）</param>
    [LibraryImport(_dllName, EntryPoint = "Physics_Step")]
    [SuppressGCTransition]
    public static partial void PhysicsStep(IntPtr engine, float dt);
}
