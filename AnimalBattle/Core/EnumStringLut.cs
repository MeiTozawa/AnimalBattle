using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Core;

/// <summary>
///     グローバルな列挙体文字列ルックアップテーブル（LUT）。
///     ホットパスにおける <see cref="Enum.ToString()" /> のリフレクション辞書検索とヒープ文字列割り当てを完全に排除します。
/// </summary>
public static class EnumStringLut {
    /// <summary>Boss 技能の識別文字列を取得します。</summary>
    public static string Get(BossSkill skill) => skill switch {
        BossSkill.Aimed3Shot => nameof(BossSkill.Aimed3Shot),
        BossSkill.RingBurst => nameof(BossSkill.RingBurst),
        BossSkill.DoubleSpiral => nameof(BossSkill.DoubleSpiral),
        BossSkill.RainDrop => nameof(BossSkill.RainDrop),
        BossSkill.CrossChaos => nameof(BossSkill.CrossChaos),
        BossSkill.FastSniper => nameof(BossSkill.FastSniper),
        BossSkill.DiveRam => nameof(BossSkill.DiveRam),
        BossSkill.HeavySlash => nameof(BossSkill.HeavySlash),
        BossSkill.DoubleCleave => nameof(BossSkill.DoubleCleave),
        BossSkill.GroundStomp => nameof(BossSkill.GroundStomp),
        BossSkill.LeapSmash => nameof(BossSkill.LeapSmash),
        BossSkill.FrenzyCharge => nameof(BossSkill.FrenzyCharge),
        BossSkill.EarthquakeRoar => nameof(BossSkill.EarthquakeRoar),
        _ => "None"
    };

    /// <summary>Boss 1 状態の識別文字列を取得します。</summary>
    public static string Get(Boss1State state) => state switch {
        Boss1State.Hover => nameof(Boss1State.Hover),
        Boss1State.AttackSelection => nameof(Boss1State.AttackSelection),
        Boss1State.Telegraph => nameof(Boss1State.Telegraph),
        Boss1State.Execute => nameof(Boss1State.Execute),
        Boss1State.Recovery => nameof(Boss1State.Recovery),
        Boss1State.PhaseTransition => nameof(Boss1State.PhaseTransition),
        Boss1State.Dead => nameof(Boss1State.Dead),
        Boss1State.None => nameof(Boss1State.None),
        _ => "Unknown"
    };

    /// <summary>Boss 2 状態の識別文字列を取得します。</summary>
    public static string Get(Boss2State state) => state switch {
        Boss2State.Idle => nameof(Boss2State.Idle),
        Boss2State.PatrolChase => nameof(Boss2State.PatrolChase),
        Boss2State.MoveToTakeoff => nameof(Boss2State.MoveToTakeoff),
        Boss2State.PlatformTraverse => nameof(Boss2State.PlatformTraverse),
        Boss2State.Telegraph => nameof(Boss2State.Telegraph),
        Boss2State.Execute => nameof(Boss2State.Execute),
        Boss2State.WallCrashStun => nameof(Boss2State.WallCrashStun),
        Boss2State.Recovery => nameof(Boss2State.Recovery),
        Boss2State.PhaseTransition => nameof(Boss2State.PhaseTransition),
        Boss2State.Dead => nameof(Boss2State.Dead),
        Boss2State.None => nameof(Boss2State.None),
        _ => "Unknown"
    };

    /// <summary>戦闘 Phase の識別文字列を取得します。</summary>
    public static string Get(Phase phase) => phase switch {
        Phase.Phase1 => "Phase 1",
        Phase.Phase2 => "Phase 2",
        Phase.Phase3 => "Phase 3",
        _ => "Unknown"
    };

