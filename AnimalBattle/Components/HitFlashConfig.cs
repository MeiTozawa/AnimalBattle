using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
///     被弾時白閃（ホワイトフラッシュ）演出の設定コンポーネント。設定ファイルから注入されます。
/// </summary>
public struct HitFlashConfig
{
    /// <summary>白閃の持続時間（秒）。</summary>
    public float Duration;

    /// <summary>白閃の発光色。</summary>
    public Color Color;

    public HitFlashConfig(float duration)
    {
        Duration = duration;
        Color = Color.White;
    }

    public HitFlashConfig(float duration, Color color)
    {
        Duration = duration;
        Color = color;
    }
}
