namespace AnimalBattle.Systems;

/// <summary>
///     Boss 攻撃予兆（Telegraph）の幾何学的計算（レーザー照準、危険矩形、突進軌道、地表衝撃波、放射光線等）を提供する静的ヘルパークラス。
/// </summary>
public static class TelegraphGeometry {
    /// <summary>
    ///     照準レーザーの終点座標を算出します。
    /// </summary>
    /// <param name="startX">開始 X 座標</param>
    /// <param name="startY">開始 Y 座標</param>
    /// <param name="targetX">目標 X 座標</param>
    /// <param name="targetY">目標 Y 座標</param>
    /// <param name="angleOffset">角度オフセット（ラジアン）</param>
    /// <param name="maxDistance">最大照射距離 (px)</param>
    /// <param name="endX">算出された終点 X 座標</param>
    /// <param name="endY">算出された終点 Y 座標</param>
    public static void CalculateAimLaserEndpoint(
        float startX,
        float startY,
        float targetX,
        float targetY,
        float angleOffset,
        float maxDistance,
        out float endX,
        out float endY)
    {
        var dx = targetX - startX;
        var dy = targetY - startY;
        var baseAngle = MathF.Atan2(dy, dx);
        var finalAngle = baseAngle + angleOffset;

        endX = startX + MathF.Cos(finalAngle) * maxDistance;
        endY = startY + MathF.Sin(finalAngle) * maxDistance;
    }

    /// <summary>
    ///     近接斬撃判定矩形の境界座標を算出します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="rangeX">斬撃有効 X 半径</param>
    /// <param name="rangeY">斬撃有効 Y 半径</param>
    /// <param name="facingRight">Boss が右向きか</param>
    /// <param name="x1">矩形左上 X 座標</param>
    /// <param name="y1">矩形左上 Y 座標</param>
    /// <param name="x2">矩形右下 X 座標</param>
    /// <param name="y2">矩形右下 Y 座標</param>
    public static void CalculateMeleeDangerRect(
        float bx,
        float by,
        float rangeX,
        float rangeY,
        bool facingRight,
        out int x1,
        out int y1,
        out int x2,
        out int y2)
    {
        y1 = (int)(by - rangeY);
        y2 = (int)(by + rangeY);

        if (facingRight)
        {
            x1 = (int)(bx - 10.0f);
            x2 = (int)(bx + rangeX);
        }
        else
        {
            x1 = (int)(bx - rangeX);
            x2 = (int)(bx + 10.0f);
        }
    }

    /// <summary>
    ///     突進軌道矩形の境界座標を算出します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="by">Boss 中心 Y 座標</param>
    /// <param name="height">突進高さ</param>
    /// <param name="facingRight">突進方向（右向きか）</param>
    /// <param name="arenaMinX">アリーナ最小 X 境界</param>
    /// <param name="arenaMaxX">アリーナ最大 X 境界</param>
    /// <param name="x1">矩形左上 X 座標</param>
    /// <param name="y1">矩形左上 Y 座標</param>
    /// <param name="x2">矩形右下 X 座標</param>
    /// <param name="y2">矩形右下 Y 座標</param>
    public static void CalculateChargeTrackRect(
        float bx,
        float by,
        float height,
        bool facingRight,
        float arenaMinX,
        float arenaMaxX,
        out int x1,
        out int y1,
        out int x2,
        out int y2)
    {
        var halfHeight = height / 2.0f;
        y1 = (int)(by - halfHeight);
        y2 = (int)(by + halfHeight);

        if (facingRight)
        {
            x1 = (int)bx;
            x2 = (int)arenaMaxX;
        }
        else
        {
            x1 = (int)arenaMinX;
            x2 = (int)bx;
        }
    }

    /// <summary>
    ///     地表衝撃波レールの境界座標を算出します。
    /// </summary>
    /// <param name="bx">Boss 中心 X 座標</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="halfSpan">拡散半幅</param>
    /// <param name="leftX1">左端 X 座標</param>
    /// <param name="rightX2">右端 X 座標</param>
    /// <param name="y1">上端 Y 座標</param>
    /// <param name="y2">下端 Y 座標</param>
    public static void CalculateGroundRails(
        float bx,
        float groundY,
        float halfSpan,
        out int leftX1,
        out int rightX2,
        out int y1,
        out int y2)
    {
        leftX1 = (int)(bx - halfSpan);
        rightX2 = (int)(bx + halfSpan);
        y1 = (int)(groundY - 8.0f);
        y2 = (int)(groundY + 8.0f);
    }

    /// <summary>
    ///     放射スパイク光線の始点・終点座標を算出します。
    /// </summary>
    /// <param name="cx">中心 X 座標</param>
    /// <param name="cy">中心 Y 座標</param>
    /// <param name="angle">照射角度（ラジアン）</param>
    /// <param name="length">光線長さ</param>
    /// <param name="x1">始点 X 座標</param>
    /// <param name="y1">始点 Y 座標</param>
    /// <param name="x2">終点 X 座標</param>
    /// <param name="y2">終点 Y 座標</param>
    public static void CalculateSpikeRay(
        float cx,
        float cy,
        float angle,
        float length,
        out int x1,
        out int y1,
        out int x2,
        out int y2)
    {
        x1 = (int)cx;
        y1 = (int)cy;
        x2 = (int)(cx + MathF.Cos(angle) * length);
        y2 = (int)(cy + MathF.Sin(angle) * length);
    }
}