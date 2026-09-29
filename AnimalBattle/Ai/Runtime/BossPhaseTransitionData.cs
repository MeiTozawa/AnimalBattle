using System.Runtime.InteropServices;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>Phase 遷移の項目別適用状況を保持するデータ構造体です。</summary>
/// <remarks>Phase 遷移データを生成します。</remarks>
[StructLayout(LayoutKind.Sequential)]
public struct BossPhaseTransitionData(Phase targetPhase, in TransitionReason reason) {
    /// <summary>遷移先 Phase です。</summary>
    public Phase TargetPhase = targetPhase;

    /// <summary>遷移理由と payload です。</summary>
    public TransitionReason Reason = reason;

    /// <summary>PhaseTransition が有効かどうかです。</summary>
    public bool IsActive = false;

    /// <summary>一度開始した Phase 閾値の bit mask です。</summary>
    public PhaseTransitionApplyMask TriggeredPhaseMask = PhaseTransitionApplyMask.None;

    /// <summary>成功した適用項目です。</summary>
    public PhaseTransitionApplyMask AppliedMask = PhaseTransitionApplyMask.None;

    /// <summary>失敗した適用項目です。</summary>
    public PhaseTransitionApplyMask FailedMask = PhaseTransitionApplyMask.None;

    /// <summary>遷移中の経過時間です。</summary>
    public float TransitionTimer = 0.0f;
}

/// <summary>Phase 遷移で適用する項目を表すビット値です。</summary>
[Flags]
public enum PhaseTransitionApplyMask {
    /// <summary>項目なしです。</summary>
    None = 0,

    /// <summary>無敵です。</summary>
    Invulnerability = 1,

    /// <summary>移動停止です。</summary>
    MovementStop = 2,

    /// <summary>Boss 弾整理です。（※Phase 遷移時は弾幕を維持するため通常適用されません）</summary>
    BulletCleanup = 4,

    /// <summary>演出です。</summary>
    Effect = 8,

    /// <summary>Phase2 の閾値を一度開始したことを示します。</summary>
    Phase2 = 16,

    /// <summary>Phase3 の閾値を一度開始したことを示します。</summary>
    Phase3 = 32
}