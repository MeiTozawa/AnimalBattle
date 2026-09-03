using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Components;
using ErrorOr;

namespace AnimalBattle.Ai.Runtime;

/// <summary>HP 閾値、PhaseTransition、死亡終端を allocation-free に判定する共通 policy です。</summary>
public readonly struct BossPhaseThresholds
{
    /// <summary>Phase2 へ移る HP 割合の閾値です。</summary>
    public float Phase2 { get; }
    /// <summary>Phase3 へ移る HP 割合の閾値です。</summary>
    public float Phase3 { get; }

    /// <summary>Phase 閾値を生成します。</summary>
    public BossPhaseThresholds(float phase2, float phase3)
    {
        Phase2 = phase2;
        Phase3 = phase3;
    }

    /// <summary>閾値が既存設定の有効範囲にあるかどうかです。</summary>
    public bool IsValid => float.IsFinite(Phase2)
        && float.IsFinite(Phase3)
        && Phase2 >= 0.0f
        && Phase2 <= 1.0f
        && Phase3 >= 0.0f
        && Phase3 <= Phase2;

    /// <summary>既存 Boss 設定の Phase 閾値です。</summary>
    public static BossPhaseThresholds Default => new(0.66f, 0.33f);

    /// <summary>HP 割合から単調に進められる Phase を取得します。</summary>
    public Phase GetPhase(float hpRatio)
    {
        float ratio = float.IsFinite(hpRatio) ? Math.Clamp(hpRatio, 0.0f, 1.0f) : 0.0f;
        return ratio > Phase2 ? Phase.Phase1 : ratio > Phase3 ? Phase.Phase2 : Phase.Phase3;
    }

    /// <summary>現在 Phase より先へ進む必要がある場合だけ次 Phase を返します。</summary>
    public bool GetNextPhase(Phase current, float hpRatio, out Phase next, out float threshold)
    {
        next = current;
        threshold = 0.0f;
        if (!IsValid || current is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return false;
        }

        Phase target = GetPhase(hpRatio);
        if ((int)target <= (int)current)
        {
            return false;
        }

        next = target;
        threshold = target == Phase.Phase2 ? Phase2 : Phase3;
        return true;
    }
}

/// <summary>PhaseTransition の項目別適用結果を表す値型です。</summary>
public readonly record struct PhaseTransitionApplicationResult(
    PhaseTransitionApplyMask AppliedMask,
    PhaseTransitionApplyMask FailedMask,
    bool IsComplete)
{
    /// <summary>失敗した適用項目があるかどうかです。</summary>
    public bool HasFailure => FailedMask != PhaseTransitionApplyMask.None;
}

/// <summary>HP 閾値と終端処理の共通判定を提供します。</summary>
public static class PhaseDeathController
{
    /// <summary>Boss1 の固定 PhaseTransition state を取得します。</summary>
    public static BossState GetPhaseTransitionState(BossType bossType)
        => bossType == BossType.Boss2 ? BossState.Boss2_PhaseTransition : BossState.Boss1_PhaseTransition;

    /// <summary>Boss 種別に対応する固定 Dead state を取得します。</summary>
    public static BossState GetDeadState(BossType bossType)
        => bossType == BossType.Boss2 ? BossState.Boss2_Dead : BossState.Boss1_Dead;

    /// <summary>HP がゼロ以下なら死亡理由を作成します。</summary>
    public static TransitionReason CreateDeathReason(float hp)
    {
        long input = float.IsFinite(hp) ? (long)MathF.Truncate(hp) : 0;
        return new TransitionReason(ReasonKind.Death, originalInput: input);
    }

    /// <summary>HP 閾値を検査し、PhaseTransition data を一度だけ開始します。</summary>
    public static ErrorOr<Success> BeginPhaseTransition(
        ref BossAiData ai,
        ref BossPhaseTransitionData transition,
        in BossPhaseThresholds thresholds,
        float currentHp,
        float maxHp,
        in TransitionReason reason)
    {
        if (transition.IsActive || ai.IsDead || maxHp <= 0.0f || !float.IsFinite(maxHp))
        {
            return Error.Validation("PhaseDeathController.BeginPhaseTransition", "PhaseTransition 開始条件を満たしていません。");
        }

        float ratio = float.IsFinite(currentHp) ? currentHp / maxHp : 0.0f;
        if (!thresholds.GetNextPhase(ai.Phase, ratio, out Phase target, out float threshold))
        {
            return Error.Validation("PhaseDeathController.BeginPhaseTransition", "次 Phase への遷移閾値に達していません。");
        }

        if (transition.TriggeredPhaseMask.HasFlag(target.ToTransitionMask()))
        {
            return Error.Validation("PhaseDeathController.BeginPhaseTransition", "該当 Phase への遷移は既に実行済みです。");
        }

        TransitionReason effectiveReason = reason.Kind == ReasonKind.Unknown
            ? new TransitionReason(ReasonKind.PhaseThreshold, threshold: threshold)
            : reason;
        transition = new BossPhaseTransitionData(target, in effectiveReason)
        {
            IsActive = true,
            TriggeredPhaseMask = transition.TriggeredPhaseMask | target.ToTransitionMask()
        };
        ai.Phase = target;
        ai.BossState = GetPhaseTransitionState(ai.BossType);
        ai.IsInvulnerable = true;
        return Result.Success;
    }