    /// <summary>技能に応じた予兆 HUD 状態バッジ文字列を取得します。</summary>
    public static string GetTelegraphBadge(BossSkill skill) => skill switch {
        BossSkill.Aimed3Shot => "[STATE: TELEGRAPHING (WIND-UP: Aimed3Shot)]",
        BossSkill.RingBurst => "[STATE: TELEGRAPHING (WIND-UP: RingBurst)]",
        BossSkill.DoubleSpiral => "[STATE: TELEGRAPHING (WIND-UP: DoubleSpiral)]",
        BossSkill.RainDrop => "[STATE: TELEGRAPHING (WIND-UP: RainDrop)]",
        BossSkill.CrossChaos => "[STATE: TELEGRAPHING (WIND-UP: CrossChaos)]",
        BossSkill.FastSniper => "[STATE: TELEGRAPHING (WIND-UP: FastSniper)]",
        BossSkill.DiveRam => "[STATE: TELEGRAPHING (WIND-UP: DiveRam)]",
        BossSkill.HeavySlash => "[STATE: TELEGRAPHING (WIND-UP: HeavySlash)]",
        BossSkill.DoubleCleave => "[STATE: TELEGRAPHING (WIND-UP: DoubleCleave)]",
        BossSkill.GroundStomp => "[STATE: TELEGRAPHING (WIND-UP: GroundStomp)]",
        BossSkill.LeapSmash => "[STATE: TELEGRAPHING (WIND-UP: LeapSmash)]",
        BossSkill.FrenzyCharge => "[STATE: TELEGRAPHING (WIND-UP: FrenzyCharge)]",
        BossSkill.EarthquakeRoar => "[STATE: TELEGRAPHING (WIND-UP: EarthquakeRoar)]",
        _ => "[STATE: TELEGRAPHING (WIND-UP: None)]"
    };

    /// <summary>技能に応じた実行 HUD 状態バッジ文字列を取得します。</summary>
    public static string GetExecuteBadge(BossSkill skill) => skill switch {
        BossSkill.Aimed3Shot => "[STATE: EXECUTING (Aimed3Shot)]",
        BossSkill.RingBurst => "[STATE: EXECUTING (RingBurst)]",
        BossSkill.DoubleSpiral => "[STATE: EXECUTING (DoubleSpiral)]",
        BossSkill.RainDrop => "[STATE: EXECUTING (RainDrop)]",
        BossSkill.CrossChaos => "[STATE: EXECUTING (CrossChaos)]",
        BossSkill.FastSniper => "[STATE: EXECUTING (FastSniper)]",
        BossSkill.DiveRam => "[STATE: EXECUTING (DiveRam)]",
        BossSkill.HeavySlash => "[STATE: EXECUTING (HeavySlash)]",
        BossSkill.DoubleCleave => "[STATE: EXECUTING (DoubleCleave)]",
        BossSkill.GroundStomp => "[STATE: EXECUTING (GroundStomp)]",
        BossSkill.LeapSmash => "[STATE: EXECUTING (LeapSmash)]",
        BossSkill.FrenzyCharge => "[STATE: EXECUTING (FrenzyCharge)]",
        BossSkill.EarthquakeRoar => "[STATE: EXECUTING (EarthquakeRoar)]",
        _ => "[STATE: EXECUTING (None)]"
    };

    /// <summary>Boss 2 の追跡技能バッジ文字列を取得します。</summary>
    public static string GetBoss2ChaseBadge(BossSkill skill) => skill switch {
        BossSkill.HeavySlash => "[STATE: CHASE (NEXT: HeavySlash)]",
        BossSkill.DoubleCleave => "[STATE: CHASE (NEXT: DoubleCleave)]",
        BossSkill.GroundStomp => "[STATE: CHASE (NEXT: GroundStomp)]",
        BossSkill.LeapSmash => "[STATE: CHASE (NEXT: LeapSmash)]",
        BossSkill.FrenzyCharge => "[STATE: CHASE (NEXT: FrenzyCharge)]",
        BossSkill.EarthquakeRoar => "[STATE: CHASE (NEXT: EarthquakeRoar)]",
        _ => "[STATE: CHASE (NEXT: None)]"
    };

    public static string GetBoss1TelegraphBadge(BossSkill skill) => GetTelegraphBadge(skill);
    public static string GetBoss1ExecuteBadge(BossSkill skill) => GetExecuteBadge(skill);
    public static string GetBoss2TelegraphBadge(BossSkill skill) => GetTelegraphBadge(skill);
    public static string GetBoss2ExecuteBadge(BossSkill skill) => GetExecuteBadge(skill);
}