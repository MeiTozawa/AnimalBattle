using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 初期化 gate、State_Module lifecycle、Frame_Update 境界を管理する共通 runtime です。
/// </summary>
public sealed class BossAiRuntime
{
    private readonly IStateRegistry _registry;
    private readonly ITransitionService _transitionService;
    private readonly IObservationService _observation;
    private readonly IEntityComponentValidator? _componentValidator;
    private readonly StateId _safeStateId;

    /// <summary>共通 runtime を生成します。</summary>
    public BossAiRuntime(
        IStateRegistry registry,
        ITransitionService transitionService,
        IObservationService observation,
        StateId safeStateId = default,
        IEntityComponentValidator? componentValidator = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(transitionService);
        ArgumentNullException.ThrowIfNull(observation);
        _registry = registry;
        _transitionService = transitionService;
        _observation = observation;
        _safeStateId = safeStateId;
        _componentValidator = componentValidator;
    }

    /// <summary>Registry、context、component を検証して初期状態へ一回入場します。</summary>
    public bool TryInitialize(BossRuntimeSlot runtime, StateId initialStateId)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (runtime.InitializationComplete && runtime.RuntimeReady)
        {
            return runtime.CurrentStateId == initialStateId;
        }

        RegistryValidationResult registryValidation = _registry.Validate();
        if (registryValidation.IsFailure)
        {
            RecordFailure(runtime, registryValidation.ErrorCode, registryValidation.Message, LifecycleOperation.Enter);
            return false;
        }

