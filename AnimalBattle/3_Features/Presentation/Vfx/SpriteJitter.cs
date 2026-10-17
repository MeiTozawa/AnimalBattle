namespace AnimalBattle.Components;

/// <summary>
///     被弾・ヒットストップ中の局所スプライト高周波振動コンポーネント。
///     描画時に座標へ微小オフセットを付与します。
/// </summary>
public struct SpriteJitter
{
    /// <summary>水平振動振幅（px）。</summary>
    public float AmplitudeX;

    /// <summary>垂直振動振幅（px）。</summary>
    public float AmplitudeY;

    /// <summary>振動周波数（Hz）。</summary>
    public float Frequency;

    /// <summary>振動タイマー累積値（秒）。</summary>
    public float Timer;

    /// <summary>現在の描画オフセット X（px）。</summary>
    public float CurrentOffsetX;

    /// <summary>現在の描画オフセット Y（px）。</summary>
    public float CurrentOffsetY;

    public SpriteJitter(float amplitudeX, float amplitudeY, float frequency)
    {
        AmplitudeX = amplitudeX;
        AmplitudeY = amplitudeY;
        Frequency = frequency;
        Timer = 0.0f;
        CurrentOffsetX = 0.0f;
        CurrentOffsetY = 0.0f;
    }
}
