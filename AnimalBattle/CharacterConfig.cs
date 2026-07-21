using System;

namespace AnimalBattle;

/// <summary>
/// キャラクターの移動、物理演算、および操作感の調整用パラメータを保持する設定クラス。
/// 本クラスの値を変更することで、キャラクターの挙動を調整できます。
/// </summary>
public class CharacterConfig
{
    /// <summary>
    /// 地上での歩行加速度を取得または設定します。
    /// </summary>
    public float WalkAcceleration { get; set; } = 1200.0f;

    /// <summary>
    /// 地上での走行（ダッシュ）加速度を取得または設定します。
    /// </summary>
    public float RunAcceleration { get; set; } = 2200.0f;

    /// <summary>
    /// 最高歩行速度を取得または設定します。
    /// </summary>
    public float MaxWalkSpeed { get; set; } = 350.0f;

    /// <summary>
    /// 最高走行速度を取得または設定します。
    /// </summary>
    public float MaxRunSpeed { get; set; } = 650.0f;

    /// <summary>
    /// 地上での摩擦減速度（キー離し時の停止力）を取得または設定します。
    /// </summary>
    public float GroundFriction { get; set; } = 2000.0f;

    /// <summary>
    /// 通常ジャンプ初速を取得または設定します。
    /// </summary>
    public float JumpForce { get; set; } = 700.0f;

    /// <summary>
    /// 空中ジャンプ（2段ジャンプ）初速を取得または設定します。
    /// </summary>
    public float DoubleJumpForce { get; set; } = 620.0f;

    /// <summary>
    /// 重力加速度を取得または設定します。
    /// </summary>
    public float Gravity { get; set; } = 1600.0f;

    /// <summary>
    /// 終端落下速度（最大落下速度）を取得または設定します。
    /// </summary>
    public float TerminalVelocity { get; set; } = 1100.0f;

    /// <summary>
    /// 急降下時の落下速度を取得または設定します。
    /// </summary>
    public float FastFallVelocity { get; set; } = 1600.0f;

    /// <summary>
    /// 空中移動加速度を取得または設定します。
    /// </summary>
    public float AirAcceleration { get; set; } = 1200.0f;

    /// <summary>
    /// 空中最高移動速度を取得または設定します。
    /// </summary>
    public float MaxAirSpeed { get; set; } = 480.0f;

    /// <summary>
    /// 空中抵抗減速度を取得または設定します。
    /// </summary>
    public float AirDrag { get; set; } = 300.0f;

    /// <summary>
    /// 走行モードへ移行する入力軸のしきい値を取得または設定します。
    /// </summary>
    public float RunThreshold { get; set; } = 0.7f;

    /// <summary>
    /// 急降下開始時の初速倍率を取得または設定します。
    /// </summary>
    public float FastFallStartMultiplier { get; set; } = 0.6f;

    /// <summary>
    /// すり抜け床をすり抜ける無効化時間を取得または設定します。
    /// </summary>
    public float DropThroughDuration { get; set; } = 0.22f;

    /// <summary>
    /// ヒットスタン（ふっとび）中の空中制御倍率を取得または設定します。
    /// </summary>
    public float HitstunAirControlScale { get; set; } = 0.25f;

    /// <summary>
    /// 着地判定の許容しきい値を取得または設定します。
    /// </summary>
    public float LandingTolerance { get; set; } = 0.05f;

    /// <summary>
    /// キャラクターの横幅（ピクセル）を取得または設定します。
    /// </summary>
    public float Width { get; set; } = 40.0f;

    /// <summary>
    /// キャラクターの高さ（ピクセル）を取得または設定します。
    /// </summary>
    public float Height { get; set; } = 70.0f;

    /// <summary>
    /// キャラクターの体重（ふっとびにくさ）を取得または設定します。
    /// </summary>
    public float Weight { get; set; } = 100.0f;

    /// <summary>
    /// 外部設定ファイル (physics.toml) からパラメータを同期・更新します。
    /// </summary>
    public void SyncFromConfig()
    {
        Gravity = PhysicsConfig.Get("gravity", Gravity);
        TerminalVelocity = PhysicsConfig.Get("terminalVelocity", TerminalVelocity);
        FastFallVelocity = PhysicsConfig.Get("fastFallVelocity", FastFallVelocity);
        WalkAcceleration = PhysicsConfig.Get("walkAcceleration", WalkAcceleration);
        RunAcceleration = PhysicsConfig.Get("runAcceleration", RunAcceleration);
        MaxWalkSpeed = PhysicsConfig.Get("maxWalkSpeed", MaxWalkSpeed);
        MaxRunSpeed = PhysicsConfig.Get("maxRunSpeed", MaxRunSpeed);
        AirAcceleration = PhysicsConfig.Get("airAcceleration", AirAcceleration);
        MaxAirSpeed = PhysicsConfig.Get("maxAirSpeed", MaxAirSpeed);
        GroundFriction = PhysicsConfig.Get("groundFriction", GroundFriction);
        AirDrag = PhysicsConfig.Get("airDrag", AirDrag);
        JumpForce = PhysicsConfig.Get("jumpForce", JumpForce);
        DoubleJumpForce = PhysicsConfig.Get("doubleJumpForce", DoubleJumpForce);
        RunThreshold = PhysicsConfig.Get("runThreshold", RunThreshold);
        FastFallStartMultiplier = PhysicsConfig.Get("fastFallStartMultiplier", FastFallStartMultiplier);
        DropThroughDuration = PhysicsConfig.Get("dropThroughDuration", DropThroughDuration);
        HitstunAirControlScale = PhysicsConfig.Get("hitstunAirControlScale", HitstunAirControlScale);
        LandingTolerance = PhysicsConfig.Get("landingTolerance", LandingTolerance);
    }
}
