using System.Drawing;

namespace AnimalBattle.Presentation.Vfx.Components;

/// <summary>
///     被弾時白閃：ホワイトフラッシュ演出の設定パラメータを保持するコンポーネント。設定ファイルから注入されます。
/// </summary>
public struct HitFlashSettings
{
    /// <summary>白閃の持続時間秒単位。</summary>
    public float Duration;

    /// <summary>白閃の発光色。</summary>
    public Color Color;

    public HitFlashSettings(float duration)
    {
        Duration = duration;
        Color = Color.White;
    }

    public HitFlashSettings(float duration, Color color)
    {
        Duration = duration;
        Color = color;
    }
}
