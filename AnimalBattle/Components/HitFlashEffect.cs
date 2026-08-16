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

    /// <summary>フラッシュ描画カラー (ARGB)</summary>
    public uint FlashColor;

    /// <summary>
    /// 指定された時間とカラーで <see cref="HitFlashEffect"/> の新しいインスタンスを初期化します。
    /// </summary>
    public HitFlashEffect(float durationSeconds = 0.15f, uint flashColor = 0xFFFFFFFF)
    {
        IsFlashing = true;
        Duration = durationSeconds;
        ElapsedTimer = 0.0f;
        FlashColor = flashColor;
    }
}
