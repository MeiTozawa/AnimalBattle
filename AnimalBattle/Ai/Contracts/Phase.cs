namespace AnimalBattle.Ai.Contracts;

/// <summary>
///     Boss AI の親 Phase 領域を定義します。
/// </summary>
public enum Phase {
    /// <summary>
    ///     Phase が未指定であることを示します。
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     第 1 Phase を示します。
    /// </summary>
    Phase1 = 1,

    /// <summary>
    ///     第 2 Phase を示します。
    /// </summary>
    Phase2 = 2,

    /// <summary>
    ///     第 3 Phase を示します。
    /// </summary>
    Phase3 = 3,

    /// <summary>
    ///     Phase の切り替え処理中であることを示します。
    /// </summary>
    PhaseTransition = 4,

    /// <summary>
    ///     Boss が終端の死亡状態であることを示します。
    /// </summary>
    Dead = 5
}