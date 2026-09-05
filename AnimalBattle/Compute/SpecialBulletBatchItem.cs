using System.Runtime.InteropServices;
using AnimalBattle.Components;

namespace AnimalBattle.Compute;

/// <summary>
/// C++ 原生拡張 (AnimalBattleExtension.dll) とバイナリ互換な特殊弾幕変調一括計算バッチ要素構造体 (32 bytes, AVX2 SIMD 整合)。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SpecialBulletBatchItem
{
    /// <summary>特殊弾幕種別 (0:Normal, 1:Magenta, 2:Yellow, 3:Cyan, 4:Black)。</summary>
    public BulletType Type;

    /// <summary>弾幕生成からの累積タイマー (秒)。</summary>
    public float ElapsedTimer;

    /// <summary>発射時基準 X 速度。</summary>
    public float BaseVx;

    /// <summary>発射時基準 Y 速度。</summary>
    public float BaseVy;

    /// <summary>基準半径 (px)。</summary>
    public float BaseRadius;

    /// <summary>C++ 原生拡張によって計算・出力される変調後 X 速度。</summary>
    public float OutVx;

    /// <summary>C++ 原生拡張によって計算・出力される変調後 Y 速度。</summary>
    public float OutVy;

    /// <summary>C++ 原生拡張によって計算・出力される変調後判定半径。</summary>
    public float OutRadius;

    /// <summary>
    /// 特殊弾幕変調計算バッチ要素を初期化します。
    /// </summary>
    /// <param name="type">特殊弾幕種別。</param>
    /// <param name="elapsedTimer">累積タイマー (秒)。</param>
    /// <param name="baseVx">基準 X 速度。</param>
    /// <param name="baseVy">基準 Y 速度。</param>
    /// <param name="baseRadius">基準半径。</param>
    public SpecialBulletBatchItem(BulletType type, float elapsedTimer, float baseVx, float baseVy, float baseRadius)
    {
        Type = type;
        ElapsedTimer = elapsedTimer;
        BaseVx = baseVx;
        BaseVy = baseVy;
        BaseRadius = baseRadius;
        OutVx = baseVx;
        OutVy = baseVy;
        OutRadius = baseRadius;
    }
}