    /// <summary>PhaseTransition の適用を項目単位で試行し、成功値を保持します。</summary>
    public static PhaseTransitionApplicationResult ApplyPhaseTransition(
        ref BossAiData ai,
        ref BossPhaseTransitionData transition,
        IMovementPort movement,
        IAttackPort attack,
        IEffectPort effect,
        float effectX,
        float effectY)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(effect);

        PhaseTransitionApplyMask applied = transition.AppliedMask;
        PhaseTransitionApplyMask failed = transition.FailedMask;
        applied |= PhaseTransitionApplyMask.Invulnerability;
        ai.IsInvulnerable = true;

        if (!applied.HasFlag(PhaseTransitionApplyMask.MovementStop))
        {
            if (!movement.Stop().IsError) applied |= PhaseTransitionApplyMask.MovementStop;
            else failed |= PhaseTransitionApplyMask.MovementStop;
        }

        if (!applied.HasFlag(PhaseTransitionApplyMask.BulletCleanup))
        {
            if (!attack.ClearBossBullets().IsError) applied |= PhaseTransitionApplyMask.BulletCleanup;
            else failed |= PhaseTransitionApplyMask.BulletCleanup;
        }

        if (!applied.HasFlag(PhaseTransitionApplyMask.Effect))
        {
            if (!effect.RequestTelegraph(effectX, effectY).IsError) applied |= PhaseTransitionApplyMask.Effect;
            else failed |= PhaseTransitionApplyMask.Effect;
        }

        transition.AppliedMask = applied;
        transition.FailedMask = failed;
        return new PhaseTransitionApplicationResult(applied, failed, true);
    }

    /// <summary>PhaseTransition を完了し、成功した項目を維持したまま通常状態へ戻せるようにします。</summary>
    public static void CompletePhaseTransition(ref BossAiData ai, ref BossPhaseTransitionData transition)
    {
        transition.IsActive = false;
        ai.IsInvulnerable = false;
    }

    /// <summary>同じ frame の死亡を確定し、終端 data を一度だけ保存します。</summary>
    public static ErrorOr<Success> CaptureDeath(
        ref BossAiData ai,
        ref BossTerminalData terminal,
        ref BossPhaseTransitionData transition,
        ref BossAttackData attack,
        float currentHp,
        long frameUpdateNumber)
    {
        if (terminal.IsTerminal || ai.IsDead || !float.IsFinite(currentHp) || currentHp > 0.0f)
        {
            return Error.Validation("PhaseDeathController.CaptureDeath", "死亡確定条件を満たしていません。");
        }

        TransitionReason reason = CreateDeathReason(currentHp);
        BossState deadState = GetDeadState(ai.BossType);
        ErrorOr<Success> captured = terminal.Capture(
            deadState,
            ai.Phase,
            in reason,
            frameUpdateNumber,
            attack.Skill,
            attack.Stage,

            currentHp);
        if (captured.IsError)
        {
            return captured;
        }

        ai.IsDead = true;
        ai.IsInvulnerable = false;
        ai.BossState = deadState;
        transition.IsActive = false;
        attack.Stage = AttackStage.None;
        attack.OneShotEmissionMask = 0;
        return Result.Success;
    }

    /// <summary>死亡 frame に移動停止と弾整理を適用します。</summary>
    public static void ApplyDeathStop(
        ref BossTerminalData terminal,
        IMovementPort movement,
        IAttackPort attack)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(attack);
        terminal.MovementStopped = !movement.Stop().IsError;
        terminal.AttackGenerationStopped = true;
        attack.ClearBossBullets();
    }

    private static PhaseTransitionApplyMask ToTransitionMask(this Phase phase)
        => phase switch
        {
            Phase.Phase2 => PhaseTransitionApplyMask.Phase2,
            Phase.Phase3 => PhaseTransitionApplyMask.Phase3,
            _ => PhaseTransitionApplyMask.None
        };
}
