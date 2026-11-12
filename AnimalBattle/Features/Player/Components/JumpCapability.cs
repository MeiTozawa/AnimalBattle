namespace AnimalBattle.Player.Components;

using System.Diagnostics;
using AnimalBattle.Configs;

/// <summary>
///     キャラクターの跳躍性能および跳躍演出パラメータ定義。
///     先行入力・コヨーテタイムおよび残りジャンプ回数の実行時状態を管理します。
/// </summary>
public struct JumpCapability
{
    /// <summary>一段目地上ジャンプ初速度：上向きは負値、px/s</summary>
    public float FirstJumpVelocity;

    /// <summary>空中二段ジャンプ初速度：上向きは負値、px/s</summary>
    public float SecondJumpVelocity;

    /// <summary>最大跳躍可能回数</summary>
    public int MaxJumps;

    /// <summary>現在の残り跳躍可能回数</summary>
    public int RemainingJumps;

    /// <summary>先行入力猶予時間秒単位</summary>
    public float JumpBufferDuration;

    /// <summary>現在の先行入力タイマー秒単位</summary>
    public float JumpBufferTimer;

    /// <summary>コヨーテタイム猶予時間秒単位</summary>
    public float CoyoteTimeDuration;

    /// <summary>現在のコヨーテタイムタイマー秒単位</summary>
    public float CoyoteTimer;

    /// <summary>ジャンプ中断（ボタン解放）時の上昇減衰率</summary>
    public float JumpCutDamping;

    /// <summary>頂点滞空判定の速度閾値：px/s</summary>
    public float ApexFloatVelocityThreshold;

    /// <summary>頂点滞空時の重力倍率</summary>
    public float ApexFloatGravityMultiplier;

    /// <summary>跳躍開始時ストレッチ拡縮率</summary>
    public Vec2f JumpStretchScale;

    /// <summary>着地時スカッシュ拡縮率</summary>
    public Vec2f LandSquashScale;

    /// <summary>着地時スカッシュ演出を発火させる最小落下速度閾値：px/s</summary>
    public float LandSquashVelocityThreshold;

    /// <summary>二段ジャンプ展開リングエフェクトの初期半径ピクセル単位</summary>
    public float JumpRingInitialRadius;

    /// <summary>二段ジャンプ展開リングエフェクトの最大拡大半径ピクセル単位</summary>
    public float JumpRingMaxRadius;

    /// <summary>二段ジャンプ展開リングエフェクトの展開持続時間秒単位</summary>
    public float JumpRingDuration;

    /// <summary>
    ///     設定オブジェクトから跳躍能力コンポーネントを構築します。
    /// </summary>
    /// <param name="cfg">跳躍設定</param>
    /// <returns>構築された JumpCapability インスタンス</returns>
    public static JumpCapability FromConfig(PlayerJumpConfig cfg)
    {
        Debug.Assert(cfg != null, "PlayerJumpConfig は null であってはなりません。");
        return new JumpCapability
        {
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
            JumpStretchScale = cfg.JumpStretchScale,
            LandSquashScale = cfg.LandSquashScale,
            LandSquashVelocityThreshold = cfg.LandSquashVelocityThreshold,
            JumpRingInitialRadius = cfg.JumpRingInitialRadius,
            JumpRingMaxRadius = cfg.JumpRingMaxRadius,
            JumpRingDuration = cfg.JumpRingDuration
        };
    }
}
