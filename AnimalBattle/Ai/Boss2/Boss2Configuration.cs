using System.Diagnostics;
using AnimalBattle.Abilities;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using AnimalBattle.Core;

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

    /// <summary>跳躍上昇の最大許容時間（秒）です。</summary>
    public float MaxAscentDuration { get; } = maxAscentDuration;

    /// <summary>着地後の硬直待機時間（秒）です。</summary>
    public float LandingRecoveryDuration { get; } = landingRecoveryDuration;

    /// <summary>足場間跳躍への移行を開始する水平距離閾値（px）です。</summary>
    public float TakeoffDistance { get; } = takeoffDistance;

    /// <summary>プレイヤー追跡時に遠隔技へ移行するまでのタイムアウト時間（秒）です。</summary>
    public float ChaseTimeout { get; } = chaseTimeout;
}

/// <summary>Boss2 戦闘判定および追跡・反風筝に関する設定値です。</summary>
public readonly struct Boss2CombatSettings(
    float collisionHeight,
    float jumpCooldownDuration,
    float takeoffVelocityThreshold,
    float moveThreshold,
    float samePlatformToleranceY,
    float airborneVelocityThreshold,
    float airborneHeightThreshold,
    float meleeTriggerRangeX,
    float meleeTriggerRangeY,
    float antiKitingRangedDistance,
    float antiKitingMidDistance,
    float antiKitingEnforceMultiplier,
    float wallCrashStunDuration,
    float playerHitboxHeight) {
    /// <summary>衝突判定の高さ（px）です。</summary>
    public float CollisionHeight { get; } = collisionHeight;

    /// <summary>跳躍後の再跳躍冷却時間（秒）です。</summary>
    public float JumpCooldownDuration { get; } = jumpCooldownDuration;

    /// <summary>離陸速度閾値（px/s）です。</summary>
    public float TakeoffVelocityThreshold { get; } = takeoffVelocityThreshold;

    /// <summary>水平移動判定の閾値（px）です。</summary>
    public float MoveThreshold { get; } = moveThreshold;

    /// <summary>同プラットフォーム判定の Y 許容誤差（px）です。</summary>
    public float SamePlatformToleranceY { get; } = samePlatformToleranceY;

    /// <summary>プレイヤー滞空判定の Y 速度閾値（px/s）です。</summary>
    public float AirborneVelocityThreshold { get; } = airborneVelocityThreshold;

    /// <summary>プレイヤー滞空判定の高度差閾値（px）です。</summary>
    public float AirborneHeightThreshold { get; } = airborneHeightThreshold;

    /// <summary>近接技トリガーの X 範囲（px）です。</summary>
    public float MeleeTriggerRangeX { get; } = meleeTriggerRangeX;

    /// <summary>近接技トリガーの Y 範囲（px）です。</summary>
    public float MeleeTriggerRangeY { get; } = meleeTriggerRangeY;

    /// <summary>反風筝遠距離技の発動距離（px）です。</summary>
    public float AntiKitingRangedDistance { get; } = antiKitingRangedDistance;

    /// <summary>反風筝中距離技の発動距離（px）です。</summary>
    public float AntiKitingMidDistance { get; } = antiKitingMidDistance;

    /// <summary>反風筝強制発動タイムアウト倍率です。</summary>
    public float AntiKitingEnforceMultiplier { get; } = antiKitingEnforceMultiplier;

    /// <summary>壁激突時の気絶時間（秒）です。</summary>
    public float WallCrashStunDuration { get; } = wallCrashStunDuration;

    /// <summary>プレイヤーの判定高さ（px）です。</summary>
    public float PlayerHitboxHeight { get; } = playerHitboxHeight;
}

/// <summary>Boss2 State_Module が読む設定の注入境界です。</summary>
public interface IBoss2Configuration {
    /// <summary>カイト対策近接技能タグを取得します。</summary>
    StringId AntiKitingMeleeSkillTag { get; }

    /// <summary>カイト対策遠隔技能タグを取得します。</summary>
    StringId AntiKitingRangedSkillTag { get; }

    /// <summary>目標ロック・飛翔技能タグを取得します。</summary>
    StringId TargetLockSkillTag { get; }

    /// <summary>壁激突スタン技能タグを取得します。</summary>
    StringId WallStunSkillTag { get; }

    /// <summary>Boss2 戦闘判定および反風筝・衝突に関する設定を取得します。</summary>
    Boss2CombatSettings CombatSettings { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);

    /// <summary>技能タグの既定冷却時間を取得します。</summary>
    float GetSkillDefaultCooldown(StringId skill);
}

/// <summary>既存 Boss2 設定の検証済み既定 snapshot です。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration {
    /// <inheritdoc />
    public StringId AntiKitingMeleeSkillTag => new(ConfigManager.Boss2.AntiKitingMeleeSkill);

    /// <inheritdoc />
    public StringId AntiKitingRangedSkillTag => new(ConfigManager.Boss2.AntiKitingRangedSkill);

    /// <inheritdoc />
    public StringId TargetLockSkillTag => new(ConfigManager.Boss2.TargetLockSkill);

    /// <inheritdoc />
    public StringId WallStunSkillTag => new(ConfigManager.Boss2.WallStunSkill);

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        var toml = ConfigManager.Boss2;

        var phaseCfg = phase switch {
            Phase.Phase1 => toml.Phase1,
            Phase.Phase2 => toml.Phase2,
            Phase.Phase3 => toml.Phase3,
            _ => AssertHelper.FailWithDefault($"[Boss2Configuration 警告] 未知のフェーズ (PhaseConfig): {phase}", toml.Phase1)
        };

        // 配置文件里的技能名称转成 StringId，避免在代码中硬编码
        IReadOnlyList<StringId> skills = phaseCfg.Skills.Count > 0
            ? phaseCfg.Skills.Select(s => new StringId(s)).ToList()
            : [];

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

    private static IReadOnlyList<AttackDecisionRule> ConvertRules(List<AttackRuleConfig>? rules) =>
        rules?.Select(AttackDecisionRule.FromConfig).ToArray() ?? [];

    /// <inheritdoc />
    public Boss2CombatSettings CombatSettings {
        get
        {
            var toml = ConfigManager.Boss2;
            return new Boss2CombatSettings(
                toml.CollisionHeight,
                toml.JumpCooldownDuration,
                toml.TakeoffVelocityThreshold,
                toml.MoveThreshold,
                toml.SamePlatformToleranceY,
                toml.AirborneVelocityThreshold,
                toml.AirborneHeightThreshold,
                toml.MeleeTriggerRangeX,
                toml.MeleeTriggerRangeY,
                toml.AntiKitingRangedDistance,
                toml.AntiKitingMidDistance,
                toml.AntiKitingEnforceMultiplier,
                toml.WallCrashStunDuration,
                ConfigManager.Player.HitboxHeight);
        }
    }

    /// <inheritdoc />
    public float GetSkillDefaultCooldown(StringId skill)
    {
        var toml = ConfigManager.Boss2;
        foreach (var rule in toml.Phase1.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;
        foreach (var rule in toml.Phase2.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;
        foreach (var rule in toml.Phase3.Rules)
            if (skill == rule.Skill && rule.Cooldown > 0) return rule.Cooldown;

        var pattern = ConfigManager.Boss2Patterns;
        return pattern.TryGetSkill(skill, out var s) ? s.DefaultCooldown : 0.0f;
    }
}