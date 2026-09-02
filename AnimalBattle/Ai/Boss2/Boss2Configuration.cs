using System.Linq;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
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
    public IReadOnlyList<AttackId> Skills { get; }
    /// <summary>この Phase の攻撃順です（互換用）。</summary>
    public IReadOnlyList<AttackId> AttackOrder => Skills;
    /// <summary>この Phase で評価される攻撃意思決定ルールです。</summary>
    public IReadOnlyList<AttackDecisionRule> Rules { get; }

    /// <summary>Boss2 Phase 設定を生成します。</summary>
    public Boss2PhaseSettings(
        float idleDuration,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float maxFallSpeed,
        float patrolRadius,
        float transitionDuration,
        IReadOnlyList<AttackId> skills,
        IReadOnlyList<AttackDecisionRule>? rules = null)
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
    }

    /// <summary>Boss2 Phase 設定を生成します（互換用）。</summary>
    public Boss2PhaseSettings(
        float idleDuration,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float patrolRadius,
        float transitionDuration,
        IReadOnlyList<AttackId> skills,
        IReadOnlyList<AttackDecisionRule>? rules = null)
        : this(idleDuration, moveSpeed, jumpForce, gravity, 850.0f, patrolRadius, transitionDuration, skills, rules)
    {
    }
}

/// <summary>Boss2 固有の攻撃種別です。</summary>
public enum Boss2AttackKind
{
    /// <summary>重劈斬撃です。</summary>
    HeavySlash,
    /// <summary>二連重斬です。</summary>
    DoubleCleave,
    /// <summary>撼地踏撃です。</summary>
    GroundStomp,
    /// <summary>泰山圧頂です。</summary>
    LeapSmash,
    /// <summary>狂暴横衝直撞です。</summary>
    FrenzyCharge,
    /// <summary>滅地狂吼です。</summary>
    EarthquakeRoar
}

/// <summary>Boss2 攻撃の定義と固有パラメータです。</summary>
public sealed class Boss2AttackPattern
{
    /// <summary>攻撃 ID です。</summary>
    public AttackId AttackId { get; }
    /// <summary>Boss2 固有の攻撃種別です。</summary>
    public Boss2AttackKind Kind { get; }
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
        AttackId attackId,
        Boss2AttackKind kind,
        AttackDefinition definition,
        float moveSpeed = 0,
        float secondHitDelay = 0,
        float interval = 0)
    {
        AttackId = attackId;
        Kind = kind;
        Definition = definition;
        MoveSpeed = moveSpeed;
        SecondHitDelay = secondHitDelay;
        Interval = interval;
    }

    /// <summary>Boss2 攻撃 pattern を生成します。</summary>
    public Boss2AttackPattern(
        AttackId attackId,
        Boss2AttackKind kind,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float moveSpeed = 0,
        float secondHitDelay = 0,
        float interval = 0)
        : this(
            attackId,
            kind,
            new AttackDefinition(attackId, telegraphDuration, executeDuration, recoveryDuration, 0),
            moveSpeed,
            secondHitDelay,
            interval)
    {
    }

    /// <summary>設定値が実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid;
}

/// <summary>Boss2 State_Module が読む設定の注入境界です。</summary>
public interface IBoss2Configuration
{
    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);
    /// <summary>指定 Attack_Id の検証済み pattern を取得します。</summary>
    bool GetAttack(AttackId attackId, out Boss2AttackPattern pattern);
}

