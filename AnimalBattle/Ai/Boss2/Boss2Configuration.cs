using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;

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
    /// <summary>Phase 切り替え時間です。</summary>
    public float TransitionDuration { get; }
    /// <summary>この Phase の攻撃順です（旧仕様との互換用）。</summary>
    public IReadOnlyList<AttackId> AttackOrder { get; }
    /// <summary>この Phase で評価される技能意思決定ルールリストです。</summary>
    public IReadOnlyList<AttackDecisionRule> Rules { get; }

    /// <summary>Boss2 Phase 設定を生成します。</summary>
    public Boss2PhaseSettings(
        float idleDuration,
        float moveSpeed,
        float jumpForce,
        float gravity,
        float maxFallSpeed,
        float transitionDuration,
        IReadOnlyList<AttackId> attackOrder,
        IReadOnlyList<AttackDecisionRule>? rules = null)
    {
        IdleDuration = idleDuration;
        MoveSpeed = moveSpeed;
        JumpForce = jumpForce;
        Gravity = gravity;
        MaxFallSpeed = maxFallSpeed;
        TransitionDuration = transitionDuration;
        AttackOrder = attackOrder ?? throw new ArgumentNullException(nameof(attackOrder));
        Rules = rules ?? [];
    }
}

/// <summary>Boss2 固有の攻撃種別です。</summary>
public enum Boss2AttackKind
{
    /// <summary>重劈斬撃です。</summary>
    HeavySlash = 1,
    /// <summary>二連重斬です。</summary>
    DoubleCleave = 2,
    /// <summary>撼地踏撃です。</summary>
    GroundStomp = 3,
    /// <summary>泰山圧頂です。</summary>
    LeapSmash = 4,
    /// <summary>狂暴突進です。</summary>
    FrenzyCharge = 5,
    /// <summary>滅地狂吼です。</summary>
    EarthquakeRoar = 6
}

/// <summary>Boss2 攻撃の共通三段階と固有移動値を保持します。</summary>
public sealed class Boss2AttackPattern
{
    /// <summary>攻撃 ID です。</summary>
    public AttackId AttackId { get; }
    /// <summary>Boss2 固有攻撃種別です。</summary>
    public Boss2AttackKind Kind { get; }
    /// <summary>共通 Telegraph、Execute、Recovery 定義です。</summary>
    public AttackDefinition Definition { get; }
    /// <summary>攻撃中の水平速度です。</summary>
    public float ActionSpeed { get; }
    /// <summary>連撃または落石の間隔です。</summary>
    public float Interval { get; }
    /// <summary>設定が実行可能かどうかです。</summary>
    public bool IsValid => Definition.IsValid && IsFinite(ActionSpeed) && IsFinite(Interval) && Interval >= 0;

