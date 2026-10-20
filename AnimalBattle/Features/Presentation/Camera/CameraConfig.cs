using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
///     Trauma モデルおよび画面振動パラメータを保持するカメラ設定データモデル。
/// </summary>
public sealed class CameraConfig
{
    /// <summary>Trauma の 1 秒あたりの線形減衰速度</summary>
    public float TraumaDecaySpeed { get; set; }

    /// <summary>指向性衝撃インパルスの 1 秒あたりの減衰速度</summary>
    public float ImpulseDecaySpeed { get; set; }

    /// <summary>カメラ最大振動オフセット（ピクセル）</summary>
    public Vec2f MaxOffset { get; set; }

    /// <summary>X 軸ノイズ第 1 周波数</summary>
    public float NoiseFrequency1 { get; set; }

    /// <summary>X 軸ノイズ第 2 周波数</summary>
    public float NoiseFrequency2 { get; set; }

    /// <summary>Y 軸ノイズ第 1 周波数</summary>
    public float NoiseFrequency3 { get; set; }

    /// <summary>Y 軸ノイズ第 2 周波数</summary>
    public float NoiseFrequency4 { get; set; }

    /// <summary>アリーナ基準中心座標（ピクセル）</summary>
    public Vec2f ArenaCenter { get; set; }

    /// <summary>プレイヤー速度に基づくカメラ前瞻オフセット係数</summary>
    public float LookAheadFactor { get; set; }

    /// <summary>カメラ追従平滑化速度（ラープ係数）</summary>
    public float FollowSpeed { get; set; }

    /// <summary>デュアルターゲット構図におけるプレイヤーの注視点重み</summary>
    public float PlayerFocusWeight { get; set; }

    /// <summary>デュアルターゲット構図における Boss の注視点重み</summary>
    public float BossFocusWeight { get; set; }

    /// <summary>カメラ最大パン移動量（ピクセル）</summary>
    public Vec2f MaxPan { get; set; }

    /// <summary>技能画面振動強度から Trauma への変換スケール</summary>
    public float AbilityShakeTraumaScale { get; set; }

    /// <summary>プレイヤー通常被弾時の付与 Trauma 量</summary>
    public float PlayerHitTrauma { get; set; }

    /// <summary>プレイヤー重打撃被弾時の付与 Trauma 量</summary>
    public float PlayerHeavyHitTrauma { get; set; }

    /// <summary>Boss 易傷クリティカル被弾時の付与 Trauma 量</summary>
    public float BossVulnerableHitTrauma { get; set; }

    /// <summary>Boss 撃破勝利大爆発時の付与 Trauma 量</summary>
    public float VictoryExplosionTrauma { get; set; }

    /// <summary>ノックバック速度から指向性インパルスへの変換比率</summary>
    public float DirectionalImpulseRatio { get; set; }
}
