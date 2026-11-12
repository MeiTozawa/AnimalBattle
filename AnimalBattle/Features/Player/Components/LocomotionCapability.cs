namespace AnimalBattle.Player.Components;

using System.Diagnostics;
using AnimalBattle.Configs;

/// <summary>
///     キャラクターの水平移動・加減速および重力キネマティクス能力コンポーネント。
/// </summary>
public struct LocomotionCapability
{
    /// <summary>水平移動基本速度：px/s</summary>
    public float MoveSpeed;

    /// <summary>重力加速度：px/s²</summary>
    public float Gravity;

    /// <summary>最大落下終端速度：px/s</summary>
    public float MaxFallSpeed;

    /// <summary>地上初速加速度：px/s²</summary>
    public float GroundAcceleration;

    /// <summary>地上自然減速加速度：px/s²</summary>
    public float GroundDeceleration;

    /// <summary>地上反転制動加速度：px/s²</summary>
    public float TurnaroundDeceleration;

    /// <summary>空中移動加速度：px/s²</summary>
    public float AirAcceleration;

    /// <summary>空中自然減速加速度：px/s²</summary>
    public float AirDeceleration;

    /// <summary>
    ///     設定オブジェクトから移動能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">運動キネマティクス設定</param>
    /// <returns>構築された LocomotionCapability インスタンス</returns>
    public static LocomotionCapability FromConfig(PlayerLocomotionConfig cfg)
    {
        Debug.Assert(cfg != null, "PlayerLocomotionConfig は null であってはなりません。");
        return new LocomotionCapability
        {
            MoveSpeed = cfg.MoveSpeed,
            Gravity = cfg.Gravity,
            MaxFallSpeed = cfg.MaxFallSpeed,
            GroundAcceleration = cfg.GroundAcceleration,
            GroundDeceleration = cfg.GroundDeceleration,
            TurnaroundDeceleration = cfg.TurnaroundDeceleration,
            AirAcceleration = cfg.AirAcceleration,
            AirDeceleration = cfg.AirDeceleration
        };
    }
}