        if (!runtime.Context.IsReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "State_Context の必須依存関係が未検証です。", LifecycleOperation.Enter);
            return false;
        }

        if (_componentValidator is not null && !_componentValidator.Validate(runtime, out string missingComponent))
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, $"Entity component が不足しています: {missingComponent}", LifecycleOperation.Enter);
            return false;
        }

        if (!_registry.TryGet(initialStateId, out IBossStateModule module))
        {
            RecordFailure(runtime, ErrorCode.UnregisteredState, "初期 State_Id が未登録です。", LifecycleOperation.Enter, initialStateId);
            return false;
        }

        runtime.MarkInitializationComplete();
        runtime.MarkRuntimeReady(true);
        TransitionReason reason = new(ReasonKind.InitialEntry);
        runtime.SetCurrentState(initialStateId, reason);
        ref StateContext context = ref runtime.Context;
        LifecycleInvocation invocation = new(
            runtime.BossType,
            runtime.EntityId,
            initialStateId,
            LifecycleOperation.Enter,
            runtime.FrameUpdateNumber,
            runtime.DeltaTime,
            runtime.Phase);
        try
        {
            module.Enter(ref context, in invocation);
            runtime.MarkStateEntered();
            RecordLifecycle(runtime, in invocation);
            return true;
        }
        catch
        {
            runtime.MarkModuleStopped();
            RecordFailure(runtime, ErrorCode.ModuleExecutionFailed, "初期 State_Module の Enter に失敗しました。", LifecycleOperation.Enter, initialStateId);
            TrySafetyTransition(runtime, runtime.FrameUpdateNumber);
            return false;
        }
    }

    /// <summary>検証済み Frame_Update を開始して現在 module の Update を一回だけ実行します。</summary>
    public bool TryUpdate(BossRuntimeSlot runtime, long frameUpdateNumber, float deltaTime, Phase phase, in PlayerObservation playerObservation)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "初期化 gate が成立していません。", LifecycleOperation.Update);
            return false;
        }

        if (runtime.LastUpdatedFrame >= 0 && frameUpdateNumber <= runtime.LastUpdatedFrame)
        {
            RecordFailure(runtime, ErrorCode.LifecycleViolation, "Frame_Update 番号が後退または重複しています。", LifecycleOperation.Update);
            return false;
        }

        if (!runtime.TryBeginFrame(frameUpdateNumber, deltaTime, phase, in playerObservation))
        {
            RecordFailure(runtime, ErrorCode.InvalidTimer, "Frame_Update の入力値が不正です。", LifecycleOperation.Update);
            return false;
        }

        return RunUpdate(runtime, frameUpdateNumber);
    }

    /// <summary>すでに開始済みの Frame_Update で現在 module を更新します。</summary>
    public bool TryUpdate(BossRuntimeSlot runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!runtime.InitializationComplete || !runtime.RuntimeReady)
        {
            RecordFailure(runtime, ErrorCode.MissingDependency, "初期化 gate が成立していません。", LifecycleOperation.Update);
            return false;
        }

        if (runtime.LastUpdatedFrame >= 0 && runtime.FrameUpdateNumber <= runtime.LastUpdatedFrame)
        {
            RecordFailure(runtime, ErrorCode.LifecycleViolation, "Frame_Update 番号が後退または重複しています。", LifecycleOperation.Update);
            return false;
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

        return _transitionService.Apply(ref runtime, in request);
    }

    private bool RunUpdate(BossRuntimeSlot runtime, long frameUpdateNumber)
    {
        if (runtime.ModuleStopped)
        {
            RecordFailure(runtime, ErrorCode.ModuleExecutionFailed, "停止済み State_Module は再実行しません。", LifecycleOperation.Update);
            runtime.TryMarkUpdated(frameUpdateNumber);
            return false;
        }

        if (!runtime.CurrentStateId.IsValid || !runtime.StateEntered || !_registry.TryGet(runtime.CurrentStateId, out IBossStateModule module))
        {
            RecordFailure(runtime, ErrorCode.UnregisteredState, "現在 State_Module が実行可能ではありません。", LifecycleOperation.Update, runtime.CurrentStateId);
            runtime.MarkModuleStopped();
            runtime.TryMarkUpdated(frameUpdateNumber);
            return false;
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
            runtime.TryMarkUpdated(frameUpdateNumber);
            RecordLifecycle(runtime, in invocation);
            return true;
        }
        catch
        {
            runtime.MarkModuleStopped();
            runtime.TryMarkUpdated(frameUpdateNumber);
            RecordFailure(runtime, ErrorCode.ModuleExecutionFailed, "State_Module の Update に失敗しました。", LifecycleOperation.Update, runtime.CurrentStateId);
            return TrySafetyTransition(runtime, frameUpdateNumber);
        }
    }

    private bool TrySafetyTransition(BossRuntimeSlot runtime, long frameUpdateNumber)
    {
        if (!_safeStateId.IsValid || runtime.CurrentStateId == _safeStateId || !_registry.TryGet(_safeStateId, out _))
        {
            return false;
        }

        TransitionReason reason = new(
            ReasonKind.ErrorRecovery,
            errorCode: ErrorCode.ModuleExecutionFailed,
            originalInput: (long)runtime.CurrentStateId.Value,
            supplementaryValue: frameUpdateNumber);
        TransitionRequest request = new(
            runtime.CurrentStateId,
            _safeStateId,
            reason,
            runtime.Phase,
            runtime.EntityId,
            frameUpdateNumber,
            _safeStateId);
        TransitionResult result = _transitionService.Apply(ref runtime, in request);
        if (result.IsRejected)
        {
            runtime.MarkModuleStopped();
            RecordTransition(runtime, in request, in result);
            return false;
        }

        return true;
    }

    private void RecordLifecycle(BossRuntimeSlot runtime, in LifecycleInvocation invocation)
    {
        ObservationEvent observation = new(
            invocation.EntityId,
            invocation.BossType,
            invocation.StateId,
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
        TryRecord(in observation);
    }

    private void RecordFailure(
        BossRuntimeSlot runtime,
        ErrorCode errorCode,
        string message,
        LifecycleOperation operation,
        StateId stateId = default)
    {
        StateId effectiveStateId = stateId.IsValid ? stateId : runtime.CurrentStateId;
        ObservationEvent observation = new(
            runtime.EntityId,
            runtime.BossType,
            effectiveStateId,
            runtime.Phase,
            operation,
            runtime.FrameUpdateNumber,
            runtime.DeltaTime,
            default,
            default,
            runtime.CurrentTransitionReason,
            DiagnosticSeverity.ExecutionError,
            errorCode,
            message);
        TryRecord(in observation);
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
        TryRecord(in observation);
    }

    private void TryRecord(in ObservationEvent observation)
    {
        try
        {
            _observation.TryRecord(in observation);
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
