using AnimalBattle.Ai.Contracts;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase を親に Movement と Attack を独立して合成する領域です。</summary>
/// <remarks>Phase 親領域と子領域を生成します。</remarks>
public sealed class PhaseRegion(PhaseChildStateRules movementRules, PhaseChildStateRules attackRules)
{

    /// <summary>現在の親 Phase です。</summary>
    public Phase CurrentPhase { get; private set; } = Phase.Unknown;
    /// <summary>親領域が有効かどうかです。</summary>
    public bool IsValid { get; private set; }
    /// <summary>Movement 子領域です。</summary>
    public MovementRegion Movement { get; } = new MovementRegion(movementRules);
    /// <summary>Attack 子領域です。</summary>
    public AttackRegion Attack { get; } = new AttackRegion(attackRules);
    /// <summary>Phase 入場回数です。</summary>
    public int EnterCount { get; private set; }
    /// <summary>Phase 退場回数です。</summary>
    public int ExitCount { get; private set; }
    /// <summary>直近 Phase 遷移理由です。</summary>
    public TransitionReason LastPhaseTransitionReason { get; private set; }
    /// <summary>直近の領域失敗です。</summary>
    public RegionFailure LastFailure { get; private set; }
    /// <summary>直近の Movement 領域の失敗情報です。</summary>
    public RegionFailure LastMovementFailure { get; private set; }
    /// <summary>直近の Attack 領域の失敗情報です。</summary>
    public RegionFailure LastAttackFailure { get; private set; }
    /// <summary>直近の同一 frame における子領域結果です。</summary>
    public CompositeRegionResult LastCompositeResult { get; private set; }

    /// <summary>初期 Phase と許可された子状態へ入場します。</summary>
    public Result<CompositeRegionResult> Initialize(
        Phase phase,
        in TransitionReason reason,
        Func<StateId, Result<bool>>? movementTransition = null,
        Func<StateId, Result<bool>>? attackTransition = null)
    {
        if (IsValid)
        {
            return phase == CurrentPhase
                ? Result<CompositeRegionResult>.Ok(CurrentResult())
                : Failure(new RegionFailure(RegionKind.Phase, RegionFailureKind.ParentInvalid, CurrentPhase, default, default, default, default, reason));
        }

        if (phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            return Failure(new RegionFailure(RegionKind.Phase, RegionFailureKind.InvalidPhase, phase, default, default, default, default, reason));
        }

        CurrentPhase = phase;
        LastPhaseTransitionReason = reason;
        EnterCount++;
        Result<RegionTransitionResult> movementResult = Movement.ActivateInitial(phase, in reason, movementTransition);
        Result<RegionTransitionResult> attackResult = Attack.ActivateInitial(phase, in reason, attackTransition);
        CompositeRegionResult composite = StoreComposite(
            movementResult.IsOk ? movementResult.ValueOrDefault() : Movement.LastTransitionResult,
            attackResult.IsOk ? attackResult.ValueOrDefault() : Attack.LastTransitionResult);
        if (!movementResult.IsOk || !attackResult.IsOk)
        {
            IsValid = false;
            return Failure(SelectFailure(
                movementResult.IsOk ? default : Movement.LastFailure,
                attackResult.IsOk ? default : Attack.LastFailure));
        }

        IsValid = true;
        LastCompositeResult = composite;
        return Result<CompositeRegionResult>.Ok(composite);
    }

