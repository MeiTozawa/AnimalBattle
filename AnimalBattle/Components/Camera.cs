namespace AnimalBattle.Components;

/// <summary>
/// 2D カメラ設定およびビューポートを保持する純粋コンポーネント。
/// </summary>
public struct Camera
{
    /// <summary>ビューポート幅</summary>
    public float ViewportWidth;

    /// <summary>ビューポート高さ</summary>
    public float ViewportHeight;

    /// <summary>カメラ中心 X 座標</summary>
    public float X;

    /// <summary>カメラ中心 Y 座標</summary>
    public float Y;

    /// <summary>
    /// 指定されたビューポートサイズで <see cref="Camera"/> の新しいインスタンスを初期化します。
    /// </summary>
    public Camera(float viewportW = 1280.0f, float viewportH = 720.0f)
    {
        ViewportWidth = viewportW;
        ViewportHeight = viewportH;
        X = viewportW / 2.0f;
        Y = viewportH / 2.0f;
    }
}
