using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;

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
    IReadOnlyList<BossSkill> attackOrder,
    IReadOnlyList<AttackDecisionRule>? rules = null,
    float maxAscentDuration = 0.80f,
    float landingRecoveryDuration = 0.10f,
    float takeoffDistance = 50.0f,
    float chaseTimeout = 3.0f) {
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
    public IReadOnlyList<BossSkill> AttackOrder { get; } = attackOrder ?? [];

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

/// <summary>Boss2 攻撃の定義と固有パラメータです。</summary>
/// <remarks>Boss2 攻撃 pattern を生成します。</remarks>
public sealed class Boss2AttackPattern(BossSkill skill, AttackDefinition definition) {
    /// <summary>技能識別子です。</summary>
    public BossSkill Skill { get; } = skill;

    /// <summary>共通 Telegraph/Execute/Recovery 定義です。</summary>
    public AttackDefinition Definition { get; } = definition;

    /// <summary>設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid;
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
    /// <summary>Boss2 戦闘判定および反風筝・衝突に関する設定を取得します。</summary>
    Boss2CombatSettings CombatSettings { get; }

    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);

    /// <summary>指定 BossSkill の検証済み pattern を取得します。</summary>
    bool GetAttack(BossSkill skill, out Boss2AttackPattern pattern);

    /// <summary>技能の既定冷却時間を取得します。</summary>
    float GetSkillDefaultCooldown(BossSkill skill);
}

/// <summary>既存 Boss2 設定の検証済み既定 snapshot です。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration {
    private readonly Boss2AttackPattern[] _attacks = [
        new(BossSkill.HeavySlash, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.HeavySlash]),
        new(BossSkill.DoubleCleave, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.DoubleCleave]),
        new(BossSkill.GroundStomp, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.GroundStomp]),
        new(BossSkill.LeapSmash, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.LeapSmash]),
        new(BossSkill.FrenzyCharge, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.FrenzyCharge]),
        new(BossSkill.EarthquakeRoar, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.EarthquakeRoar])
    ];

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        var toml = ConfigManager.Boss2;
        Boss2PipelineDefinition.Pipeline.TryGetLocomotion(phase, out var loco);

        var phaseCfg = phase switch {
            Phase.Phase1 => toml.Phase1,
            Phase.Phase2 => toml.Phase2,
            Phase.Phase3 => toml.Phase3,
            _ => toml.Phase1
        };

        IReadOnlyList<BossSkill> skills = phase switch {
            Phase.Phase1 => [BossSkill.HeavySlash, BossSkill.GroundStomp, BossSkill.HeavySlash, BossSkill.LeapSmash],
            Phase.Phase2 =>
                [BossSkill.DoubleCleave, BossSkill.GroundStomp, BossSkill.LeapSmash, BossSkill.DoubleCleave],
            Phase.Phase3 =>
                [BossSkill.FrenzyCharge, BossSkill.EarthquakeRoar, BossSkill.DoubleCleave, BossSkill.LeapSmash],
            _ => [BossSkill.HeavySlash]
        };

        var jumpForce = loco.JumpForce > 0 ? loco.JumpForce : phaseCfg.JumpForce;
        var gravity = loco.Gravity > 0 ? loco.Gravity : toml.Gravity;
        var maxFallSpeed = loco.MaxFallSpeed > 0 ? loco.MaxFallSpeed : 850.0f;
        var maxAscent = loco.MaxAscentDuration > 0 ? loco.MaxAscentDuration : 0.80f;
        var landingRecovery = loco.LandingRecoveryDuration > 0 ? loco.LandingRecoveryDuration : 0.10f;
        var takeoffDistance = loco.TakeoffDistance > 0 ? loco.TakeoffDistance : 50.0f;
        var chaseTimeout = phaseCfg.ChaseTimeout > 0 ? phaseCfg.ChaseTimeout :
            toml.ChaseTimeout > 0 ? toml.ChaseTimeout : 3.0f;

        return new Boss2PhaseSettings(
            phaseCfg.IdleDuration,
            phaseCfg.MoveSpeed,
            jumpForce,
            gravity,
            maxFallSpeed,
            toml.PatrolRadius,
            phaseCfg.TransitionDuration,
            skills,
            Boss2PipelineDefinition.Pipeline.GetRules(phase),
            maxAscent,
            landingRecovery,
            takeoffDistance,
            chaseTimeout);
    }

    /// <inheritdoc />
    public bool GetAttack(BossSkill skill, out Boss2AttackPattern pattern)
    {
        try
        {
            if (Boss2PipelineDefinition.Pipeline.TryGetAttack(skill, out var definition))
            {
                pattern = new Boss2AttackPattern(skill, definition);
                return true;
            }
        }
        catch (Exception ex)
        {
            // TOML 未ロード環境はフォールバック
            Debug.WriteLine($"[Boss2Configuration 警告] 攻撃パターンの取得中に例外が発生しました（フォールバックします）: {ex.Message}");
        }

        for (var index = 0; index < _attacks.Length; index++)
            if (_attacks[index].Skill == skill)
            {
                pattern = _attacks[index];
                return true;
            }

        pattern = null!;
        return false;
    }

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
    public float GetSkillDefaultCooldown(BossSkill skill)
    {
        var pattern = ConfigManager.Pattern;
        return skill switch {
            BossSkill.EarthquakeRoar => pattern.EarthquakeRoar.DefaultCooldown,
            BossSkill.LeapSmash => pattern.LeapSmash.DefaultCooldown,
            BossSkill.GroundStomp => pattern.GroundStomp.DefaultCooldown,
            BossSkill.DoubleCleave => pattern.DoubleCleave.DefaultCooldown,
            BossSkill.HeavySlash => pattern.HeavySlash.DefaultCooldown,
            BossSkill.FrenzyCharge => pattern.FrenzyCharge.DefaultCooldown,
            _ => 3.0f
        };
    }
}