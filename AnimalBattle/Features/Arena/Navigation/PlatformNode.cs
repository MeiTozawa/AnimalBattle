namespace AnimalBattle.Maps.Navigation;

/// <summary>
///     2D プラットフォームナビゲーショングラフにおける単一の独立プラットフォームノード。
///     プラットフォームの表面幾何範囲、着地基準面 Y 座標、および空間包含判定メソッドを保持します。
/// </summary>
/// <remarks>
///     <see cref="PlatformNode" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="id">プラットフォーム ID</param>
/// <param name="leftX">左端 X 座標</param>
/// <param name="rightX">右端 X 座標</param>
/// <param name="topY">上面 Y 座標</param>
public sealed class PlatformNode(int id, float leftX, float rightX, float topY) {
    /// <summary>プラットフォームの一意な識別番号 ゼロ基準</summary>
    public int Id { get; } = id;

    /// <summary>プラットフォーム上面：着地基準面の Y 座標</summary>
    public float TopY { get; } = topY;

    /// <summary>プラットフォームの左端 X 座標</summary>
    public float LeftX { get; } = leftX;

    /// <summary>プラットフォームの右端 X 座標</summary>
    public float RightX { get; } = rightX;

    /// <summary>プラットフォームの中心 X 座標</summary>
    public float CenterX => (LeftX + RightX) * 0.5f;

    /// <summary>プラットフォームの横幅</summary>
    private float Width => RightX - LeftX;

    /// <summary>
    ///     指定された X 座標がプラットフォームの水平範囲内に含まれているかを判定します。
    /// </summary>
    /// <param name="x">検査する X 座標</param>
    /// <param name="margin">許容マージンピクセル単位</param>
    /// <returns>範囲内に含まれている場合は true</returns>
    public bool ContainsX(float x, float margin = 0.0f)
    {
        return x >= LeftX - margin && x <= RightX + margin;
    }

    /// <summary>
    ///     プラットフォームの文字列表現を取得します。
    /// </summary>
    public override string ToString()
    {
        return $"PlatformNode #{Id} [X: {LeftX:F0}..{RightX:F0} (W:{Width:F0}), TopY: {TopY:F0}]";
    }
}
