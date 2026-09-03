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
    /// <summary>Boss1 の共通 Execute 状態です。</summary>
    public const BossState Execute = BossState.Boss1_Execute;

    /// <summary>Aimed3Shot の Execute 状態です（Execute の別名）。</summary>
    public const BossState Aimed3ShotExecute = Execute;
    /// <summary>RingBurst の Execute 状態です（Execute の別名）。</summary>
    public const BossState RingBurstExecute = Execute;
    /// <summary>DoubleSpiral の Execute 状態です（Execute の別名）。</summary>
    public const BossState DoubleSpiralExecute = Execute;
    /// <summary>RainDrop の Execute 状態です（Execute の別名）。</summary>
    public const BossState RainDropExecute = Execute;
    /// <summary>CrossChaos の Execute 状態です（Execute の別名）。</summary>
    public const BossState CrossChaosExecute = Execute;
    /// <summary>FastSniper の Execute 状態です（Execute の別名）。</summary>
    public const BossState FastSniperExecute = Execute;
    /// <summary>DiveRam の Execute 状態です（Execute の別名）。</summary>
    public const BossState DiveRamExecute = Execute;

    /// <summary>Boss1 の Recovery 状態です。</summary>
    public const BossState Recovery = BossState.Boss1_Recovery;
    /// <summary>Boss1 の PhaseTransition 状態です。</summary>
    public const BossState PhaseTransition = BossState.Boss1_PhaseTransition;
    /// <summary>Boss1 の Dead 状態です。</summary>
    public const BossState Dead = BossState.Boss1_Dead;

    /// <summary>指定技能の固定 Execute BossState を取得します（全技能共通で Execute を返します）。</summary>
    public static BossState GetExecuteState(BossSkill skill)
    {
        return skill.IsValid() ? Execute : BossState.Unknown;
    }
}



