using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>Debug 表示が有効なときだけ付与する純粋な診断データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossDebugData
{
    /// <summary>現在の State_Id です。</summary>
    public StateId StateId;
    /// <summary>現在の Phase です。</summary>
    public Phase Phase;
    /// <summary>現在の Attack_Id です。</summary>
    public AttackId AttackId;
    /// <summary>直近の遷移理由です。</summary>
    public TransitionReason TransitionReason;
    /// <summary>無敵状態です。</summary>
    public bool IsInvulnerable;
    /// <summary>攻撃段階です。</summary>
    public AttackStage AttackStage;
    /// <summary>Frame_Update 番号です。</summary>
    public long FrameUpdateNumber;
}