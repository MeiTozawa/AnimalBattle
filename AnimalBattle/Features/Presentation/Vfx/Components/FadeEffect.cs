namespace AnimalBattle.Presentation.Vfx.Components;

/// <summary>
///     アルファ値：透明度のフェードイン・フェードアウト演算データを保持する純粋データコンポーネント。
/// </summary>
/// <remarks>
///     指定されたパラメータで <see cref="FadeEffect" /> の新しいインスタンスを初期化します。
/// </remarks>
/// <param name="duration">演出の総時間秒単位</param>
/// <param name="startAlpha">開始アルファ値 0から255の範囲</param>
/// <param name="endAlpha">目標アルファ値 0から255の範囲</param>
/// <param name="ease">適用するイージング関数タイプ</param>
public struct FadeEffect(
    float duration,
    float startAlpha = 255.0f,
    float endAlpha = 0.0f,
    EaseType ease = EaseType.OutSine) {
    /// <summary>開始アルファ値 0から255の範囲</summary>
    public float StartAlpha = startAlpha;

    /// <summary>目標アルファ値 0から255の範囲</summary>
    public float EndAlpha = endAlpha;

    /// <summary>現在のアルファ値 0から255の範囲</summary>
    public float CurrentAlpha = startAlpha;

    /// <summary>演出の総時間秒単位</summary>
    public float Duration = duration;

    /// <summary>経過タイマー秒単位</summary>
    public float ElapsedTimer = 0.0f;

    /// <summary>適用するイージング関数タイプ</summary>
    public EaseType Ease = ease;

    /// <summary>演出が完了したかどうかを取得します。</summary>
    public readonly bool IsFinished => ElapsedTimer >= Duration;

    /// <summary>
    ///     デフォルト値：持続時間 0.2 秒、OutSine イージングで <see cref="FadeEffect" /> の新しいインスタンスを初期化します。
    /// </summary>
    public FadeEffect() : this(0.2f)
    {
    }
}