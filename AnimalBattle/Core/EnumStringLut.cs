using AnimalBattle.Abilities;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Core;

/// <summary>
///     グローバルな列挙体文字列ルックアップテーブル（LUT）。
///     ホットパスにおける <see cref="Enum.ToString()" /> のリフレクション辞書検索とヒープ文字列割り当てを完全に排除します。
/// </summary>
public static class EnumStringLut {
    /// <summary>技能識別文字列を取得します。</summary>
    public static string Get(AbilityId skill) => skill switch {
        AbilityId.Aimed3Shot => nameof(AbilityId.Aimed3Shot),
        AbilityId.RingBurst => nameof(AbilityId.RingBurst),
        AbilityId.DoubleSpiral => nameof(AbilityId.DoubleSpiral),
        AbilityId.RainDrop => nameof(AbilityId.RainDrop),
        AbilityId.CrossChaos => nameof(AbilityId.CrossChaos),
        AbilityId.FastSniper => nameof(AbilityId.FastSniper),
        AbilityId.DiveRam => nameof(AbilityId.DiveRam),
        AbilityId.HeavySlash => nameof(AbilityId.HeavySlash),
        AbilityId.DoubleCleave => nameof(AbilityId.DoubleCleave),
        AbilityId.GroundStomp => nameof(AbilityId.GroundStomp),
        AbilityId.LeapSmash => nameof(AbilityId.LeapSmash),
        AbilityId.FrenzyCharge => nameof(AbilityId.FrenzyCharge),
        AbilityId.EarthquakeRoar => nameof(AbilityId.EarthquakeRoar),
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
    public static string GetTelegraphBadge(AbilityId skill) => skill switch {
        AbilityId.Aimed3Shot => "[STATE: TELEGRAPHING (WIND-UP: Aimed3Shot)]",
        AbilityId.RingBurst => "[STATE: TELEGRAPHING (WIND-UP: RingBurst)]",
        AbilityId.DoubleSpiral => "[STATE: TELEGRAPHING (WIND-UP: DoubleSpiral)]",
        AbilityId.RainDrop => "[STATE: TELEGRAPHING (WIND-UP: RainDrop)]",
        AbilityId.CrossChaos => "[STATE: TELEGRAPHING (WIND-UP: CrossChaos)]",
        AbilityId.FastSniper => "[STATE: TELEGRAPHING (WIND-UP: FastSniper)]",
        AbilityId.DiveRam => "[STATE: TELEGRAPHING (WIND-UP: DiveRam)]",
        AbilityId.HeavySlash => "[STATE: TELEGRAPHING (WIND-UP: HeavySlash)]",
        AbilityId.DoubleCleave => "[STATE: TELEGRAPHING (WIND-UP: DoubleCleave)]",
        AbilityId.GroundStomp => "[STATE: TELEGRAPHING (WIND-UP: GroundStomp)]",
        AbilityId.LeapSmash => "[STATE: TELEGRAPHING (WIND-UP: LeapSmash)]",
        AbilityId.FrenzyCharge => "[STATE: TELEGRAPHING (WIND-UP: FrenzyCharge)]",
        AbilityId.EarthquakeRoar => "[STATE: TELEGRAPHING (WIND-UP: EarthquakeRoar)]",
        _ => "[STATE: TELEGRAPHING (WIND-UP: None)]"
    };

    /// <summary>技能に応じた実行 HUD 状態バッジ文字列を取得します。</summary>
    public static string GetExecuteBadge(AbilityId skill) => skill switch {
        AbilityId.Aimed3Shot => "[STATE: EXECUTING (Aimed3Shot)]",
        AbilityId.RingBurst => "[STATE: EXECUTING (RingBurst)]",
        AbilityId.DoubleSpiral => "[STATE: EXECUTING (DoubleSpiral)]",
        AbilityId.RainDrop => "[STATE: EXECUTING (RainDrop)]",
        AbilityId.CrossChaos => "[STATE: EXECUTING (CrossChaos)]",
        AbilityId.FastSniper => "[STATE: EXECUTING (FastSniper)]",
        AbilityId.DiveRam => "[STATE: EXECUTING (DiveRam)]",
        AbilityId.HeavySlash => "[STATE: EXECUTING (HeavySlash)]",
        AbilityId.DoubleCleave => "[STATE: EXECUTING (DoubleCleave)]",
        AbilityId.GroundStomp => "[STATE: EXECUTING (GroundStomp)]",
        AbilityId.LeapSmash => "[STATE: EXECUTING (LeapSmash)]",
        AbilityId.FrenzyCharge => "[STATE: EXECUTING (FrenzyCharge)]",
        AbilityId.EarthquakeRoar => "[STATE: EXECUTING (EarthquakeRoar)]",
        _ => "[STATE: EXECUTING (None)]"
    };

    /// <summary>Boss 2 の追跡技能バッジ文字列を取得します。</summary>
    public static string GetBoss2ChaseBadge(AbilityId skill) => skill switch {
        AbilityId.HeavySlash => "[STATE: CHASE (NEXT: HeavySlash)]",
        AbilityId.DoubleCleave => "[STATE: CHASE (NEXT: DoubleCleave)]",
        AbilityId.GroundStomp => "[STATE: CHASE (NEXT: GroundStomp)]",
        AbilityId.LeapSmash => "[STATE: CHASE (NEXT: LeapSmash)]",
        AbilityId.FrenzyCharge => "[STATE: CHASE (NEXT: FrenzyCharge)]",
        AbilityId.EarthquakeRoar => "[STATE: CHASE (NEXT: EarthquakeRoar)]",
        _ => "[STATE: CHASE (NEXT: None)]"
    };

    public static string GetBoss1TelegraphBadge(AbilityId skill) => GetTelegraphBadge(skill);
    public static string GetBoss1ExecuteBadge(AbilityId skill) => GetExecuteBadge(skill);
    public static string GetBoss2TelegraphBadge(AbilityId skill) => GetTelegraphBadge(skill);
    public static string GetBoss2ExecuteBadge(AbilityId skill) => GetExecuteBadge(skill);
}