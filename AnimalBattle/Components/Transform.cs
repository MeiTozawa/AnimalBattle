namespace AnimalBattle.Components;

/// <summary>
/// 2D トランスフォーム（位置・拡大率・回転）を表すコンポーネント。
/// </summary>
public struct Transform
{
    /// <summary>X 座標</summary>
    public float X;

    /// <summary>Y 座標</summary>
    public float Y;

    /// <summary>X 軸拡大率</summary>
    public float ScaleX;

    /// <summary>Y 軸拡大率</summary>
    public float ScaleY;

    /// <summary>回転角度（ラジアン）</summary>
    public float Rotation;

    /// <summary>
    /// 指定された位置、拡大率、回転で <see cref="Transform"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Transform(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, float rotation = 0.0f)
    {
        X = x;
        Y = y;
        ScaleX = scaleX;
        ScaleY = scaleY;
        Rotation = rotation;
    }
}
