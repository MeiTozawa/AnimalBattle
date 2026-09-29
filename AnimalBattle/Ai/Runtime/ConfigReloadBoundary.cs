namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     検証済み設定 snapshot を pending から active へ反映できる lifecycle 境界です。
/// </summary>
public enum ConfigReloadBoundary {
    /// <summary>状態遷移または攻撃選択以外の境界です。</summary>
    None = 0,

    /// <summary>現在 module の Exit 後、次 module の Enter 前の状態遷移境界です。</summary>
    Transition = 1,

    /// <summary>次の攻撃を選択する境界です。</summary>
    AttackSelection = 2
}