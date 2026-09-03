using System.Linq;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss2;

/// <summary>Boss2 の Phase ごとの検証済み移動・攻撃設定です。</summary>
public readonly struct Boss2PhaseSettings
{
    /// <summary>攻撃開始までの待機時間です。</summary>
    public float IdleDuration { get; }
    /// <summary>地上移動速度です。</summary>
    public float MoveSpeed { get; }
    /// <summary>跳躍初速の絶対値です。</summary>
    public float JumpForce { get; }
    /// <summary>重力加速度です。</summary>
    public float Gravity { get; }
    /// <summary>最大落下速度です。</summary>
    public float MaxFallSpeed { get; }
    /// <summary>巡回範囲半径です。</summary>
    public float PatrolRadius { get; }
    /// <summary>Phase 遷移演出時間です。</summary>
    public float TransitionDuration { get; }
    /// <summary>この Phase で実行可能な攻撃順序です。</summary>
    public IReadOnlyList<BossSkill> Skills { get; }
    /// <summary>この Phase の攻撃順です（互換用）。</summary>
    public IReadOnlyList<BossSkill> AttackOrder => Skills;
    /// <summary>この Phase で評価される攻撃意思決定ルールです。</summary>
    public IReadOnlyList<AttackDecisionRule> Rules { get; }
    /// <summary>跳躍上昇の最大許容時間（秒）です。</summary>
    public float MaxAscentDuration { get; }
    /// <summary>着地後の硬直待機時間（秒）です。</summary>
    public float LandingRecoveryDuration { get; }
    /// <summary>足場間跳躍への移行を開始する水平距離閾値（px）です。</summary>
    public float TakeoffDistance { get; }

    /// <summary>Boss2 Phase 設定を生成します。</summary>
    public Boss2PhaseSettings(
        float idleDuration,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float maxFallSpeed,
        float patrolRadius,
        float transitionDuration,
        IReadOnlyList<BossSkill> skills,
        IReadOnlyList<AttackDecisionRule>? rules = null,
        float maxAscentDuration = 0.80f,
        float landingRecoveryDuration = 0.10f,
        float takeoffDistance = 50.0f)
    {
        IdleDuration = idleDuration;
        MoveSpeed = moveSpeed;
        JumpForce = jumpForce;
        Gravity = gravity;
        MaxFallSpeed = maxFallSpeed;
        PatrolRadius = patrolRadius;
        TransitionDuration = transitionDuration;
        Skills = skills ?? [];
        Rules = rules ?? [];
        MaxAscentDuration = maxAscentDuration;
        LandingRecoveryDuration = landingRecoveryDuration;
        TakeoffDistance = takeoffDistance;
    }
}

/// <summary>Boss2 攻撃の定義と固有パラメータです。</summary>
public sealed class Boss2AttackPattern
{
    /// <summary>技能識別子です。</summary>
    public BossSkill Skill { get; }
    /// <summary>共通 Telegraph/Execute/Recovery 定義です。</summary>
    public AttackDefinition Definition { get; }
    /// <summary>移動速度です。</summary>
    public float MoveSpeed { get; }
    /// <summary>二段目判定の発生遅延です。</summary>
    public float SecondHitDelay { get; }
    /// <summary>多段発射の間隔です。</summary>
    public float Interval { get; }

    /// <summary>Boss2 攻撃 pattern を生成します。</summary>
    public Boss2AttackPattern(
        BossSkill skill,
        AttackDefinition definition,
        float moveSpeed = 0,
        float secondHitDelay = 0,
        float interval = 0)
    {
        Skill = skill;
        Definition = definition;
        MoveSpeed = moveSpeed;
        SecondHitDelay = secondHitDelay;
        Interval = interval;
    }

    /// <summary>設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid;
}

/// <summary>Boss2 State_Module が読む設定の注入境界です。</summary>
public interface IBoss2Configuration
{
    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);
    /// <summary>指定 BossSkill の検証済み pattern を取得します。</summary>
    bool GetAttack(BossSkill skill, out Boss2AttackPattern pattern);
}

