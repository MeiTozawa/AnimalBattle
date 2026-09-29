using System;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Core;

/// <summary>
/// 全局枚举字符串预烘焙查找表（LUT）。
/// 彻底废黜热路径中的 Enum.ToString()、反射字典查找与堆字符串分配。
/// </summary>
public static class EnumStringLut
{
    private static readonly string[] Boss1SkillNames =
    [
        "None",
        "Aimed3Shot",   // 101 -> index 1
        "RingBurst",    // 102 -> index 2
        "DoubleSpiral", // 103 -> index 3
        "RainDrop",     // 104 -> index 4
        "CrossChaos",   // 105 -> index 5
        "FastSniper",   // 106 -> index 6
        "DiveRam"       // 107 -> index 7
    ];

    private static readonly string[] Boss2SkillNames =
    [
        "None",
        "HeavySlash",    // 201 -> index 1
        "DoubleCleave",  // 202 -> index 2
        "GroundStomp",   // 203 -> index 3
        "LeapSmash",     // 204 -> index 4
        "FrenzyCharge",  // 205 -> index 5
        "EarthquakeRoar" // 206 -> index 6
    ];

    private static readonly string[] Boss1StateNames =
    [
        "None",
        "Hover",
        "AttackSelection",
        "Telegraph",
        "Execute",
        "Recovery",
        "PhaseTransition",
        "Dead"
    ];

    private static readonly string[] Boss2StateNames =
    [
        "None",
        "Idle",
        "PatrolChase",
        "MoveToTakeoff",
        "PlatformTraverse",
        "Telegraph",
        "Execute",
        "WallCrashStun",
        "Recovery",
        "PhaseTransition",
        "Dead"
    ];

    private static readonly string[] PhaseNames =
    [
        "Unknown",
        "Phase 1",
        "Phase 2",
        "Phase 3",
        "Phase Transition",
        "Dead"
    ];

    // 预烘焙 HUD 完整徽章文本，避免任何帧内插值
    private static readonly string[] Boss1TelegraphBadges =
    [
        "[STATE: TELEGRAPHING (WIND-UP: None)]",
        "[STATE: TELEGRAPHING (WIND-UP: Aimed3Shot)]",
        "[STATE: TELEGRAPHING (WIND-UP: RingBurst)]",
        "[STATE: TELEGRAPHING (WIND-UP: DoubleSpiral)]",
        "[STATE: TELEGRAPHING (WIND-UP: RainDrop)]",
        "[STATE: TELEGRAPHING (WIND-UP: CrossChaos)]",
        "[STATE: TELEGRAPHING (WIND-UP: FastSniper)]",
        "[STATE: TELEGRAPHING (WIND-UP: DiveRam)]"
    ];

    private static readonly string[] Boss1ExecuteBadges =
    [
        "[STATE: EXECUTING (None)]",
        "[STATE: EXECUTING (Aimed3Shot)]",
        "[STATE: EXECUTING (RingBurst)]",
        "[STATE: EXECUTING (DoubleSpiral)]",
        "[STATE: EXECUTING (RainDrop)]",
        "[STATE: EXECUTING (CrossChaos)]",
        "[STATE: EXECUTING (FastSniper)]",
        "[STATE: EXECUTING (DiveRam)]"
    ];

    private static readonly string[] Boss2TelegraphBadges =
    [
        "[STATE: TELEGRAPHING (WIND-UP: None)]",
        "[STATE: TELEGRAPHING (WIND-UP: HeavySlash)]",
        "[STATE: TELEGRAPHING (WIND-UP: DoubleCleave)]",
        "[STATE: TELEGRAPHING (WIND-UP: GroundStomp)]",
        "[STATE: TELEGRAPHING (WIND-UP: LeapSmash)]",
        "[STATE: TELEGRAPHING (WIND-UP: FrenzyCharge)]",
        "[STATE: TELEGRAPHING (WIND-UP: EarthquakeRoar)]"
    ];

    private static readonly string[] Boss2ExecuteBadges =
    [
        "[STATE: EXECUTING (None)]",
        "[STATE: EXECUTING (HeavySlash)]",
        "[STATE: EXECUTING (DoubleCleave)]",
        "[STATE: EXECUTING (GroundStomp)]",
        "[STATE: EXECUTING (LeapSmash)]",
        "[STATE: EXECUTING (FrenzyCharge)]",
        "[STATE: EXECUTING (EarthquakeRoar)]"
    ];

    private static readonly string[] Boss2ChaseBadges =
    [
        "[STATE: CHASE (NEXT: None)]",
        "[STATE: CHASE (NEXT: HeavySlash)]",
        "[STATE: CHASE (NEXT: DoubleCleave)]",
        "[STATE: CHASE (NEXT: GroundStomp)]",
        "[STATE: CHASE (NEXT: LeapSmash)]",
        "[STATE: CHASE (NEXT: FrenzyCharge)]",
        "[STATE: CHASE (NEXT: EarthquakeRoar)]"
    ];

    public static string Get(BossSkill skill)
    {
        ushort val = (ushort)skill;
        if (val >= 101 && val <= 107)
        {
            return Boss1SkillNames[val - 100];
        }
        if (val >= 201 && val <= 206)
        {
            return Boss2SkillNames[val - 200];
        }
        return "None";
    }

    public static string Get(Boss1State state)
    {
        int idx = (int)state;
        return (idx >= 0 && idx < Boss1StateNames.Length) ? Boss1StateNames[idx] : "Unknown";
    }

    public static string Get(Boss2State state)
    {
        int idx = (int)state;
        return (idx >= 0 && idx < Boss2StateNames.Length) ? Boss2StateNames[idx] : "Unknown";
    }

    public static string Get(Phase phase)
    {
        int idx = (int)phase;
        return (idx >= 0 && idx < PhaseNames.Length) ? PhaseNames[idx] : "Unknown";
    }

    public static string GetBoss1TelegraphBadge(BossSkill skill)
    {
        ushort val = (ushort)skill;
        int idx = (val >= 101 && val <= 107) ? val - 100 : 0;
        return Boss1TelegraphBadges[idx];
    }

    public static string GetBoss1ExecuteBadge(BossSkill skill)
    {
        ushort val = (ushort)skill;
        int idx = (val >= 101 && val <= 107) ? val - 100 : 0;
        return Boss1ExecuteBadges[idx];
    }

    public static string GetBoss2TelegraphBadge(BossSkill skill)
    {
        ushort val = (ushort)skill;
        int idx = (val >= 201 && val <= 206) ? val - 200 : 0;
        return Boss2TelegraphBadges[idx];
    }

    public static string GetBoss2ExecuteBadge(BossSkill skill)
    {
        ushort val = (ushort)skill;
        int idx = (val >= 201 && val <= 206) ? val - 200 : 0;
        return Boss2ExecuteBadges[idx];
    }

    public static string GetBoss2ChaseBadge(BossSkill skill)
    {
        ushort val = (ushort)skill;
        int idx = (val >= 201 && val <= 206) ? val - 200 : 0;
        return Boss2ChaseBadges[idx];
    }
}
