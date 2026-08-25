namespace AnimalBattle.Ai.Contracts;

/// <summary>
/// 実行時に使用する固定 Boss AI 状態を表します。
/// </summary>
/// <remarks>
/// 値の追加・削除・実行時登録は行わず、各値を具体的な State_Module に静的に対応させます。
/// </remarks>
public enum BossState
{
    /// <summary>安全な未指定状態です。</summary>
    Unknown = 0,

    /// <summary>Boss1 の浮遊移動状態です。</summary>
    Boss1_Hover,
    /// <summary>Boss1 の攻撃選択状態です。</summary>
    Boss1_AttackSelection,
    /// <summary>Boss1 の Telegraph 状態です。</summary>
    Boss1_Telegraph,
    /// <summary>Boss1 の Aimed3Shot Execute 状態です。</summary>
    Boss1_Aimed3ShotExecute,
    /// <summary>Boss1 の RingBurst Execute 状態です。</summary>
    Boss1_RingBurstExecute,
    /// <summary>Boss1 の DoubleSpiral Execute 状態です。</summary>
    Boss1_DoubleSpiralExecute,
    /// <summary>Boss1 の RainDrop Execute 状態です。</summary>
    Boss1_RainDropExecute,
    /// <summary>Boss1 の CrossChaos Execute 状態です。</summary>
    Boss1_CrossChaosExecute,
    /// <summary>Boss1 の FastSniper Execute 状態です。</summary>
    Boss1_FastSniperExecute,
    /// <summary>Boss1 の DiveRam Execute 状態です。</summary>
    Boss1_DiveRamExecute,
    /// <summary>Boss1 の Recovery 状態です。</summary>
    Boss1_Recovery,
    /// <summary>Boss1 の PhaseTransition 状態です。</summary>
    Boss1_PhaseTransition,
    /// <summary>Boss1 の Dead 状態です。</summary>
    Boss1_Dead,

    /// <summary>Boss2 の待機状態です。</summary>
    Boss2_Idle,
    /// <summary>Boss2 の足場追跡状態です。</summary>
    Boss2_PatrolChase,
    /// <summary>Boss2 の跳躍開始位置への移動状態です。</summary>
    Boss2_MoveToTakeoff,
    /// <summary>Boss2 の足場間跳躍上昇状態です。</summary>
    Boss2_PlatformJump,
    /// <summary>Boss2 の足場間跳躍落下状態です。</summary>
    Boss2_PlatformFall,
    /// <summary>Boss2 の着地回復状態です。</summary>
    Boss2_LandingRecovery,
    /// <summary>Boss2 HeavySlash の Telegraph 状態です。</summary>
    Boss2_HeavySlashTelegraph,
    /// <summary>Boss2 DoubleCleave の Telegraph 状態です。</summary>
    Boss2_DoubleCleaveTelegraph,
    /// <summary>Boss2 GroundStomp の Telegraph 状態です。</summary>
    Boss2_GroundStompTelegraph,
    /// <summary>Boss2 LeapSmash の Telegraph 状態です。</summary>
    Boss2_LeapSmashTelegraph,
    /// <summary>Boss2 FrenzyCharge の Telegraph 状態です。</summary>
    Boss2_FrenzyChargeTelegraph,
    /// <summary>Boss2 EarthquakeRoar の Telegraph 状態です。</summary>
    Boss2_EarthquakeRoarTelegraph,
    /// <summary>Boss2 HeavySlash Execute 状態です。</summary>
    Boss2_HeavySlashExecute,
    /// <summary>Boss2 DoubleCleave Execute 状態です。</summary>
    Boss2_DoubleCleaveExecute,
    /// <summary>Boss2 GroundStomp Execute 状態です。</summary>
    Boss2_GroundStompExecute,
    /// <summary>Boss2 LeapSmash 上昇 Execute 状態です。</summary>
    Boss2_LeapSmashAscent,
    /// <summary>Boss2 LeapSmash 落下 Execute 状態です。</summary>
    Boss2_LeapSmashDescent,
    /// <summary>Boss2 FrenzyCharge Execute 状態です。</summary>
    Boss2_FrenzyChargeExecute,
    /// <summary>Boss2 EarthquakeRoar Execute 状態です。</summary>
    Boss2_EarthquakeRoarExecute,
    /// <summary>Boss2 の壁激突気絶状態です。</summary>
    Boss2_WallCrashStun,
    /// <summary>Boss2 の Recovery 状態です。</summary>
    Boss2_Recovery,
    /// <summary>Boss2 の PhaseTransition 状態です。</summary>
    Boss2_PhaseTransition,
    /// <summary>Boss2 の Dead 状態です。</summary>
    Boss2_Dead
}

/// <summary>固定 BossState の判定を提供します。</summary>
public static class BossStateExtensions
{
    /// <summary>Unknown ではない固定状態かどうかを取得します。</summary>
    public static bool IsValid(this BossState state) => state != BossState.Unknown;
}
