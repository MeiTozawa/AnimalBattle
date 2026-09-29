namespace AnimalBattle.Ai.Dsl;

/// <summary>
///     Phase ごとの足場間跳躍・落下・着地硬直など、非戦闘機動（Locomotion）の検証済みパラメータを保持する純粋データ構造体です。
/// </summary>
/// <param name="JumpForce">跳躍の初速絶対値（px/s）。</param>
/// <param name="Gravity">適用される重力加速度（px/s^2）。</param>
/// <param name="MaxFallSpeed">最大落下終端速度（px/s）。</param>
/// <param name="MaxAscentDuration">跳躍上昇の最大許容時間（秒）。タイムアウトによる落下強制移行。</param>
/// <param name="LandingRecoveryDuration">着地後の硬直待機時間（秒）。</param>
/// <param name="TakeoffDistance">足場間跳躍への移行を開始するプレイヤーとの水平距離閾値（px）。</param>
public readonly record struct PhaseLocomotionSettings(
    float JumpForce,
    float Gravity = 980.0f,
    float MaxFallSpeed = 850.0f,
    float MaxAscentDuration = 0.80f,
    float LandingRecoveryDuration = 0.10f,
    float TakeoffDistance = 50.0f);