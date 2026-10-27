namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     無敵時間中の周期的点滅演出を保持する表現コンポーネント。
///     論理可視性 Sprite.IsVisible を改変せず、独立レンダーパスで描画スキップを制御します。
/// </summary>
public struct InvincibleBlinkEffect
{
    /// <summary>無敵点滅の持続時間（秒）。</summary>
    public float Duration;

    /// <summary>経過時間タイマー（秒）。</summary>
    public float ElapsedTimer;

    /// <summary>点滅周波数（Hz）。</summary>
    public float Frequency;

    public InvincibleBlinkEffect(float duration, float frequency = 30.0f)
    {
        Duration = duration;
        ElapsedTimer = 0.0f;
        Frequency = frequency;
    }

    /// <summary>点滅演出が完了したか判定します。</summary>
    public readonly bool IsFinished => ElapsedTimer >= Duration;

    /// <summary>現在フレームが点滅の非表示（滅）位相であるか判定します。</summary>
    public readonly bool IsBlinkHidden => ((int)(ElapsedTimer * Frequency) % 2) != 0;
}
