using System.Runtime.InteropServices;
using AnimalBattle.Components;

namespace AnimalBattle.Compute;

/// <summary>
/// C++ 原生拡張 (AnimalBattleExtension.dll) とバイナリ互換な特殊弾幕変調一括計算バッチ要素構造体 (32 bytes, AVX2 SIMD 整合)。
/// </summary>
/// <remarks>
/// 特殊弾幕変調計算バッチ要素を初期化します。
/// </remarks>
/// <param name="type">特殊弾幕種別。</param>
/// <param name="elapsedTimer">累積タイマー (秒)。</param>
/// <param name="baseVx">基準 X 速度。</param>
/// <param name="baseVy">基準 Y 速度。</param>
/// <param name="baseRadius">基準半径。</param>
[StructLayout(LayoutKind.Sequential)]
public struct SpecialBulletBatchItem(BulletType type, float elapsedTimer, float baseVx, float baseVy, float baseRadius)
{
    /// <summary>特殊弾幕種別 (0:Normal, 1:Magenta, 2:Yellow, 3:Cyan, 4:Black)。</summary>
    public BulletType Type = type;

    /// <summary>弾幕生成からの累積タイマー (秒)。</summary>
    public float ElapsedTimer = elapsedTimer;

    /// <summary>発射時基準 X 速度。</summary>
    public float BaseVx = baseVx;

    /// <summary>発射時基準 Y 速度。</summary>
    public float BaseVy = baseVy;

    /// <summary>基準半径 (px)。</summary>
    public float BaseRadius = baseRadius;

    /// <summary>C++ 原生拡張によって計算・出力される変調後 X 速度。</summary>
    public float OutVx = baseVx;

    /// <summary>C++ 原生拡張によって計算・出力される変調後 Y 速度。</summary>
    public float OutVy = baseVy;

    /// <summary>C++ 原生拡張によって計算・出力される変調後判定半径。</summary>
    public float OutRadius = baseRadius;
}
