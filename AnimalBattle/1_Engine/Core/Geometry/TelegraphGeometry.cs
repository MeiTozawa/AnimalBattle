namespace AnimalBattle.Core.Geometry;

/// <summary>
///     Boss 攻撃予兆（Telegraph）の幾何学的計算（レーザー照準、危険矩形、突進軌道、地表衝撃波、放射光線等）を提供する静的ヘルパークラス。
/// </summary>
public static class TelegraphGeometry {
    /// <summary>
    ///     照準レーザーの終点座標を算出します。
    /// </summary>
    /// <param name="start">開始座標</param>
    /// <param name="target">目標座標</param>
    /// <param name="angleOffset">角度オフセット（ラジアン）</param>
    /// <param name="maxDistance">最大照射距離 (px)</param>
    /// <returns>算出された終点座標</returns>
    public static Vec2f CalculateAimLaserEndpoint(
        Vec2f start,
        Vec2f target,
        float angleOffset,
        float maxDistance)
    {
        var diff = target - start;
        var baseAngle = MathF.Atan2(diff.Y, diff.X);
        var finalAngle = baseAngle + angleOffset;

        return new Vec2f(
            start.X + MathF.Cos(finalAngle) * maxDistance,
            start.Y + MathF.Sin(finalAngle) * maxDistance
        );
    }

    /// <summary>
    ///     近接斬撃判定矩形の境界座標を算出します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="range">斬撃有効半径 (X: 半径, Y: 半径)</param>
    /// <param name="facingRight">Boss が右向きか</param>
    /// <param name="p1">矩形左上座標</param>
    /// <param name="p2">矩形右下座標</param>
    public static void CalculateMeleeDangerRect(
        Vec2f bPos,
        Vec2f range,
        bool facingRight,
        out Vec2i p1,
        out Vec2i p2)
    {
        var y1 = (int)(bPos.Y - range.Y);
        var y2 = (int)(bPos.Y + range.Y);

        int x1, x2;
        if (facingRight)
        {
            x1 = (int)(bPos.X - 10.0f);
            x2 = (int)(bPos.X + range.X);
        }
        else
        {
            x1 = (int)(bPos.X - range.X);
            x2 = (int)(bPos.X + 10.0f);
        }

        p1 = new Vec2i(x1, y1);
        p2 = new Vec2i(x2, y2);
    }

    /// <summary>
    ///     突進軌道矩形の境界座標を算出します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="height">突進高さ</param>
    /// <param name="facingRight">突進方向（右向きか）</param>
    /// <param name="arenaMinX">アリーナ最小 X 境界</param>
    /// <param name="arenaMaxX">アリーナ最大 X 境界</param>
    /// <param name="p1">矩形左上座標</param>
    /// <param name="p2">矩形右下座標</param>
    public static void CalculateChargeTrackRect(
        Vec2f bPos,
        float height,
        bool facingRight,
        float arenaMinX,
        float arenaMaxX,
        out Vec2i p1,
        out Vec2i p2)
    {
        var halfHeight = height / 2.0f;
        var y1 = (int)(bPos.Y - halfHeight);
        var y2 = (int)(bPos.Y + halfHeight);

        int x1, x2;
        if (facingRight)
        {
            x1 = (int)bPos.X;
            x2 = (int)arenaMaxX;
        }
        else
        {
            x1 = (int)arenaMinX;
            x2 = (int)bPos.X;
        }

        p1 = new Vec2i(x1, y1);
        p2 = new Vec2i(x2, y2);
    }

    /// <summary>
    ///     地表衝撃波レールの境界座標を算出します。
    /// </summary>
    /// <param name="bPos">Boss 中心座標</param>
    /// <param name="groundY">地面 Y 座標</param>
    /// <param name="halfSpan">拡散半幅</param>
    /// <param name="p1">左端上部座標</param>
    /// <param name="p2">右端下部座標</param>
    public static void CalculateGroundRails(
        Vec2f bPos,
        float groundY,
        float halfSpan,
        out Vec2i p1,
        out Vec2i p2)
    {
        var x1 = (int)(bPos.X - halfSpan);
        var x2 = (int)(bPos.X + halfSpan);
        var y1 = (int)(groundY - 8.0f);
        var y2 = (int)(groundY + 8.0f);

        p1 = new Vec2i(x1, y1);
        p2 = new Vec2i(x2, y2);
    }

    /// <summary>
    ///     放射スパイク光線の始点・終点座標を算出します。
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="angle">照射角度（ラジアン）</param>
    /// <param name="length">光線長さ</param>
    /// <param name="p1">始点座標</param>
    /// <param name="p2">終点座標</param>
    public static void CalculateSpikeRay(
        Vec2f center,
        float angle,
        float length,
        out Vec2i p1,
        out Vec2i p2)
    {
        p1 = (Vec2i)center;
        p2 = new Vec2i(
            (int)(center.X + MathF.Cos(angle) * length),
            (int)(center.Y + MathF.Sin(angle) * length)
        );
    }
}
