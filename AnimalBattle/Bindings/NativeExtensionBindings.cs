using System.Runtime.InteropServices;
using AnimalBattle.Components;
using AnimalBattle.Core.Physics;

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

    #region Batch SIMD Drawing Extension

    /// <summary>
    /// 大量の円（弾幕・パーティクル等）に対して、SIMD 並列視口カリングと一括描画を実行します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_BatchDrawCircles")]
    [SuppressGCTransition]
    public static partial void Extension_BatchDrawCircles(
        Transform* transforms, int count, int radius, uint color, [MarshalAs(UnmanagedType.U1)] bool fill,
        int clipMinX, int clipMaxX, int clipMinY, int clipMaxY);

    /// <summary>
    /// プレイヤー銃弾（発光外層＋内層コア＋ビームトレイル）に対して、SIMD 並列視口カリングと一括描画を実行します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_BatchDrawPlayerBullets")]
    [SuppressGCTransition]
    public static partial void Extension_BatchDrawPlayerBullets(
        Transform* transforms, Velocity* velocities, int count,
        int outerRadius, uint outerColor,
        int innerRadius, uint innerColor,
        int trailOffset, int trailRadius, uint trailColor,
        int clipMinX, int clipMaxX, int clipMinY, int clipMaxY);

    /// <summary>
    /// Boss 弾幕（多層同心光暈・枠線・コア）に対して、SIMD 並列視口カリングと一括描画を実行します。
    /// </summary>
    [LibraryImport(_dllName, EntryPoint = "Extension_BatchDrawBossBullets")]
    [SuppressGCTransition]
    public static partial void Extension_BatchDrawBossBullets(
        Transform* transforms, int count,
        int radius, uint fillColor, uint outlineColor,
        int outlinePadding, float coreRadiusRatio, int coreMinRadius, uint coreColor,
        int clipMinX, int clipMaxX, int clipMinY, int clipMaxY);

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