/// <summary>既存 Boss2 設定の検証済み既定 snapshot です。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration
{
    private readonly Boss2AttackPattern[] _attacks =
    [
        new Boss2AttackPattern(BossSkill.HeavySlash, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.HeavySlash], 360.0f),
        new Boss2AttackPattern(BossSkill.DoubleCleave, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.DoubleCleave], 420.0f, 0.30f),
        new Boss2AttackPattern(BossSkill.GroundStomp, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.GroundStomp]),
        new Boss2AttackPattern(BossSkill.LeapSmash, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.LeapSmash], 680.0f),
        new Boss2AttackPattern(BossSkill.FrenzyCharge, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.FrenzyCharge], 620.0f),
        new Boss2AttackPattern(BossSkill.EarthquakeRoar, Boss2PipelineDefinition.Pipeline.Attacks[BossSkill.EarthquakeRoar], interval: 0.25f)
    ];

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        Boss2Config toml = ConfigManager.Boss2;
        Boss2PipelineDefinition.Pipeline.TryGetLocomotion(phase, out PhaseLocomotionSettings loco);

        Boss2PhaseConfig phaseCfg = phase switch
        {
            Phase.Phase1 => toml.Phase1,
            Phase.Phase2 => toml.Phase2,
            Phase.Phase3 => toml.Phase3,
            _ => toml.Phase1
        };

        IReadOnlyList<BossSkill> skills = phase switch
        {
            Phase.Phase1 => [BossSkill.HeavySlash, BossSkill.GroundStomp, BossSkill.HeavySlash, BossSkill.LeapSmash],
            Phase.Phase2 => [BossSkill.DoubleCleave, BossSkill.GroundStomp, BossSkill.LeapSmash, BossSkill.DoubleCleave],
            Phase.Phase3 => [BossSkill.FrenzyCharge, BossSkill.EarthquakeRoar, BossSkill.DoubleCleave, BossSkill.LeapSmash],
            _ => [BossSkill.HeavySlash]
        };

        float jumpForce = loco.JumpForce > 0 ? loco.JumpForce : phaseCfg.JumpForce;
        float gravity = loco.Gravity > 0 ? loco.Gravity : toml.Gravity;
        float maxFallSpeed = loco.MaxFallSpeed > 0 ? loco.MaxFallSpeed : 850.0f;
        float maxAscent = loco.MaxAscentDuration > 0 ? loco.MaxAscentDuration : 0.80f;
        float landingRecovery = loco.LandingRecoveryDuration > 0 ? loco.LandingRecoveryDuration : 0.10f;
        float takeoffDistance = loco.TakeoffDistance > 0 ? loco.TakeoffDistance : 50.0f;

        return new Boss2PhaseSettings(
            idleDuration: phaseCfg.IdleDuration,
            moveSpeed: phaseCfg.MoveSpeed,
            jumpForce: jumpForce,
            gravity: gravity,
            maxFallSpeed: maxFallSpeed,
            patrolRadius: toml.PatrolRadius,
            transitionDuration: phaseCfg.TransitionDuration,
            skills: skills,
            rules: Boss2PipelineDefinition.Pipeline.GetRules(phase),
            maxAscentDuration: maxAscent,
            landingRecoveryDuration: landingRecovery,
            takeoffDistance: takeoffDistance);
    }

    /// <inheritdoc />
    public bool GetAttack(BossSkill skill, out Boss2AttackPattern pattern)
    {
        try
        {
            PatternConfig toml = ConfigManager.Pattern;
            if (toml is not null && Boss2PipelineDefinition.Pipeline.TryGetAttack(skill, out AttackDefinition definition))
            {
                Boss2AttackPattern? configuredPattern = skill switch
                {
                    BossSkill.HeavySlash =>
                        new(skill, definition, toml.HeavySlash.DashSpeed),

                    BossSkill.DoubleCleave =>
                        new(skill, definition, toml.DoubleCleave.DashSpeed, toml.DoubleCleave.SlashInterval),

                    BossSkill.GroundStomp =>
                        new(skill, definition),

                    BossSkill.LeapSmash =>
                        new(skill, definition, toml.LeapSmash.JumpForce),

                    BossSkill.FrenzyCharge =>
                        new(skill, definition, toml.FrenzyCharge.ChargeSpeed),

                    BossSkill.EarthquakeRoar =>
                        new(skill, definition, interval: toml.EarthquakeRoar.Interval),

                    _ => null
                };

                if (configuredPattern is { IsValid: true })
                {
                    pattern = configuredPattern;
                    return true;
                }
            }
        }
        catch
        {
            // TOML 未ロード環境はフォールバック
        }

        for (int index = 0; index < _attacks.Length; index++)
        {
            if (_attacks[index].Skill == skill)
            {
                pattern = _attacks[index];
                return true;
            }
        }

        pattern = null!;
        return false;
    }
}


