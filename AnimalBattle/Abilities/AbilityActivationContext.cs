namespace AnimalBattle.Abilities;

/// <summary>
///     スキル発動時の実行コンテキストパラメータ
/// </summary>
public readonly struct AbilityActivationContext(float direction = 1.0f) {
    /// <summary>施法朝向（正: 右向き、負: 左向き）。</summary>
    public float Direction { get; } = direction;
}
