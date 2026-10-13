namespace AnimalBattle.Abilities;

/// <summary>
///     技能激活时的执行上下文参数（施法朝向等）。
/// </summary>
public readonly struct AbilityActivationContext(float direction = 1.0f) {
    /// <summary>施法朝向（正: 右向き、負: 左向き）。</summary>
    public float Direction { get; } = direction;
}
