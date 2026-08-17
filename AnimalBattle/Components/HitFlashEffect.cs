using System.Drawing;

namespace AnimalBattle.Components;

/// <summary>
/// 被弾時のヒットフラッシュ演出データを保持する純粋コンポーネント。
/// </summary>
public struct HitFlashEffect
{
    /// <summary>ヒットフラッシュ表示フラグ</summary>
    public bool IsFlashing;

    /// <summary>フラッシュ総時間（秒）</summary>
    public float Duration;

    /// <summary>経過タイマー（秒）</summary>
    public float ElapsedTimer;

    /// <summary>フラッシュ描画カラー (<see cref="Color"/>)</summary>
    public Color FlashColor;

    /// <summary>
    /// 指定された時間とカラーで <see cref="HitFlashEffect"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="durationSeconds">フラッシュ総時間</param>
    /// <param name="flashColor">フラッシュ描画色（省略時は半透明白）</param>
    public HitFlashEffect(float durationSeconds = 0.15f, Color? flashColor = null)
    {
        IsFlashing = true;
        Duration = durationSeconds;
        ElapsedTimer = 0.0f;
        FlashColor = flashColor ?? Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF);
    }
}
