using System.Drawing;

namespace AnimalBattle.Presentation.Vfx.Components;

/// <summary>
///     被弾時のヒットフラッシュ：白閃演出データを保持する純粋コンポーネント。
/// </summary>
/// <remarks>
///     指定された時間とカラーで <see cref="HitFlashEffect" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="durationSeconds">フラッシュ総時間秒単位</param>
/// <param name="flashColor">フラッシュ描画色：省略時は白</param>
public struct HitFlashEffect(float durationSeconds, Color flashColor) {
    /// <summary>フラッシュ総持続時間秒単位</summary>
    public float Duration = durationSeconds;

    /// <summary>経過タイマー秒単位</summary>
    public float ElapsedTimer = 0.0f;

    /// <summary>フラッシュ描画カラー <see cref="Color" /></summary>
    public Color FlashColor = flashColor;

    /// <summary>白閃強度 1.0からゼロへ減衰する値。</summary>
    public readonly float Intensity => 1.0f - Math.Clamp(ElapsedTimer / Math.Max(0.001f, Duration), 0.0f, 1.0f);

    /// <summary>フラッシュ演出が完了したかどうかを取得します。</summary>
    public readonly bool IsFinished => ElapsedTimer >= Duration;

    /// <summary>フラッシュ中であるかを取得します。</summary>
    public readonly bool IsFlashing => !IsFinished;
}