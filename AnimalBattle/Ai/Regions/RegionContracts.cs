using AnimalBattle.Ai.Contracts;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase ごとの子状態許可範囲を表します。</summary>
public readonly struct RegionStateRange
{
    /// <summary>安全な初期 State_Id です。</summary>
    public StateId InitialStateId { get; }
    /// <summary>許可範囲の最小 State_Id です。</summary>
    public StateId MinimumStateId { get; }
    /// <summary>許可範囲の最大 State_Id です。</summary>
    public StateId MaximumStateId { get; }

    /// <summary>状態範囲を生成します。</summary>
    public RegionStateRange(StateId initialStateId, StateId minimumStateId, StateId maximumStateId)
    {
        InitialStateId = initialStateId;
        MinimumStateId = minimumStateId;
        MaximumStateId = maximumStateId;
    }

    /// <summary>State_Id が範囲内かどうかを取得します。</summary>
    public bool Contains(StateId stateId) => stateId.IsValid
        && MinimumStateId.IsValid
        && MaximumStateId.IsValid
        && stateId.Value >= MinimumStateId.Value
        && stateId.Value <= MaximumStateId.Value;
}

/// <summary>Phase 1〜3 の子状態許可範囲を保持します。</summary>
public sealed class PhaseChildStateRules
{
    private readonly RegionStateRange _phase1;
    private readonly RegionStateRange _phase2;
    private readonly RegionStateRange _phase3;

    /// <summary>許可範囲を生成します。</summary>
    public PhaseChildStateRules(RegionStateRange phase1, RegionStateRange phase2, RegionStateRange phase3)
    {
        _phase1 = phase1;
        _phase2 = phase2;
        _phase3 = phase3;
    }

    /// <summary>指定 Phase の許可範囲を取得します。</summary>
    public bool TryGet(Phase phase, out RegionStateRange range)
    {
        range = phase switch
        {
            Phase.Phase1 => _phase1,
            Phase.Phase2 => _phase2,
            Phase.Phase3 => _phase3,
            _ => default
        };
        return phase is Phase.Phase1 or Phase.Phase2 or Phase.Phase3;
    }
}

/// <summary>状態領域の種類です。</summary>
public enum RegionKind
{
    /// <summary>Phase 親領域です。</summary>
    Phase = 1,
    /// <summary>Movement 子領域です。</summary>
    Movement = 2,
    /// <summary>Attack 子領域です。</summary>
    Attack = 3
}

/// <summary>領域遷移の失敗分類です。</summary>
public enum RegionFailureKind
{
    /// <summary>失敗なしです。</summary>
    None = 0,
    /// <summary>親 Phase が不正です。</summary>
    InvalidPhase = 1,
    /// <summary>子状態が許可範囲外です。</summary>
    ChildOutOfRange = 2,
    /// <summary>安全な初期状態へ遷移できません。</summary>
    SafeInitialTransitionFailed = 3,
    /// <summary>親領域が無効化済みです。</summary>
    ParentInvalid = 4,
    /// <summary>終端状態の子更新です。</summary>
    TerminalPhase = 5
}

/// <summary>領域で観測可能な失敗情報です。</summary>
public readonly record struct RegionFailure(
    RegionKind Region,
    RegionFailureKind Kind,
    Phase Phase,
    StateId CurrentStateId,
    StateId RequestedStateId,
    StateId IntermediateStateId,
    StateId SafeInitialStateId,
    TransitionReason Reason);

/// <summary>子領域または親領域の遷移結果です。</summary>
public readonly record struct RegionTransitionResult(
    bool IsSuccess,
    bool WasRecovered,
    bool ParentInvalid,
    StateId PreviousStateId,
    StateId CurrentStateId,
    RegionFailure Failure)
{
    /// <summary>成功した変更を保持しながら別領域が失敗した部分成功かどうかです。</summary>
    public bool IsPartialSuccess => IsSuccess && WasRecovered;
}

/// <summary>Phase、Movement、Attack の同一 frame 更新結果です。</summary>
public readonly record struct CompositeRegionResult(
    bool IsSuccess,
    bool IsPartialSuccess,
    bool ParentInvalid,
    RegionTransitionResult Movement,
    RegionTransitionResult Attack,
    RegionFailure Failure);
