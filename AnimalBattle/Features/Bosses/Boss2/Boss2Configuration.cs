using AnimalBattle.Abilities.Attack;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の Phase ごとの検証済み移動・攻撃設定です。</summary>
/// <remarks>Boss2 Phase 設定を生成します。</remarks>
public readonly struct Boss2PhaseSettings(
    float idleDuration,
    float moveSpeed,
    float jumpForce,
    float gravity,
    float maxFallSpeed,
    float patrolRadius,
    float transitionDuration,
    IReadOnlyList<StringId> attackOrder,
    IReadOnlyList<AttackDecisionRule> rules,
    float maxAscentDuration,
    float landingRecoveryDuration,
    float takeoffDistance,
    float chaseTimeout) {
    /// <summary>攻撃開始までの待機時間です。</summary>
    public float IdleDuration { get; } = idleDuration;

    /// <summary>地上移動速度です。</summary>
    public float MoveSpeed { get; } = moveSpeed;

    /// <summary>跳躍初速の絶対値です。</summary>
    public float JumpForce { get; } = jumpForce;

    /// <summary>重力加速度です。</summary>
    public float Gravity { get; } = gravity;

    /// <summary>最大落下速度です。</summary>
    public float MaxFallSpeed { get; } = maxFallSpeed;

    /// <summary>巡回範囲半径です。</summary>
    public float PatrolRadius { get; } = patrolRadius;

    /// <summary>Phase 遷移演出時間です。</summary>
    public float TransitionDuration { get; } = transitionDuration;

    /// <summary>この Phase で実行可能な攻撃順序です。</summary>
    public IReadOnlyList<StringId> AttackOrder { get; } = attackOrder ?? [];

    /// <summary>この Phase で評価される攻撃意思決定ルールです。</summary>
    public IReadOnlyList<AttackDecisionRule> Rules { get; } = rules ?? [];

    /// <summary>跳躍上昇の最大許容時間秒単位です。</summary>
    public float MaxAscentDuration { get; } = maxAscentDuration;

    /// <summary>着地後の硬直待機時間秒単位です。</summary>
    public float LandingRecoveryDuration { get; } = landingRecoveryDuration;

    /// <summary>足場間跳躍への移行を開始する水平距離閾値：pxです。</summary>
    public float TakeoffDistance { get; } = takeoffDistance;

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでのタイムアウト時間秒単位です。</summary>
    public float ChaseTimeout { get; } = chaseTimeout;
}

/// <summary>Boss2 戦闘判定および追跡・反風等に関する設定値です。</summary>
public readonly struct Boss2CombatSettings(
    float collisionHeight,
    float jumpCooldownDuration,
    float takeoffVelocityThreshold,
    float moveThreshold,
    float samePlatformToleranceY,
    float airborneVelocityThreshold,
    float airborneHeightThreshold,
    Vec2f meleeTriggerRange,
    float antiKitingRangedDistance,
    float antiKitingMidDistance,
    float antiKitingEnforceMultiplier,
    float playerCollisionHeight) {
    /// <summary>衝突判定の高さ：pxです。</summary>
    public float CollisionHeight { get; } = collisionHeight;

    /// <summary>跳躍後の再跳躍冷却時間秒単位です。</summary>
    public float JumpCooldownDuration { get; } = jumpCooldownDuration;

    /// <summary>離陸速度閾値：px/sです。</summary>
    public float TakeoffVelocityThreshold { get; } = takeoffVelocityThreshold;

    /// <summary>水平移動判定の閾値：pxです。</summary>
    public float MoveThreshold { get; } = moveThreshold;

    /// <summary>同プラットフォーム判定の Y 許容誤差：pxです。</summary>
    public float SamePlatformToleranceY { get; } = samePlatformToleranceY;

    /// <summary>プレイヤー滞空判定の Y 速度閾値：px/sです。</summary>
    public float AirborneVelocityThreshold { get; } = airborneVelocityThreshold;

    /// <summary>プレイヤー滞空判定の高度差閾値：pxです。</summary>
    public float AirborneHeightThreshold { get; } = airborneHeightThreshold;

    /// <summary>近接技トリガーの判定範囲 Vec2f型 です。</summary>
    public Vec2f MeleeTriggerRange { get; } = meleeTriggerRange;

    /// <summary>反風等遠距離技の発動距離：pxです。</summary>
    public float AntiKitingRangedDistance { get; } = antiKitingRangedDistance;

    /// <summary>反風等中距離技の発動距離：pxです。</summary>
    public float AntiKitingMidDistance { get; } = antiKitingMidDistance;

    /// <summary>反風等強制発動タイムアウト倍率です。</summary>
    public float AntiKitingEnforceMultiplier { get; } = antiKitingEnforceMultiplier;

    /// <summary>プレイヤーの判定高さ：pxです。</summary>
    public float PlayerCollisionHeight { get; } = playerCollisionHeight;
}

/// <summary>Boss2 State_Module が読む設定の注入境界です。</summary>
public interface IBoss2Configuration {
    /// <summary>カイト対策近接技能タグを取得します。</summary>
    StringId AntiKitingMeleeSkillTag { get; }

    /// <summary>カイト対策遠隔技能タグを取得します。</summary>
    StringId AntiKitingRangedSkillTag { get; }

    /// <summary>Boss2 戦闘判定および反風等・衝突に関する設定を取得します。</summary>
    Boss2CombatSettings CombatSettings { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);

    /// <summary>技能タグの既定冷却時間を取得します。</summary>
    float GetSkillDefaultCooldown(StringId skill);

