namespace AnimalBattle.Configs;

/// <summary>
///     プレイヤーの跳躍性能、先行入力、コヨーテタイムおよび跳躍演出設定。
/// </summary>
public sealed class PlayerJumpConfig
{
    /// <summary>一段目地上ジャンプ初速度：上向き負値、px/s</summary>
    public float FirstJumpVelocity { get; set; }

    /// <summary>空中二段ジャンプ初速度：上向き負値、px/s</summary>
    public float SecondJumpVelocity { get; set; }

    /// <summary>最大跳躍可能回数</summary>
    public int MaxJumps { get; set; }

    /// <summary>先行入力猶予時間秒単位</summary>
    public float JumpBufferDuration { get; set; }

    /// <summary>コヨーテタイム猶予時間秒単位</summary>
    public float CoyoteTimeDuration { get; set; }

    /// <summary>ジャンプ中断（ボタン解放）時の上昇減衰率</summary>
    public float JumpCutDamping { get; set; }

    /// <summary>頂点滞空判定の速度閾値：px/s</summary>
    public float ApexFloatVelocityThreshold { get; set; }

    /// <summary>頂点滞空時の重力倍率</summary>
    public float ApexFloatGravityMultiplier { get; set; }

    /// <summary>跳躍開始時ストレッチ拡縮率</summary>
    public Vec2f JumpStretchScale { get; set; }

    /// <summary>二段ジャンプ空気リング演出の初期半径ピクセル単位</summary>
    public float JumpRingInitialRadius { get; set; }

    /// <summary>二段ジャンプ空気リング演出の最大拡散半径ピクセル単位</summary>
    public float JumpRingMaxRadius { get; set; }

    /// <summary>二段ジャンプ空気リング演出のフェード持続時間秒単位</summary>
    public float JumpRingDuration { get; set; }

    /// <summary>着地時スカッシュ拡縮率</summary>
    public Vec2f LandSquashScale { get; set; }

    /// <summary>着地時スカッシュ演出を発火させる最小落下速度閾値：px/s</summary>
    public float LandSquashVelocityThreshold { get; set; }
}
