using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// Entity ごとに初期化時確保して再利用する Boss AI 実行領域です。
/// </summary>
public sealed class BossRuntimeSlot
{
    private StateContext _context;
    private BossConfigSnapshot _currentConfig;
    private BossConfigSnapshot _pendingConfig;
    private bool _hasPendingConfig;

    /// <summary>Entity 単位の状態 context への参照を取得します。</summary>
    public ref StateContext Context => ref _context;
    /// <summary>現在有効な設定 snapshot を取得します。</summary>
    public BossConfigSnapshot CurrentConfig => _currentConfig;
    /// <summary>保留中の設定 snapshot があるかどうかを取得します。</summary>
    public bool HasPendingConfig => _hasPendingConfig;
    /// <summary>Entity 識別子を取得します。</summary>
    public ulong EntityId => _context.EntityId;
    /// <summary>Boss 種別を取得します。</summary>
    public BossType BossType => _context.BossType;
    /// <summary>Frame_Update 番号を取得します。</summary>
    public long FrameUpdateNumber => _context.FrameUpdateNumber;
    /// <summary>現在の Frame_Update の経過時間を取得します。</summary>
    public float DeltaTime => _context.DeltaTime;
    /// <summary>現在の親 Phase を取得します。</summary>
    public Phase Phase => _context.Phase;
    /// <summary>初期化完了 gate の状態を取得します。</summary>
    public bool InitializationComplete { get; private set; }
    /// <summary>runtime 準備完了 gate の状態を取得します。</summary>
    public bool RuntimeReady { get; private set; }
    /// <summary>現在の module 登録世代を取得します。</summary>
    public long ModuleGeneration { get; private set; }
    /// <summary>最後に処理したエラー fingerprint を取得します。</summary>
    public ulong LastErrorFingerprint { get; private set; }

    /// <summary>移動 command の事前確保 buffer を取得します。</summary>
    public ICommandBuffer<MovementCommand> MovementCommands { get; }
    /// <summary>攻撃 command の事前確保 buffer を取得します。</summary>
    public ICommandBuffer<AttackCommand> AttackCommands { get; }
    /// <summary>演出 command の事前確保 buffer を取得します。</summary>
    public ICommandBuffer<EffectCommand> EffectCommands { get; }

    /// <summary>
    /// Entity 用の context、ports、snapshot、bounded command buffer を初期化します。
    /// </summary>
    public BossRuntimeSlot(
        ulong entityId,
        BossType bossType,
        IConfigProvider configProvider,
        IMovementPort movement,
        IAttackPort attack,
        IEffectPort effect,
        IObservationService observation,
        IRandomSource random,
        int movementCommandCapacity = 32,
        int attackCommandCapacity = 32,
        int effectCommandCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        var configSnapshot = configProvider.Current;
        if (!configSnapshot.IsValid)
        {
            throw new ArgumentException("設定 provider が未検証 snapshot を返しました。", nameof(configProvider));
        }

        MovementCommands = new PreallocatedCommandBuffer<MovementCommand>(movementCommandCapacity);
        AttackCommands = new PreallocatedCommandBuffer<AttackCommand>(attackCommandCapacity);
        EffectCommands = new PreallocatedCommandBuffer<EffectCommand>(effectCommandCapacity);
        _currentConfig = configSnapshot;
        _context = new StateContext(
            entityId,
            bossType,
            configProvider,
            configSnapshot,
            movement,
            attack,
            effect,
            observation,
            random);
        ModuleGeneration = 0;
    }

    /// <summary>初期化完了を runtime gate に反映します。</summary>
    public void MarkInitializationComplete() => InitializationComplete = true;

    /// <summary>依存関係検証後に runtime 使用可能状態を反映します。</summary>
    public void MarkRuntimeReady() => RuntimeReady = InitializationComplete;

    /// <summary>module 差し替え後の登録世代を更新します。</summary>
    public void SetModuleGeneration(long generation)
    {
        if (generation < 0) throw new ArgumentOutOfRangeException(nameof(generation));
        ModuleGeneration = generation;
    }

    /// <summary>エラー fingerprint を記録します。</summary>
    public void SetLastErrorFingerprint(ulong fingerprint) => LastErrorFingerprint = fingerprint;

    /// <summary>設定更新境界で次の有効 snapshot を保留します。</summary>
    public void QueueConfig(BossConfigSnapshot configSnapshot)
    {
        if (!configSnapshot.IsValid) throw new ArgumentException("設定 snapshot が未検証です。", nameof(configSnapshot));
        _pendingConfig = configSnapshot;
        _hasPendingConfig = true;
    }

    /// <summary>安全な境界で保留 snapshot を現在値へ反映します。</summary>
    public bool ApplyPendingConfig()
    {
        if (!_hasPendingConfig)
        {
            return false;
        }

        _currentConfig = _pendingConfig;
        _context.SetConfigSnapshot(_currentConfig);
        _pendingConfig = default;
        _hasPendingConfig = false;
        return true;
    }

    /// <summary>両 gate と入力値を検証し、同一 Entity の Frame_Update を開始します。</summary>
    public bool TryBeginFrame(long frameUpdateNumber, float deltaTime, Phase phase, in PlayerObservation playerObservation)
    {
        if (!InitializationComplete || !RuntimeReady)
        {
            return false;
        }

        return _context.TryBeginFrame(frameUpdateNumber, deltaTime, phase, in playerObservation);
    }

    /// <summary>Frame_Update の command 数を 0 に戻します。配列は再確保しません。</summary>
    public void ClearCommands()
    {
        MovementCommands.Clear();
        AttackCommands.Clear();
        EffectCommands.Clear();
    }
}
