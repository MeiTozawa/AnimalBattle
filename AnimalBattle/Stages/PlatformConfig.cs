namespace AnimalBattle.Stages;

/// <summary>
/// ステージ上の足場（プラットフォーム）データを定義するクラス。
/// </summary>
public class PlatformConfig
{
    /// <summary>中心 X 座標</summary>
    public float X { get; set; }

    /// <summary>中心 Y 座標</summary>
    public float Y { get; set; }

    /// <summary>幅</summary>
    public float Width { get; set; }

    /// <summary>高さ</summary>
    public float Height { get; set; }

    /// <summary>プラットフォーム塗りつぶしカラー (ARGB)</summary>
    public uint FillColor { get; set; }

    /// <summary>プラットフォーム外枠ハイライトカラー (ARGB)</summary>
    public uint OutlineColor { get; set; }
}
