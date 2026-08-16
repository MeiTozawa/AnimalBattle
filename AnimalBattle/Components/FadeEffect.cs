using AnimalBattle.Core;

namespace AnimalBattle.Components;

/// <summary>
/// アルファ値（透明度）のフェードイン・フェードアウト演算データを保持する純粋コンポーネント。
/// </summary>
public struct FadeEffect
{
    /// <summary>開始アルファ値 (0～255)</summary>
    public float StartAlpha;

    /// <summary>目標アルファ値 (0～255)</summary>
    public float EndAlpha;

    /// <summary>現在のアルファ値 (0～255)</summary>
    public float CurrentAlpha;

    /// <summary>演出の総時間（秒）</summary>
    public float Duration;

    /// <summary>経過タイマー（秒）</summary>
    public float ElapsedTimer;

    /// <summary>適用するイージング関数タイプ</summary>
    public EaseType Ease;

    /// <summary>
    /// 指定されたパラメータで <see cref="FadeEffect"/> の新しいインスタンスを初期化します。
    /// </summary>
    public FadeEffect(float duration, float initialAlpha = 1.0f, float targetAlpha = 0.0f, EaseType ease = EaseType.Linear)
    {
        StartAlpha = initialAlpha;
        EndAlpha = targetAlpha;
        CurrentAlpha = initialAlpha;
        Duration = duration;
        ElapsedTimer = 0.0f;
        Ease = ease;
    }

    /// <summary>
    /// フェードアウト効果（不透明 255 -> 透明 0）を生成します。
    /// </summary>
    public static FadeEffect FadeOut(float durationSeconds, EaseType ease = EaseType.OutSine)
    {
        return new FadeEffect(255.0f, 0.0f, durationSeconds, ease);
    }

    /// <summary>
    /// フェードイン効果（透明 0 -> 不透明 255）を生成します。
    /// </summary>
    public static FadeEffect FadeIn(float durationSeconds, EaseType ease = EaseType.OutSine)
    {
        return new FadeEffect(0.0f, 255.0f, durationSeconds, ease);
    }
}
