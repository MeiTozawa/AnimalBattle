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
    /// <summary>足場間機動（跳躍・落下・着地）の統合状態です。</summary>
    public const BossState PlatformTraverse = BossState.Boss2_PlatformTraverse;

    /// <summary>足場間跳躍の上昇状態です（PlatformTraverse の別名）。</summary>
    public const BossState PlatformJump = PlatformTraverse;
    /// <summary>足場間跳躍の落下状態です（PlatformTraverse の別名）。</summary>
    public const BossState PlatformFall = PlatformTraverse;
    /// <summary>着地後の回復状態です（PlatformTraverse の別名）。</summary>
    public const BossState LandingRecovery = PlatformTraverse;

    /// <summary>Boss2 の共通攻撃予兆状態です。</summary>
    public const BossState Telegraph = BossState.Boss2_Telegraph;
    /// <summary>Boss2 の共通攻撃実行状態です。</summary>
    public const BossState Execute = BossState.Boss2_Execute;

    /// <summary>HeavySlash の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState HeavySlashTelegraph = Telegraph;
    /// <summary>DoubleCleave の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState DoubleCleaveTelegraph = Telegraph;
    /// <summary>GroundStomp の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState GroundStompTelegraph = Telegraph;
    /// <summary>LeapSmash の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState LeapSmashTelegraph = Telegraph;
    /// <summary>FrenzyCharge の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState FrenzyChargeTelegraph = Telegraph;
    /// <summary>EarthquakeRoar の Telegraph 状態です（Telegraph の別名）。</summary>
    public const BossState EarthquakeRoarTelegraph = Telegraph;

    /// <summary>HeavySlash の Execute 状態です（Execute の別名）。</summary>
    public const BossState HeavySlashExecute = Execute;
    /// <summary>DoubleCleave の Execute 状態です（Execute の別名）。</summary>
    public const BossState DoubleCleaveExecute = Execute;
    /// <summary>GroundStomp の Execute 状態です（Execute の別名）。</summary>
    public const BossState GroundStompExecute = Execute;
    /// <summary>LeapSmash の Execute 状態です（Execute の別名）。</summary>
    public const BossState LeapSmashExecute = Execute;
    /// <summary>FrenzyCharge の Execute 状態です（Execute の別名）。</summary>
    public const BossState FrenzyChargeExecute = Execute;
    /// <summary>EarthquakeRoar の Execute 状態です（Execute の別名）。</summary>
    public const BossState EarthquakeRoarExecute = Execute;

    /// <summary>壁激突後の気絶状態です。</summary>
    public const BossState WallCrashStun = BossState.Boss2_WallCrashStun;
    /// <summary>攻撃後の回復状態です。</summary>
    public const BossState Recovery = BossState.Boss2_Recovery;
    /// <summary>Phase 切り替え状態です。</summary>
    public const BossState PhaseTransition = BossState.Boss2_PhaseTransition;
    /// <summary>死亡終端状態です。</summary>
    public const BossState Dead = BossState.Boss2_Dead;

    /// <summary>技能に対応する固定 Telegraph BossState を取得します（全技能共通で Telegraph を返します）。</summary>
    public static BossState GetTelegraphState(BossSkill skill)
    {
        return skill.IsValid() ? Telegraph : BossState.Unknown;
    }

    /// <summary>技能に対応する固定 Execute BossState を取得します（全技能共通で Execute を返します）。</summary>
    public static BossState GetExecuteState(BossSkill skill)
    {
        return skill.IsValid() ? Execute : BossState.Unknown;
    }
}




