using System.Linq;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Ai.Boss1;

/// <summary>Boss1 の移動に必要な Phase 設定の不変 snapshot です。</summary>
public readonly struct Boss1PhaseSettings
{
    /// <summary>Hover の継続時間です。</summary>
    public float HoverDuration { get; }
    /// <summary>Hover の基準 Y 座標です。</summary>
    public float HoverBaseY { get; }
    /// <summary>水平移動の角速度です。</summary>
    public float HoverSpeedX { get; }
    /// <summary>水平移動の振幅です。</summary>
    public float HoverAmpX { get; }
    /// <summary>垂直移動の角速度です。</summary>
    public float HoverSpeedY { get; }
    /// <summary>垂直移動の振幅です。</summary>
    public float HoverAmpY { get; }
    /// <summary>プレイヤーへの追従重みです。</summary>
    public float PlayerPursuitWeight { get; }
    /// <summary>座標追従の平滑補間速度です。</summary>
    public float HoverLerpSpeed { get; }
    /// <summary>突進予兆の振動量です。</summary>
    public float TelegraphShakeIntensity { get; }
    /// <summary>攻撃後の帰還速度です。</summary>
    public float RecoverFlybackSpeed { get; }
    /// <summary>帰還開始の Recovery 比率です。</summary>
    public float RecoverDelayRatio { get; }
    /// <summary>Phase 遷移時間です。</summary>
    public float TransitionDuration { get; }
    /// <summary>Phase 遷移中の帰還速度です。</summary>
    public float TransitionFloatSpeed { get; }
    /// <summary>この Phase の攻撃順です。</summary>
    public IReadOnlyList<AttackId> AttackOrder { get; }

    /// <summary>Boss1 Phase 設定を生成します。</summary>
    public Boss1PhaseSettings(
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
        IReadOnlyList<AttackId> attackOrder)
    {
        HoverDuration = hoverDuration;
        HoverBaseY = hoverBaseY;
        HoverSpeedX = hoverSpeedX;
        HoverAmpX = hoverAmpX;
        HoverSpeedY = hoverSpeedY;
        HoverAmpY = hoverAmpY;
        PlayerPursuitWeight = playerPursuitWeight;
        HoverLerpSpeed = hoverLerpSpeed;
        TelegraphShakeIntensity = telegraphShakeIntensity;
        RecoverFlybackSpeed = recoverFlybackSpeed;
        RecoverDelayRatio = recoverDelayRatio;
        TransitionDuration = transitionDuration;
        TransitionFloatSpeed = transitionFloatSpeed;
        AttackOrder = attackOrder ?? throw new ArgumentNullException(nameof(attackOrder));
    }
}

/// <summary>Boss1 固有の攻撃種別です。</summary>
public enum Boss1AttackKind
{
    /// <summary>自機狙い三方向弾です。</summary>
    Aimed3Shot = 1,
    /// <summary>全方向リング弾です。</summary>
    RingBurst = 2,
    /// <summary>双螺旋弾です。</summary>
    DoubleSpiral = 3,
    /// <summary>降雨弾です。</summary>
    RainDrop = 4,
    /// <summary>交差波弾です。</summary>
    CrossChaos = 5,
    /// <summary>高速狙撃弾です。</summary>
    FastSniper = 6,
    /// <summary>俯衝突進です。</summary>
    DiveRam = 7
}

/// <summary>Boss1 攻撃の三段階と固有パラメータを不変に保持します。</summary>
public sealed class Boss1AttackPattern
{
    /// <summary>安定した攻撃 ID です。</summary>
    public AttackId AttackId { get; }
    /// <summary>Boss1 固有の攻撃種別です。</summary>
    public Boss1AttackKind Kind { get; }
    /// <summary>共通 Telegraph/Execute/Recovery 定義です。</summary>
    public AttackDefinition Definition { get; }
    /// <summary>連射間隔です。</summary>
    public float Interval { get; }
    /// <summary>弾速です。</summary>
    public float BulletSpeed { get; }
    /// <summary>弾数です。</summary>
    public int BulletCount { get; }
    /// <summary>角度変化量または拡散角です。</summary>
    public float AngleStep { get; }
    /// <summary>降雨弾の最小 X 座標です。</summary>
    public float MinX { get; }
    /// <summary>降雨弾の最大 X 座標です。</summary>
    public float MaxX { get; }
    /// <summary>突進の設定速度倍率です。</summary>
    public float DashSpeed { get; }
    /// <summary>突進の easing 種別です。</summary>
    public EaseType EaseType { get; }

