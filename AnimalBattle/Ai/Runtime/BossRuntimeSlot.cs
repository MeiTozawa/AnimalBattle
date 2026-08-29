using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Components;

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
    private BossState _currentBossState;
    private TransitionReason _currentTransitionReason;
    private bool _stateEntered;
    private bool _moduleStopped;
    private long _lastUpdatedFrame = -1;
    private long _commandFrame = -1;
    private int _transitionDepth;
    private bool _failureActive;
    private BossAiData _aiData;
    private BossMovementData _movementData;
    private BossAttackData _attackData;
    private BossPhaseTransitionData _phaseTransitionData;
    private BossTerminalData _terminalData;
    private BossCompatibilityData _compatibilityData;
    private BossDebugData _debugData;
    private bool _debugEnabled;
    private bool _hasDebugData;

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
    /// <summary>現在有効な State_Id を取得します。</summary>
    public BossState CurrentStateId => _currentBossState;
    /// <summary>互換用の現在状態名です。</summary>
    [Obsolete("CurrentStateId を使用してください。")]
    public BossState CurrentBossState => CurrentStateId;
    /// <summary>状態 module の Enter が完了しているかどうかを取得します。</summary>
    public bool StateEntered => _stateEntered;
    /// <summary>module が停止済みかどうかを取得します。</summary>
    public bool ModuleStopped => _moduleStopped;
    /// <summary>最後に Update を実行した Frame_Update 番号を取得します。</summary>
    public long LastUpdatedFrame => _lastUpdatedFrame;
    /// <summary>現在状態に適用した遷移理由を取得します。</summary>
    public TransitionReason CurrentTransitionReason => _currentTransitionReason;
    /// <summary>初期化完了 gate の状態を取得します。</summary>
    public bool InitializationComplete { get; private set; }
    /// <summary>runtime 準備完了 gate の状態を取得します。</summary>
    public bool RuntimeReady { get; private set; }
    /// <summary>最後に処理したエラー fingerprint を取得します。</summary>
    public ulong LastErrorFingerprint { get; private set; }
    /// <summary>最後に同一 fingerprint を処理した Frame_Update 番号を取得します。</summary>
    public long LastHandledFrame { get; private set; } = -1;
    /// <summary>処理エラー以外の理由で module が停止した理由を取得します。</summary>
    public ErrorCode ModuleStopReason { get; private set; } = ErrorCode.None;
    /// <summary>module を停止した Frame_Update 番号を取得します。</summary>
    public long ModuleStoppedFrame { get; private set; } = -1;
    /// <summary>安全遷移を既に一度試行したかどうかを取得します。</summary>
    public bool SafetyTransitionAttempted { get; private set; }
    /// <summary>安全な復帰先が使えず fail-closed になったかどうかを取得します。</summary>
    public bool IsFailClosed { get; private set; }
    /// <summary>最後に適用した安全値の成功項目を取得します。</summary>
    public SafetyParameterApplyMask AppliedSafetyParameters { get; private set; }
    /// <summary>最後に適用した安全値の失敗項目を取得します。</summary>
    public SafetyParameterApplyMask FailedSafetyParameters { get; private set; }
    /// <summary>Debug_Data を生成・更新する設定を取得します。</summary>
    public bool DebugEnabled => _debugEnabled;
    /// <summary>現在の Debug_Data が利用可能かどうかを取得します。</summary>
    public bool HasDebugData => _hasDebugData;
    /// <summary>攻撃を新規生成できる状態かどうかを取得します。</summary>
    public bool CanGenerateAttack => !IsFailClosed && !AiData.IsDead && AiData.AttackGenerationEnabled;

    /// <summary>HUD と WorldRender が読む互換 view を取得します。</summary>
    public BossCompatibilityData CompatibilityView
    {
        get
        {
            BossCompatibilityData view = _compatibilityData;
            view.LegacyAttackValue = _attackData.AttackId.Value;
            view.LegacyStateTimer = _aiData.StateTimer;
            view.LegacyStateDuration = _aiData.StateDuration;
            view.LegacyIsInvulnerable = _aiData.IsInvulnerable;
            view.UpdateSource = _aiData.UpdateSource;
            view.BossState = _currentBossState;
            view.Phase = _aiData.Phase;
            view.AttackId = _attackData.AttackId;
            view.TransitionReason = _currentTransitionReason;
            view.IsInvulnerable = _aiData.IsInvulnerable;
            view.AttackStage = _attackData.Stage;
            view.HasDebugData = _hasDebugData;
            view.FrameUpdateNumber = _aiData.FrameUpdateNumber;
            return view;
        }
    }

    /// <summary>現在の Debug_Data を取得します。Debug 無効時は生成しません。</summary>
    /// <param name="debugData">生成済みの Debug_Data。</param>
    /// <returns>Debug_Data が有効な場合は true。</returns>
    public bool GetDebugData(out BossDebugData debugData)
    {
        debugData = _debugData;
        return _hasDebugData;
    }

    /// <summary>Debug_Data の生成可否を切り替えます。</summary>
    /// <param name="enabled">Debug_Data を生成・更新する場合は true。</param>
    public void SetDebugEnabled(bool enabled)
    {
        if (_debugEnabled == enabled)
        {
            return;
        }

        _debugEnabled = enabled;
        if (!enabled)
        {
            _debugData = default;
            _hasDebugData = false;
            return;
        }

        RefreshDebugData();
    }

    /// <summary>現在の runtime 値から Debug_Data を更新します。</summary>
    /// <remarks>Debug 無効時は値を生成せず、既存値も破棄します。</remarks>
    public void RefreshDebugData()
    {
        if (!_debugEnabled)
        {
            _debugData = default;
            _hasDebugData = false;
            return;
        }

        _debugData = new BossDebugData
        {
            BossState = _currentBossState,
            Phase = _aiData.Phase,
            AttackId = _attackData.AttackId,
            TransitionReason = _currentTransitionReason,
            IsInvulnerable = _aiData.IsInvulnerable,
            AttackStage = _attackData.Stage,
            FrameUpdateNumber = _aiData.FrameUpdateNumber
        };
        _hasDebugData = true;
    }

    /// <summary>移動 command の事前確保 buffer を取得します。</summary>
    public PreallocatedCommandBuffer<MovementCommand> MovementCommands { get; }
    /// <summary>攻撃 command の事前確保 buffer を取得します。</summary>
    public PreallocatedCommandBuffer<AttackCommand> AttackCommands { get; }
    /// <summary>演出 command の事前確保 buffer を取得します。</summary>
    public PreallocatedCommandBuffer<EffectCommand> EffectCommands { get; }

    /// <summary>死亡後も HUD、衝突、Restart が参照できる終端 data です。</summary>
    public ref BossTerminalData TerminalData => ref _terminalData;
    /// <summary>PhaseTransition の項目別適用 data です。</summary>
    public ref BossPhaseTransitionData PhaseTransitionData => ref _phaseTransitionData;
    /// <summary>同一 frame の攻撃 stage を保持する data です。</summary>
    public ref BossAttackData AttackData => ref _attackData;
    /// <summary>移行時に復元した Movement data を保持します。</summary>
    public ref BossMovementData MovementData => ref _movementData;
    /// <summary>共通 ECS data 互換の AI 値です。</summary>
    public ref BossAiData AiData => ref _aiData;

    /// <summary>指定 command の index から one-shot key を runtime 経由で取得します。</summary>
    public bool GetOneShotKey(CommandComponentKind component, int index, out CommandOneShotKey key)
    {
        return component switch
        {
            CommandComponentKind.Movement => MovementCommands.GetOneShotKey(index, out key),
            CommandComponentKind.Attack => AttackCommands.GetOneShotKey(index, out key),
            CommandComponentKind.Effect => EffectCommands.GetOneShotKey(index, out key),
            _ => InvalidOneShotKey(out key)
        };
    }

    /// <summary>指定 index の移動 command を runtime 経由で取得します。</summary>
    public bool GetMovementCommand(int index, out MovementCommand command)
    {
        if ((uint)index >= (uint)MovementCommands.Count)
        {
            command = default;
            return false;
        }

        command = MovementCommands[index];
        return true;
    }

    /// <summary>指定 index の攻撃 command を runtime 経由で取得します。</summary>
    public bool GetAttackCommand(int index, out AttackCommand command)
    {
        if (!CanGenerateAttack || (uint)index >= (uint)AttackCommands.Count)
        {
            command = default;
            return false;
        }

        command = AttackCommands[index];
        return true;
    }

    /// <summary>指定 index の演出 command を runtime 経由で取得します。</summary>
    public bool GetEffectCommand(int index, out EffectCommand command)
    {
        if ((uint)index >= (uint)EffectCommands.Count)
        {
            command = default;
            return false;
        }

        command = EffectCommands[index];
        return true;
    }

    /// <summary>指定 command buffer の直近の overflow / one-shot 診断を取得します。</summary>
    public bool GetCommandDiagnostic(CommandComponentKind component, out CommandBufferDiagnostic diagnostic)
    {
        diagnostic = component switch
        {
            CommandComponentKind.Movement => MovementCommands.LastDiagnostic,
            CommandComponentKind.Attack => AttackCommands.LastDiagnostic,
            CommandComponentKind.Effect => EffectCommands.LastDiagnostic,
            _ => default
        };
        return diagnostic.Reason != CommandBufferFailureReason.None;
    }

    private static bool InvalidOneShotKey(out CommandOneShotKey key)
    {
        key = default;
        return false;
    }

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
        int effectCommandCapacity = 16,
        ICommandBufferDiagnosticSink? commandDiagnosticSink = null,
        bool debugEnabled = false,
        PreallocatedCommandBuffer<MovementCommand>? movementCommandBuffer = null,
        PreallocatedCommandBuffer<AttackCommand>? attackCommandBuffer = null,
        PreallocatedCommandBuffer<EffectCommand>? effectCommandBuffer = null)
    {
        ArgumentNullException.ThrowIfNull(configProvider);
        BossConfigSnapshot configSnapshot = configProvider.Current;
        if (!configSnapshot.IsValid)
        {
            throw new ArgumentException("設定 provider が未検証 snapshot を返しました。", nameof(configProvider));
        }

        MovementCommands = movementCommandBuffer ?? new PreallocatedCommandBuffer<MovementCommand>(movementCommandCapacity, commandDiagnosticSink);
        AttackCommands = attackCommandBuffer ?? new PreallocatedCommandBuffer<AttackCommand>(attackCommandCapacity, commandDiagnosticSink);
        EffectCommands = effectCommandBuffer ?? new PreallocatedCommandBuffer<EffectCommand>(effectCommandCapacity, commandDiagnosticSink);
        _currentConfig = configSnapshot;
        _aiData = new BossAiData(bossType, BossState.Unknown, Phase.Phase1);
        _movementData = new BossMovementData(BossState.Unknown);
        _attackData = new BossAttackData(AttackId.Unknown);
        _phaseTransitionData = default;
        _terminalData = default;
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
        _debugEnabled = debugEnabled;
        RefreshDebugData();
    }

    /// <summary>
    /// MigrationAdapter が検証した旧値を runtime の source of truth として取り込みます。
    /// </summary>
    /// <param name="migration">検証済みの移行結果。</param>
    /// <returns>現在の Entity/Boss に適用できた場合は Success。</returns>
    internal ErrorOr.ErrorOr<ErrorOr.Success> ApplyMigration(in MigrationResult migration)
    {
        if (!migration.IsSuccess || migration.BossType != BossType || migration.AiData.BossState == BossState.Unknown)
        {
            return ErrorOr.Error.Validation("BossRuntimeSlot.ApplyMigration", "Migration 結果が無効です。");
        }

        _context.SetPhaseForTransition(migration.AiData.Phase);
        _aiData = migration.AiData;
        _aiData.UpdateSource = UpdateSource.NewRuntime;
        _movementData = migration.MovementData;
        _attackData = migration.AttackData;
        _compatibilityData = migration.CompatibilityData;
        _compatibilityData.UpdateSource = UpdateSource.NewRuntime;
        _currentBossState = _aiData.BossState;
        _currentTransitionReason = _compatibilityData.TransitionReason;
        _stateEntered = false;
        _moduleStopped = false;
        _hasDebugData = false;
        RefreshDebugData();
        return ErrorOr.Result.Success;
    }

    /// <summary>初期化完了を runtime gate に反映します。</summary>
    public void MarkInitializationComplete() => InitializationComplete = true;

    /// <summary>依存関係検証後に runtime 使用可能状態を反映します。</summary>
    public void MarkRuntimeReady() => MarkRuntimeReady(true);

    /// <summary>依存関係検証結果を runtime gate に反映します。</summary>
    public void MarkRuntimeReady(bool dependenciesValid)
    {
        RuntimeReady = InitializationComplete && dependenciesValid && _context.IsReady;
    }

    /// <summary>エラー fingerprint を記録します。</summary>
    public void SetLastErrorFingerprint(ulong fingerprint)
    {
        LastErrorFingerprint = fingerprint;
        _failureActive = fingerprint != 0;
    }

    /// <summary>同じ異常の二重処理を抑止し、初回処理 frame を保存します。</summary>
    internal bool HandleFailure(ulong fingerprint, long frameUpdateNumber)
    {
        if (fingerprint == 0 || (_failureActive && LastErrorFingerprint == fingerprint))
        {
            return false;
        }

        LastErrorFingerprint = fingerprint;
        LastHandledFrame = frameUpdateNumber;
        _failureActive = true;
        return true;
    }

    /// <summary>正常な frame の完了後に次の異常を受け付けられるようにします。</summary>
    internal void MarkHealthyFrame()
    {
        if (!IsFailClosed && !ModuleStopped)
        {
            ModuleStopReason = ErrorCode.None;
            _failureActive = false;
        }
    }

    /// <summary>処理エラー以外で停止した module を依存関係検証後に再開します。</summary>
    public ErrorOr.ErrorOr<ErrorOr.Success> RevalidateStoppedModule()
    {
        if (!ModuleStopped || IsFailClosed || ModuleStopReason == ErrorCode.ModuleExecutionFailed)
        {
            return ErrorOr.Error.Failure("BossRuntimeSlot.Revalidate", "再開条件を満たしていません。");
        }

        if (!_context.IsReady)
        {
            return ErrorOr.Error.Failure("BossRuntimeSlot.Revalidate", "Context が準備完了していません。");
        }

        _moduleStopped = false;
        ModuleStopReason = ErrorCode.None;
        return ErrorOr.Result.Success;
    }

    /// <summary>module を停止し、停止理由と frame を保存します。</summary>
    internal void StopModule(ErrorCode reason)
    {
        _moduleStopped = true;
        _stateEntered = false;
        ModuleStopReason = reason;
        ModuleStoppedFrame = FrameUpdateNumber;
    }

    /// <summary>安全値を項目単位で適用し、成功値を保持します。</summary>
    public SafetyParameterApplicationResult ApplySafetyParameters(in SafetyParameter parameter)
    {
        SafetyParameterApplicationResult result = SafetyParameterApplier.Apply(
            this,
            in parameter,
            AppliedSafetyParameters,
            FailedSafetyParameters);
        AppliedSafetyParameters = result.AppliedMask;
        FailedSafetyParameters = result.FailedMask;
        RefreshDebugData();
        return result;
    }

    /// <summary>安全遷移の再帰的な再試行を一回に制限します。</summary>
    internal ErrorOr.ErrorOr<ErrorOr.Success> BeginSafetyTransition()
    {
        if (SafetyTransitionAttempted)
        {
            return ErrorOr.Error.Failure("BossRuntimeSlot.SafetyTransition", "安全遷移は既に試行済みです。");
        }

        SafetyTransitionAttempted = true;
        return ErrorOr.Result.Success;
    }

    /// <summary>安全復帰先が使えない場合に Dead/停止へ固定します。</summary>
    internal void FailClosed(BossState deadState, in TransitionReason reason, long frameUpdateNumber)
    {
        IsFailClosed = true;
        StopModule(ErrorCode.SafetyRecoveryFailed);
        _currentBossState = deadState.IsValid() ? deadState : BossState.Unknown;
        _currentTransitionReason = reason;
        _aiData.BossState = _currentBossState;
        _aiData.IsDead = deadState.IsValid();
        _aiData.AttackGenerationEnabled = false;
        _aiData.IsInvulnerable = true;
        _attackData.Stage = AttackStage.None;
        _attackData.OneShotEmissionMask = 0;
        _terminalData.IsTerminal = true;
        _terminalData.DeadState = _currentBossState;
        _terminalData.Phase = Phase;
        _terminalData.DeathReason = reason;
        _terminalData.FrameUpdateNumber = frameUpdateNumber;
        _terminalData.AttackStage = AttackStage.None;
        _terminalData.AttackGenerationStopped = true;
        ClearCommands();
        RefreshDebugData();
    }

    /// <summary>設定更新境界で次の有効 snapshot を保留します。</summary>
    public void QueueConfig(BossConfigSnapshot configSnapshot)
    {
        if (!configSnapshot.IsValid) throw new ArgumentException("設定 snapshot が未検証です。", nameof(configSnapshot));
        _pendingConfig = configSnapshot;
        _hasPendingConfig = true;
    }

    /// <summary>
    /// 保留 snapshot を指定した reload 境界で現在値へ反映します。
    /// </summary>
    /// <param name="boundary">設定を反映できる lifecycle 境界。</param>
    /// <returns>snapshot を反映した場合は true。</returns>
    public bool ApplyPendingConfig(ConfigReloadBoundary boundary)
    {
        if (boundary is not (ConfigReloadBoundary.Transition or ConfigReloadBoundary.AttackSelection)
            || !_hasPendingConfig)
        {
            return false;
        }

        _currentConfig = _pendingConfig;
        _context.SetConfigSnapshot(_currentConfig);
        _pendingConfig = default;
        _hasPendingConfig = false;
        return true;
    }

    /// <summary>
    /// 任意の実行時点からの設定反映を拒否します。
    /// </summary>
    /// <remarks>
    /// 設定 reload は Transition または AttackSelection 境界からだけ行います。
    /// 既存呼び出しとの互換性のためメソッドは残しますが、境界なしでは反映しません。
    /// </remarks>
    [Obsolete("ApplyPendingConfig(ConfigReloadBoundary) を使用してください。")]
    public bool ApplyPendingConfig() => false;

    /// <summary>状態遷移境界で保留 snapshot を反映します。</summary>
    internal bool ApplyPendingConfigAtTransition()
        => ApplyPendingConfig(ConfigReloadBoundary.Transition);

    /// <summary>攻撃選択境界で保留 snapshot を反映します。</summary>
    internal bool ApplyPendingConfigAtAttackSelection()
        => ApplyPendingConfig(ConfigReloadBoundary.AttackSelection);

    /// <summary>両 gate と入力値を検証し、同一 Entity の Frame_Update を開始します。</summary>
    public ErrorOr.ErrorOr<ErrorOr.Success> BeginFrame(long frameUpdateNumber, float deltaTime, Phase phase, in PlayerObservation playerObservation)
    {
        if (!InitializationComplete || !RuntimeReady)
        {
            return ErrorOr.Error.Failure("BossRuntimeSlot.GateClosed", "初期化 gate または runtime ready gate が未成立です。");
        }

        ErrorOr.ErrorOr<ErrorOr.Success> beginResult = _context.BeginFrame(frameUpdateNumber, deltaTime, phase, in playerObservation);
        if (beginResult.IsError)
        {
            return beginResult;
        }

        if (_commandFrame != frameUpdateNumber)
        {
            ClearCommands();
            _commandFrame = frameUpdateNumber;
        }

        _aiData.FrameUpdateNumber = frameUpdateNumber;
        _aiData.Phase = phase;
        _aiData.UpdateSource = UpdateSource.NewRuntime;
        RefreshDebugData();
        return ErrorOr.Result.Success;
    }

    /// <summary>Frame_Update の command 数を 0 に戻します。配列は再確保しません。</summary>
    public void ClearCommands()
    {
        MovementCommands.Clear();
        AttackCommands.Clear();
        EffectCommands.Clear();
    }

    internal void SetCurrentState(BossState stateId, TransitionReason reason)
    {
        _currentBossState = stateId;
        _currentTransitionReason = reason;
        _aiData.BossState = stateId;
        _aiData.Phase = _context.Phase;
        _aiData.IsDead = stateId is BossState.Boss1_Dead or BossState.Boss2_Dead;
        if (_aiData.IsDead)
        {
            _aiData.AttackGenerationEnabled = false;
        }
        _stateEntered = false;
        _moduleStopped = false;
        RefreshDebugData();
    }

    internal void MarkStateEntered()
    {
        _stateEntered = true;
        _moduleStopped = false;
        ModuleStopReason = ErrorCode.None;
        _failureActive = false;
    }

    internal void MarkModuleStopped(ErrorCode reason = ErrorCode.ModuleExecutionFailed)
    {
        StopModule(reason);
    }

    internal ErrorOr.ErrorOr<ErrorOr.Success> MarkUpdated(long frameUpdateNumber)
    {
        if (_lastUpdatedFrame == frameUpdateNumber)
        {
            return ErrorOr.Error.Validation("BossRuntimeSlot.MarkUpdated", "同一フレームで既に更新済みです。");
        }

        _lastUpdatedFrame = frameUpdateNumber;
        return ErrorOr.Result.Success;
    }

    internal ErrorOr.ErrorOr<ErrorOr.Success> EnterTransition()
    {
        if (_transitionDepth != 0)
        {
            return ErrorOr.Error.Validation("BossRuntimeSlot.EnterTransition", "遷移深度が不正です。");
        }

        _transitionDepth = 1;
        return ErrorOr.Result.Success;
    }

    internal void ExitTransition()
    {
        _transitionDepth = 0;
    }
}