/// <summary>既存 Boss2 設定の検証済み既定 snapshot です。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration
{
    private readonly Boss2PhaseSettings[] _phases =
    [
        new Boss2PhaseSettings(0.8f, 160.0f, 580.0f, 980.0f, 400.0f, 1.0f, [Boss2AttackIds.HeavySlash, Boss2AttackIds.GroundStomp, Boss2AttackIds.HeavySlash, Boss2AttackIds.LeapSmash], Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase1)),
        new Boss2PhaseSettings(0.5f, 220.0f, 640.0f, 980.0f, 400.0f, 1.0f, [Boss2AttackIds.DoubleCleave, Boss2AttackIds.GroundStomp, Boss2AttackIds.LeapSmash, Boss2AttackIds.DoubleCleave], Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase2)),
        new Boss2PhaseSettings(0.3f, 280.0f, 700.0f, 980.0f, 400.0f, 1.2f, [Boss2AttackIds.FrenzyCharge, Boss2AttackIds.EarthquakeRoar, Boss2AttackIds.DoubleCleave, Boss2AttackIds.LeapSmash], Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase3))
    ];
    private readonly Boss2AttackPattern[] _attacks =
    [
        new Boss2AttackPattern(Boss2AttackIds.HeavySlash, Boss2AttackKind.HeavySlash, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.HeavySlash], 360.0f),
        new Boss2AttackPattern(Boss2AttackIds.DoubleCleave, Boss2AttackKind.DoubleCleave, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.DoubleCleave], 420.0f, 0.30f),
        new Boss2AttackPattern(Boss2AttackIds.GroundStomp, Boss2AttackKind.GroundStomp, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.GroundStomp]),
        new Boss2AttackPattern(Boss2AttackIds.LeapSmash, Boss2AttackKind.LeapSmash, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.LeapSmash], 680.0f),
        new Boss2AttackPattern(Boss2AttackIds.FrenzyCharge, Boss2AttackKind.FrenzyCharge, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.FrenzyCharge], 620.0f),
        new Boss2AttackPattern(Boss2AttackIds.EarthquakeRoar, Boss2AttackKind.EarthquakeRoar, Boss2PipelineDefinition.Pipeline.Attacks[Boss2AttackIds.EarthquakeRoar], interval: 0.25f)
    ];

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        int index = phase switch
        {
            Phase.Phase1 => 0,
            Phase.Phase2 => 1,
            Phase.Phase3 => 2,
            _ => 0
        };

        try
        {
            Boss2PhaseConfig tomlPhase = phase switch
            {
                Phase.Phase1 => ConfigManager.Boss2.Phase1,
                Phase.Phase2 => ConfigManager.Boss2.Phase2,
                Phase.Phase3 => ConfigManager.Boss2.Phase3,
                _ => ConfigManager.Boss2.Phase1
            };

            if (tomlPhase is not null && tomlPhase.IdleDuration > 0.0f)
            {
                AttackId[] skills = tomlPhase.Skills.Count > 0
                    ? tomlPhase.Skills.Select(ResolveAttackId).Where(id => id.IsValid).ToArray()
                    : _phases[index].Skills.ToArray();

                IReadOnlyList<AttackDecisionRule> rules = tomlPhase.Rules.Count > 0
                    ? tomlPhase.Rules.Select(r => new AttackDecisionRule(
                        ResolveAttackId(r.Skill),
                        r.Cooldown,
                        r.MinDistance,
                        r.MaxDistance,
                        r.Weight,
                        r.RequireSamePlatform)).Where(r => r.AttackId.IsValid).ToList()
                    : _phases[index].Rules;

                return new Boss2PhaseSettings(
                    tomlPhase.IdleDuration,
                    tomlPhase.MoveSpeed,
                    tomlPhase.JumpForce,
                    ConfigManager.Boss2.Gravity > 0 ? ConfigManager.Boss2.Gravity : _phases[index].Gravity,
                    ConfigManager.Boss2.PatrolRadius > 0 ? ConfigManager.Boss2.PatrolRadius : _phases[index].PatrolRadius,
                    tomlPhase.TransitionDuration,
                    skills.Length > 0 ? skills : _phases[index].Skills,
                    rules.Count > 0 ? rules : _phases[index].Rules);
            }
        }
        catch
        {
            // TOML 未ロード環境はフォールバック
        }

        return _phases[index];
    }

    /// <inheritdoc />
    public bool GetAttack(AttackId attackId, out Boss2AttackPattern pattern)
    {
        try
        {
            PatternConfig toml = ConfigManager.Pattern;
            if (toml is not null)
            {
                Boss2AttackPattern? configuredPattern = attackId switch
                {
                    _ when attackId == Boss2AttackIds.HeavySlash =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.HeavySlash, toml.HeavySlash.WindUpTime, toml.HeavySlash.CastDuration, toml.HeavySlash.RecoveryTime, 360.0f),
                    _ when attackId == Boss2AttackIds.DoubleCleave =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.DoubleCleave, toml.DoubleCleave.WindUpTime, toml.DoubleCleave.CastDuration, toml.DoubleCleave.RecoveryTime, 420.0f, 0.30f),
                    _ when attackId == Boss2AttackIds.GroundStomp =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.GroundStomp, toml.GroundStomp.WindUpTime, toml.GroundStomp.CastDuration, toml.GroundStomp.RecoveryTime),
                    _ when attackId == Boss2AttackIds.LeapSmash =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.LeapSmash, toml.LeapSmash.WindUpTime, toml.LeapSmash.CastDuration, toml.LeapSmash.RecoveryTime, toml.LeapSmash.JumpForce),
                    _ when attackId == Boss2AttackIds.FrenzyCharge =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.FrenzyCharge, toml.FrenzyCharge.WindUpTime, toml.FrenzyCharge.CastDuration, toml.FrenzyCharge.RecoveryTime, 620.0f),
                    _ when attackId == Boss2AttackIds.EarthquakeRoar =>
                        new Boss2AttackPattern(attackId, Boss2AttackKind.EarthquakeRoar, toml.EarthquakeRoar.WindUpTime, toml.EarthquakeRoar.CastDuration, toml.EarthquakeRoar.RecoveryTime, interval: 0.25f),
                    _ => null
                };

                if (configuredPattern is not null && configuredPattern.IsValid)
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
            if (_attacks[index].AttackId == attackId)
            {
                pattern = _attacks[index];
                return true;
            }
        }

        pattern = null!;
        return false;
    }

    private static AttackId ResolveAttackId(string skillName)
    {
        return skillName switch
        {
            "HeavySlash" => Boss2AttackIds.HeavySlash,
            "DoubleCleave" => Boss2AttackIds.DoubleCleave,
            "GroundStomp" => Boss2AttackIds.GroundStomp,
            "LeapSmash" => Boss2AttackIds.LeapSmash,
            "FrenzyCharge" => Boss2AttackIds.FrenzyCharge,
            "EarthquakeRoar" => Boss2AttackIds.EarthquakeRoar,
            _ => AttackId.Unknown
        };
    }
}
