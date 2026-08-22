using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase に従属する独立した Attack 子領域です。</summary>
public sealed class AttackRegion
{
    private readonly PhaseChildStateRules _rules;
    private StateId _currentStateId;
    private bool _isActive;

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

    /// <summary>Phase の安全な初期状態へ入場します。</summary>
    public RegionTransitionResult TryActivateInitial(Phase phase, in TransitionReason reason, Func<StateId, bool>? transition = null)
    {
        if (!_rules.TryGet(phase, out RegionStateRange range))
        {
            return Fail(RegionFailureKind.InvalidPhase, phase, _currentStateId, default, default, reason);
        }

        bool accepted = transition is null || transition(range.InitialStateId);
        if (!accepted)
        {
            return Fail(RegionFailureKind.SafeInitialTransitionFailed, phase, _currentStateId, default, range.InitialStateId, reason);
        }

        StateId previous = _currentStateId;
        _currentStateId = range.InitialStateId;
        _isActive = true;
        EnterCount++;
        LastFailure = default;
        return new RegionTransitionResult(true, false, false, previous, _currentStateId, default);
    }

    /// <summary>指定 State_Id へ遷移し、範囲外なら安全初期状態を試行します。</summary>
    public RegionTransitionResult TryTransition(Phase phase, StateId requestedStateId, in TransitionReason reason, Func<StateId, bool>? safeTransition = null)
    {
        if (!_isActive)
        {
            return TryActivateInitial(phase, in reason, safeTransition);
        }

        if (!_rules.TryGet(phase, out RegionStateRange range))
        {
            return Fail(RegionFailureKind.InvalidPhase, phase, _currentStateId, requestedStateId, default, reason);
        }

        if (range.Contains(requestedStateId))
        {
            StateId previous = _currentStateId;
            _currentStateId = requestedStateId;
            EnterCount++;
            LastFailure = default;
            return new RegionTransitionResult(true, false, false, previous, _currentStateId, default);
        }

        RegionTransitionResult recovery = TryActivateInitial(phase, in reason, safeTransition);
        if (recovery.IsSuccess)
        {
            RegionFailure failure = new(RegionKind.Attack, RegionFailureKind.ChildOutOfRange, phase, requestedStateId, requestedStateId, recovery.CurrentStateId, range.InitialStateId, reason);
            LastFailure = failure;
            return new RegionTransitionResult(false, true, false, recovery.PreviousStateId, recovery.CurrentStateId, failure);
        }

        return recovery;
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

    private RegionTransitionResult Fail(RegionFailureKind kind, Phase phase, StateId current, StateId requested, StateId safeInitial, in TransitionReason reason)
    {
        RegionFailure failure = new(RegionKind.Attack, kind, phase, current, requested, current, safeInitial, reason);
        LastFailure = failure;
        return new RegionTransitionResult(false, false, kind == RegionFailureKind.SafeInitialTransitionFailed, current, current, failure);
    }
}