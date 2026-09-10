using System.Collections.ObjectModel;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// ConfigManager の型付き設定を固定 BossState と BossSkill に解決する境界です。
/// </summary>
/// <remarks>
/// この型より内側へ ConfigManager、TOML の状態名、可変設定オブジェクトを渡しません。
/// 検証済み snapshot だけを公開し、検証に失敗した入力は現在の snapshot を変更しません。
/// </remarks>
public sealed class ConfigProvider : IConfigProvider

{
    private readonly IObservationService? _observation;
    private BossConfigSnapshot _current;
    private ConfigProviderResult _lastResult;

    /// <summary>現在有効な検証済み snapshot を取得します。</summary>
    public BossConfigSnapshot Current => _current;

    /// <summary>直近の検証結果を取得します。</summary>
    public ConfigProviderResult LastResult => _lastResult;

    /// <summary>
    /// ConfigManager の現在値を検証して provider を生成します。
    /// </summary>
    /// <param name="version">snapshot version。</param>
    /// <param name="sourceName">設定の入力元。</param>
    /// <param name="observation">設定診断を記録する任意の観測サービス。</param>
    /// <returns>検証結果を保持する provider。</returns>
    public static ConfigProvider FromConfigManager(
        long version = 0,
        string? sourceName = null,
        IObservationService? observation = null)
    {
        return new ConfigProvider(
            ConfigManager.Boss1,
            ConfigManager.Boss2,
            ConfigManager.Pattern,
            version,
            sourceName,
            observation);
    }

    /// <summary>ConfigManager の現在値を検証する provider を生成します。</summary>
    public ConfigProvider()
        : this(ConfigManager.Boss1, ConfigManager.Boss2, ConfigManager.Pattern)
    {
    }

    /// <summary>
    /// ConfigManager の型付き設定を検証して provider を生成します。
    /// </summary>
    /// <param name="boss1">Boss1 設定。</param>
    /// <param name="boss2">Boss2 設定。</param>
    /// <param name="pattern">攻撃パターン設定。</param>
    /// <param name="version">snapshot version。</param>
    /// <param name="sourceName">設定の入力元。</param>
    /// <param name="observation">設定診断を記録する任意の観測サービス。</param>
    public ConfigProvider(
        Boss1Config boss1,
        Boss2Config boss2,
        PatternConfig pattern,
        long version = 0,
        string? sourceName = null,
        IObservationService? observation = null)
    {
        ArgumentNullException.ThrowIfNull(boss1);
        ArgumentNullException.ThrowIfNull(boss2);
        ArgumentNullException.ThrowIfNull(pattern);
        _observation = observation;
        _current = BossConfigSnapshot.Empty;
        _lastResult = BuildSnapshot(
            boss1,
            boss2,
            pattern,
            version,
            sourceName,
            observation,
            out BossConfigSnapshot snapshot);
        if (_lastResult.IsSuccess)
        {
            _current = snapshot;
        }
    }

    /// <summary>
    /// 現在の snapshot を変更せずに新しい設定を検証し、成功時だけ公開します。
    /// </summary>
    /// <param name="boss1">Boss1 設定。</param>
    /// <param name="boss2">Boss2 設定。</param>
    /// <param name="pattern">攻撃パターン設定。</param>
    /// <param name="version">新しい snapshot version。</param>
    /// <param name="sourceName">設定の入力元。</param>
    /// <returns>検証に成功して snapshot を公開した場合は true。</returns>
    public bool Publish(
        Boss1Config boss1,
        Boss2Config boss2,
        PatternConfig pattern,
        long version = 0,
        string? sourceName = null)
    {
        ConfigProviderResult result = BuildSnapshot(
            boss1,
            boss2,
            pattern,
            version,
            sourceName,
            _observation,
            out BossConfigSnapshot snapshot);
        _lastResult = result;
        if (!result.IsSuccess)
        {
            return false;
        }

        _current = snapshot;
        return true;
    }

