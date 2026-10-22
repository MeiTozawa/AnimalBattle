using AnimalBattle.Core;

namespace AnimalBattle.Presentation.Vfx.Components;

/// <summary>
///     被弾・ヒットストップ中の局所スプライト高周波振動コンポーネント。
///     描画時に座標へ微小オフセットを付与します。
/// </summary>
public struct SpriteJitter
{
    /// <summary>振動振幅 Vec2f型</summary>
    public Vec2f Amplitude;

    /// <summary>振動周波数：Hz</summary>
    public float Frequency;

    /// <summary>振動タイマー累積値秒単位</summary>
    public float Timer;

    /// <summary>現在の描画オフセット Vec2f型</summary>
    public Vec2f CurrentOffset;

    public SpriteJitter(Vec2f amplitude, float frequency)
    {
        Amplitude = amplitude;
        Frequency = frequency;
        Timer = 0.0f;
        CurrentOffset = Vec2f.Zero;
    }
}