    /// <summary>Phase を変更し、子を終了して新 Phase の初期子を選択します。</summary>
    public Result<CompositeRegionResult> ChangePhase(
        Phase phase,
        in TransitionReason reason,
        Func<StateId, Result<bool>>? movementTransition = null,
        Func<StateId, Result<bool>>? attackTransition = null)
    {
        if (!IsValid || phase == Phase.Unknown || (int)phase < (int)CurrentPhase)
        {
            RegionFailureKind kind = !IsValid ? RegionFailureKind.ParentInvalid : RegionFailureKind.InvalidPhase;
            return Failure(new RegionFailure(RegionKind.Phase, kind, CurrentPhase, default, default, default, default, reason));
        }

        if (phase == CurrentPhase)
        {
            return Result<CompositeRegionResult>.Ok(CurrentResult());
        }

        ExitCount++;
        Movement.Exit();
        Attack.Exit();
        CurrentPhase = phase;
        LastPhaseTransitionReason = reason;
        EnterCount++;

        if (phase == Phase.Dead)
        {
            IsValid = true;
            LastCompositeResult = CurrentResult();
            return Result<CompositeRegionResult>.Ok(LastCompositeResult);
        }

        IsValid = false;
        Result<RegionTransitionResult> movementResult = Movement.ActivateInitial(phase, in reason, movementTransition);
        Result<RegionTransitionResult> attackResult = Attack.ActivateInitial(phase, in reason, attackTransition);
        CompositeRegionResult composite = StoreComposite(
            movementResult.IsOk ? movementResult.ValueOrDefault() : Movement.LastTransitionResult,
            attackResult.IsOk ? attackResult.ValueOrDefault() : Attack.LastTransitionResult);
        if (!movementResult.IsOk || !attackResult.IsOk)
        {
            return Failure(SelectFailure(
                movementResult.IsOk ? default : Movement.LastFailure,
                attackResult.IsOk ? default : Attack.LastFailure));
        }

        IsValid = true;
        LastCompositeResult = composite;
        return Result<CompositeRegionResult>.Ok(composite);
    }

    /// <summary>Phase 非変更時に Movement と Attack を独立更新します。</summary>
    public Result<CompositeRegionResult> UpdateChildren(
        StateId movementStateId,
        StateId attackStateId,
        in TransitionReason reason,
        Func<StateId, Result<bool>>? movementRecovery = null,
        Func<StateId, Result<bool>>? attackRecovery = null)
    {
        Result<bool> validation = ValidateChildUpdate(in reason);
        if (!validation.IsOk)
        {
            return Result<CompositeRegionResult>.Err(LastFailure.ToSimpleError());
        }

        Result<RegionTransitionResult> movementResult = Movement.Transition(CurrentPhase, movementStateId, in reason, movementRecovery);
        // Movement の失敗後も Attack を同じ Frame_Update で評価し、成功した値を保持します。
        Result<RegionTransitionResult> attackResult = Attack.Transition(CurrentPhase, attackStateId, in reason, attackRecovery);
        CompositeRegionResult composite = StoreComposite(
            movementResult.IsOk ? movementResult.ValueOrDefault() : Movement.LastTransitionResult,
            attackResult.IsOk ? attackResult.ValueOrDefault() : Attack.LastTransitionResult);
        if (!movementResult.IsOk || !attackResult.IsOk)
        {
            return Result<CompositeRegionResult>.Err(SelectFailure(
                movementResult.IsOk ? default : Movement.LastFailure,
                attackResult.IsOk ? default : Attack.LastFailure).ToSimpleError());
        }

        return Result<CompositeRegionResult>.Ok(composite);
    }

    /// <summary>Movement だけを変更し、Attack の現在値を保持します。</summary>
    public Result<RegionTransitionResult> UpdateMovement(
        StateId movementStateId,
        in TransitionReason reason,
        Func<StateId, Result<bool>>? safeTransition = null)
    {
        Result<bool> validation = ValidateChildUpdate(in reason);
        if (!validation.IsOk)
        {
            return Result<RegionTransitionResult>.Err(LastFailure.ToSimpleError());
        }

        Result<RegionTransitionResult> result = Movement.Transition(CurrentPhase, movementStateId, in reason, safeTransition);
        RecordSingleChildResult(RegionKind.Movement, result.IsOk ? result.ValueOrDefault() : Movement.LastTransitionResult);
        return result;
    }

    /// <summary>Attack だけを変更し、Movement の現在値を保持します。</summary>
    public Result<RegionTransitionResult> UpdateAttack(
        StateId attackStateId,
        in TransitionReason reason,
        Func<StateId, Result<bool>>? safeTransition = null)
    {
        Result<bool> validation = ValidateChildUpdate(in reason);
        if (!validation.IsOk)
        {
            return Result<RegionTransitionResult>.Err(LastFailure.ToSimpleError());
        }

        Result<RegionTransitionResult> result = Attack.Transition(CurrentPhase, attackStateId, in reason, safeTransition);
        RecordSingleChildResult(RegionKind.Attack, result.IsOk ? result.ValueOrDefault() : Attack.LastTransitionResult);
        return result;
    }

