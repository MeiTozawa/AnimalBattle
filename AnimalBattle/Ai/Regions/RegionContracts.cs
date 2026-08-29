using AnimalBattle.Ai.Contracts;
using ErrorOr;

namespace AnimalBattle.Ai.Regions;

/// <summary>Phase ごとの子状態許可範囲を表します。</summary>
/// <remarks>状態範囲を生成します。</remarks>
public readonly struct RegionStateRange(StateId initialStateId, StateId minimumStateId, StateId maximumStateId)
{
    /// <summary>安全な初期 State_Id です。</summary>
    public StateId InitialStateId { get; } = initialStateId;
    /// <summary>許可範囲の最小 State_Id です。</summary>
    public StateId MinimumStateId { get; } = minimumStateId;
    /// <summary>許可範囲の最大 State_Id です。</summary>
    public StateId MaximumStateId { get; } = maximumStateId;

    /// <summary>State_Id が範囲内かどうかを取得します。</summary>
    public bool Contains(StateId stateId) => stateId.IsValid
        && MinimumStateId.IsValid
        && MaximumStateId.IsValid
        && stateId.Value >= MinimumStateId.Value
        && stateId.Value <= MaximumStateId.Value;
}

/// <summary>Phase 1〜3 の子状態許可範囲を保持します。</summary>
/// <remarks>許可範囲を生成します。</remarks>
public sealed class PhaseChildStateRules(RegionStateRange phase1, RegionStateRange phase2, RegionStateRange phase3)
{
    private readonly RegionStateRange _phase1 = phase1;
    private readonly RegionStateRange _phase2 = phase2;
    private readonly RegionStateRange _phase3 = phase3;

    /// <summary>指定 Phase の許可範囲を取得します。</summary>
    public ErrorOr<RegionStateRange> Get(Phase phase)
    {
        return phase switch
        {
            Phase.Phase1 => _phase1,
            Phase.Phase2 => _phase2,
            Phase.Phase3 => _phase3,
            _ => Error.Validation("Region.InvalidPhase", $"子領域の許可範囲を取得できない Phase です: {phase}。")
        };
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
    TransitionReason Reason)
{
    /// <summary>ErrorOr 型が扱える共通エラー表現へ変換します。</summary>
    public Error ToError()
    {
        string description = $"{Region} 領域の遷移に失敗しました (原因: {Kind}, Phase: {Phase}, "
            + $"Current: {CurrentStateId.Value}, Requested: {RequestedStateId.Value}, "
            + $"Intermediate: {IntermediateStateId.Value}, SafeInitial: {SafeInitialStateId.Value}, Reason: {Reason.Kind})。";
        return Kind == RegionFailureKind.ChildOutOfRange
            ? Error.Validation("Region.ChildOutOfRange", description)
            : Error.Failure("Region.TransitionFailed", description);
    }

    /// <summary>共通エラー表現を取得します。</summary>
    public Error Error => ToError();

    /// <summary>領域失敗を共通エラーへ暗黙変換します。</summary>
    public static implicit operator Error(RegionFailure failure) => failure.ToError();
}

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

    /// <summary>状態識別子が実際に変更されたかどうかを取得します。</summary>
    public bool IsChanged => IsSuccess && PreviousStateId != CurrentStateId;

    /// <summary>この遷移に失敗情報が含まれるかどうかを取得します。</summary>
    public bool HasFailure => Failure.Kind != RegionFailureKind.None;
}

/// <summary>Phase、Movement、Attack の同一 frame 更新結果です。</summary>
public readonly record struct CompositeRegionResult(
    bool IsSuccess,
    bool IsPartialSuccess,
    bool ParentInvalid,
    RegionTransitionResult Movement,
    RegionTransitionResult Attack,
    RegionFailure Failure)
{
    /// <summary>いずれかの子領域で失敗が発生したかどうかを取得します。</summary>
    public bool HasFailure => Movement.HasFailure || Attack.HasFailure || Failure.Kind != RegionFailureKind.None;

    /// <summary>失敗した子領域を取得します。複数失敗時は検証順序の先頭です。</summary>
    public RegionKind FailedRegion => Failure.Kind == RegionFailureKind.None ? default : Failure.Region;
}
