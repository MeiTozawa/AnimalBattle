namespace AnimalBattle.Components;

/// <summary>
///     キャラクターの跳躍性能および運動学パラメータ定義。
///     KinematicMovementSystem による重力・跳躍計算に使用されます。
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
            RemainingJumps = cfg.MaxJumps
        };
    }
}
