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
/// <param name="height">プラットフォーム高さ</param>
public sealed class PlatformNode(int id, float leftX, float rightX, float topY, float height) {
    /// <summary>プラットフォームの一意な識別番号 (0-based)</summary>
    public int Id { get; } = id;

    /// <summary>プラットフォーム上面（着地基準面）の Y 座標</summary>
    public float TopY { get; } = topY;

    /// <summary>プラットフォームの左端 X 座標</summary>
    public float LeftX { get; } = leftX;

    /// <summary>プラットフォームの右端 X 座標</summary>
    public float RightX { get; } = rightX;

    /// <summary>プラットフォームの厚み（高さ）</summary>
    public float Height { get; } = height;

    /// <summary>プラットフォームの中心 X 座標</summary>
    public float CenterX => (LeftX + RightX) * 0.5f;

    /// <summary>プラットフォームの横幅</summary>
    public float Width => RightX - LeftX;

    /// <summary>
    ///     指定された X 座標がプラットフォームの水平範囲内に含まれているかを判定します。
    /// </summary>
    /// <param name="x">検査する X 座標</param>
    /// <param name="margin">許容マージン（ピクセル）</param>
    /// <returns>範囲内に含まれている場合は true</returns>
    public bool ContainsX(float x, float margin = 0.0f)
    {
        return x >= LeftX - margin && x <= RightX + margin;
    }

    /// <summary>
    ///     指定された足元座標がこのプラットフォーム上に着地・立脚しているかを判定します。
    /// </summary>
    /// <param name="x">エンティティの中心 X 座標</param>
    /// <param name="footY">エンティティの足元 Y 座標</param>
    /// <param name="toleranceX">水平方向の許容誤差</param>
    /// <param name="toleranceY">垂直方向の許容誤差</param>
    /// <returns>プラットフォーム上に立脚している場合は true</returns>
    public bool IsStandingOn(float x, float footY, float toleranceX = 20.0f, float toleranceY = 25.0f)
    {
        var inX = ContainsX(x, toleranceX);
        var inY = MathF.Abs(footY - TopY) <= toleranceY;
        return inX && inY;
    }

    /// <summary>
    ///     プラットフォームの文字列表現を取得します。
    /// </summary>
    public override string ToString()
    {
        return $"PlatformNode #{Id} [X: {LeftX:F0}..{RightX:F0} (W:{Width:F0}), TopY: {TopY:F0}]";
    }
}