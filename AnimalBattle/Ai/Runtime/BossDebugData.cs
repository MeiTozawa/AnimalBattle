using System.Runtime.InteropServices;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>Debug 表示が有効なときだけ生成する診断データ構造体です。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossDebugData
{
    /// <summary>現在の State_Id です。</summary>
    public BossState BossState;

    /// <summary>現在の Phase です。</summary>
    public Phase Phase;

    /// <summary>現在の技能です。</summary>
    public BossSkill Skill;

    /// <summary>直近の遷移理由です。</summary>
    public TransitionReason TransitionReason;

    /// <summary>無敵状態です。</summary>
    public bool IsInvulnerable;

    /// <summary>攻撃段階です。</summary>
    public AttackStage AttackStage;

    /// <summary>Frame_Update 番号です。</summary>
    public long FrameUpdateNumber;
}
