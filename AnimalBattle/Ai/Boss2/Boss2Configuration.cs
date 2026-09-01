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
            Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase1)),
        new Boss2PhaseSettings(
            0.5f,
            220.0f,
            640.0f,
            980.0f,
            850.0f,
            1.0f,
            [Boss2AttackIds.DoubleCleave, Boss2AttackIds.GroundStomp, Boss2AttackIds.LeapSmash, Boss2AttackIds.DoubleCleave],
            Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase2)),
        new Boss2PhaseSettings(
            0.3f,
            280.0f,
            700.0f,
            980.0f,
            850.0f,
            1.2f,
            [Boss2AttackIds.FrenzyCharge, Boss2AttackIds.EarthquakeRoar, Boss2AttackIds.DoubleCleave, Boss2AttackIds.LeapSmash],
            Boss2PipelineDefinition.Pipeline.GetRules(Phase.Phase3))
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
