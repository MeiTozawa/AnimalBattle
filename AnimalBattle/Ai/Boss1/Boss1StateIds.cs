using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の固定 BossState と AttackId の対応を提供します。</summary>
public static class Boss1StateIds
{
    /// <summary>Boss1 の Hover 移動状態です。</summary>
    public const BossState Hover = BossState.Boss1_Hover;
    /// <summary>Boss1 の攻撃選択状態です。</summary>
    public const BossState AttackSelection = BossState.Boss1_AttackSelection;
    /// <summary>Boss1 の Telegraph 状態です。</summary>
    public const BossState Telegraph = BossState.Boss1_Telegraph;
    /// <summary>Aimed3Shot の Execute 状態です。</summary>
    public const BossState Aimed3ShotExecute = BossState.Boss1_Aimed3ShotExecute;
    /// <summary>RingBurst の Execute 状態です。</summary>
    public const BossState RingBurstExecute = BossState.Boss1_RingBurstExecute;
    /// <summary>DoubleSpiral の Execute 状態です。</summary>
    public const BossState DoubleSpiralExecute = BossState.Boss1_DoubleSpiralExecute;
    /// <summary>RainDrop の Execute 状態です。</summary>
    public const BossState RainDropExecute = BossState.Boss1_RainDropExecute;
    /// <summary>CrossChaos の Execute 状態です。</summary>
    public const BossState CrossChaosExecute = BossState.Boss1_CrossChaosExecute;
    /// <summary>FastSniper の Execute 状態です。</summary>
    public const BossState FastSniperExecute = BossState.Boss1_FastSniperExecute;
    /// <summary>DiveRam の Execute 状態です。</summary>
    public const BossState DiveRamExecute = BossState.Boss1_DiveRamExecute;
    /// <summary>Boss1 の Recovery 状態です。</summary>
    public const BossState Recovery = BossState.Boss1_Recovery;
    /// <summary>Boss1 の PhaseTransition 状態です。</summary>
    public const BossState PhaseTransition = BossState.Boss1_PhaseTransition;
    /// <summary>Boss1 の Dead 状態です。</summary>
    public const BossState Dead = BossState.Boss1_Dead;

    /// <summary>指定攻撃の固定 Execute BossState を取得します。</summary>
    public static BossState GetExecuteState(AttackId attackId)
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
            _ => BossState.Unknown
        };
    }

    /// <summary>旧 API 名から固定 Execute BossState を取得します。</summary>
    [Obsolete("StateId から BossState への移行用です。")]
    public static BossState GetExecuteStateId(AttackId attackId) => GetExecuteState(attackId);
}

/// <summary>Boss1 固有の安定した攻撃識別子です。</summary>
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
