using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の固定 BossState と BossSkill の対応を提供します。</summary>
public static class Boss2StateIds

{
    /// <summary>待機状態です。</summary>
    public const BossState Idle = BossState.Boss2_Idle;
    /// <summary>同一足場の追跡状態です。</summary>
    public const BossState PatrolChase = BossState.Boss2_PatrolChase;
    /// <summary>跳躍開始位置への移動状態です。</summary>
    public const BossState MoveToTakeoff = BossState.Boss2_MoveToTakeoff;
    /// <summary>足場間跳躍の上昇状態です。</summary>
    public const BossState PlatformJump = BossState.Boss2_PlatformJump;
    /// <summary>足場間跳躍の落下状態です。</summary>
    public const BossState PlatformFall = BossState.Boss2_PlatformFall;
    /// <summary>着地後の回復状態です。</summary>
    public const BossState LandingRecovery = BossState.Boss2_LandingRecovery;
    /// <summary>HeavySlash の Telegraph 状態です。</summary>
    public const BossState HeavySlashTelegraph = BossState.Boss2_HeavySlashTelegraph;
    /// <summary>DoubleCleave の Telegraph 状態です。</summary>
    public const BossState DoubleCleaveTelegraph = BossState.Boss2_DoubleCleaveTelegraph;
    /// <summary>GroundStomp の Telegraph 状態です。</summary>
    public const BossState GroundStompTelegraph = BossState.Boss2_GroundStompTelegraph;
    /// <summary>LeapSmash の Telegraph 状態です。</summary>
    public const BossState LeapSmashTelegraph = BossState.Boss2_LeapSmashTelegraph;
    /// <summary>FrenzyCharge の Telegraph 状態です。</summary>
    public const BossState FrenzyChargeTelegraph = BossState.Boss2_FrenzyChargeTelegraph;
    /// <summary>EarthquakeRoar の Telegraph 状態です。</summary>
    public const BossState EarthquakeRoarTelegraph = BossState.Boss2_EarthquakeRoarTelegraph;
    /// <summary>HeavySlash の Execute 状態です。</summary>
    public const BossState HeavySlashExecute = BossState.Boss2_HeavySlashExecute;
    /// <summary>DoubleCleave の Execute 状態です。</summary>
    public const BossState DoubleCleaveExecute = BossState.Boss2_DoubleCleaveExecute;
    /// <summary>GroundStomp の Execute 状態です。</summary>
    public const BossState GroundStompExecute = BossState.Boss2_GroundStompExecute;
    /// <summary>LeapSmash の上昇 Execute 状態です。</summary>
    public const BossState LeapSmashAscent = BossState.Boss2_LeapSmashAscent;
    /// <summary>LeapSmash の落下 Execute 状態です。</summary>
    public const BossState LeapSmashDescent = BossState.Boss2_LeapSmashDescent;
    /// <summary>FrenzyCharge の Execute 状態です。</summary>
    public const BossState FrenzyChargeExecute = BossState.Boss2_FrenzyChargeExecute;
    /// <summary>EarthquakeRoar の Execute 状態です。</summary>
    public const BossState EarthquakeRoarExecute = BossState.Boss2_EarthquakeRoarExecute;
    /// <summary>壁激突後の気絶状態です。</summary>
    public const BossState WallCrashStun = BossState.Boss2_WallCrashStun;
    /// <summary>攻撃後の回復状態です。</summary>
    public const BossState Recovery = BossState.Boss2_Recovery;
    /// <summary>Phase 切り替え状態です。</summary>
    public const BossState PhaseTransition = BossState.Boss2_PhaseTransition;
    /// <summary>死亡終端状態です。</summary>
    public const BossState Dead = BossState.Boss2_Dead;

    /// <summary>技能に対応する固定 Telegraph BossState を取得します。</summary>
    public static BossState GetTelegraphState(BossSkill skill)
    {
        return skill switch
        {
            BossSkill.HeavySlash => HeavySlashTelegraph,
            BossSkill.DoubleCleave => DoubleCleaveTelegraph,
            BossSkill.GroundStomp => GroundStompTelegraph,
            BossSkill.LeapSmash => LeapSmashTelegraph,
            BossSkill.FrenzyCharge => FrenzyChargeTelegraph,
            BossSkill.EarthquakeRoar => EarthquakeRoarTelegraph,
            _ => BossState.Unknown
        };
    }

    /// <summary>技能に対応する固定 Execute BossState を取得します。</summary>
    public static BossState GetExecuteState(BossSkill skill)
    {
        return skill switch
        {
            BossSkill.HeavySlash => HeavySlashExecute,
            BossSkill.DoubleCleave => DoubleCleaveExecute,
            BossSkill.GroundStomp => GroundStompExecute,
            BossSkill.LeapSmash => LeapSmashAscent,
            BossSkill.FrenzyCharge => FrenzyChargeExecute,
            BossSkill.EarthquakeRoar => EarthquakeRoarExecute,
            _ => BossState.Unknown
        };
    }
}