    /// <summary>Boss1 攻撃 pattern を生成します。</summary>
    public Boss1AttackPattern(
        AttackId attackId,
        Boss1AttackKind kind,
        AttackDefinition definition,
        float interval = 0,
        float bulletSpeed = 0,
        int bulletCount = 0,
        float angleStep = 0,
        float minX = 0,
        float maxX = 0,
        float dashSpeed = 0,
        EaseType easeType = EaseType.Linear)
    {
        AttackId = attackId;
        Kind = kind;
        Definition = definition;
        Interval = interval;
        BulletSpeed = bulletSpeed;
        BulletCount = bulletCount;
        AngleStep = angleStep;
        MinX = minX;
        MaxX = maxX;
        DashSpeed = dashSpeed;
        EaseType = easeType;
    }

    /// <summary>Boss1 攻撃 pattern を生成します。</summary>
    public Boss1AttackPattern(
        AttackId attackId,
        Boss1AttackKind kind,
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
            attackId,
            kind,
            new AttackDefinition(attackId, telegraphDuration, executeDuration, recoveryDuration, 0),
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

    /// <summary>三段階と固有パラメータが実行可能かどうかを取得します。</summary>
    public bool IsValid => Definition.IsValid
        && (Kind == Boss1AttackKind.DiveRam || (Interval > 0 && BulletSpeed > 0))
        && (Kind != Boss1AttackKind.RainDrop || MaxX > MinX);
}

/// <summary>Boss1 State_Module が読む設定の注入境界です。</summary>
public interface IBoss1Configuration
{
    /// <summary>指定 Phase の検証済み設定を取得します。</summary>
    Boss1PhaseSettings GetPhase(Phase phase);
    /// <summary>指定 Attack_Id の検証済み pattern を取得します。</summary>
    bool GetAttack(AttackId attackId, out Boss1AttackPattern pattern);
}

/// <summary>既存 boss1.toml と patterns.toml の既定値を保持する Boss1 設定です。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration
{
    private readonly Boss1PhaseSettings[] _phases =
    [
        new Boss1PhaseSettings(0.8f, 220.0f, 1.6f, 240.0f, 1.8f, 60.0f, 0.12f, 3.5f, 8.0f, 4.0f, 0.5f, 1.0f, 3.0f, [Boss1AttackIds.Aimed3Shot, Boss1AttackIds.DiveRam, Boss1AttackIds.RingBurst]),
        new Boss1PhaseSettings(0.6f, 250.0f, 2.0f, 350.0f, 2.4f, 90.0f, 0.15f, 4.0f, 10.0f, 4.5f, 0.45f, 1.0f, 3.5f, [Boss1AttackIds.DoubleSpiral, Boss1AttackIds.DiveRam, Boss1AttackIds.RainDrop]),
        new Boss1PhaseSettings(0.4f, 280.0f, 2.6f, 420.0f, 3.0f, 120.0f, 0.20f, 4.5f, 12.0f, 5.0f, 0.40f, 1.0f, 4.0f, [Boss1AttackIds.CrossChaos, Boss1AttackIds.DiveRam, Boss1AttackIds.FastSniper])
    ];

    private readonly Boss1AttackPattern[] _attacks =
    [
        new Boss1AttackPattern(Boss1AttackIds.Aimed3Shot, Boss1AttackKind.Aimed3Shot, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.Aimed3Shot], 0.45f, 372.0f, 3, 0.16f),
        new Boss1AttackPattern(Boss1AttackIds.RingBurst, Boss1AttackKind.RingBurst, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.RingBurst], 0.50f, 228.0f, 16, 0.25f),
        new Boss1AttackPattern(Boss1AttackIds.DoubleSpiral, Boss1AttackKind.DoubleSpiral, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.DoubleSpiral], 0.075f, 288.0f, 2, 0.22f),
        new Boss1AttackPattern(Boss1AttackIds.RainDrop, Boss1AttackKind.RainDrop, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.RainDrop], 0.32f, 312.0f, 2, 0, 60.0f, 1220.0f),
        new Boss1AttackPattern(Boss1AttackIds.CrossChaos, Boss1AttackKind.CrossChaos, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.CrossChaos], 0.10f, 312.0f, 4, 0.18f),
        new Boss1AttackPattern(Boss1AttackIds.FastSniper, Boss1AttackKind.FastSniper, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.FastSniper], 0.30f, 510.0f, 1),
        new Boss1AttackPattern(Boss1AttackIds.DiveRam, Boss1AttackKind.DiveRam, Boss1PipelineDefinition.Pipeline.Attacks[Boss1AttackIds.DiveRam], dashSpeed: 960.0f, easeType: EaseType.InQuad)
    ];

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        int index = ((int)phase) - 1;
        if (index < 0 || index >= _phases.Length)
        {
            return _phases[0];
        }

        try
        {
            Boss1PhaseFsmConfig tomlPhase = phase switch
            {
                Phase.Phase1 => ConfigManager.Boss1.Phase1,
                Phase.Phase2 => ConfigManager.Boss1.Phase2,
                Phase.Phase3 => ConfigManager.Boss1.Phase3,
                _ => ConfigManager.Boss1.Phase1
            };

            if (tomlPhase is not null && tomlPhase.HoverDuration > 0.0f)
            {
                AttackId[] order = tomlPhase.Skills.Count > 0
                    ? tomlPhase.Skills.Select(ResolveAttackId).Where(id => id.IsValid).ToArray()
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
    public bool GetAttack(AttackId attackId, out Boss1AttackPattern pattern)
    {
        try
        {
            PatternConfig toml = ConfigManager.Pattern;
            if (toml is not null)
            {
                switch (attackId.Value)
                {
                    case 1: // Aimed3Shot
                        Aimed3ShotConfig a3 = toml.Aimed3Shot;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.Aimed3Shot, a3.WindUpTime, a3.CastDuration, a3.RecoveryTime, a3.Interval, a3.BulletSpeed, a3.BulletCount, a3.SpreadAngle);
                        return true;
                    case 2: // RingBurst
                        RingBurstConfig rb = toml.RingBurst;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.RingBurst, rb.WindUpTime, rb.CastDuration, rb.RecoveryTime, rb.Interval, rb.BulletSpeed, rb.BulletCount, rb.AngleStep);
                        return true;
                    case 3: // DoubleSpiral
                        DoubleSpiralConfig ds = toml.DoubleSpiral;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.DoubleSpiral, ds.WindUpTime, ds.CastDuration, ds.RecoveryTime, ds.Interval, ds.BulletSpeed, 2, ds.AngleStep);
                        return true;
                    case 4: // RainDrop
                        RainDropConfig rd = toml.RainDrop;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.RainDrop, rd.WindUpTime, rd.CastDuration, rd.RecoveryTime, rd.Interval, rd.BulletSpeed, 2, 0.0f, rd.MinX, rd.MaxX);
                        return true;
                    case 5: // CrossChaos
                        CrossChaosConfig cc = toml.CrossChaos;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.CrossChaos, cc.WindUpTime, cc.CastDuration, cc.RecoveryTime, cc.Interval, cc.BulletSpeed, 4, cc.AngleStep);
                        return true;
                    case 6: // FastSniper
                        FastSniperConfig fs = toml.FastSniper;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.FastSniper, fs.WindUpTime, fs.CastDuration, fs.RecoveryTime, fs.Interval, fs.BulletSpeed, 1);
                        return true;
                    case 7: // DiveRam
                        DiveRamConfig dr = toml.DiveRam;
                        pattern = new Boss1AttackPattern(attackId, Boss1AttackKind.DiveRam, dr.WindUpTime, dr.CastDuration, dr.RecoveryTime, dashSpeed: dr.DashSpeed, easeType: dr.EaseType);
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
            "Aimed3Shot" => Boss1AttackIds.Aimed3Shot,
            "RingBurst" => Boss1AttackIds.RingBurst,
            "DoubleSpiral" => Boss1AttackIds.DoubleSpiral,
            "RainDrop" => Boss1AttackIds.RainDrop,
            "CrossChaos" => Boss1AttackIds.CrossChaos,
            "FastSniper" => Boss1AttackIds.FastSniper,
            "DiveRam" => Boss1AttackIds.DiveRam,
            _ => AttackId.Unknown
        };
    }
}
