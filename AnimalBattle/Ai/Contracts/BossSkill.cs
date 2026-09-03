namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// Boss の技能・招式を表す強タイプ列挙体です。
/// </summary>
public enum BossSkill : ushort
{
    /// <summary>無効または未指定の技能です。</summary>
    None = 0,

    // Boss 1: Ancient Cat Overlord 技能 (101~107)
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

    // Boss 2: Ground Crusher 技能 (201~206)
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
/// <see cref="BossSkill"/> の拡張メソッドを提供します。
/// </summary>
public static class BossSkillExtensions
{
    /// <summary>
    /// 技能が有効かどうかを判定します。
    /// </summary>
    public static bool IsValid(this BossSkill skill) => skill != BossSkill.None;

    /// <summary>
    /// 技能の整数値を取得します。
    /// </summary>
    public static int AsInt(this BossSkill skill) => (int)skill;
}