    /// <summary>標準 version で設定 snapshot を検証します。</summary>
    /// <param name="boss1">Boss1 設定。</param>
    /// <param name="boss2">Boss2 設定。</param>
    /// <param name="pattern">攻撃パターン設定。</param>
    /// <param name="snapshot">成功時に生成された snapshot。</param>
    /// <returns>検証結果。</returns>
    public static ConfigProviderResult BuildSnapshot(
        Boss1Config? boss1,
        Boss2Config? boss2,
        PatternConfig? pattern,
        out BossConfigSnapshot snapshot)
    {
        return BuildSnapshot(boss1, boss2, pattern, 0, null, null, out snapshot);
    }

    /// <summary>
    /// 型付き設定を検証して不変 snapshot を生成します。
    /// </summary>
    /// <param name="boss1">Boss1 設定。</param>
    /// <param name="boss2">Boss2 設定。</param>
    /// <param name="pattern">攻撃パターン設定。</param>
    /// <param name="version">snapshot version。</param>
    /// <param name="sourceName">設定の入力元。</param>
    /// <param name="observation">設定診断を記録する任意の観測サービス。</param>
    /// <param name="snapshot">成功時に生成された snapshot。</param>
    /// <returns>検証結果。</returns>
    public static ConfigProviderResult BuildSnapshot(
        Boss1Config? boss1,
        Boss2Config? boss2,
        PatternConfig? pattern,
        long version,
        string? sourceName,
        IObservationService? observation,
        out BossConfigSnapshot snapshot)
    {
        snapshot = BossConfigSnapshot.Empty;
        List<DiagnosticResult> diagnostics = new();

        if (version < 0)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "設定 version が負です。");
        }

        if (boss1 is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "Boss1 設定がありません。");
        }

        if (boss2 is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "Boss2 設定がありません。");
        }

        if (pattern is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "攻撃パターン設定がありません。");
        }

        if (diagnostics.Count > 0)
        {
            return FailureResult(diagnostics, observation);
        }

        ValidateBoss1(boss1!, diagnostics);
        ValidateBoss2(boss2!, diagnostics);
        ValidatePattern(pattern!, diagnostics);
        List<AttackDefinition> attacks = BuildAttackDefinitions(pattern!, diagnostics);
        ValidateFixedAttackMappings(diagnostics);
        ValidateRequiredDefinitions(attacks, diagnostics);

        if (diagnostics.Count > 0)
        {
            return FailureResult(diagnostics, observation);
        }

        List<BossPhaseSnapshot> phases = BuildPhaseSnapshots(boss1!, boss2!, diagnostics);
        if (diagnostics.Count > 0)
        {
            return FailureResult(diagnostics, observation);
        }

        snapshot = BossConfigSnapshot.CreateValidated(version, sourceName, phases, attacks);
        return ConfigProviderResult.Success(snapshot);
    }

    /// <summary>
    /// 固定 BossState 名を検証済み enum 値へ解決します。
    /// </summary>
    /// <param name="name">TOML 境界から受け取った状態名。</param>
    /// <param name="state">解決された固定 BossState。</param>
    /// <returns>具体的な固定状態へ解決できた場合は true。</returns>
    public static bool ResolveBossState(string? name, out BossState state)
    {
        if (string.IsNullOrWhiteSpace(name)
            || !Enum.TryParse(name, false, out state)
            || !state.IsValid()
            || !FixedBossStateModuleCatalog.Get(state, out _))
        {
            state = BossState.Unknown;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Boss 種別と TOML の攻撃名を固定 BossSkill へ解決します。
    /// </summary>
    /// <param name="bossType">攻撃を所有する Boss 種別。</param>
    /// <param name="name">TOML 境界から受け取った攻撃名。</param>
    /// <param name="skill">解決された固定 BossSkill。</param>
    /// <returns>具体的な攻撃へ解決できた場合は true。</returns>
    public static bool ResolveAttack(BossType bossType, string? name, out BossSkill skill)
    {
        skill = bossType switch
        {
            BossType.Boss1 => ResolveBoss1Attack(name),
            BossType.Boss2 => ResolveBoss2Attack(name),
            _ => BossSkill.None
        };

        return skill.IsValid() && AttackStateModuleMap.Get(skill, out AttackStateModuleMapping mapping)
            && mapping.BossType == bossType;
    }

    private static List<BossPhaseSnapshot> BuildPhaseSnapshots(
        Boss1Config boss1,
        Boss2Config boss2,
        List<DiagnosticResult> diagnostics)
    {
        List<BossPhaseSnapshot> phases = new();
        AddBoss1Phase(boss1.Phase1, Phase.Phase1, BossType.Boss1, phases, diagnostics);
        AddBoss1Phase(boss1.Phase2, Phase.Phase2, BossType.Boss1, phases, diagnostics);
        AddBoss1Phase(boss1.Phase3, Phase.Phase3, BossType.Boss1, phases, diagnostics);
        AddBoss2Phase(boss2.Phase1, Phase.Phase1, BossType.Boss2, phases, diagnostics);
        AddBoss2Phase(boss2.Phase2, Phase.Phase2, BossType.Boss2, phases, diagnostics);
        AddBoss2Phase(boss2.Phase3, Phase.Phase3, BossType.Boss2, phases, diagnostics);
        return phases;
    }

    private static void AddBoss1Phase(
        Boss1PhaseFsmConfig? config,
        Phase phase,
        BossType bossType,
        List<BossPhaseSnapshot> phases,
        List<DiagnosticResult> diagnostics)
    {
        if (config is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{bossType} の {phase} 設定がありません。", phase: phase);
            return;
        }

        List<BossSkill> attacks = ResolveSkillSequence(bossType, config.Skills, phase, diagnostics);
        phases.Add(new BossPhaseSnapshot(
            bossType,
            phase,
            config.HpThreshold,
            0,
            config.HoverSpeedX,
            0,
            config.HoverDuration,
            config.TransitionDuration,
            attacks));
    }

    private static void AddBoss2Phase(
        Boss2PhaseConfig? config,
        Phase phase,
        BossType bossType,
        List<BossPhaseSnapshot> phases,
        List<DiagnosticResult> diagnostics)
    {
        if (config is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{bossType} の {phase} 設定がありません。", phase: phase);
            return;
        }

        List<BossSkill> attacks = ResolveSkillSequence(bossType, config.Skills, phase, diagnostics);
        phases.Add(new BossPhaseSnapshot(
            bossType,
            phase,
            config.HpThreshold,
            config.IdleDuration,
            config.MoveSpeed,
            config.JumpForce,
            0,
            config.TransitionDuration,
            attacks));
    }

    private static List<BossSkill> ResolveSkillSequence(
        BossType bossType,
        IReadOnlyList<string>? skills,
        Phase phase,
        List<DiagnosticResult> diagnostics)
    {
        List<BossSkill> result = new();
        if (skills is null || skills.Count == 0)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{bossType} の {phase} に攻撃順序がありません。", phase: phase);
            return result;
        }

        for (int index = 0; index < skills.Count; index++)
        {
            string? skill = skills[index];
            if (!ResolveAttack(bossType, skill, out BossSkill resolvedSkill))
            {
                AddFailure(
                    diagnostics,
                    ErrorCode.InvalidBossSkill,
                    $"{bossType} の {phase} の攻撃名を解決できません: {skill ?? "(null)"}。",
                    resolvedSkill,
                    phase: phase);
                result.Add(BossSkill.None);
                continue;
            }

            result.Add(resolvedSkill);
        }

        return result;
    }

    private static void ValidatePattern(PatternConfig pattern, List<DiagnosticResult> diagnostics)
    {
        ValidatePatternTiming(diagnostics, BossSkill.Aimed3Shot, pattern.Aimed3Shot?.SkillName, pattern.Aimed3Shot?.WindUpTime ?? float.NaN, pattern.Aimed3Shot?.CastDuration ?? float.NaN, pattern.Aimed3Shot?.RecoveryTime ?? float.NaN, pattern.Aimed3Shot?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.RingBurst, pattern.RingBurst?.SkillName, pattern.RingBurst?.WindUpTime ?? float.NaN, pattern.RingBurst?.CastDuration ?? float.NaN, pattern.RingBurst?.RecoveryTime ?? float.NaN, pattern.RingBurst?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.DoubleSpiral, pattern.DoubleSpiral?.SkillName, pattern.DoubleSpiral?.WindUpTime ?? float.NaN, pattern.DoubleSpiral?.CastDuration ?? float.NaN, pattern.DoubleSpiral?.RecoveryTime ?? float.NaN, pattern.DoubleSpiral?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.RainDrop, pattern.RainDrop?.SkillName, pattern.RainDrop?.WindUpTime ?? float.NaN, pattern.RainDrop?.CastDuration ?? float.NaN, pattern.RainDrop?.RecoveryTime ?? float.NaN, pattern.RainDrop?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.CrossChaos, pattern.CrossChaos?.SkillName, pattern.CrossChaos?.WindUpTime ?? float.NaN, pattern.CrossChaos?.CastDuration ?? float.NaN, pattern.CrossChaos?.RecoveryTime ?? float.NaN, pattern.CrossChaos?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.FastSniper, pattern.FastSniper?.SkillName, pattern.FastSniper?.WindUpTime ?? float.NaN, pattern.FastSniper?.CastDuration ?? float.NaN, pattern.FastSniper?.RecoveryTime ?? float.NaN, pattern.FastSniper?.Interval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.DiveRam, pattern.DiveRam?.SkillName, pattern.DiveRam?.WindUpTime ?? float.NaN, pattern.DiveRam?.CastDuration ?? float.NaN, pattern.DiveRam?.RecoveryTime ?? float.NaN, 0);
        ValidatePatternTiming(diagnostics, BossSkill.HeavySlash, pattern.HeavySlash?.SkillName, pattern.HeavySlash?.WindUpTime ?? float.NaN, pattern.HeavySlash?.CastDuration ?? float.NaN, pattern.HeavySlash?.RecoveryTime ?? float.NaN, 0);
        ValidatePatternTiming(diagnostics, BossSkill.DoubleCleave, pattern.DoubleCleave?.SkillName, pattern.DoubleCleave?.WindUpTime ?? float.NaN, pattern.DoubleCleave?.CastDuration ?? float.NaN, pattern.DoubleCleave?.RecoveryTime ?? float.NaN, pattern.DoubleCleave?.SlashInterval ?? float.NaN);
        ValidatePatternTiming(diagnostics, BossSkill.GroundStomp, pattern.GroundStomp?.SkillName, pattern.GroundStomp?.WindUpTime ?? float.NaN, pattern.GroundStomp?.CastDuration ?? float.NaN, pattern.GroundStomp?.RecoveryTime ?? float.NaN, 0);
        ValidatePatternTiming(diagnostics, BossSkill.LeapSmash, pattern.LeapSmash?.SkillName, pattern.LeapSmash?.WindUpTime ?? float.NaN, pattern.LeapSmash?.CastDuration ?? float.NaN, pattern.LeapSmash?.RecoveryTime ?? float.NaN, 0);
        ValidatePatternTiming(diagnostics, BossSkill.FrenzyCharge, pattern.FrenzyCharge?.SkillName, pattern.FrenzyCharge?.WindUpTime ?? float.NaN, pattern.FrenzyCharge?.CastDuration ?? float.NaN, pattern.FrenzyCharge?.RecoveryTime ?? float.NaN, 0);
        ValidatePatternTiming(diagnostics, BossSkill.EarthquakeRoar, pattern.EarthquakeRoar?.SkillName, pattern.EarthquakeRoar?.WindUpTime ?? float.NaN, pattern.EarthquakeRoar?.CastDuration ?? float.NaN, pattern.EarthquakeRoar?.RecoveryTime ?? float.NaN, pattern.EarthquakeRoar?.Interval ?? float.NaN);
        ValidateNonNegative(diagnostics, pattern.RainDrop?.MinX ?? float.NaN, "RainDrop.MinX");
        ValidateNonNegative(diagnostics, pattern.RainDrop?.MaxX ?? float.NaN, "RainDrop.MaxX");
        ValidateNonNegative(diagnostics, pattern.EarthquakeRoar?.MinX ?? float.NaN, "EarthquakeRoar.MinX");
        ValidateNonNegative(diagnostics, pattern.EarthquakeRoar?.MaxX ?? float.NaN, "EarthquakeRoar.MaxX");
        if (pattern.RainDrop is not null && pattern.RainDrop.MinX > pattern.RainDrop.MaxX)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "RainDrop の範囲順序が不正です。", BossSkill.RainDrop);
        }

        if (pattern.EarthquakeRoar is not null && pattern.EarthquakeRoar.MinX > pattern.EarthquakeRoar.MaxX)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "EarthquakeRoar の範囲順序が不正です。", BossSkill.EarthquakeRoar);
        }
    }

    private static void ValidatePatternTiming(
        List<DiagnosticResult> diagnostics,
        BossSkill skill,
        string? skillName,
        float telegraphDuration,
        float executeDuration,
        float recoveryDuration,
        float interval)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"BossSkill {skill} の SkillName がありません。", skill);
        }

        ValidatePositive(diagnostics, telegraphDuration, $"BossSkill {skill}.TelegraphDuration");
        ValidatePositive(diagnostics, executeDuration, $"BossSkill {skill}.ExecuteDuration");
        ValidatePositive(diagnostics, recoveryDuration, $"BossSkill {skill}.RecoveryDuration");
        if (interval != 0)
        {
            ValidatePositive(diagnostics, interval, $"BossSkill {skill}.Interval");
        }
    }

    private static List<AttackDefinition> BuildAttackDefinitions(
        PatternConfig pattern,
        List<DiagnosticResult> diagnostics)
    {
        List<AttackDefinition> definitions = new();
        AddDefinition(definitions, diagnostics, BossSkill.Aimed3Shot, pattern.Aimed3Shot);
        AddDefinition(definitions, diagnostics, BossSkill.RingBurst, pattern.RingBurst);
        AddDefinition(definitions, diagnostics, BossSkill.DoubleSpiral, pattern.DoubleSpiral);
        AddDefinition(definitions, diagnostics, BossSkill.RainDrop, pattern.RainDrop);
        AddDefinition(definitions, diagnostics, BossSkill.CrossChaos, pattern.CrossChaos);
        AddDefinition(definitions, diagnostics, BossSkill.FastSniper, pattern.FastSniper);
        AddDefinition(definitions, diagnostics, BossSkill.DiveRam, pattern.DiveRam);
        AddDefinition(definitions, diagnostics, BossSkill.HeavySlash, pattern.HeavySlash);
        AddDefinition(definitions, diagnostics, BossSkill.DoubleCleave, pattern.DoubleCleave);
        AddDefinition(definitions, diagnostics, BossSkill.GroundStomp, pattern.GroundStomp);
        AddDefinition(definitions, diagnostics, BossSkill.LeapSmash, pattern.LeapSmash);
        AddDefinition(definitions, diagnostics, BossSkill.FrenzyCharge, pattern.FrenzyCharge);
        AddDefinition(definitions, diagnostics, BossSkill.EarthquakeRoar, pattern.EarthquakeRoar);
        return definitions;
    }

    private static void AddDefinition(
        List<AttackDefinition> definitions,
        List<DiagnosticResult> diagnostics,
        BossSkill skill,
        object? config)
    {
        if (config is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"BossSkill {skill} の設定がありません。", skill);
            return;
        }

        (float telegraph, float execute, float recovery, float actualRange) = config switch
        {
            Aimed3ShotConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            RingBurstConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            DoubleSpiralConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            RainDropConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            CrossChaosConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            FastSniperConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.BulletRadius),
            DiveRamConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.ImpactRadius),
            HeavySlashConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, MathF.Max(value.SlashRangeX, value.SlashRangeY)),
            DoubleCleaveConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, MathF.Max(value.SlashRangeX, value.SlashRangeY)),
            GroundStompConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.ShockwaveRadius),
            LeapSmashConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.ShrapnelRadius),
            FrenzyChargeConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.ShrapnelRadius),
            EarthquakeRoarConfig value => (value.WindUpTime, value.CastDuration, value.RecoveryTime, value.RockRadius),
            _ => (0, 0, 0, -1)
        };

        AttackDefinition definition = new(
            skill,
            telegraph,
            execute,
            recovery,
            actualRange);
        if (!definition.IsValid)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"BossSkill {skill} の時間または範囲が不正です。", skill);
            return;
        }

        definitions.Add(definition);
    }

    private static void ValidateRequiredDefinitions(
        IReadOnlyList<AttackDefinition> definitions,
        List<DiagnosticResult> diagnostics)
    {
        HashSet<BossSkill> ids = new();
        for (int index = 0; index < definitions.Count; index++)
        {
            AttackDefinition definition = definitions[index];
            if (!ids.Add(definition.Skill))
            {
                AddFailure(diagnostics, ErrorCode.InvalidBossSkill, $"BossSkill {definition.Skill} が重複しています。", definition.Skill);
            }
        }

        if (ids.Count != AttackStateModuleMap.Entries.Count)
        {
            AddFailure(diagnostics, ErrorCode.InvalidBossSkill, "全ての固定 BossSkill の設定が必要です。");
        }
    }

    private static void ValidateFixedAttackMappings(List<DiagnosticResult> diagnostics)
    {
        if (!AttackStateModuleMap.IsComplete(out string attackDiagnostic))
        {
            AddFailure(diagnostics, ErrorCode.InvalidBossSkill, attackDiagnostic);
            return;
        }


        for (int index = 0; index < AttackStateModuleMap.Entries.Count; index++)
        {
            AttackStateModuleMapping mapping = AttackStateModuleMap.Entries[index];
            if (!FixedBossStateModuleCatalog.Get(mapping.TelegraphState, out FixedBossStateModule telegraph)
                || telegraph.BossType != mapping.BossType
                || !FixedBossStateModuleCatalog.Get(mapping.ExecuteState, out FixedBossStateModule execute)
                || execute.BossType != mapping.BossType)
            {
                AddFailure(
                    diagnostics,
                    ErrorCode.UnregisteredState,
                    $"BossSkill {mapping.Skill} の具体的 module 対応がありません。",
                    mapping.Skill);
            }
        }
    }



    private static void ValidateBoss1(Boss1Config config, List<DiagnosticResult> diagnostics)
    {
        ValidatePositive(diagnostics, config.MaxHp, "Boss1.MaxHp");
        ValidateFinite(diagnostics, config.SpawnX, "Boss1.SpawnX");
        ValidateFinite(diagnostics, config.SpawnY, "Boss1.SpawnY");
        ValidatePositive(diagnostics, config.ScaleX, "Boss1.ScaleX");
        ValidatePositive(diagnostics, config.ScaleY, "Boss1.ScaleY");
        ValidateBoss1Phase(config.Phase1, Phase.Phase1, diagnostics);
        ValidateBoss1Phase(config.Phase2, Phase.Phase2, diagnostics);
        ValidateBoss1Phase(config.Phase3, Phase.Phase3, diagnostics);
        ValidateThresholdOrder(config.Phase1?.HpThreshold ?? float.NaN, config.Phase2?.HpThreshold ?? float.NaN, config.Phase3?.HpThreshold ?? float.NaN, diagnostics);
    }

    private static void ValidateBoss2(Boss2Config config, List<DiagnosticResult> diagnostics)
    {
        ValidatePositive(diagnostics, config.MaxHp, "Boss2.MaxHp");
        ValidateFinite(diagnostics, config.SpawnX, "Boss2.SpawnX");
        ValidateFinite(diagnostics, config.SpawnY, "Boss2.SpawnY");
        ValidatePositive(diagnostics, config.ScaleX, "Boss2.ScaleX");
        ValidatePositive(diagnostics, config.ScaleY, "Boss2.ScaleY");
        ValidatePositive(diagnostics, config.Gravity, "Boss2.Gravity");
        ValidatePositive(diagnostics, config.PatrolRadius, "Boss2.PatrolRadius");
        ValidateBoss2Phase(config.Phase1, Phase.Phase1, diagnostics);
        ValidateBoss2Phase(config.Phase2, Phase.Phase2, diagnostics);
        ValidateBoss2Phase(config.Phase3, Phase.Phase3, diagnostics);
        ValidateThresholdOrder(config.Phase1?.HpThreshold ?? float.NaN, config.Phase2?.HpThreshold ?? float.NaN, config.Phase3?.HpThreshold ?? float.NaN, diagnostics);
    }

    private static void ValidateBoss1Phase(Boss1PhaseFsmConfig? config, Phase phase, List<DiagnosticResult> diagnostics)
    {
        if (config is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"Boss1 の {phase} 設定がありません。", phase: phase);
            return;
        }

        ValidateThreshold(diagnostics, config.HpThreshold, $"Boss1.{phase}.HpThreshold", phase);
        ValidatePositive(diagnostics, config.HoverDuration, $"Boss1.{phase}.HoverDuration", phase);
        ValidatePositive(diagnostics, config.TransitionDuration, $"Boss1.{phase}.TransitionDuration", phase);
        ValidateRange(diagnostics, config.PlayerPursuitWeight, 0, 1, $"Boss1.{phase}.PlayerPursuitWeight", phase);
        ValidateRange(diagnostics, config.RecoverDelayRatio, 0, 1, $"Boss1.{phase}.RecoverDelayRatio", phase);
        ValidateNonNegative(diagnostics, config.HoverSpeedX, $"Boss1.{phase}.HoverSpeedX", phase);
        ValidateNonNegative(diagnostics, config.HoverSpeedY, $"Boss1.{phase}.HoverSpeedY", phase);
        ValidateNonNegative(diagnostics, config.HoverAmpX, $"Boss1.{phase}.HoverAmpX", phase);
        ValidateNonNegative(diagnostics, config.HoverAmpY, $"Boss1.{phase}.HoverAmpY", phase);
        ValidateSkillNames(BossType.Boss1, config.Skills, phase, diagnostics);
    }

    private static void ValidateBoss2Phase(Boss2PhaseConfig? config, Phase phase, List<DiagnosticResult> diagnostics)
    {
        if (config is null)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"Boss2 の {phase} 設定がありません。", phase: phase);
            return;
        }

        ValidateThreshold(diagnostics, config.HpThreshold, $"Boss2.{phase}.HpThreshold", phase);
        ValidatePositive(diagnostics, config.IdleDuration, $"Boss2.{phase}.IdleDuration", phase);
        ValidatePositive(diagnostics, config.TransitionDuration, $"Boss2.{phase}.TransitionDuration", phase);
        ValidateNonNegative(diagnostics, config.MoveSpeed, $"Boss2.{phase}.MoveSpeed", phase);
        ValidateNonNegative(diagnostics, config.JumpForce, $"Boss2.{phase}.JumpForce", phase);
        ValidateSkillNames(BossType.Boss2, config.Skills, phase, diagnostics);
    }

    private static void ValidateSkillNames(
        BossType bossType,
        IReadOnlyList<string>? skills,
        Phase phase,
        List<DiagnosticResult> diagnostics)
    {
        if (skills is null || skills.Count == 0)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{bossType} の {phase} に必須の Skills がありません。", phase: phase);
            return;
        }

        for (int index = 0; index < skills.Count; index++)
        {
            if (!ResolveAttack(bossType, skills[index], out BossSkill skill))
            {
                AddFailure(diagnostics, ErrorCode.InvalidBossSkill, $"{bossType} の {phase} の Skills に未知の攻撃があります。", skill, phase);
            }
        }

    }

    private static void ValidateThresholdOrder(float first, float second, float third, List<DiagnosticResult> diagnostics)
    {
        if (!IsFinite(first) || !IsFinite(second) || !IsFinite(third) || first <= second || second <= third)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, "Phase の HP 閾値の順序が不正です。");
        }
    }

    private static void ValidateThreshold(List<DiagnosticResult> diagnostics, float value, string name, Phase phase)
    {
        ValidateRange(diagnostics, value, 0, 1, name, phase);
    }

    private static void ValidatePositive(List<DiagnosticResult> diagnostics, float value, string name, Phase phase = Phase.Unknown)
    {
        if (!IsFinite(value) || value <= 0)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{name} は正の有限値である必要があります。", phase: phase);
        }
    }

    private static void ValidateNonNegative(List<DiagnosticResult> diagnostics, float value, string name, Phase phase = Phase.Unknown)
    {
        if (!IsFinite(value) || value < 0)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{name} は 0 以上の有限値である必要があります。", phase: phase);
        }
    }

    private static void ValidateFinite(List<DiagnosticResult> diagnostics, float value, string name)
    {
        if (!IsFinite(value))
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{name} は有限値である必要があります。");
        }
    }

    private static void ValidateRange(
        List<DiagnosticResult> diagnostics,
        float value,
        float minimum,
        float maximum,
        string name,
        Phase phase = Phase.Unknown)
    {
        if (!IsFinite(value) || value < minimum || value > maximum)
        {
            AddFailure(diagnostics, ErrorCode.InvalidConfiguration, $"{name} が有効範囲外です。", phase: phase);
        }
    }

    private static ConfigProviderResult FailureResult(
        List<DiagnosticResult> diagnostics,
        IObservationService? observation)
    {
        if (observation is not null)
        {
            for (int index = 0; index < diagnostics.Count; index++)
            {
                DiagnosticResult diagnostic = diagnostics[index];
                try
                {
                    ObservationEvent observationEvent = new(
                        diagnostic.EntityId,
                        BossType.Unknown,
                        diagnostic.State,
                        diagnostic.Phase,
                        LifecycleOperation.Update,
                        diagnostic.FrameUpdateNumber,
                        0,
                        diagnostic.Skill,
                        severity: diagnostic.Severity,
                        errorCode: diagnostic.ErrorCode,
                        message: diagnostic.Message);
                    observation.Record(in observationEvent);
                }
                catch
                {
                    // 診断記録の失敗で設定検証の結果を成功に変えません。
                }
            }
        }

        return ConfigProviderResult.Failure(diagnostics);
    }

    private static void AddFailure(
        List<DiagnosticResult> diagnostics,
        ErrorCode errorCode,
        string message,
        BossSkill skill = default,
        Phase phase = Phase.Unknown)
    {
        DiagnosticResult diagnostic = DiagnosticResult.Failure(
            DiagnosticSeverity.Warning,
            errorCode,
            message,
            skill: skill,
            phase: phase);
        diagnostics.Add(diagnostic);
    }

    private static BossSkill ResolveBoss1Attack(string? name)
    {
        return Enum.TryParse<BossSkill>(name, ignoreCase: true, out BossSkill skill) ? skill : BossSkill.None;
    }

    private static BossSkill ResolveBoss2Attack(string? name)
    {
        return Enum.TryParse<BossSkill>(name, ignoreCase: true, out BossSkill skill) ? skill : BossSkill.None;
    }



    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// ConfigProvider の検証結果と、失敗時の構造化診断を表します。
