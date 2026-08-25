using System.Collections.ObjectModel;
using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 一つの Boss と Phase に対する検証済み設定を表します。
/// </summary>
public readonly struct BossPhaseSnapshot
{
    /// <summary>対象 Boss 種別を取得します。</summary>
    public BossType BossType { get; }

    /// <summary>対象 Phase を取得します。</summary>
    public Phase Phase { get; }

    /// <summary>この Phase が有効になる HP 割合の上限を取得します。</summary>
    public float HpThreshold { get; }

    /// <summary>攻撃前の待機時間を取得します。</summary>
    public float IdleDuration { get; }

    /// <summary>移動速度を取得します。</summary>
    public float MoveSpeed { get; }

    /// <summary>跳躍初速を取得します。</summary>
    public float JumpForce { get; }

    /// <summary>浮遊待機時間を取得します。</summary>
    public float HoverDuration { get; }

    /// <summary>Phase 遷移時間を取得します。</summary>
    public float TransitionDuration { get; }

    /// <summary>検証済みの攻撃順序を取得します。</summary>
    public IReadOnlyList<AttackId> AttackSequence { get; }

    /// <summary>
    /// 検証済み Phase 設定を生成します。
    /// </summary>
    /// <param name="bossType">対象 Boss 種別。</param>
    /// <param name="phase">対象 Phase。</param>
    /// <param name="hpThreshold">HP 割合の上限。</param>
    /// <param name="idleDuration">Boss2 の待機時間。</param>
    /// <param name="moveSpeed">Boss2 の移動速度。</param>
    /// <param name="jumpForce">Boss2 の跳躍初速。</param>
    /// <param name="hoverDuration">Boss1 の浮遊待機時間。</param>
    /// <param name="transitionDuration">Phase 遷移時間。</param>
    /// <param name="attackSequence">固定 AttackId に解決済みの攻撃順序。</param>
    public BossPhaseSnapshot(
        BossType bossType,
        Phase phase,
        float hpThreshold,
        float idleDuration,
        float moveSpeed,
        float jumpForce,
        float hoverDuration,
        float transitionDuration,
        IReadOnlyList<AttackId> attackSequence)
    {
        ArgumentNullException.ThrowIfNull(attackSequence);

        BossType = bossType;
        Phase = phase;
        HpThreshold = hpThreshold;
        IdleDuration = idleDuration;
        MoveSpeed = moveSpeed;
        JumpForce = jumpForce;
        HoverDuration = hoverDuration;
        TransitionDuration = transitionDuration;
        AttackSequence = new ReadOnlyCollection<AttackId>(attackSequence.ToArray());
    }
}

/// <summary>Boss 種別と Phase の組を表す snapshot のキーです。</summary>
public readonly record struct BossPhaseKey(BossType BossType, Phase Phase);