namespace AnimalBattle.Abilities;

/// <summary>
///     ゲーム内のすべての戦闘技能および能力を表す型安全な統一識別子です。
/// </summary>
public enum AbilityId : ushort {
    /// <summary>無効または未指定の技能です。</summary>
    None = 0,

    // プレイヤー技能 (1..99)
    /// <summary>自機射撃。</summary>
    PlayerShoot = 1,

    /// <summary>自機ダッシュ・回避。</summary>
    PlayerDash = 2,

    // Boss 1: Ancient Cat Overlord 技能 (101..107)
    /// <summary>Boss 1: 3方向狙撃弾（Aimed3Shot）</summary>
    Aimed3Shot = 101,

    /// <summary>Boss 1: 環状弾幕バースト（RingBurst）</summary>
    RingBurst = 102,

    /// <summary>Boss 1: 二重螺旋弾幕（DoubleSpiral）</summary>
    DoubleSpiral = 103,

    /// <summary>Boss 1: 天空雨滴弾（RainDrop）</summary>
    RainDrop = 104,

    /// <summary>Boss 1: 十字交差弾（CrossChaos）</summary>
    CrossChaos = 105,

    /// <summary>Boss 1: 高速スナイプ（FastSniper）</summary>
    FastSniper = 106,

    /// <summary>Boss 1: 俯衝突進（DiveRam）</summary>
    DiveRam = 107,

    // Boss 2: Ground Crusher 技能 (201..206)
    /// <summary>Boss 2: 近接重劈（HeavySlash）</summary>
    HeavySlash = 201,

    /// <summary>Boss 2: 二連撃（DoubleCleave）</summary>
    DoubleCleave = 202,

    /// <summary>Boss 2: 撼地踏撃（GroundStomp）</summary>
    GroundStomp = 203,

    /// <summary>Boss 2: 泰山圧頂（LeapSmash）</summary>
    LeapSmash = 204,

    /// <summary>Boss 2: 狂暴突進（FrenzyCharge）</summary>
    FrenzyCharge = 205,

    /// <summary>Boss 2: 滅地狂吼（EarthquakeRoar）</summary>
    EarthquakeRoar = 206
}

/// <summary>
///     <see cref="AbilityId" /> の拡張メソッドを提供します。
/// </summary>
public static class AbilityIdExtensions {
    /// <summary>能力が有効かどうかを判定します。</summary>
    public static bool IsValid(this AbilityId id) => id != AbilityId.None;

    /// <summary>能力の整数値を取得します。</summary>
    public static int AsInt(this AbilityId id) => (int)id;
}
