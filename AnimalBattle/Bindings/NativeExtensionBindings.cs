using System.Runtime.InteropServices;
using AnimalBattle.Components;

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
}
