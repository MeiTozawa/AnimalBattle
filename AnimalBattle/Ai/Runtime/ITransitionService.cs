namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     状態遷移の検証とライフサイクル適用を行う境界です。
/// </summary>
public interface ITransitionService {
    /// <summary>遷移要求を検証します。</summary>
    TransitionResult Request(in TransitionRequest request);

    /// <summary>検証済み遷移を Exit、data commit、Enter の順で適用します。</summary>
    TransitionResult Apply(ref BossRuntimeSlot runtime, in TransitionRequest request);
}