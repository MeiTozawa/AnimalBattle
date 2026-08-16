using AnimalBattle.Core;

namespace AnimalBattle.Stages;
/// <summary>
/// ステージ上の足場（プラットフォーム）の座標・属性情報を保持する純粋データクラス。
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
    /// 足場の下端 Y 座標を取得または設定します。
    /// </summary>
    public float Y2 { get; set; }

    /// <summary>
    /// 足場の属性（実体足場、すり抜け足場）を取得または設定します。
    /// </summary>
    public PlatformType Type { get; set; }

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

    /// <summary>
    /// C++ 物理エンジン上で割り当てられたプラットフォーム ID（RuntimeId のエイリアス）。
    /// </summary>
    public int PhysicsPlatformId
    {
        get => RuntimeId;
        set => RuntimeId = value;
    }

    /// <summary>
    /// 境界ボックス領域を取得します。
    /// </summary>
    public Rect<float> BoundingBox => new(X1, Y1, X2, Y2);

    /// <summary>
    /// デフォルト設定で <see cref="PlatformData"/> の新しいインスタンスを初期化します。
    /// </summary>
    public PlatformData()
    {
    }

    /// <summary>
    /// 座標および外観プロパティを指定して <see cref="PlatformData"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="x">中心 X 座標</param>
    /// <param name="y">中心 Y 座標</param>
    /// <param name="width">足場幅</param>
    /// <param name="height">足場高さ</param>
    /// <param name="type">足場属性 (Solid / SemiSolid)</param>
    /// <param name="fillColor">塗りつぶしカラー</param>
    /// <param name="outlineColor">枠線カラー</param>
    public PlatformData(float x, float y, float width, float height, PlatformType type, uint fillColor, uint outlineColor)
    {
        X1 = x - width / 2.0f;
        Y1 = y - height / 2.0f;
        X2 = x + width / 2.0f;
        Y2 = y + height / 2.0f;
        Type = type;
        FillColor = fillColor;
        OutlineColor = outlineColor;
    }
}
