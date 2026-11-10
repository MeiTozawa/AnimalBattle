namespace AnimalBattle.Player.Components;

/// <summary>
///     自機の特殊能力（必殺技）発動用エネルギーゲージコンポーネント。
///     被ダメージ・与ダメージによって蓄積され、スロット単位で消費されます。
/// </summary>
public struct EnergyGauge
{
    /// <summary>現在蓄積されているエネルギー量（0.0f ～ MaxEnergy）</summary>
    public float CurrentEnergy;

    /// <summary>最大保持可能エネルギー総量</summary>
    public float MaxEnergy;

    /// <summary>1スロット発動に必要なエネルギー容量</summary>
    public float SegmentCapacity;

    /// <summary>連続発動時の誤爆防止・内部インターバルタイマー（秒）</summary>
    public float CooldownTimer;

    /// <summary>現在使用可能なスロット数を取得します</summary>
    public readonly int ReadySegments => (int)(CurrentEnergy / MathF.Max(1.0f, SegmentCapacity));

    /// <summary>特殊能力が発動可能かどうかを判定します</summary>
    public readonly bool CanUseAbility => ReadySegments >= 1 && CooldownTimer <= 0.0f;
}
