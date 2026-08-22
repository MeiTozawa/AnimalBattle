using System.Runtime.InteropServices;

using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Components;

/// <summary>Phase 遷移の項目別適用状況を保持する純粋な ECS データです。</summary>
[StructLayout(LayoutKind.Sequential)]
public struct BossPhaseTransitionData
{
    /// <summary>遷移先 Phase です。</summary>
    public Phase TargetPhase;
    /// <summary>遷移理由と payload です。</summary>
    public TransitionReason Reason;
    /// <summary>成功した適用項目です。</summary>
    public PhaseTransitionApplyMask AppliedMask;
    /// <summary>失敗した適用項目です。</summary>
    public PhaseTransitionApplyMask FailedMask;
    /// <summary>遷移中の経過時間です。</summary>
    public float TransitionTimer;

    /// <summary>Phase 遷移データを生成します。</summary>
    public BossPhaseTransitionData(Phase targetPhase, in TransitionReason reason)
    {
        TargetPhase = targetPhase;
        Reason = reason;
        AppliedMask = PhaseTransitionApplyMask.None;
        FailedMask = PhaseTransitionApplyMask.None;
        TransitionTimer = 0.0f;
    }
}

/// <summary>Phase 遷移で適用する項目を表すビット値です。</summary>
[Flags]
public enum PhaseTransitionApplyMask
{
    /// <summary>項目なしです。</summary>
    None = 0,
    /// <summary>無敵です。</summary>
    Invulnerability = 1,
    /// <summary>移動停止です。</summary>
    MovementStop = 2,
    /// <summary>Boss 弾整理です。</summary>
    BulletCleanup = 4,
    /// <summary>演出です。</summary>
    Effect = 8
}