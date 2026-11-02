namespace AnimalBattle.Presentation.Vfx;

using System.Drawing;

/// <summary>
/// 視覚エフェクト（火花・破片・マズルフラッシュなど）の描画サイズと色を保持するコンポーネント。
/// </summary>
public record struct VfxParticle(float Radius, Color FillColor, Color OutlineColor);
