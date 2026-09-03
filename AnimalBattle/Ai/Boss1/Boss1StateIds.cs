using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の固定 BossState と BossSkill の対応を提供します。</summary>
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

    /// <summary>指定技能の固定 Execute BossState を取得します。</summary>
    public static BossState GetExecuteState(BossSkill skill)
    {
        return skill switch
        {
            BossSkill.Aimed3Shot => Aimed3ShotExecute,
            BossSkill.RingBurst => RingBurstExecute,
            BossSkill.DoubleSpiral => DoubleSpiralExecute,
            BossSkill.RainDrop => RainDropExecute,
            BossSkill.CrossChaos => CrossChaosExecute,
            BossSkill.FastSniper => FastSniperExecute,
            BossSkill.DiveRam => DiveRamExecute,
            _ => BossState.Unknown
        };
    }
}



