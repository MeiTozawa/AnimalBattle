using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Stages;

/// <summary>
/// ステージ上の足場（プラットフォーム）の情報を保持するデータ構造体クラス。
/// </summary>
public class PlatformData
{
    /// <summary>
    /// 足場の左端 X 座標を取得または設定します。
    /// </summary>
    public float X1 { get; set; }

    /// <summary>
    /// 足場の上端 Y 座標を取得または設定します。
    /// </summary>
    public float Y1 { get; set; }

    /// <summary>
    /// 足場の右端 X 座標を取得または設定します。
    /// </summary>
    public float X2 { get; set; }

    /// <summary>
    /// 足物の下端 Y 座標を取得または設定します。
    /// </summary>
    public float Y2 { get; set; }

    /// <summary>
    /// 足場の属性（実体足場、すり抜け足場）を取得または設定します。
    /// </summary>
    public NativeEngine.PlatformType Type { get; set; }

    /// <summary>
    /// 足場の塗りつぶしカラー (ARGB) を取得または設定します。
    /// </summary>
    public uint FillColor { get; set; }

    /// <summary>
    /// 足場の枠線カラー (ARGB) を取得または設定します。
    /// </summary>
    public uint OutlineColor { get; set; }

    /// <summary>
    /// C++ 物理エンジン上で割り当てられたランタイムIDを取得または設定します。
    /// </summary>
    public int RuntimeId { get; set; } = -1;
}
