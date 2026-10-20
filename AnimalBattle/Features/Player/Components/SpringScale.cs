using AnimalBattle.Core;

namespace AnimalBattle.Player.Components;

/// <summary>
///     二階減衰振動スプリングモデルに基づくスカッシュ＆ストレッチ（押し潰し・引き伸ばし）変形スケールコンポーネント。
///     目標スケール（Target）に向かって物理的に滑らかに揺れ戻りながら収束します。
/// </summary>
public struct SpringScale
{
    /// <summary>現在のスケール倍率 (Vec2f)</summary>
    public Vec2f Current;

    /// <summary>スケール変化速度 (Vec2f)</summary>
    public Vec2f Velocity;

    /// <summary>目標スケール倍率 (Vec2f)</summary>
    public Vec2f Target;

    /// <summary>固有振動数（Hz）</summary>
    public float Frequency;

    /// <summary>減衰比（0.0f ～ 1.0f）</summary>
    public float DampingRatio;

    /// <summary>
    ///     初期スケールと振動パラメータを指定してコンポーネントを構築します。
    /// </summary>
    /// <param name="defaultScale">初期スケール (Vec2f)</param>
    /// <param name="frequency">固有振動数 (Hz)</param>
    /// <param name="dampingRatio">減衰比</param>
    public SpringScale(Vec2f defaultScale, float frequency, float dampingRatio)
    {
        Current = defaultScale;
        Velocity = Vec2f.Zero;
        Target = defaultScale;
        Frequency = frequency;
        DampingRatio = dampingRatio;
    }
}
