using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using ErrorOr;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     State_Module が使用する Entity 単位の依存関係と Frame_Update 値の境界です。
/// </summary>
public struct StateContext {
    private readonly IConfigProvider? _configProvider;
    private BossConfigSnapshot _configSnapshot;
    private readonly IMovementPort? _movement;
    private readonly IAttackPort? _attack;
    private readonly IEffectPort? _effect;
    private readonly IRandomSource? _random;

    /// <summary>Boss Entity の識別子を取得します。</summary>
    public ulong EntityId { get; }

    /// <summary>Boss 種別を取得します。</summary>
    public BossType BossType { get; }

    /// <summary>Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber { get; private set; }

    /// <summary>現在の Frame_Update の経過時間を取得します。</summary>
    public float DeltaTime { get; private set; }

    /// <summary>現在の親 Phase を取得します。</summary>
    public Phase Phase { get; private set; }

    /// <summary>同じ Frame_Update で共有するプレイヤー観測を取得します。</summary>
    public PlayerObservation PlayerObservation { get; private set; }

    /// <summary>検証済み設定 snapshot の提供元を取得します。</summary>
    public IConfigProvider ConfigProvider => _configProvider!;

    /// <summary>同じ Frame_Update で共有する設定 snapshot を取得します。</summary>
    public BossConfigSnapshot ConfigSnapshot => _configSnapshot;

    /// <summary>移動 command port を取得します。</summary>
    public IMovementPort Movement => _movement!;

    /// <summary>攻撃 command port を取得します。</summary>
    public IAttackPort Attack => _attack!;

    /// <summary>演出 command port を取得します。</summary>
    public IEffectPort Effect => _effect!;

    /// <summary>明示的に注入された乱数源を取得します。</summary>
    public IRandomSource Random => _random!;

    /// <summary>全依存関係と現在値が実行可能な状態かどうかを取得します。</summary>
    public bool IsReady => EntityId != 0
     && BossType != BossType.Unknown
     && FrameUpdateNumber >= 0
     && IsFinite(DeltaTime)
     && DeltaTime >= 0
     && Phase != Phase.Unknown
     && _configProvider is not null
     && _configSnapshot.IsValid
     && _movement is not null
     && _attack is not null
     && _effect is not null
     && _random is not null;

    /// <summary>StateContext を初期化時の依存関係で生成します。</summary>
    public StateContext(
        ulong entityId,
        BossType bossType,
        IConfigProvider configProvider,
        BossConfigSnapshot configSnapshot,
        IMovementPort movement,
        IAttackPort attack,
        IEffectPort effect,
        IRandomSource random)
    {
        if (entityId == 0) throw new ArgumentOutOfRangeException(nameof(entityId));
        if (bossType == BossType.Unknown) throw new ArgumentOutOfRangeException(nameof(bossType));
        ArgumentNullException.ThrowIfNull(configProvider);
        if (!configSnapshot.IsValid) throw new ArgumentException("設定 snapshot が未検証です。", nameof(configSnapshot));
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(effect);
        ArgumentNullException.ThrowIfNull(random);

        EntityId = entityId;
        BossType = bossType;
        FrameUpdateNumber = 0;
        DeltaTime = 0;
        Phase = Phase.Phase1;
        _configProvider = configProvider;
        _configSnapshot = configSnapshot;
        _movement = movement;
        _attack = attack;
        _effect = effect;
        _random = random;
        PlayerObservation = default;
    }

    /// <summary>reload 境界で現在の State_Module が参照する設定 snapshot を反映します。</summary>
    public void SetConfigSnapshot(BossConfigSnapshot configSnapshot)
    {
        if (!configSnapshot.IsValid) throw new ArgumentException("設定 snapshot が未検証です。", nameof(configSnapshot));
        _configSnapshot = configSnapshot;
    }

    /// <summary>PhaseTransition の受理後に target Phase を context へ反映します。</summary>
    public void SetPhaseForTransition(Phase phase)
    {
        if (phase is not (Phase.Phase1 or Phase.Phase2 or Phase.Phase3))
            throw new ArgumentOutOfRangeException(nameof(phase));

        Phase = phase;
    }

    /// <summary>フレーム境界の値を検証して context に設定します。</summary>
    public ErrorOr<Success> BeginFrame(long frameUpdateNumber, float deltaTime, Phase phase,
        in PlayerObservation playerObservation)
    {
        if (frameUpdateNumber < 0 || !IsFinite(deltaTime) || deltaTime < 0 || phase == Phase.Unknown)
            return Error.Validation("StateContext.BeginFrame", "フレーム更新パラメータが不正です。");

        if (playerObservation.FrameUpdateNumber != frameUpdateNumber)
            return Error.Validation("StateContext.BeginFrame", "プレイヤー観測のフレーム番号が一致しません。");

        FrameUpdateNumber = frameUpdateNumber;
        DeltaTime = deltaTime;
        Phase = phase;
        PlayerObservation = playerObservation;
        return Result.Success;
    }

    /// <summary>フレーム境界の値を初期状態にリセットします。</summary>
    /// <param name="initialPhase">初期 Phase（既定値: Phase1）。</param>
    public void Reset(Phase initialPhase = Phase.Phase1)
    {
        FrameUpdateNumber = 0;
        DeltaTime = 0.0f;
        Phase = initialPhase;
        PlayerObservation = default;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}