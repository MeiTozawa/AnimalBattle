using System.Runtime.InteropServices;
using AnimalBattle.Compute;

namespace AnimalBattle.Bindings;

/// <summary>
/// GDExtension 形式の C++ 原生拡張ライブラリ (AnimalBattleExtension.dll) との P/Invoke バインディングを提供する静的クラス。
/// </summary>
internal static unsafe partial class NativeExtensionBindings
{
    private const string _dllName = "AnimalBattleExtension.dll";

    #region Batch SIMD Physics Extension

    /// <summary>
    /// 大量の弾丸 Transform および Velocity 配列に対して、SIMD / OpenMP 並列による一括物理移動と画面外境界判定を実行します。
    /// </summary>
    /// <param name="transforms">Transform 連続メモリ配列へのポインタ</param>
    /// <param name="velocities">Velocity 連続メモリ配列へのポインタ</param>
    /// <param name="count">処理対象の弾丸数</param>
    /// <param name="deltaTime">経過時間 (秒)</param>
    /// <param name="minX">画面最小 X 境界</param>
    /// <param name="maxX">画面最大 X 境界</param>
    /// <param name="minY">画面最小 Y 境界</param>
    /// <param name="maxY">画面最大 Y 境界</param>
    /// <param name="outDespawnFlags">画面外デスポーンフラグ配列への出力ポインタ（1: デスポーン, 0: 生存）</param>
    [LibraryImport(_dllName, EntryPoint = "Extension_BatchBulletMovement")]
    [SuppressGCTransition]
    public static partial void Extension_BatchBulletMovement(
        Transform* transforms,
        Velocity* velocities,
        int count,
        float deltaTime,
        float minX,
        float maxX,
        float minY,
        float maxY,
        byte* outDespawnFlags);

    #endregion

    #region Batch Compute Extension

    /// <summary>
    /// イージング一括評価および特殊弾幕変調一括計算を C++ 原生拡張で同期並列実行します（完全無状態純粋計算核）。
    /// </summary>
    /// <param name="easeItems">EaseBatchItem 連続メモリ配列へのポインタ。</param>
    /// <param name="easeCount">処理対象のイージング要素数。</param>
    /// <param name="bulletItems">SpecialBulletBatchItem 連続メモリ配列へのポインタ。</param>
    /// <param name="bulletCount">処理対象の特殊弾幕要素数。</param>
    [LibraryImport(_dllName, EntryPoint = "Extension_ProcessBatchCompute")]
    [SuppressGCTransition]
    public static partial void Extension_ProcessBatchCompute(
        EaseBatchItem* easeItems,
        int easeCount,
        SpecialBulletBatchItem* bulletItems,
        int bulletCount);


    /// <summary>
    /// イージング計算要求配列を C++ 原生拡張へ一括転送し並列計算を実行します。
    /// </summary>
    /// <param name="items">EaseBatchItem 連続メモリ配列へのポインタ。</param>
    /// <param name="count">処理対象の要素数。</param>
    [LibraryImport(_dllName, EntryPoint = "Extension_ProcessBatchEasing")]
    [SuppressGCTransition]
    public static partial void Extension_ProcessBatchEasing(EaseBatchItem* items, int count);

    /// <summary>
    /// 特殊弾幕変調要求配列を C++ 原生拡張へ一括転送し並列計算を実行します。
    /// </summary>
    /// <param name="items">SpecialBulletBatchItem 連続メモリ配列へのポインタ。</param>
    /// <param name="count">処理対象の要素数。</param>
    [LibraryImport(_dllName, EntryPoint = "Extension_ProcessBatchSpecialBullets")]
    [SuppressGCTransition]
    public static partial void Extension_ProcessBatchSpecialBullets(SpecialBulletBatchItem* items, int count);


    /// <summary>
    /// 単一の特殊弾幕変調値を C++ 原生拡張で同期計算します。
    /// </summary>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="elapsedTimer">累積タイマー (秒)。</param>
    /// <param name="baseVx">基準 X 速度。</param>
    /// <param name="baseVy">基準 Y 速度。</param>
    /// <param name="baseRadius">基準半径。</param>
    /// <param name="outVx">変調後 X 速度出力。</param>
    /// <param name="outVy">変調後 Y 速度出力。</param>
    /// <param name="outRadius">変調後半径出力。</param>
    [LibraryImport(_dllName, EntryPoint = "Extension_CalculateSpecialBulletModifier")]
    [SuppressGCTransition]
    public static partial void Extension_CalculateSpecialBulletModifier(
        BulletType type,
        float elapsedTimer,
        float baseVx,
        float baseVy,
        float baseRadius,
        out float outVx,
        out float outVy,
        out float outRadius);

    #endregion

    #region Boss Trajectory Extension

    /// <summary>
    /// Boss のホバリング調和軌道（2軸正弦・余弦波動＋プレイヤー追従重み＋平滑 Lerp 補間）を計算します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_CalculateBossHoverPosition")]
    [SuppressGCTransition]
    public static partial void Extension_CalculateBossHoverPosition(
        in Vec2f currentPos,
        in Vec2f basePos,
        in Vec2f playerPos,
        float hoverTimer,
        float hoverSpeedX,
        float hoverSpeedY,
        float hoverAmpX,
        float hoverAmpY,
        float playerPursuitWeight,
        float hoverLerpSpeed,
        float deltaTime,
        out Vec2f outPos);

    /// <summary>
    /// Boss の急降下突進 (DiveRam) 軌道（指定されたイージング曲線による始点・終点補間）を計算します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_CalculateBossDiveRamPosition")]
    [SuppressGCTransition]
    public static partial void Extension_CalculateBossDiveRamPosition(
        in Vec2f startPos,
        in Vec2f targetPos,
        float stateTimer,
        float stateDuration,
        EaseType easeType,
        out Vec2f outPos);

    /// <summary>
    /// Boss の帰還・初期位置復帰軌道（目標アンカーへの指数平滑補間）を計算します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_CalculateBossReturnPosition")]
    [SuppressGCTransition]
    public static partial void Extension_CalculateBossReturnPosition(
        in Vec2f currentPos,
        in Vec2f targetBasePos,
        float returnSpeed,
        float deltaTime,
        out Vec2f outPos);

    #endregion
}