    private Result<bool> ValidateChildUpdate(in TransitionReason reason)
    {
        if (!IsValid)
        {
            LastFailure = new RegionFailure(RegionKind.Phase, RegionFailureKind.ParentInvalid, CurrentPhase, default, default, default, default, reason);
            return SimpleError.NotValid("Phase 親領域が無効なため子領域を更新できません。");
        }

        if (CurrentPhase == Phase.Dead)
        {
            LastFailure = new RegionFailure(RegionKind.Phase, RegionFailureKind.TerminalPhase, CurrentPhase, default, default, default, default, reason);
            return SimpleError.NotValid("終端 Phase の子領域は更新できません。");
        }

        return Result<bool>.Ok(true);
    }

    private CompositeRegionResult CurrentResult()
    {
        RegionTransitionResult movement = new(true, false, false, Movement.CurrentStateId, Movement.CurrentStateId, default);
        RegionTransitionResult attack = new(true, false, false, Attack.CurrentStateId, Attack.CurrentStateId, default);
        return Combine(movement, attack);
    }

    private static CompositeRegionResult Combine(RegionTransitionResult movement, RegionTransitionResult attack)
    {
        bool parentInvalid = movement.ParentInvalid || attack.ParentInvalid;
        bool hasSuccessfulChange = movement.IsChanged || attack.IsChanged;
        bool partial = hasSuccessfulChange && (movement.HasFailure || attack.HasFailure || !movement.IsChanged || !attack.IsChanged);
        RegionFailure failure = movement.Failure.Kind != RegionFailureKind.None ? movement.Failure : attack.Failure;
        bool isSuccess = !parentInvalid && movement.IsSuccess && attack.IsSuccess && hasSuccessfulChange;
        return new CompositeRegionResult(isSuccess, partial, parentInvalid, movement, attack, failure);
    }

    private CompositeRegionResult StoreComposite(RegionTransitionResult movement, RegionTransitionResult attack)
    {
        LastMovementFailure = movement.HasFailure ? movement.Failure : default;
        LastAttackFailure = attack.HasFailure ? attack.Failure : default;
        LastFailure = movement.HasFailure
            ? movement.Failure
            : attack.HasFailure
                ? attack.Failure
                : default;
        CompositeRegionResult composite = Combine(movement, attack);
        LastCompositeResult = composite;
        if (composite.ParentInvalid || (composite.HasFailure && composite.Failure.Kind is RegionFailureKind.SafeInitialTransitionFailed or RegionFailureKind.InvalidPhase))
        {
            IsValid = false;
        }

        return composite;
    }

    private static RegionFailure SelectFailure(RegionFailure movement, RegionFailure attack)
    {
        return movement.Kind != RegionFailureKind.None ? movement : attack;
    }

    private void RecordSingleChildResult(RegionKind region, RegionTransitionResult result)
    {
        if (region == RegionKind.Movement)
        {
            LastMovementFailure = result.HasFailure ? result.Failure : default;
        }
        else if (region == RegionKind.Attack)
        {
            LastAttackFailure = result.HasFailure ? result.Failure : default;
        }

        if (result.HasFailure)
        {
            ApplyChildFailure(result.Failure);
        }
        else if (LastFailure.Region == region)
        {
            LastFailure = default;
        }
    }

    private void ApplyChildFailure(RegionFailure failure)
    {
        LastFailure = failure;
        if (failure.Region == RegionKind.Movement)
        {
            LastMovementFailure = failure;
        }
        else if (failure.Region == RegionKind.Attack)
        {
            LastAttackFailure = failure;
        }

        if (failure.Kind is RegionFailureKind.ParentInvalid
            or RegionFailureKind.SafeInitialTransitionFailed
            or RegionFailureKind.InvalidPhase)
        {
            IsValid = false;
        }
    }

    private Result<CompositeRegionResult> Failure(RegionFailure failure)
    {
        LastFailure = failure;
        return Result<CompositeRegionResult>.Err(failure.ToSimpleError());
    }

    /// <summary>親領域を明示的に無効化します。</summary>
    public void Invalidate(in RegionFailure failure)
    {
        IsValid = false;
        LastFailure = failure;
    }
}
