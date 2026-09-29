using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;

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
    IReadOnlyList<BossSkill> attackOrder) {
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
    public IReadOnlyList<BossSkill> AttackOrder { get; } =
        attackOrder ?? throw new ArgumentNullException(nameof(attackOrder));
}

/// <summary>Boss1 攻撃の三段階と固有パラメータを不変に保持します。</summary>
/// <remarks>Boss1 攻撃 pattern を生成します。</remarks>
public sealed class Boss1AttackPattern(
    BossSkill skill,
    AttackDefinition definition,
    float interval = 0,
    float bulletSpeed = 0,
    int bulletCount = 0,
    float angleStep = 0,
    float minX = 0,
    float maxX = 0,
    float dashSpeed = 0,
    EaseType easeType = EaseType.Linear) {
    /// <summary>Boss1 攻撃 pattern を生成します。</summary>
    public Boss1AttackPattern(
        BossSkill skill,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float interval = 0,
        float bulletSpeed = 0,
        int bulletCount = 0,
        float angleStep = 0,
        float minX = 0,
        float maxX = 0,
        float dashSpeed = 0,
        EaseType easeType = EaseType.Linear)
        : this(
            skill,
            new AttackDefinition(skill, telegraphDuration, executeDuration, recoveryDuration, 0),
            interval,
            bulletSpeed,
            bulletCount,
            angleStep,
            minX,
            maxX,
            dashSpeed,
            easeType)
    {
    }

    /// <summary>安定した技能識別子です。</summary>
    public BossSkill Skill { get; } = skill;

    /// <summary>共通 Telegraph/Execute/Recovery 定義です。</summary>
    public AttackDefinition Definition { get; } = definition;

    /// <summary>連射間隔です。</summary>
    public float Interval { get; } = interval;

    /// <summary>弾速です。</summary>
    public float BulletSpeed { get; } = bulletSpeed;

    /// <summary>弾数です。</summary>
    public int BulletCount { get; } = bulletCount;

    /// <summary>角度変化量または拡散角です。</summary>
    public float AngleStep { get; } = angleStep;

    /// <summary>降雨弾の最小 X 座標です。</summary>
    public float MinX { get; } = minX;

    /// <summary>降雨弾の最大 X 座標です。</summary>
    public float MaxX { get; } = maxX;

    /// <summary>突進の設定速度倍率です。</summary>
    public float DashSpeed { get; } = dashSpeed;

    /// <summary>突進の easing 種別です。</summary>
    public EaseType EaseType { get; } = easeType;

    /// <summary>三段階と固有パラメータが実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid
     && (Skill == BossSkill.DiveRam || (Interval > 0 && BulletSpeed > 0))
     && (Skill != BossSkill.RainDrop || MaxX > MinX);
}

/// <summary>Boss1 State_Module が読む設定の注入境界です。</summary>
public interface IBoss1Configuration {
    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss1PhaseSettings GetPhase(Phase phase);

    /// <summary>指定技能の検証済み pattern を取得します。</summary>
    bool GetAttack(BossSkill skill, out Boss1AttackPattern pattern);
}

