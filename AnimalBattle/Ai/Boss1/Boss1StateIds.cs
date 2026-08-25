using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の State_Id と Attack_Id を集中管理します。</summary>
public static class Boss1StateIds
{
    /// <summary>Boss1 の Hover 移動状態です。</summary>
    public static readonly StateId Hover = new(101);
    /// <summary>Boss1 の攻撃選択状態です。</summary>
    public static readonly StateId AttackSelection = new(102);
    /// <summary>Boss1 共通 Telegraph 状態です。</summary>
    public static readonly StateId Telegraph = new(103);
    /// <summary>Aimed3Shot の Execute 状態です。</summary>
    public static readonly StateId Aimed3ShotExecute = new(104);
    /// <summary>RingBurst の Execute 状態です。</summary>
    public static readonly StateId RingBurstExecute = new(105);
    /// <summary>DoubleSpiral の Execute 状態です。</summary>
    public static readonly StateId DoubleSpiralExecute = new(106);
    /// <summary>RainDrop の Execute 状態です。</summary>
    public static readonly StateId RainDropExecute = new(107);
    /// <summary>CrossChaos の Execute 状態です。</summary>
    public static readonly StateId CrossChaosExecute = new(108);
    /// <summary>FastSniper の Execute 状態です。</summary>
    public static readonly StateId FastSniperExecute = new(109);
    /// <summary>DiveRam の Execute 状態です。</summary>
    public static readonly StateId DiveRamExecute = new(110);
    /// <summary>Boss1 共通 Recovery 状態です。</summary>
    public static readonly StateId Recovery = new(111);
    /// <summary>Boss1 の PhaseTransition 状態です。</summary>
    public static readonly StateId PhaseTransition = new(112);
    /// <summary>Boss1 の Dead 状態です。</summary>
    public static readonly StateId Dead = new(113);

    /// <summary>指定攻撃の Execute State_Id を取得します。</summary>
    public static StateId GetExecuteStateId(AttackId attackId)
    {
        return attackId.Value switch
        {
            1 => Aimed3ShotExecute,
            2 => RingBurstExecute,
            3 => DoubleSpiralExecute,
            4 => RainDropExecute,
            5 => CrossChaosExecute,
            6 => FastSniperExecute,
            7 => DiveRamExecute,
            _ => StateId.Unknown
        };
    }
}

/// <summary>Boss1 の安定した攻撃識別子です。</summary>
public static class Boss1AttackIds
{
    /// <summary>自機狙い三方向弾です。</summary>
    public static readonly AttackId Aimed3Shot = new(1);
    /// <summary>全方向リング弾です。</summary>
    public static readonly AttackId RingBurst = new(2);
    /// <summary>双螺旋弾です。</summary>
    public static readonly AttackId DoubleSpiral = new(3);
    /// <summary>降雨弾です。</summary>
    public static readonly AttackId RainDrop = new(4);
    /// <summary>交差波弾です。</summary>
    public static readonly AttackId CrossChaos = new(5);
    /// <summary>高速狙撃弾です。</summary>
    public static readonly AttackId FastSniper = new(6);
    /// <summary>俯衝突進です。</summary>
    public static readonly AttackId DiveRam = new(7);
}