    /// <summary>Phase 遷移能力設定を取得します。</summary>
    PhaseTransitionAbilityConfig PhaseTransition { get; }
}

/// <summary>ワールドから設定を取得する Boss2 設定プロバイダです。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration {
    private readonly Flecs.NET.Core.World _world;

    private Boss2Config? _cachedConfig;
    private float _cachedPlayerHeight = -1f;
    private Boss2PhaseSettings _phase1Settings;
    private Boss2PhaseSettings _phase2Settings;
    private Boss2PhaseSettings _phase3Settings;
    private Boss2CombatSettings _cachedCombatSettings;
    private PhaseTransitionAbilityConfig _phaseTransition = default!;
    private StringId _antiKitingMeleeSkillTag;
    private StringId _antiKitingRangedSkillTag;

    public Boss2DefaultConfiguration(Flecs.NET.Core.World world)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        _world = world;
        EnsureCached(); // 初期化時にキャッシュ構築
    }

    /// <summary>設定インスタンス更新時のみキャッシュを再構築します。</summary>
    private void EnsureCached()
    {
        var toml = _world.Get<Boss2Config>();
        var playerConfig = _world.GetActivePlayerConfig();
        var playerHeight = playerConfig?.CollisionSize.Y ?? 0.0f;

        if (ReferenceEquals(_cachedConfig, toml) && MathF.Abs(_cachedPlayerHeight - playerHeight) < 0.001f)
        {
            return;
        }

        _cachedConfig = toml;
        _cachedPlayerHeight = playerHeight;
        _phaseTransition = toml.PhaseTransition;
        _antiKitingMeleeSkillTag = new(toml.AntiKitingMeleeSkill);
        _antiKitingRangedSkillTag = new(toml.AntiKitingRangedSkill);

        _phase1Settings = CreatePhaseSettings(toml, toml.Phase1);
        _phase2Settings = CreatePhaseSettings(toml, toml.Phase2);
        _phase3Settings = CreatePhaseSettings(toml, toml.Phase3);

        _cachedCombatSettings = new Boss2CombatSettings(
            toml.CollisionSize.Y,
            toml.JumpCooldownDuration,
            toml.TakeoffVelocityThreshold,
            toml.MoveThreshold,
            toml.SamePlatformToleranceY,
            toml.AirborneVelocityThreshold,
            toml.AirborneHeightThreshold,
            toml.MeleeTriggerRange,
            toml.AntiKitingRangedDistance,
            toml.AntiKitingMidDistance,
            toml.AntiKitingEnforceMultiplier,
            playerHeight);
    }

    private static Boss2PhaseSettings CreatePhaseSettings(Boss2Config toml, Boss2PhaseConfig phaseCfg)
    {
        StringId[] skills;
        if (phaseCfg.Skills is { Count: > 0 })
        {
            skills = new StringId[phaseCfg.Skills.Count];
            for (var i = 0; i < phaseCfg.Skills.Count; i++)
            {
                skills[i] = new StringId(phaseCfg.Skills[i]);
            }
        }
        else
        {
            skills = [];
        }

        var jumpForce = phaseCfg.JumpForce > 0 ? phaseCfg.JumpForce : toml.JumpForce;
        var chaseTimeout = phaseCfg.ChaseTimeout > 0 ? phaseCfg.ChaseTimeout : toml.ChaseTimeout;
        var rules = ConvertRules(phaseCfg.Rules);

        return new Boss2PhaseSettings(
            phaseCfg.IdleDuration,
            phaseCfg.MoveSpeed,
            jumpForce,
            toml.Gravity,
            toml.MaxFallSpeed,
            toml.PatrolRadius,
            phaseCfg.TransitionDuration,
            skills,
            rules,
            toml.MaxAscentDuration,
            toml.LandingRecoveryDuration,
            toml.TakeoffDistance,
            chaseTimeout);
    }

    private static IReadOnlyList<AttackDecisionRule> ConvertRules(List<AttackRuleConfig>? rules)
    {
        if (rules is not { Count: > 0 }) return [];
        var result = new AttackDecisionRule[rules.Count];
        for (var i = 0; i < rules.Count; i++)
        {
            result[i] = AttackDecisionRule.FromConfig(rules[i]);
        }
        return result;
    }

    /// <inheritdoc />
    public PhaseTransitionAbilityConfig PhaseTransition => _phaseTransition;

    /// <inheritdoc />
    public StringId AntiKitingMeleeSkillTag => _antiKitingMeleeSkillTag;

    /// <inheritdoc />
    public StringId AntiKitingRangedSkillTag => _antiKitingRangedSkillTag;

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        EnsureCached();
        return phase switch {
            Phase.Phase1 => _phase1Settings,
            Phase.Phase2 => _phase2Settings,
            Phase.Phase3 => _phase3Settings,
            _ => AssertHelper.FailWithDefault($"[Boss2Configuration 警告] 未知のフェーズ (PhaseConfig): {phase}", _phase1Settings)
        };
    }

    /// <inheritdoc />
    public Boss2CombatSettings CombatSettings => _cachedCombatSettings;

    /// <inheritdoc />
    public float GetSkillDefaultCooldown(StringId skill)
    {
        var toml = _world.Get<Boss2Config>();
        foreach (var rule in toml.Phase1.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;
        foreach (var rule in toml.Phase2.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;
        foreach (var rule in toml.Phase3.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;

        var pattern = _world.Get<Boss2PatternConfig>();
        return pattern.TryGetSkill(skill, out var s) ? s.DefaultCooldown : 0.0f;
    }
}
