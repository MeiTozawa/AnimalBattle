using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の移動に必要な Phase 設定の不変 snapshot です。</summary>
/// <remarks>Boss1 Phase 設定を生成します。</remarks>
public readonly struct Boss1PhaseSettings(
    float hoverDuration,
    float hoverBaseY,
    Vec2f hoverSpeed,
    Vec2f hoverAmp,
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

    /// <summary>浮遊移動の角速度です。</summary>
    public Vec2f HoverSpeed { get; } = hoverSpeed;

    /// <summary>浮遊移動の振幅です。</summary>
    public Vec2f HoverAmp { get; } = hoverAmp;

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
        attackOrder ?? AssertHelper.FailWithDefault("[Boss1PhaseSettings 警告] attackOrder が null です。空配列にフォールバックします。", Array.Empty<StringId>());
}

/// <summary>Boss1 State_Module が読む設定の注入境界です。</summary>
public interface IBoss1Configuration {
    /// <summary>急降下突進技能識別タグを取得します。</summary>
    StringId DiveSkillTag { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss1PhaseSettings GetPhase(Phase phase);

    /// <summary>指定技能タグのデフォルトクールダウン時間を取得します。</summary>
    float GetSkillDefaultCooldown(StringId skill);

    /// <summary>Phase 遷移能力設定を取得します。</summary>
    PhaseTransitionAbilityConfig PhaseTransition { get; }
}

/// <summary>ワールドから設定を取得する Boss1 設定プロバイダです。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration {
    private readonly World _world;

    private Boss1Config? _cachedConfig;
    private Boss1PhaseSettings _phase1Settings;
    private Boss1PhaseSettings _phase2Settings;
    private Boss1PhaseSettings _phase3Settings;
    private StringId _diveSkillTag;
    private PhaseTransitionAbilityConfig _phaseTransition = default!;

    public Boss1DefaultConfiguration(World world)
    {
        System.Diagnostics.Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        _world = world;
        EnsureCached(); // 初期化時にキャッシュ構築
    }

    /// <summary>設定インスタンス更新時のみキャッシュを再構築します。</summary>
    private void EnsureCached()
    {
        var b1Cfg = _world.Get<Boss1Config>();
        if (ReferenceEquals(_cachedConfig, b1Cfg)) return;

        _cachedConfig = b1Cfg;
        _diveSkillTag = new StringId(b1Cfg.DiveSkill);
        _phaseTransition = b1Cfg.PhaseTransition;
        _phase1Settings = CreatePhaseSettings(b1Cfg.Phase1);
        _phase2Settings = CreatePhaseSettings(b1Cfg.Phase2);
        _phase3Settings = CreatePhaseSettings(b1Cfg.Phase3);
    }

    private static Boss1PhaseSettings CreatePhaseSettings(Boss1PhaseFsmConfig tomlPhase)
    {
        StringId[] order;
        if (tomlPhase.Skills is { Count: > 0 })
        {
            var validCount = 0;
            var temp = new StringId[tomlPhase.Skills.Count];
            foreach (var t in tomlPhase.Skills)
            {
                var id = new StringId(t);
                if (!id.IsEmpty)
                {
                    temp[validCount++] = id;
                }
            }

            if (validCount == temp.Length)
            {
                order = temp;
            }
            else
            {
                order = new StringId[validCount];
                Array.Copy(temp, order, validCount);
            }
        }
        else
        {
            order = [];
        }

        return new Boss1PhaseSettings(
            tomlPhase.HoverDuration,
            tomlPhase.HoverBaseY,
            tomlPhase.HoverSpeed,
            tomlPhase.HoverAmp,
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
    public StringId DiveSkillTag => _diveSkillTag;

    /// <inheritdoc />
    public PhaseTransitionAbilityConfig PhaseTransition => _phaseTransition;

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        EnsureCached();
        return phase switch {
            Phase.Phase1 => _phase1Settings,
            Phase.Phase2 => _phase2Settings,
            Phase.Phase3 => _phase3Settings,
            _ => AssertHelper.FailWithDefault($"[Boss1Configuration 警告] 未知のフェーズ: {phase}", _phase1Settings)
        };
    }

    /// <inheritdoc />
    public float GetSkillDefaultCooldown(StringId skill)
    {
        var toml = _world.Get<Boss1PatternConfig>();
        return toml.TryGetSkill(skill, out var skillConfig) ? skillConfig.DefaultCooldown : 0.0f;
    }
}
