using System;
using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
/// 被弾時のヒットフラッシュ（白閃）演出データを保持する純粋コンポーネント。
/// </summary>
public struct HitFlashEffect
{
    /// <summary>フラッシュ総持続時間（秒）</summary>
    public float Duration;

    /// <summary>経過タイマー（秒）</summary>
    public float ElapsedTimer;

    /// <summary>フラッシュ描画カラー (<see cref="Color"/>)</summary>
    public Color FlashColor;

    /// <summary>白閃強度 (1.0 から 0.0 への Ease-Out 減衰値)。</summary>
    public readonly float Intensity => 1.0f - Math.Clamp(ElapsedTimer / Math.Max(0.001f, Duration), 0.0f, 1.0f);

    /// <summary>フラッシュ演出が完了したかどうかを取得します。</summary>
    public readonly bool IsFinished => ElapsedTimer >= Duration;

    /// <summary>フラッシュ中であるかを取得します。</summary>
    public readonly bool IsFlashing => !IsFinished;

    /// <summary>
    /// 規定値（0.08 秒、白）で <see cref="HitFlashEffect"/> の新しいインスタンスを初期化します。
    /// </summary>
    public HitFlashEffect()
        : this(0.08f, Color.White)
    {
    }

    /// <summary>
    /// 指定された時間とカラーで <see cref="HitFlashEffect"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="durationSeconds">フラッシュ総時間（秒）</param>
    /// <param name="flashColor">フラッシュ描画色（省略時は白）</param>
    public HitFlashEffect(float durationSeconds, Color? flashColor = null)
    {
        Duration = durationSeconds;
        ElapsedTimer = 0.0f;
        FlashColor = flashColor ?? Color.White;
    }
}
