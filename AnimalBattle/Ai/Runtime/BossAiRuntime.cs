using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Components;
using ErrorOr;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 初期化 gate、State_Module lifecycle、Frame_Update 境界を管理する共通 runtime です。
/// </summary>
public sealed class BossAiRuntime
{
    private readonly IEnumModuleMap _registry;
    private readonly ITransitionService _transitionService;
    private readonly IObservationService _observation;
    private readonly Func<BossType, IBossBehaviorState?>? _behaviorStateProvider;
    private readonly IEntityComponentValidator? _componentValidator;
    private readonly BossState _safeBossState;

    /// <summary>共通 runtime を生成します。</summary>
    public BossAiRuntime(
        IEnumModuleMap registry,
        ITransitionService transitionService,
        IObservationService observation,
        Func<BossType, IBossBehaviorState?>? behaviorStateProvider = null,
        BossState safeBossState = default,
        IEntityComponentValidator? componentValidator = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(transitionService);
        ArgumentNullException.ThrowIfNull(observation);
        _registry = registry;
        _transitionService = transitionService;
        _observation = observation;
        _behaviorStateProvider = behaviorStateProvider;
        _safeBossState = safeBossState;
        _componentValidator = componentValidator;
    }

    /// <summary>Registry、context、component を検証して初期状態へ一回入場します。</summary>
    public ErrorOr<Success> Initialize(BossRuntimeSlot runtime, BossState initialBossState)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runtime.InitializationComplete && runtime.RuntimeReady)
        {
            return runtime.CurrentStateId == initialBossState
                ? Result.Success
                : Error.Validation("BossAiRuntime.Initialize", "初期状態が既存の状態と一致しません。");
        }

        EnumCompletenessResult registryValidation = _registry.ValidateComplete();
        if (registryValidation.IsFailure)
        {
            RecordFailure(runtime, registryValidation.ErrorCode, registryValidation.Message, LifecycleOperation.Enter);
            return Error.Failure("BossAiRuntime.Initialize", registryValidation.Message);
        }

        if (!runtime.Context.IsReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "State_Context の必須依存関係が未検証です。", LifecycleOperation.Enter);
            return Error.Failure("BossAiRuntime.Initialize", "State_Context の必須依存関係が未検証です。");
        }

        if (_componentValidator is not null && !_componentValidator.Validate(runtime, out string missingComponent))
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, $"Entity component が不足しています: {missingComponent}", LifecycleOperation.Enter);
            return Error.Failure("BossAiRuntime.Initialize", $"Entity component が不足しています: {missingComponent}");
        }

        if (!_registry.Get(initialBossState, out IBossStateModule module))
        {
            RecordFailure(runtime, ErrorCode.UnregisteredState, "初期 State_Id が固定 map に対応していません。", LifecycleOperation.Enter, initialBossState);
            return Error.Failure("BossAiRuntime.Initialize", "初期 State_Id が固定 map に対応していません。");
        }

        runtime.MarkInitializationComplete();
        runtime.MarkRuntimeReady(true);
        TransitionReason reason = new(ReasonKind.InitialEntry);
        runtime.SetCurrentState(initialBossState, reason);
        ref StateContext context = ref runtime.Context;
        LifecycleInvocation invocation = new(
            runtime.BossType,
            runtime.EntityId,
            initialBossState,
            LifecycleOperation.Enter,
            runtime.FrameUpdateNumber,
            runtime.DeltaTime,
            runtime.Phase);
        try
        {
            module.Enter(ref context, in invocation);
            runtime.MarkStateEntered();
            RecordLifecycle(runtime, in invocation);
            ProcessPendingTransitions(runtime, runtime.FrameUpdateNumber);
            return Result.Success;
        }
        catch (Exception ex)
        {
            HandleLifecycleFailure(
                runtime,
                ErrorCode.ModuleExecutionFailed,
                "初期 State_Module の Enter に失敗しました。",
                LifecycleOperation.Enter,
                initialBossState,
                runtime.FrameUpdateNumber);
            return Error.Failure("BossAiRuntime.Initialize", ex.Message);
        }
    }

    /// <summary>検証済み Frame_Update を開始して現在 module の Update を一回だけ実行します。</summary>
    public ErrorOr<Success> Update(BossRuntimeSlot runtime, long frameUpdateNumber, float deltaTime, Phase phase, in PlayerObservation playerObservation)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "初期化 gate が成立していません。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "初期化 gate が成立していません。");
        }

        if (runtime.LastUpdatedFrame >= 0 && frameUpdateNumber <= runtime.LastUpdatedFrame)
        {
            RecordFailure(runtime, ErrorCode.LifecycleViolation, "Frame_Update 番号が後退または重複しています。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "Frame_Update 番号が後退または重複しています。");
        }

        ErrorOr<Success> beginResult = runtime.BeginFrame(frameUpdateNumber, deltaTime, phase, in playerObservation);
        if (beginResult.IsError)
        {
            RecordFailure(runtime, ErrorCode.InvalidTimer, "Frame_Update の入力値が不正です。", LifecycleOperation.Update);
            return beginResult;
        }

        return RunUpdate(runtime, frameUpdateNumber);
    }

    /// <summary>HP を同じ Frame_Update で評価し、PhaseTransition または Dead を先に確定します。</summary>
    public ErrorOr<Success> Update(
        BossRuntimeSlot runtime,
        long frameUpdateNumber,
        float deltaTime,
        Phase phase,
        in PlayerObservation playerObservation,
        float currentHp,
        float maxHp,
        in BossPhaseThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "初期化 gate が成立していません。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "初期化 gate が成立していません。");
        }

        if (runtime.LastUpdatedFrame >= 0 && frameUpdateNumber <= runtime.LastUpdatedFrame)
        {
            RecordFailure(runtime, ErrorCode.LifecycleViolation, "Frame_Update 番号が後退または重複しています。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "Frame_Update 番号が後退または重複しています。");
        }

        ErrorOr<Success> beginResult = runtime.BeginFrame(frameUpdateNumber, deltaTime, phase, in playerObservation);
        if (beginResult.IsError)
        {
            RecordFailure(runtime, ErrorCode.InvalidTimer, "Frame_Update の入力値が不正です。", LifecycleOperation.Update);
            return beginResult;
        }

        if (!float.IsFinite(currentHp) || !float.IsFinite(maxHp) || maxHp <= 0.0f)
        {
            return HandleLifecycleFailure(
                runtime,
                ErrorCode.InvalidConfiguration,
                "Boss の HP 設定が不正です。",
                LifecycleOperation.Update,
                runtime.CurrentStateId,
                frameUpdateNumber);
        }

        runtime.ClearCommands();
        if (runtime.TerminalData.IsTerminal)
        {
            PhaseDeathController.ApplyDeathStop(ref runtime.TerminalData, runtime.Context.Movement, runtime.Context.Attack);
            runtime.RefreshDebugData();
            runtime.MarkUpdated(frameUpdateNumber);
            return Result.Success;
        }

        if (currentHp <= 0.0f)
        {
            return ApplyDeathInCurrentFrame(runtime, currentHp, frameUpdateNumber);
        }

        if (thresholds.GetNextPhase(phase, currentHp / maxHp, out Phase nextPhase, out float threshold)
            && !runtime.PhaseTransitionData.IsActive
            && runtime.CurrentStateId is not (BossState.Boss1_PhaseTransition or BossState.Boss2_PhaseTransition or BossState.Boss1_Dead or BossState.Boss2_Dead))
        {
            TransitionReason reason = new(ReasonKind.PhaseThreshold, threshold: threshold);
            BossState transitionState = PhaseDeathController.GetPhaseTransitionState(runtime.BossType);
            TransitionRequest request = new(
                runtime.CurrentStateId,
                transitionState,
                reason,
                nextPhase,
                runtime.EntityId,
                frameUpdateNumber,
                transitionState);
            runtime.Context.SetPhaseForTransition(nextPhase);
            TransitionResult result = _transitionService.Apply(ref runtime, in request);
            if (result.IsAccepted)
            {
                runtime.PhaseTransitionData = new BossPhaseTransitionData(nextPhase, in reason)
                {
                    IsActive = true,
                    TriggeredPhaseMask = nextPhase == Phase.Phase2
                        ? PhaseTransitionApplyMask.Phase2
                        : PhaseTransitionApplyMask.Phase3
                };
                runtime.AiData.Phase = nextPhase;
                runtime.AiData.BossState = transitionState;
                runtime.RefreshDebugData();
                runtime.MarkUpdated(frameUpdateNumber);
                return Result.Success;
            }

            runtime.MarkUpdated(frameUpdateNumber);
            return Error.Failure("BossAiRuntime.TransitionRejected", result.Message);
        }

        return RunUpdate(runtime, frameUpdateNumber);
    }

    /// <summary>既存設定の Phase 閾値で HP-aware 更新を実行します。</summary>
    public ErrorOr<Success> Update(
        BossRuntimeSlot runtime,
        long frameUpdateNumber,
        float deltaTime,
        Phase phase,
        in PlayerObservation playerObservation,
        float currentHp,
        float maxHp)
        => Update(runtime, frameUpdateNumber, deltaTime, phase, in playerObservation, currentHp, maxHp, BossPhaseThresholds.Default);

    private ErrorOr<Success> ApplyDeathInCurrentFrame(BossRuntimeSlot runtime, float currentHp, long frameUpdateNumber)
    {
        BossState deadState = PhaseDeathController.GetDeadState(runtime.BossType);
        TransitionReason reason = PhaseDeathController.CreateDeathReason(currentHp);
        if (!runtime.AiData.IsDead)
        {
            runtime.AiData.Phase = runtime.Context.Phase;
            runtime.AiData.BossType = runtime.BossType;
            PhaseDeathController.CaptureDeath(
                ref runtime.AiData,
                ref runtime.TerminalData,
                ref runtime.PhaseTransitionData,
                ref runtime.AttackData,
                currentHp,
                frameUpdateNumber);
        }

        PhaseDeathController.ApplyDeathStop(ref runtime.TerminalData, runtime.Context.Movement, runtime.Context.Attack);
        if (runtime.CurrentStateId == deadState)
        {
            runtime.MarkUpdated(frameUpdateNumber);
            return Result.Success;
        }

        TransitionRequest request = new(
            runtime.CurrentStateId,
            deadState,
            reason,
            runtime.Context.Phase,
            runtime.EntityId,
            frameUpdateNumber,
            deadState);
        TransitionResult result = _transitionService.Apply(ref runtime, in request);
        runtime.RefreshDebugData();
        runtime.MarkUpdated(frameUpdateNumber);
        return result.IsAccepted ? Result.Success : Error.Failure("BossAiRuntime.DeathTransitionRejected", result.Message);
    }


    /// <summary>
    /// ランタイムスロットのフレーム更新処理を実行します。
    /// </summary>
    /// <param name="runtime">更新対象のランタイムスロット</param>
    /// <returns>正常更新時は Success</returns>
    public ErrorOr<Success> Update(BossRuntimeSlot runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "初期化 gate が成立していません。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "初期化 gate が成立していません。");
        }

        if (runtime.LastUpdatedFrame >= 0 && runtime.FrameUpdateNumber <= runtime.LastUpdatedFrame)
        {
            RecordFailure(runtime, ErrorCode.LifecycleViolation, "Frame_Update 番号が後退または重複しています。", LifecycleOperation.Update);
            return Error.Failure("BossAiRuntime.Update", "Frame_Update 番号が後退または重複しています。");
        }

        return RunUpdate(runtime, runtime.FrameUpdateNumber);
    }

    /// <summary>Transition_Service に要求を渡して安全に適用します。</summary>
    public TransitionResult ApplyTransition(BossRuntimeSlot runtime, in TransitionRequest request)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            TransitionResult rejected = TransitionResult.Rejected(in request, runtime.CurrentStateId, ErrorCode.MissingDependency, "初期化 gate 前の遷移を拒否しました。");
            RecordTransition(runtime, in request, in rejected);
            return rejected;
        }

        TransitionResult result = _transitionService.Apply(ref runtime, in request);
        if (result.IsRejected && result.ErrorCode == ErrorCode.ModuleExecutionFailed)
        {
            HandleLifecycleFailure(
                runtime,
                ErrorCode.ModuleExecutionFailed,
                "遷移中の State_Module lifecycle に失敗しました。",
                LifecycleOperation.Update,
                result.CurrentStateId,
                request.FrameUpdateNumber);
        }

        return result;
    }

    private ErrorOr<Success> RunUpdate(BossRuntimeSlot runtime, long frameUpdateNumber)
    {
        if (runtime.IsFailClosed)
        {
            runtime.MarkUpdated(frameUpdateNumber);
            return Error.Failure("BossAiRuntime.FailClosed", "Runtime が fail-closed 状態です。");
        }

        if (runtime.ModuleStopped)
        {
            if (!runtime.RevalidateStoppedModule().IsError)
            {
                return Error.Failure("BossAiRuntime.ModuleStopped", "停止済み State_Module を再開しました。");
            }

            RecordFailure(runtime, runtime.ModuleStopReason == ErrorCode.None
                ? ErrorCode.ModuleExecutionFailed
                : runtime.ModuleStopReason, "停止済み State_Module は再実行しません。", LifecycleOperation.Update);
            runtime.MarkUpdated(frameUpdateNumber);
            return Error.Failure("BossAiRuntime.ModuleStopped", "停止済み State_Module は再実行しません。");
        }

        if (!runtime.CurrentStateId.IsValid() || !runtime.StateEntered || !_registry.Get(runtime.CurrentStateId, out IBossStateModule module))
        {
            return HandleLifecycleFailure(
                runtime,
                ErrorCode.UnregisteredState,
                "現在 State_Module が固定 map から取得できません。",
                LifecycleOperation.Update,
                runtime.CurrentStateId,
                frameUpdateNumber);
        }

        runtime.ClearCommands();
        ref StateContext context = ref runtime.Context;
        LifecycleInvocation invocation = new(
            runtime.BossType,
            runtime.EntityId,
            runtime.CurrentStateId,
            LifecycleOperation.Update,
            frameUpdateNumber,
            runtime.DeltaTime,
            runtime.Phase);
        try
        {
            module.Update(ref context, in invocation);

            IBossBehaviorState? behaviorState = _behaviorStateProvider?.Invoke(runtime.BossType);
            if (behaviorState is not null)
            {
                runtime.MovementData.FacingRight = behaviorState.FacingRight;
            }

            runtime.MarkUpdated(frameUpdateNumber);
            runtime.MarkHealthyFrame();
            runtime.RefreshDebugData();
            RecordLifecycle(runtime, in invocation);

            // State_Module からの遷移要求を消費して適用します
            ProcessPendingTransitions(runtime, frameUpdateNumber);

            return Result.Success;
        }
        catch (Exception ex)
        {
            return HandleLifecycleFailure(
                runtime,
                ErrorCode.ModuleExecutionFailed,
                $"State_Module の Update に失敗しました: {ex.Message}",
                LifecycleOperation.Update,
                runtime.CurrentStateId,
                frameUpdateNumber);
        }
    }

    /// <summary>Boss の行動状態（BehaviorState）を取得します。</summary>
    /// <param name="bossType">Boss の種別。</param>
    /// <returns>登録済みの行動状態インターフェース、存在しない場合は null。</returns>
    public IBossBehaviorState? GetBehaviorState(BossType bossType)
    {
        return _behaviorStateProvider?.Invoke(bossType);
    }

    /// <summary>Boss の現在位置を BehaviorState へ同期します。</summary>
    /// <param name="bossType">Boss の種別。</param>
    /// <param name="x">Boss の X 座標。</param>
    /// <param name="y">Boss の Y 座標。</param>
    public void SynchronizeBossPosition(BossType bossType, float x, float y)
    {
        IBossBehaviorState? state = _behaviorStateProvider?.Invoke(bossType);
        state?.SynchronizePosition(x, y);
    }

    /// <summary>Boss の現在位置、速度、接地状態およびプレイヤー生存状態を BehaviorState へ同期します。</summary>
    /// <param name="bossType">Boss の種別。</param>
    /// <param name="x">Boss の X 座標。</param>
    /// <param name="y">Boss の Y 座標。</param>
    /// <param name="vx">Boss の X 速度。</param>
    /// <param name="vy">Boss の Y 速度。</param>
    /// <param name="isGrounded">Boss が接地しているかどうか。</param>
    /// <param name="isPlayerAlive">プレイヤーが生存しているかどうか。</param>
    public void SynchronizeBossState(BossType bossType, float x, float y, float vx, float vy, bool isGrounded, bool isPlayerAlive)
    {
        IBossBehaviorState? state = _behaviorStateProvider?.Invoke(bossType);
        state?.SynchronizeState(x, y, vx, vy, isGrounded, isPlayerAlive);
    }

    private void ProcessPendingTransitions(BossRuntimeSlot runtime, long frameUpdateNumber)
    {
        if (_behaviorStateProvider is null)
        {
            return;
        }

        IBossBehaviorState? behaviorState = _behaviorStateProvider(runtime.BossType);
        if (behaviorState is null)
        {
            return;
        }

        int maxTransitions = 4;
        while (maxTransitions-- > 0)
        {
            if (!behaviorState.ConsumeTransition(out BossState nextStateId, out TransitionReason reason))
            {
                break;
            }

            if (!nextStateId.IsValid())
            {
                break;
            }

            TransitionRequest request = new(
                runtime.CurrentStateId,
                nextStateId,
                reason,
                runtime.Phase,
                runtime.EntityId,
                frameUpdateNumber,
                nextStateId);

            TransitionResult result = _transitionService.Apply(ref runtime, in request);
            if (!result.IsAccepted)
            {
                break;
            }
        }
    }

    private ErrorOr<Success> HandleLifecycleFailure(
        BossRuntimeSlot runtime,
        ErrorCode errorCode,
        string message,
        LifecycleOperation operation,
        BossState stateId,
        long frameUpdateNumber)
    {
        if (!RecordFailure(runtime, errorCode, message, operation, stateId, frameUpdateNumber))
        {
            runtime.MarkUpdated(frameUpdateNumber);
            return Error.Failure("BossAiRuntime.LifecycleFailure", message);
        }

        runtime.MarkModuleStopped(errorCode);
        SafetyParameterApplicationResult safety = runtime.ApplySafetyParameters(SafetyParameter.Default);
        RecordSafetyFailures(runtime, in safety, frameUpdateNumber);
        ErrorOr<Success> recovered = SafetyTransition(runtime, frameUpdateNumber);
        if (recovered.IsError)
        {
            TransitionReason reason = new(
                ReasonKind.ErrorRecovery,
                errorCode: ErrorCode.SafetyRecoveryFailed,
                originalInput: (long)runtime.CurrentStateId,
                supplementaryValue: frameUpdateNumber);
            runtime.FailClosed(PhaseDeathController.GetDeadState(runtime.BossType), in reason, frameUpdateNumber);
            RecordFailureObservation(runtime, ErrorCode.SafetyRecoveryFailed, "安全復帰先を使用できないため停止しました。", LifecycleOperation.Update, runtime.CurrentStateId);
        }

        runtime.MarkUpdated(frameUpdateNumber);
        return recovered;
    }

    private ErrorOr<Success> SafetyTransition(BossRuntimeSlot runtime, long frameUpdateNumber)
    {
        if (runtime.BeginSafetyTransition().IsError)
        {
            return Error.Failure("BossAiRuntime.SafetyTransition", "安全遷移は既に試行済みです。");
        }

        BossState safeState = _safeBossState.IsValid()
            ? _safeBossState
            : PhaseDeathController.GetPhaseTransitionState(runtime.BossType) switch
            {
                BossState.Boss2_PhaseTransition => BossState.Boss2_Idle,
                _ => BossState.Boss1_Hover
            };
        if (!safeState.IsValid() || runtime.CurrentStateId == safeState || !_registry.Get(safeState, out _))
        {
            return Error.Failure("BossAiRuntime.SafetyTransition", "安全復帰先が無効です。");
        }

        TransitionReason reason = new(
            ReasonKind.ErrorRecovery,
            errorCode: ErrorCode.ModuleExecutionFailed,
            originalInput: (long)runtime.CurrentStateId,
            supplementaryValue: frameUpdateNumber);
        TransitionRequest request = new(
            runtime.CurrentStateId,
            safeState,
            reason,
            runtime.Phase,
            runtime.EntityId,
            frameUpdateNumber,
            safeState);
        TransitionResult result = _transitionService.Apply(ref runtime, in request);
        if (result.IsRejected)
        {
            runtime.MarkModuleStopped(ErrorCode.SafetyRecoveryFailed);
            RecordTransition(runtime, in request, in result);
            return Error.Failure("BossAiRuntime.SafetyTransition", result.Message);
        }

        return Result.Success;
    }

    private void RecordSafetyFailures(
        BossRuntimeSlot runtime,
        in SafetyParameterApplicationResult result,
        long frameUpdateNumber)
    {
        if (!result.HasFailure)
        {
            return;
        }

        ObservationEvent observation = new(
            runtime.EntityId,
            runtime.BossType,
            runtime.CurrentStateId,
            runtime.Phase,
            LifecycleOperation.Update,
            frameUpdateNumber,
            runtime.DeltaTime,
            transitionReason: runtime.CurrentTransitionReason,
            severity: DiagnosticSeverity.ExecutionError,
            errorCode: ErrorCode.SafetyRecoveryFailed,
            message: $"安全値の一部適用に失敗しました: {result.FailedMask}。適用済み値を保持します。");
        RecordObservation(in observation);
    }

    private void RecordLifecycle(BossRuntimeSlot runtime, in LifecycleInvocation invocation)
    {
        ObservationEvent observation = new(
            invocation.EntityId,
            invocation.BossType,
            invocation.State,
            invocation.Phase,
            invocation.Operation,
            invocation.FrameUpdateNumber,
            invocation.DeltaTime,
            default,
            default,
            runtime.CurrentTransitionReason,
            DiagnosticSeverity.Information,
            ErrorCode.None,
            GetLifecycleMessage(invocation.Operation));
        RecordObservation(in observation);
    }

    private bool RecordFailure(
        BossRuntimeSlot runtime,
        ErrorCode errorCode,
        string message,
        LifecycleOperation operation,
        BossState stateId = default,
        long frameUpdateNumber = long.MinValue)
    {
        BossState effectiveBossState = stateId.IsValid() ? stateId : runtime.CurrentStateId;
        long effectiveFrame = frameUpdateNumber == long.MinValue ? runtime.FrameUpdateNumber : frameUpdateNumber;
        ulong fingerprint = ComputeFailureFingerprint(errorCode, effectiveBossState, operation);
        if (!runtime.HandleFailure(fingerprint, effectiveFrame))
        {
            return false;
        }

        RecordFailureObservation(runtime, errorCode, message, operation, effectiveBossState);
        return true;
    }

    private void RecordFailureObservation(
        BossRuntimeSlot runtime,
        ErrorCode errorCode,
        string message,
        LifecycleOperation operation,
        BossState stateId)
    {
        ObservationEvent observation = new(
            runtime.EntityId,
            runtime.BossType,
            stateId,
            runtime.Phase,
            operation,
            runtime.LastHandledFrame,
            runtime.DeltaTime,
            default,
            default,
            runtime.CurrentTransitionReason,
            DiagnosticSeverity.ExecutionError,
            errorCode,
            message);
        RecordObservation(in observation);
    }

    private static ulong ComputeFailureFingerprint(ErrorCode errorCode, BossState stateId, LifecycleOperation operation)
    {
        // 実行中に変化しない FNV-1a で、同じ無効入力を frame を跨いで同一視します。
        ulong hash = 14695981039346656037UL;
        hash ^= (byte)errorCode;
        hash *= 1099511628211UL;
        hash ^= (byte)operation;
        hash *= 1099511628211UL;
        hash ^= (uint)stateId;
        hash *= 1099511628211UL;
        return hash;
    }

    private void RecordTransition(BossRuntimeSlot runtime, in TransitionRequest request, in TransitionResult result)
    {
        ObservationEvent observation = new(
            runtime.EntityId,
            runtime.BossType,
            result.CurrentStateId,
            request.Phase,
            LifecycleOperation.Update,
            request.FrameUpdateNumber,
            runtime.DeltaTime,
            request.Reason.AttackId,
            default,
            request.Reason,
            DiagnosticSeverity.TransitionRejected,
            result.ErrorCode,
            result.Message,
            request.FromStateId,
            request.ToStateId);
        RecordObservation(in observation);
    }

    private void RecordObservation(in ObservationEvent observation)
    {
        try
        {
            _observation.Record(in observation);
        }
        catch
        {
            // 記録失敗だけを理由に正常 lifecycle を停止しません。
        }
    }

    private static string GetLifecycleMessage(LifecycleOperation operation)
    {
        return operation switch
        {
            LifecycleOperation.Enter => "状態へ入場しました。",
            LifecycleOperation.Update => "状態を更新しました。",
            LifecycleOperation.Exit => "状態から退場しました。",
            _ => "状態処理を実行しました。"
        };
    }
}

/// <summary>Entity に必要な ECS component の検証境界です。</summary>
public interface IEntityComponentValidator
{
    /// <summary>runtime が必要とする component を検証します。</summary>
    bool Validate(BossRuntimeSlot runtime, out string missingComponent);
}
