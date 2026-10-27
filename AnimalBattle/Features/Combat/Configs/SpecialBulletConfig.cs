namespace AnimalBattle.Configs;

/// <summary>
/// 特殊弾変調パラメータ設定クラス。
/// </summary>
public sealed class SpecialBulletConfig
{
    /// <summary>青弾および黒弾の法線正弦波揺動振幅</summary>
    public float CyanWaveAmplitude { get; set; }

    /// <summary>青弾および黒弾の法線正弦波揺動周波数</summary>
    public float CyanWaveFrequency { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動振幅</summary>
    public float MagentaPulseAmplitude { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動周波数</summary>
    public float MagentaPulseFrequency { get; set; }

    /// <summary>黄弾公転衛星の回転角速度（ラジアン/秒）</summary>
    public float YellowOrbitSpeed { get; set; }

    /// <summary>Boss 被弾反撃黒弾の移動速度（px/s）</summary>
    public float CounterBulletSpeed { get; set; }

    /// <summary>Boss 被弾反撃黒弾の判定半径（px）</summary>
    public float CounterBulletRadius { get; set; }

    /// <summary>Phase 1 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase1 { get; set; }

    /// <summary>Phase 2 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase2 { get; set; }

    /// <summary>Phase 3 時の反撃黒弾発射数</summary>
    public int CounterBulletCountPhase3 { get; set; }

    /// <summary>Boss 被弾反撃黒弾が近接攻撃で破壊不能かどうかのフラグ</summary>
    public bool CounterBulletIsIndestructible { get; set; }
}