/// <summary>既存 boss1.toml と patterns.toml の既定値を保持する Boss1 設定です。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration {
    private readonly Boss1AttackPattern[] _attacks = [
        new(BossSkill.Aimed3Shot, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.Aimed3Shot], 0.45f, 372.0f, 3,
            0.16f),
        new(BossSkill.RingBurst, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.RingBurst], 0.50f, 228.0f, 16,
            0.25f),
        new(BossSkill.DoubleSpiral, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.DoubleSpiral], 0.075f, 288.0f, 2,
            0.22f),
        new(BossSkill.RainDrop, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.RainDrop], 0.32f, 312.0f, 2, 0,
            60.0f, 1220.0f),
        new(BossSkill.CrossChaos, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.CrossChaos], 0.10f, 312.0f, 4,
            0.18f),
        new(BossSkill.FastSniper, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.FastSniper], 0.30f, 510.0f, 1),
        new(BossSkill.DiveRam, Boss1PipelineDefinition.Pipeline.Attacks[BossSkill.DiveRam], dashSpeed: 960.0f,
            easeType: EaseType.InQuad)
    ];

    private readonly Boss1PhaseSettings[] _phases = [
        new(0.8f, 220.0f, 1.6f, 240.0f, 1.8f, 60.0f, 0.12f, 3.5f, 8.0f, 4.0f, 0.5f, 1.0f, 3.0f,
            [BossSkill.Aimed3Shot, BossSkill.DiveRam, BossSkill.RingBurst]),
        new(0.6f, 250.0f, 2.0f, 350.0f, 2.4f, 90.0f, 0.15f, 4.0f, 10.0f, 4.5f, 0.45f, 1.0f, 3.5f,
            [BossSkill.DoubleSpiral, BossSkill.DiveRam, BossSkill.RainDrop]),
        new(0.4f, 280.0f, 2.6f, 420.0f, 3.0f, 120.0f, 0.20f, 4.5f, 12.0f, 5.0f, 0.40f, 1.0f, 4.0f,
            [BossSkill.CrossChaos, BossSkill.DiveRam, BossSkill.FastSniper])
    ];

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        var index = (int)phase - 1;
        if (index < 0 || index >= _phases.Length) return _phases[0];

        try
        {
            var tomlPhase = phase switch {
                Phase.Phase1 => ConfigManager.Boss1.Phase1,
                Phase.Phase2 => ConfigManager.Boss1.Phase2,
                Phase.Phase3 => ConfigManager.Boss1.Phase3,
                _ => ConfigManager.Boss1.Phase1
            };

            if (tomlPhase is not null && tomlPhase.HoverDuration > 0.0f)
            {
                var order = tomlPhase.Skills.Count > 0
                    ? tomlPhase.Skills.Select(ResolveBossSkill).Where(id => id.IsValid()).ToArray()
                    : _phases[index].AttackOrder.ToArray();


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
                    order.Length > 0 ? order : _phases[index].AttackOrder);
            }
        }
        catch
        {
            // TOML 未ロード環境（単体テスト等）はデフォルト設定を返却
        }

        return _phases[index];
    }

    /// <inheritdoc />
    public bool GetAttack(BossSkill skill, out Boss1AttackPattern pattern)
    {
        try
        {
            var toml = ConfigManager.Pattern;
            if (toml is not null)
            {
                Boss1AttackPattern? configuredPattern = skill switch {
                    BossSkill.Aimed3Shot =>
                        new Boss1AttackPattern(skill, toml.Aimed3Shot.WindUpTime, toml.Aimed3Shot.CastDuration,
                            toml.Aimed3Shot.RecoveryTime, toml.Aimed3Shot.Interval, toml.Aimed3Shot.BulletSpeed,
                            toml.Aimed3Shot.BulletCount, toml.Aimed3Shot.SpreadAngle),

                    BossSkill.RingBurst =>
                        new Boss1AttackPattern(skill, toml.RingBurst.WindUpTime, toml.RingBurst.CastDuration,
                            toml.RingBurst.RecoveryTime, toml.RingBurst.Interval, toml.RingBurst.BulletSpeed,
                            toml.RingBurst.BulletCount, toml.RingBurst.AngleStep),

                    BossSkill.DoubleSpiral =>
                        new Boss1AttackPattern(skill, toml.DoubleSpiral.WindUpTime, toml.DoubleSpiral.CastDuration,
                            toml.DoubleSpiral.RecoveryTime, toml.DoubleSpiral.Interval, toml.DoubleSpiral.BulletSpeed,
                            2, toml.DoubleSpiral.AngleStep),

                    BossSkill.RainDrop =>
                        new Boss1AttackPattern(skill, toml.RainDrop.WindUpTime, toml.RainDrop.CastDuration,
                            toml.RainDrop.RecoveryTime, toml.RainDrop.Interval, toml.RainDrop.BulletSpeed, 2, 0.0f,
                            toml.RainDrop.MinX, toml.RainDrop.MaxX),

                    BossSkill.CrossChaos =>
                        new Boss1AttackPattern(skill, toml.CrossChaos.WindUpTime, toml.CrossChaos.CastDuration,
                            toml.CrossChaos.RecoveryTime, toml.CrossChaos.Interval, toml.CrossChaos.BulletSpeed, 4,
                            toml.CrossChaos.AngleStep),

                    BossSkill.FastSniper =>
                        new Boss1AttackPattern(skill, toml.FastSniper.WindUpTime, toml.FastSniper.CastDuration,
                            toml.FastSniper.RecoveryTime, toml.FastSniper.Interval, toml.FastSniper.BulletSpeed, 1),

                    BossSkill.DiveRam =>
                        new Boss1AttackPattern(skill, toml.DiveRam.WindUpTime, toml.DiveRam.CastDuration,
                            toml.DiveRam.RecoveryTime, dashSpeed: toml.DiveRam.DashSpeed,
                            easeType: toml.DiveRam.EaseType),

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

        for (var index = 0; index < _attacks.Length; index++)
            if (_attacks[index].Skill == skill)
            {
                pattern = _attacks[index];
                return true;
            }

        pattern = null!;
        return false;
    }

    private static BossSkill ResolveBossSkill(string skillName)
    {
        return Enum.TryParse(skillName, true, out BossSkill id) ? id : BossSkill.None;
    }
}