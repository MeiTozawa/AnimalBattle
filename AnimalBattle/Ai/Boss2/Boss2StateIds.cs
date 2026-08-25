using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の State_Id と Attack_Id を集中管理します。</summary>
public static class Boss2StateIds
{
    /// <summary>待機状態です。</summary>
    public static readonly StateId Idle = new(201);
    /// <summary>同一足場の追跡状態です。</summary>
    public static readonly StateId PatrolChase = new(202);
    /// <summary>跳躍開始位置への移動状態です。</summary>
    public static readonly StateId MoveToTakeoff = new(203);
    /// <summary>足場間跳躍の上昇状態です。</summary>
    public static readonly StateId PlatformJump = new(204);
    /// <summary>足場間跳躍の落下状態です。</summary>
    public static readonly StateId PlatformFall = new(205);
    /// <summary>着地後の回復状態です。</summary>
    public static readonly StateId LandingRecovery = new(206);
    /// <summary>HeavySlash の Telegraph 状態です。</summary>
    public static readonly StateId HeavySlashTelegraph = new(207);
    /// <summary>DoubleCleave の Telegraph 状態です。</summary>
    public static readonly StateId DoubleCleaveTelegraph = new(208);
    /// <summary>GroundStomp の Telegraph 状態です。</summary>
    public static readonly StateId GroundStompTelegraph = new(209);
    /// <summary>LeapSmash の Telegraph 状態です。</summary>
    public static readonly StateId LeapSmashTelegraph = new(210);
    /// <summary>FrenzyCharge の Telegraph 状態です。</summary>
    public static readonly StateId FrenzyChargeTelegraph = new(211);
    /// <summary>EarthquakeRoar の Telegraph 状態です。</summary>
    public static readonly StateId EarthquakeRoarTelegraph = new(212);
    /// <summary>HeavySlash の Execute 状態です。</summary>
    public static readonly StateId HeavySlashExecute = new(213);
    /// <summary>DoubleCleave の Execute 状態です。</summary>
    public static readonly StateId DoubleCleaveExecute = new(214);
    /// <summary>GroundStomp の Execute 状態です。</summary>
    public static readonly StateId GroundStompExecute = new(215);
    /// <summary>LeapSmash の上昇 Execute 状態です。</summary>
    public static readonly StateId LeapSmashAscent = new(216);
    /// <summary>LeapSmash の落下 Execute 状態です。</summary>
    public static readonly StateId LeapSmashDescent = new(217);
    /// <summary>FrenzyCharge の Execute 状態です。</summary>
    public static readonly StateId FrenzyChargeExecute = new(218);
    /// <summary>EarthquakeRoar の Execute 状態です。</summary>
    public static readonly StateId EarthquakeRoarExecute = new(219);
    /// <summary>壁激突後の気絶状態です。</summary>
    public static readonly StateId WallCrashStun = new(220);
    /// <summary>攻撃後の回復状態です。</summary>
    public static readonly StateId Recovery = new(221);
    /// <summary>Phase 切り替え状態です。</summary>
    public static readonly StateId PhaseTransition = new(222);
    /// <summary>死亡終端状態です。</summary>
    public static readonly StateId Dead = new(223);

    /// <summary>攻撃 ID に対応する Telegraph State_Id を取得します。</summary>
    public static StateId GetTelegraphStateId(AttackId attackId)
    {
        return attackId.Value switch
        {
            201 => HeavySlashTelegraph,
            202 => DoubleCleaveTelegraph,
            203 => GroundStompTelegraph,
            204 => LeapSmashTelegraph,
            205 => FrenzyChargeTelegraph,
            206 => EarthquakeRoarTelegraph,
            _ => StateId.Unknown
        };
    }

    /// <summary>攻撃 ID に対応する Execute State_Id を取得します。</summary>
    public static StateId GetExecuteStateId(AttackId attackId)
    {
        return attackId.Value switch
        {
            201 => HeavySlashExecute,
            202 => DoubleCleaveExecute,
            203 => GroundStompExecute,
            204 => LeapSmashAscent,
            205 => FrenzyChargeExecute,
            206 => EarthquakeRoarExecute,
            _ => StateId.Unknown
        };
    }
}

/// <summary>Boss2 固有の安定した攻撃識別子です。</summary>
public static class Boss2AttackIds
{
    /// <summary>重劈斬撃です。</summary>
    public static readonly AttackId HeavySlash = new(201);
    /// <summary>二連重斬です。</summary>
    public static readonly AttackId DoubleCleave = new(202);
    /// <summary>撼地踏撃です。</summary>
    public static readonly AttackId GroundStomp = new(203);
    /// <summary>泰山圧頂です。</summary>
    public static readonly AttackId LeapSmash = new(204);
    /// <summary>狂暴突進です。</summary>
    public static readonly AttackId FrenzyCharge = new(205);
    /// <summary>滅地狂吼です。</summary>
    public static readonly AttackId EarthquakeRoar = new(206);
}
