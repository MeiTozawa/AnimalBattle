using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の固定 BossState と AttackId の対応を提供します。</summary>
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

    /// <summary>攻撃 ID に対応する固定 Telegraph BossState を取得します。</summary>
    public static BossState GetTelegraphState(AttackId attackId)
    {
        return attackId.Value switch
        {
            Boss2AttackIds.HeavySlashValue => HeavySlashTelegraph,
            Boss2AttackIds.DoubleCleaveValue => DoubleCleaveTelegraph,
            Boss2AttackIds.GroundStompValue => GroundStompTelegraph,
            Boss2AttackIds.LeapSmashValue => LeapSmashTelegraph,
            Boss2AttackIds.FrenzyChargeValue => FrenzyChargeTelegraph,
            Boss2AttackIds.EarthquakeRoarValue => EarthquakeRoarTelegraph,
            _ => BossState.Unknown
        };
    }

    /// <summary>攻撃 ID に対応する固定 Execute BossState を取得します。</summary>
    public static BossState GetExecuteState(AttackId attackId)
    {
        return attackId.Value switch
        {
            Boss2AttackIds.HeavySlashValue => HeavySlashExecute,
            Boss2AttackIds.DoubleCleaveValue => DoubleCleaveExecute,
            Boss2AttackIds.GroundStompValue => GroundStompExecute,
            Boss2AttackIds.LeapSmashValue => LeapSmashAscent,
            Boss2AttackIds.FrenzyChargeValue => FrenzyChargeExecute,
            Boss2AttackIds.EarthquakeRoarValue => EarthquakeRoarExecute,
            _ => BossState.Unknown
        };
    }

    /// <summary>旧 API 名から固定 Telegraph BossState を取得します。</summary>
    [Obsolete("StateId から BossState への移行用です。")]
    public static BossState GetTelegraphStateId(AttackId attackId) => GetTelegraphState(attackId);

    /// <summary>旧 API 名から固定 Execute BossState を取得します。</summary>
    [Obsolete("StateId から BossState への移行用です。")]
    public static BossState GetExecuteStateId(AttackId attackId) => GetExecuteState(attackId);
}

/// <summary>Boss2 固有の安定した攻撃識別子です。</summary>
public static class Boss2AttackIds
{
    /// <summary>重劈斬撃の数値 ID です。</summary>
    public const int HeavySlashValue = 201;
    /// <summary>二連重斬の数値 ID です。</summary>
    public const int DoubleCleaveValue = 202;
    /// <summary>撼地踏撃の数値 ID です。</summary>
    public const int GroundStompValue = 203;
    /// <summary>泰山圧頂の数値 ID です。</summary>
    public const int LeapSmashValue = 204;
    /// <summary>狂暴突進の数値 ID です。</summary>
    public const int FrenzyChargeValue = 205;
    /// <summary>滅地狂吼の数値 ID です。</summary>
    public const int EarthquakeRoarValue = 206;

    /// <summary>重劈斬撃です。</summary>
    public static readonly AttackId HeavySlash = new(HeavySlashValue);
    /// <summary>二連重斬です。</summary>
    public static readonly AttackId DoubleCleave = new(DoubleCleaveValue);
    /// <summary>撼地踏撃です。</summary>
    public static readonly AttackId GroundStomp = new(GroundStompValue);
    /// <summary>泰山圧頂です。</summary>
    public static readonly AttackId LeapSmash = new(LeapSmashValue);
    /// <summary>狂暴突進です。</summary>
    public static readonly AttackId FrenzyCharge = new(FrenzyChargeValue);
    /// <summary>滅地狂吼です。</summary>
    public static readonly AttackId EarthquakeRoar = new(EarthquakeRoarValue);
}

