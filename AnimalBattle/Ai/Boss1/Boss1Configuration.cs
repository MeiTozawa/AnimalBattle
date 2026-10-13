using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の移動に必要な Phase 設定の不変 snapshot です。</summary>
/// <remarks>Boss1 Phase 設定を生成します。</remarks>
public readonly struct Boss1PhaseSettings(
    float hoverDuration,
    float hoverBaseY,
    float hoverSpeedX,
    float hoverAmpX,
    float hoverSpeedY,
    float hoverAmpY,
    float playerPursuitWeight,
    float hoverLerpSpeed,
    float telegraphShakeIntensity,
    float recoverFlybackSpeed,
    float recoverDelayRatio,
    float transitionDuration,
    float transitionFloatSpeed,
    IReadOnlyList<StringId> attackOrder) {
    /// <summary>Hover の継続時間です。</summary>
    public float HoverDuration { get; } = hoverDuration;

    /// <summary>Hover の基準 Y 座標です。</summary>
    public float HoverBaseY { get; } = hoverBaseY;

    /// <summary>水平移動の角速度です。</summary>
    public float HoverSpeedX { get; } = hoverSpeedX;

    /// <summary>水平移動の振幅です。</summary>
    public float HoverAmpX { get; } = hoverAmpX;

    /// <summary>垂直移動の角速度です。</summary>
    public float HoverSpeedY { get; } = hoverSpeedY;

    /// <summary>垂直移動の振幅です。</summary>
    public float HoverAmpY { get; } = hoverAmpY;

    /// <summary>プレイヤーへの追従重みです。</summary>
    public float PlayerPursuitWeight { get; } = playerPursuitWeight;

    /// <summary>座標追従の平滑補間速度です。</summary>
    public float HoverLerpSpeed { get; } = hoverLerpSpeed;

    /// <summary>突進予兆の振動量です。</summary>
    public float TelegraphShakeIntensity { get; } = telegraphShakeIntensity;

    /// <summary>攻撃後の帰還速度です。</summary>
    public float RecoverFlybackSpeed { get; } = recoverFlybackSpeed;

    /// <summary>帰還開始の Recovery 比率です。</summary>
    public float RecoverDelayRatio { get; } = recoverDelayRatio;

    /// <summary>Phase 遷移時間です。</summary>
    public float TransitionDuration { get; } = transitionDuration;

    /// <summary>Phase 遷移中の帰還速度です。</summary>
    public float TransitionFloatSpeed { get; } = transitionFloatSpeed;

    /// <summary>この Phase の技能順です。</summary>
    public IReadOnlyList<StringId> AttackOrder { get; } =
        attackOrder ?? throw new ArgumentNullException(nameof(attackOrder));
}

/// <summary>Boss1 State_Module が読む設定の注入境界です。</summary>
public interface IBoss1Configuration {
    /// <summary>急降下突進技能識別タグを取得します。</summary>
    StringId DiveSkillTag { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss1PhaseSettings GetPhase(Phase phase);

    /// <summary>指定技能タグのデフォルトクールダウン時間を取得します。</summary>
    float GetSkillDefaultCooldown(StringId skill);
}

/// <summary>既定値を保持する Boss1 設定です。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration {
    /// <inheritdoc />
    public StringId DiveSkillTag => new(ConfigManager.Boss1.DiveSkill);

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        var tomlPhase = phase switch {
            Phase.Phase1 => ConfigManager.Boss1.Phase1,
            Phase.Phase2 => ConfigManager.Boss1.Phase2,
            Phase.Phase3 => ConfigManager.Boss1.Phase3,
            _ => AssertHelper.FailWithDefault($"[Boss1Configuration 警告] 未知のフェーズ: {phase}", ConfigManager.Boss1.Phase1)
        };

        var order = tomlPhase.Skills.Count > 0
            ? tomlPhase.Skills.Select(s => new StringId(s)).Where(id => !id.IsEmpty).ToArray()
            : [];

        return new Boss1PhaseSettings(
            tomlPhase.HoverDuration,
            tomlPhase.HoverBaseY,
            tomlPhase.HoverSpeedX,
            tomlPhase.HoverAmpX,
            tomlPhase.HoverSpeedY,
            tomlPhase.HoverAmpY,
            tomlPhase.PlayerPursuitWeight,
            tomlPhase.HoverLerpSpeed,
            tomlPhase.TelegraphShakeIntensity,
            tomlPhase.RecoverFlybackSpeed,
            tomlPhase.RecoverDelayRatio,
            tomlPhase.TransitionDuration,
            tomlPhase.TransitionFloatSpeed,
            order);
    }

    /// <inheritdoc />
    public float GetSkillDefaultCooldown(StringId skill)
    {
        var toml = ConfigManager.Boss1Patterns;
        return toml.TryGetSkill(skill, out var skillConfig) ? skillConfig.DefaultCooldown : 0.0f;
    }
}