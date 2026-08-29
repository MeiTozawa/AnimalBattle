using AnimalBattle.Ai.Contracts;
using ErrorOr;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase に従属する独立した Attack 子領域です。</summary>
public sealed class AttackRegion
{
    private readonly PhaseChildStateRules _rules;
    private StateId _currentStateId;
    private bool _isActive;
    private RegionTransitionResult _lastTransitionResult;

    /// <summary>Attack 子領域を生成します。</summary>
    public AttackRegion(PhaseChildStateRules rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    /// <summary>現在の Attack State_Id です。</summary>
    public StateId CurrentStateId => _currentStateId;
    /// <summary>子領域が有効かどうかです。</summary>
    public bool IsActive => _isActive;
    /// <summary>入場回数です。</summary>
    public int EnterCount { get; private set; }
    /// <summary>退場回数です。</summary>
    public int ExitCount { get; private set; }
    /// <summary>直近の失敗情報です。</summary>
    public RegionFailure LastFailure { get; private set; }
    /// <summary>直近の中間 State_Id です。</summary>
    public StateId IntermediateStateId => LastFailure.IntermediateStateId;
    /// <summary>直近の遷移結果を取得します。失敗時も中間状態を含みます。</summary>
    public RegionTransitionResult LastTransitionResult => _lastTransitionResult;

    /// <summary>Phase の安全な初期状態へ入場します。</summary>
    public ErrorOr<RegionTransitionResult> ActivateInitial(
        Phase phase,
        in TransitionReason reason,
        Func<StateId, ErrorOr<Success>>? transition = null)
    {
        ErrorOr<RegionStateRange> rangeResult = _rules.Get(phase);
        if (rangeResult.IsError)
        {
            return Fail(RegionFailureKind.InvalidPhase, phase, _currentStateId, default, default, reason);
        }

        RegionStateRange range = rangeResult.Value;
        ErrorOr<Success> transitionResult = transition is null
            ? Result.Success
            : transition(range.InitialStateId);
        if (transitionResult.IsError)
        {
            return Fail(RegionFailureKind.SafeInitialTransitionFailed, phase, _currentStateId, default, range.InitialStateId, reason);
        }

        StateId previous = _currentStateId;
        _currentStateId = range.InitialStateId;
        _isActive = true;
        EnterCount++;
        LastFailure = default;
        RegionTransitionResult result = new(true, false, false, previous, _currentStateId, default);
        _lastTransitionResult = result;
        return result;
    }

    /// <summary>指定 State_Id へ遷移し、範囲外なら安全初期状態を試行します。</summary>
    public ErrorOr<RegionTransitionResult> Transition(
        Phase phase,
        StateId requestedStateId,
        in TransitionReason reason,
        Func<StateId, ErrorOr<Success>>? safeTransition = null)
    {
        if (!_isActive)
        {
            return ActivateInitial(phase, in reason, safeTransition);
        }

        ErrorOr<RegionStateRange> rangeResult = _rules.Get(phase);
        if (rangeResult.IsError)
        {
            return Fail(RegionFailureKind.InvalidPhase, phase, _currentStateId, requestedStateId, default, reason);
        }

        RegionStateRange range = rangeResult.Value;
        if (range.Contains(requestedStateId))
        {
            StateId previous = _currentStateId;
            if (requestedStateId == _currentStateId)
            {
                LastFailure = default;
                RegionTransitionResult unchanged = new(true, false, false, previous, _currentStateId, default);
                _lastTransitionResult = unchanged;
                return unchanged;
            }

            _currentStateId = requestedStateId;
            EnterCount++;
            LastFailure = default;
            RegionTransitionResult changed = new(true, false, false, previous, _currentStateId, default);
            _lastTransitionResult = changed;
            return changed;
        }

        ErrorOr<RegionTransitionResult> recoveryResult = ActivateInitial(phase, in reason, safeTransition);
        if (recoveryResult.IsError)
        {
            RegionFailure safeFailure = LastFailure with
            {
                RequestedStateId = requestedStateId,
                IntermediateStateId = _currentStateId
            };
            LastFailure = safeFailure;
            _lastTransitionResult = new RegionTransitionResult(false, false, true, _currentStateId, _currentStateId, safeFailure);
            return safeFailure.ToError();
        }

        RegionTransitionResult recovery = recoveryResult.Value;
        RegionFailure failure = new(RegionKind.Attack, RegionFailureKind.ChildOutOfRange, phase, recovery.PreviousStateId, requestedStateId, recovery.CurrentStateId, range.InitialStateId, reason);
        LastFailure = failure;
        RegionTransitionResult recovered = new(true, true, false, recovery.PreviousStateId, recovery.CurrentStateId, failure);
        _lastTransitionResult = recovered;
        return recovered;
    }

    /// <summary>Phase 変更前に Attack 子領域を終了します。</summary>
    public void Exit()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        ExitCount++;
    }

    private ErrorOr<RegionTransitionResult> Fail(
        RegionFailureKind kind,
        Phase phase,
        StateId current,
        StateId requested,
        StateId safeInitial,
        in TransitionReason reason)
    {
        RegionFailure failure = new(RegionKind.Attack, kind, phase, current, requested, current, safeInitial, reason);
        LastFailure = failure;
        _lastTransitionResult = new RegionTransitionResult(false, false, true, current, current, failure);
        return failure.ToError();
    }
}