    /// <summary>Boss2 攻撃 pattern を生成します。</summary>
    public Boss2AttackPattern(AttackId attackId, Boss2AttackKind kind, AttackDefinition definition, float actionSpeed = 0, float interval = 0)
    {
        AttackId = attackId;
        Kind = kind;
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        ActionSpeed = actionSpeed;
        Interval = interval;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

/// <summary>Boss2 State_Module が読む設定の注入境界です。</summary>
public interface IBoss2Configuration
{
    /// <summary>指定 Phase の設定を取得します。</summary>
    Boss2PhaseSettings GetPhase(Phase phase);
    /// <summary>指定 Attack_Id の pattern を取得します。</summary>
    bool GetAttack(AttackId attackId, out Boss2AttackPattern pattern);
}

/// <summary>既存 Boss2 設定の検証済み既定 snapshot です。</summary>
public sealed class Boss2DefaultConfiguration : IBoss2Configuration
{
    private readonly Boss2PhaseSettings[] _phases =
    [
        new Boss2PhaseSettings(
            0.8f,
            160.0f,
            580.0f,
            980.0f,
            850.0f,
            1.0f,
            [Boss2AttackIds.HeavySlash, Boss2AttackIds.GroundStomp, Boss2AttackIds.HeavySlash, Boss2AttackIds.LeapSmash],
            [
                new AttackDecisionRule(Boss2AttackIds.HeavySlash, cooldown: 2.0f, minDistance: 0.0f, maxDistance: 160.0f, weight: 100.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.GroundStomp, cooldown: 5.0f, minDistance: 50.0f, maxDistance: 350.0f, weight: 70.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.LeapSmash, cooldown: 8.0f, minDistance: 150.0f, maxDistance: 600.0f, weight: 80.0f, preferAirborneBonus: 1.5f)
            ]),
        new Boss2PhaseSettings(
            0.5f,
            220.0f,
            640.0f,
            980.0f,
            850.0f,
            1.0f,
            [Boss2AttackIds.DoubleCleave, Boss2AttackIds.GroundStomp, Boss2AttackIds.LeapSmash, Boss2AttackIds.DoubleCleave],
            [
                new AttackDecisionRule(Boss2AttackIds.DoubleCleave, cooldown: 3.0f, minDistance: 0.0f, maxDistance: 175.0f, weight: 100.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.GroundStomp, cooldown: 4.5f, minDistance: 50.0f, maxDistance: 400.0f, weight: 70.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.LeapSmash, cooldown: 6.0f, minDistance: 120.0f, maxDistance: 650.0f, weight: 90.0f, preferAirborneBonus: 1.8f)
            ]),
        new Boss2PhaseSettings(
            0.3f,
            280.0f,
            700.0f,
            980.0f,
            850.0f,
            1.2f,
            [Boss2AttackIds.FrenzyCharge, Boss2AttackIds.EarthquakeRoar, Boss2AttackIds.DoubleCleave, Boss2AttackIds.LeapSmash],
            [
                new AttackDecisionRule(Boss2AttackIds.FrenzyCharge, cooldown: 7.0f, minDistance: 250.0f, maxDistance: 650.0f, weight: 100.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.EarthquakeRoar, cooldown: 10.0f, minDistance: 0.0f, maxDistance: 1000.0f, weight: 80.0f),
                new AttackDecisionRule(Boss2AttackIds.DoubleCleave, cooldown: 2.5f, minDistance: 0.0f, maxDistance: 175.0f, weight: 90.0f, requireSamePlatform: true),
                new AttackDecisionRule(Boss2AttackIds.LeapSmash, cooldown: 5.0f, minDistance: 120.0f, maxDistance: 700.0f, weight: 85.0f, preferAirborneBonus: 2.0f)
            ])
    ];
    private readonly Boss2AttackPattern[] _attacks =
    [
        new Boss2AttackPattern(Boss2AttackIds.HeavySlash, Boss2AttackKind.HeavySlash, new AttackDefinition(Boss2AttackIds.HeavySlash, 0.45f, 0.35f, 0.55f, 160.0f, [new AttackComponent(AttackComponentKind.Hitbox, 0, 80.0f, -10.0f, triggerTime: 0.0f), new AttackComponent(AttackComponentKind.Movement, 1, 360.0f, 0.0f, triggerTime: 0.0f)]), 360.0f),
        new Boss2AttackPattern(Boss2AttackIds.DoubleCleave, Boss2AttackKind.DoubleCleave, new AttackDefinition(Boss2AttackIds.DoubleCleave, 0.35f, 0.70f, 0.50f, 175.0f, [new AttackComponent(AttackComponentKind.Hitbox, 0, 80.0f, -10.0f, triggerTime: 0.0f), new AttackComponent(AttackComponentKind.Hitbox, 1, 95.0f, -10.0f, triggerTime: 0.30f), new AttackComponent(AttackComponentKind.Movement, 2, 420.0f, 0.0f, triggerTime: 0.0f)]), 420.0f, 0.30f),
        new Boss2AttackPattern(Boss2AttackIds.GroundStomp, Boss2AttackKind.GroundStomp, new AttackDefinition(Boss2AttackIds.GroundStomp, 0.50f, 0.40f, 0.60f, 420.0f, [new AttackComponent(AttackComponentKind.Hitbox, 0, 0.0f, 35.0f)])),
        new Boss2AttackPattern(Boss2AttackIds.LeapSmash, Boss2AttackKind.LeapSmash, new AttackDefinition(Boss2AttackIds.LeapSmash, 0.40f, 0.75f, 0.70f, 580.0f, [new AttackComponent(AttackComponentKind.Movement, 0, 0.0f, -680.0f), new AttackComponent(AttackComponentKind.Hitbox, 1, 0.0f, 45.0f)]), 680.0f),
        new Boss2AttackPattern(Boss2AttackIds.FrenzyCharge, Boss2AttackKind.FrenzyCharge, new AttackDefinition(Boss2AttackIds.FrenzyCharge, 0.55f, 1.30f, 0.75f, 480.0f, [new AttackComponent(AttackComponentKind.Movement, 0, 620.0f, 0.0f), new AttackComponent(AttackComponentKind.Hitbox, 1, 0.0f, 0.0f)]), 620.0f),
        new Boss2AttackPattern(Boss2AttackIds.EarthquakeRoar, Boss2AttackKind.EarthquakeRoar, new AttackDefinition(Boss2AttackIds.EarthquakeRoar, 0.60f, 1.80f, 0.90f, 1220.0f, [new AttackComponent(AttackComponentKind.Bullet, 0, 0.0f, 360.0f)]), interval: 0.25f)
    ];

    /// <inheritdoc />
    public Boss2PhaseSettings GetPhase(Phase phase)
    {
        int index = phase switch
        {
            Phase.Phase1 => 0,
            Phase.Phase2 => 1,
            Phase.Phase3 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "Boss2 の Phase が不正です。")
        };
        return _phases[index];
    }

    /// <inheritdoc />
    public bool GetAttack(AttackId attackId, out Boss2AttackPattern pattern)
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
