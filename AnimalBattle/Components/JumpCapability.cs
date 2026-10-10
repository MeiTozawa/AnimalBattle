namespace AnimalBattle.Components;

/// <summary>
///     キャラクターの跳躍性能および運動学パラメータ定義。
///     MovementSystem による重力・跳躍計算に使用されます。
/// </summary>
public struct JumpCapability
{
    /// <summary>水平移動基本速度（px/s）。</summary>
    public float MoveSpeed;

    /// <summary>重力加速度（px/s²）。</summary>
    public float Gravity;

    /// <summary>最大落下終端速度（px/s）。</summary>
    public float MaxFallSpeed;

    /// <summary>一段目初速度（上向きは負値、px/s）。</summary>
    public float FirstJumpVelocity;

    /// <summary>空中二段目初速度（上向きは負値、px/s）。</summary>
    public float SecondJumpVelocity;

    /// <summary>最大跳躍可能回数。</summary>
    public int MaxJumps;

    /// <summary>現在の残り跳躍可能回数。</summary>
    public int RemainingJumps;

    /// <summary>先行入力猶予時間（秒）。</summary>
    public float JumpBufferDuration;

    /// <summary>現在の先行入力タイマー（秒）。</summary>
    public float JumpBufferTimer;

    /// <summary>コヨーテタイム猶予時間（秒）。</summary>
    public float CoyoteTimeDuration;

    /// <summary>現在のコヨーテタイムタイマー（秒）。</summary>
    public float CoyoteTimer;

    /// <summary>ジャンプ中断時の上昇速度減衰率。</summary>
    public float JumpCutDamping;

    /// <summary>頂点滞空判定の速度閾値（px/s）。</summary>
    public float ApexFloatVelocityThreshold;

    /// <summary>頂点滞空時の重力倍率。</summary>
    public float ApexFloatGravityMultiplier;

    /// <summary>地面初速加速度（px/s²）。</summary>
    public float GroundAcceleration;

    /// <summary>地面自然減速加速度（px/s²）。</summary>
    public float GroundDeceleration;

    /// <summary>地面反転制動加速度（px/s²）。</summary>
    public float TurnaroundDeceleration;

    /// <summary>空中移動加速度（px/s²）。</summary>
    public float AirAcceleration;

    /// <summary>空中自然減速加速度（px/s²）。</summary>
    public float AirDeceleration;

    /// <summary>着地時スカッシュ演出を発火させる最小下落速度閾値（px/s）。</summary>
    public float LandSquashVelocityThreshold;

    /// <summary>
    ///     プレイヤー設定（PlayerConfig）から跳躍能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">プレイヤー設定。</param>
    /// <returns>構築された JumpCapability インスタンス。</returns>
    public static JumpCapability FromPlayerConfig(Configs.PlayerConfig cfg)
    {
        System.Diagnostics.Debug.Assert(cfg != null, "PlayerConfig は null であってはなりません。");
        return new JumpCapability
        {
            MoveSpeed = cfg.MoveSpeed,
            Gravity = cfg.Gravity,
            MaxFallSpeed = cfg.MaxFallSpeed,
            FirstJumpVelocity = cfg.FirstJumpVelocity,
            SecondJumpVelocity = cfg.SecondJumpVelocity,
            MaxJumps = cfg.MaxJumps,
            RemainingJumps = cfg.MaxJumps,
            JumpBufferDuration = cfg.JumpBufferDuration,
            JumpBufferTimer = 0.0f,
            CoyoteTimeDuration = cfg.CoyoteTimeDuration,
            CoyoteTimer = 0.0f,
            JumpCutDamping = cfg.JumpCutDamping,
            ApexFloatVelocityThreshold = cfg.ApexFloatVelocityThreshold,
            ApexFloatGravityMultiplier = cfg.ApexFloatGravityMultiplier,
            GroundAcceleration = cfg.GroundAcceleration,
            GroundDeceleration = cfg.GroundDeceleration,
            TurnaroundDeceleration = cfg.TurnaroundDeceleration,
            AirAcceleration = cfg.AirAcceleration,
            AirDeceleration = cfg.AirDeceleration,
            LandSquashVelocityThreshold = cfg.LandSquashVelocityThreshold
        };
    }
}
