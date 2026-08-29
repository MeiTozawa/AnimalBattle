using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
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
    {
        AttackId = attackId;
        Kind = kind;
        Definition = new AttackDefinition(attackId, telegraphDuration, executeDuration, recoveryDuration, 0);
        Interval = interval;
        BulletSpeed = bulletSpeed;
        BulletCount = bulletCount;
        AngleStep = angleStep;
        MinX = minX;
        MaxX = maxX;
        DashSpeed = dashSpeed;
        EaseType = easeType;
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
    bool TryGetAttack(AttackId attackId, out Boss1AttackPattern pattern);
}

/// <summary>既存 boss1.toml と patterns.toml の既定値を保持する Boss1 設定です。</summary>
public sealed class Boss1DefaultConfiguration : IBoss1Configuration
{
    private readonly Boss1PhaseSettings[] _phases;
    private readonly Boss1AttackPattern[] _attacks;

    /// <summary>既存設定と同じ値で Boss1 設定を初期化します。</summary>
    public Boss1DefaultConfiguration()
    {
        _phases =
        [
            new Boss1PhaseSettings(0.8f, 220.0f, 1.6f, 240.0f, 1.8f, 60.0f, 0.12f, 8.0f, 4.0f, 0.5f, 1.0f, 3.0f, [Boss1AttackIds.Aimed3Shot, Boss1AttackIds.DiveRam, Boss1AttackIds.RingBurst]),
            new Boss1PhaseSettings(0.6f, 250.0f, 2.0f, 350.0f, 2.4f, 90.0f, 0.15f, 10.0f, 4.5f, 0.45f, 1.0f, 3.5f, [Boss1AttackIds.DoubleSpiral, Boss1AttackIds.DiveRam, Boss1AttackIds.RainDrop]),
            new Boss1PhaseSettings(0.4f, 280.0f, 2.6f, 420.0f, 3.0f, 120.0f, 0.20f, 12.0f, 5.0f, 0.40f, 1.0f, 4.0f, [Boss1AttackIds.CrossChaos, Boss1AttackIds.DiveRam, Boss1AttackIds.FastSniper])
        ];

        _attacks =
        [
            new Boss1AttackPattern(Boss1AttackIds.Aimed3Shot, Boss1AttackKind.Aimed3Shot, 0.40f, 0.90f, 0.50f, 0.45f, 372.0f, 3, 0.16f),
            new Boss1AttackPattern(Boss1AttackIds.RingBurst, Boss1AttackKind.RingBurst, 0.55f, 1.00f, 0.70f, 0.50f, 228.0f, 16, 0.25f),
            new Boss1AttackPattern(Boss1AttackIds.DoubleSpiral, Boss1AttackKind.DoubleSpiral, 0.45f, 1.60f, 0.60f, 0.075f, 288.0f, 2, 0.22f),
            new Boss1AttackPattern(Boss1AttackIds.RainDrop, Boss1AttackKind.RainDrop, 0.35f, 1.80f, 0.80f, 0.32f, 312.0f, 2, 0, 60.0f, 1220.0f),
            new Boss1AttackPattern(Boss1AttackIds.CrossChaos, Boss1AttackKind.CrossChaos, 0.35f, 2.00f, 0.50f, 0.10f, 312.0f, 4, 0.18f),
            new Boss1AttackPattern(Boss1AttackIds.FastSniper, Boss1AttackKind.FastSniper, 0.50f, 0.60f, 0.60f, 0.30f, 510.0f, 1),
            new Boss1AttackPattern(Boss1AttackIds.DiveRam, Boss1AttackKind.DiveRam, 0.55f, 0.45f, 0.65f, dashSpeed: 960.0f, easeType: EaseType.InQuad)
        ];
    }

    /// <inheritdoc />
    public Boss1PhaseSettings GetPhase(Phase phase)
    {
        int index = phase switch
        {
            Phase.Phase1 => 0,
            Phase.Phase2 => 1,
            Phase.Phase3 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "Boss1 の Phase が不正です。")
        };
        return _phases[index];
    }

    /// <inheritdoc />
    public bool TryGetAttack(AttackId attackId, out Boss1AttackPattern pattern)
    {
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
}