/// </summary>
public readonly struct ConfigProviderResult
{
    /// <summary>検証に成功したかどうかを取得します。</summary>
    public bool IsSuccess { get; }

    /// <summary>検証に失敗したかどうかを取得します。</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>成功時に公開された snapshot を取得します。</summary>
    public BossConfigSnapshot Snapshot { get; }

    /// <summary>検証で発生した診断を取得します。</summary>
    public IReadOnlyList<DiagnosticResult> Diagnostics { get; }

    private ConfigProviderResult(
        bool isSuccess,
        BossConfigSnapshot snapshot,
        IReadOnlyList<DiagnosticResult> diagnostics)
    {
        IsSuccess = isSuccess;
        Snapshot = snapshot;
        Diagnostics = diagnostics;
    }

    /// <summary>成功結果を生成します。</summary>
    /// <param name="snapshot">公開する検証済み snapshot。</param>
    /// <returns>成功結果。</returns>
    public static ConfigProviderResult Success(BossConfigSnapshot snapshot)
    {
        return new ConfigProviderResult(true, snapshot, Array.Empty<DiagnosticResult>());
    }

    /// <summary>失敗結果を生成します。</summary>
    /// <param name="diagnostics">検証診断。</param>
    /// <returns>失敗結果。</returns>
    public static ConfigProviderResult Failure(IReadOnlyList<DiagnosticResult> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        return new ConfigProviderResult(
            false,
            BossConfigSnapshot.Empty,
            new ReadOnlyCollection<DiagnosticResult>(diagnostics.ToArray()));
    }
}
