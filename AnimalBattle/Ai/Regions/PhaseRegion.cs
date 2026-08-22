using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase を親に Movement と Attack を独立して合成する領域です。</summary>
public sealed class PhaseRegion
{
    /// <summary>Phase 親領域と子領域を生成します。</summary>
    public PhaseRegion(PhaseChildStateRules movementRules, PhaseChildStateRules attackRules)
    {
        Movement = new MovementRegion(movementRules);
        Attack = new AttackRegion(attackRules);
    }

    /// <summary>現在の親 Phase です。</summary>
    public Phase CurrentPhase { get; private set; } = Phase.Unknown;
    /// <summary>親領域が有効かどうかです。</summary>
    public bool IsValid { get; private set; }
    /// <summary>Movement 子領域です。</summary>
    public MovementRegion Movement { get; }
    /// <summary>Attack 子領域です。</summary>
    public AttackRegion Attack { get; }
    /// <summary>Phase 入場回数です。</summary>
    public int EnterCount { get; private set; }
    /// <summary>Phase 退場回数です。</summary>
    public int ExitCount { get; private set; }
    /// <summary>直近 Phase 遷移理由です。</summary>
    public TransitionReason LastPhaseTransitionReason { get; private set; }
    /// <summary>直近の領域失敗です。</summary>
    public RegionFailure LastFailure { get; private set; }

    /// <summary>初期 Phase と許可された子状態へ入場します。</summary>
    public bool TryInitialize(Phase phase, in TransitionReason reason, Func<StateId, bool>? movementTransition = null, Func<StateId, bool>? attackTransition = null)
    {
        if (IsValid)
        {
            return CurrentPhase == phase;
        }

        if (phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
        {
            LastFailure = new RegionFailure(RegionKind.Phase, RegionFailureKind.InvalidPhase, phase, default, default, default, default, reason);
            return false;
        }

        CurrentPhase = phase;
        LastPhaseTransitionReason = reason;
        EnterCount++;
        RegionTransitionResult movement = Movement.TryActivateInitial(phase, in reason, movementTransition);
        RegionTransitionResult attack = Attack.TryActivateInitial(phase, in reason, attackTransition);
        IsValid = movement.IsSuccess && attack.IsSuccess;
        if (!IsValid)
        {
            LastFailure = movement.IsSuccess ? attack.Failure : movement.Failure;
        }
        return IsValid;
    }

    /// <summary>Phase を変更し、子を終了して新 Phase の初期子を選択します。</summary>
    public bool TryChangePhase(Phase phase, in TransitionReason reason, Func<StateId, bool>? movementTransition = null, Func<StateId, bool>? attackTransition = null)
    {
        if (!IsValid || phase == Phase.Unknown || (int)phase < (int)CurrentPhase)
        {
            LastFailure = new RegionFailure(
                RegionKind.Phase,
                !IsValid ? RegionFailureKind.ParentInvalid : RegionFailureKind.InvalidPhase,
                CurrentPhase,
                default,
                default,
                default,
                default,
                reason);
            return false;
        }

        if (phase == CurrentPhase)
        {
            return true;
        }

        ExitCount++;
        Movement.Exit();
        Attack.Exit();
        CurrentPhase = phase;
        LastPhaseTransitionReason = reason;
        EnterCount++;

        RegionTransitionResult movement = phase == Phase.Dead
            ? new RegionTransitionResult(true, false, false, default, default, default)
            : Movement.TryActivateInitial(phase, in reason, movementTransition);
        RegionTransitionResult attack = phase == Phase.Dead
            ? new RegionTransitionResult(true, false, false, default, default, default)
            : Attack.TryActivateInitial(phase, in reason, attackTransition);
        IsValid = movement.IsSuccess && attack.IsSuccess;
        if (!IsValid)
        {
            LastFailure = movement.IsSuccess ? attack.Failure : movement.Failure;
        }
        return IsValid;
    }

    /// <summary>Phase 非変更時に Movement と Attack を独立更新します。</summary>
    public CompositeRegionResult TryUpdateChildren(StateId movementStateId, StateId attackStateId, in TransitionReason reason, Func<StateId, bool>? movementRecovery = null, Func<StateId, bool>? attackRecovery = null)
    {
        if (!IsValid)
        {
            RegionFailure parentFailure = new(RegionKind.Phase, RegionFailureKind.ParentInvalid, CurrentPhase, default, default, default, default, reason);
            LastFailure = parentFailure;
            return new CompositeRegionResult(false, false, true, default, default, parentFailure);
        }

        if (CurrentPhase == Phase.Dead)
        {
            RegionFailure terminalFailure = new(RegionKind.Phase, RegionFailureKind.TerminalPhase, CurrentPhase, default, default, default, default, reason);
            LastFailure = terminalFailure;
            return new CompositeRegionResult(false, false, false, default, default, terminalFailure);
        }

        RegionTransitionResult movement = Movement.TryTransition(CurrentPhase, movementStateId, in reason, movementRecovery);
        RegionTransitionResult attack = Attack.TryTransition(CurrentPhase, attackStateId, in reason, attackRecovery);
        bool parentInvalid = movement.ParentInvalid || attack.ParentInvalid;
        bool movementChanged = movement.IsSuccess;
        bool attackChanged = attack.IsSuccess;
        bool partial = movementChanged && !attackChanged || attackChanged && !movementChanged || movement.WasRecovered || attack.WasRecovered;
        if (parentInvalid)
        {
            IsValid = false;
            LastFailure = movement.ParentInvalid ? movement.Failure : attack.Failure;
        }

        RegionFailure failure = movement.Failure.Kind != RegionFailureKind.None ? movement.Failure : attack.Failure;
        return new CompositeRegionResult(!parentInvalid && (movementChanged || attackChanged), partial, parentInvalid, movement, attack, failure);
    }

    /// <summary>親領域を明示的に無効化します。</summary>
    public void Invalidate(in RegionFailure failure)
    {
        IsValid = false;
        LastFailure = failure;
    }
}
