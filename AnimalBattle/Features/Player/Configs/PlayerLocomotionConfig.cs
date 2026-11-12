namespace AnimalBattle.Configs;

/// <summary>
///     プレイヤーの地上・空中移動および運動キネマティクス設定。
/// </summary>
public sealed class PlayerLocomotionConfig
{
    /// <summary>水平移動基本速度：px/s</summary>
    public float MoveSpeed { get; set; }

    /// <summary>重力加速度：px/s²</summary>
    public float Gravity { get; set; }

    /// <summary>最大落下終端速度：px/s</summary>
    public float MaxFallSpeed { get; set; }

    /// <summary>地上初速加速度：px/s²</summary>
    public float GroundAcceleration { get; set; }

    /// <summary>地上自然減速加速度：px/s²</summary>
    public float GroundDeceleration { get; set; }

    /// <summary>地上反転制動加速度：px/s²</summary>
    public float TurnaroundDeceleration { get; set; }

    /// <summary>空中移動加速度：px/s²</summary>
    public float AirAcceleration { get; set; }

    /// <summary>空中自然減速加速度：px/s²</summary>
    public float AirDeceleration { get; set; }
}
