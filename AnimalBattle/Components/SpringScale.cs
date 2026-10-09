namespace AnimalBattle.Components;

/// <summary>
///     二階減衰振動スプリングモデルに基づくスカッシュ＆ストレッチ（押し潰し・引き伸ばし）変形スケールコンポーネント。
///     目標スケール（TargetX, TargetY）に向かって物理的に滑らかに揺れ戻りながら収束します。
/// </summary>
public struct SpringScale
{
    /// <summary>現在の X 軸スケール倍率</summary>
    public float CurrentX;

    /// <summary>現在の Y 軸スケール倍率</summary>
    public float CurrentY;

    /// <summary>X 軸スケール変化速度</summary>
    public float VelX;

    /// <summary>Y 軸スケール変化速度</summary>
    public float VelY;

    /// <summary>目標 X 軸スケール倍率（通常 1.0f）</summary>
    public float TargetX;

    /// <summary>目標 Y 軸スケール倍率（通常 1.0f）</summary>
    public float TargetY;

    /// <summary>固有振動数（Hz）</summary>
    public float Frequency;

    /// <summary>減衰比（0.0f ～ 1.0f）</summary>
    public float DampingRatio;

    /// <summary>
    ///     初期スケールと振動パラメータを指定してコンポーネントを構築します。
    /// </summary>
    /// <param name="defaultX">初期 X スケール</param>
    /// <param name="defaultY">初期 Y スケール</param>
    /// <param name="frequency">固有振動数 (Hz)</param>
    /// <param name="dampingRatio">減衰比</param>
    public SpringScale(float defaultX, float defaultY, float frequency, float dampingRatio)
    {
        CurrentX = defaultX;
        CurrentY = defaultY;
        VelX = 0.0f;
        VelY = 0.0f;
        TargetX = defaultX;
        TargetY = defaultY;
        Frequency = frequency;
        DampingRatio = dampingRatio;
    }
}
