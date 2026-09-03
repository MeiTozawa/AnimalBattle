using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
/// 遷移検証、即時循環防止、ライフサイクル適用を一元管理します。
/// </summary>
public sealed class TransitionService : ITransitionService
{
    private readonly IEnumModuleMap _registry;
    private readonly IObservationService? _observation;
    private readonly Func<BossState, BossState, bool>? _isAllowed;
    private readonly BossState _defaultSafeRecoveryStateId;
    private readonly BossState _deadStateId;

    /// <summary>遷移サービスを生成します。</summary>
    public TransitionService(
        IEnumModuleMap registry,
        IObservationService? observation = null,
        Func<BossState, BossState, bool>? isAllowed = null,
        BossState defaultSafeRecoveryStateId = default,
        BossState deadStateId = default)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _observation = observation;
        _isAllowed = isAllowed;
        _defaultSafeRecoveryStateId = defaultSafeRecoveryStateId;
        _deadStateId = deadStateId.IsValid() ? deadStateId : BossState.Boss1_Dead;
    }

    /// <summary>検証順序に従って遷移要求を検証します。</summary>
    public TransitionResult Request(in TransitionRequest request)
    {
        TransitionResult result = ValidateRequest(in request, request.FromStateId, true);
        if (result.IsAccepted)
        {
            RecordTransition(in request, result, BossType.Unknown);
        }

        return result;
    }

    /// <summary>合法な要求を Exit、data commit、Enter の順に適用します。</summary>
    public TransitionResult Apply(ref BossRuntimeSlot runtime, in TransitionRequest request)
    {
        TransitionResult validation = ValidateRequest(in request, runtime.CurrentStateId, false);
        if (validation.IsRejected)
        {
            RecordTransition(in request, validation, runtime.BossType);
            return validation;
        }

        if (runtime.CurrentStateId != request.FromStateId)
        {
            TransitionResult stateMismatch = Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionNotAllowed, "現在状態と遷移要求の State_Id が一致しません。", false);
            RecordTransition(in request, stateMismatch, runtime.BossType);
            return stateMismatch;
        }

        if (request.EntityId != runtime.EntityId || request.FrameUpdateNumber != runtime.FrameUpdateNumber || request.Phase != runtime.Phase)
        {
            TransitionResult frameMismatch = Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionNotAllowed, "遷移要求の Entity または Frame_Update が一致しません。", false);
            RecordTransition(in request, frameMismatch, runtime.BossType);
            return frameMismatch;
        }

        if (runtime.EnterTransition().IsError)
        {
            TransitionResult cycle = Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionCycle, "即時遷移の再帰を拒否しました。", false);
            RecordTransition(in request, cycle, runtime.BossType);
            return cycle;
        }

        try
        {
            if (!_registry.Get(request.FromStateId, out IBossStateModule currentModule)
                || !_registry.Get(request.ToStateId, out IBossStateModule nextModule))
            {
                TransitionResult unregistered = Reject(in request, runtime.CurrentStateId, ErrorCode.UnregisteredState, "固定 map に対応する State_Module がありません。", false);
                RecordTransition(in request, unregistered, runtime.BossType);
                return unregistered;
            }

            ref StateContext context = ref runtime.Context;
            if (runtime.StateEntered)
            {
                LifecycleInvocation exitInvocation = CreateInvocation(runtime, request.FromStateId, LifecycleOperation.Exit, runtime.DeltaTime);
                try
                {
                    currentModule.Exit(ref context, in exitInvocation);
                }
                catch
                {
                    runtime.MarkModuleStopped(ErrorCode.ModuleExecutionFailed);
                    TransitionResult exitFailure = TransitionResult.Rejected(in request, runtime.CurrentStateId, ErrorCode.ModuleExecutionFailed, "現在 State_Module の Exit に失敗しました。");
                    RecordTransition(in request, exitFailure, runtime.BossType);
                    return exitFailure;
                }
            }

            if (request.ToStateId == BossState.Boss1_AttackSelection)
            {
                runtime.ApplyPendingConfigAtAttackSelection();
            }
            else
            {
                runtime.ApplyPendingConfigAtTransition();
            }

            TransitionReason previousReason = runtime.CurrentTransitionReason;
            runtime.SetCurrentState(request.ToStateId, request.Reason);
            LifecycleInvocation enterInvocation = CreateInvocation(runtime, request.ToStateId, LifecycleOperation.Enter, runtime.DeltaTime);
            try
            {
                nextModule.Enter(ref context, in enterInvocation);
                runtime.MarkStateEntered();
            }
            catch
            {
                runtime.SetCurrentState(request.FromStateId, previousReason);
                runtime.MarkModuleStopped();
                TransitionResult enterFailure = TransitionResult.Rejected(in request, runtime.CurrentStateId, ErrorCode.ModuleExecutionFailed, "遷移先 State_Module の Enter に失敗しました。");
                RecordTransition(in request, enterFailure, runtime.BossType);
                return enterFailure;
            }

            TransitionResult accepted = TransitionResult.Accepted(in request, runtime.CurrentStateId);
            RecordTransition(in request, accepted, runtime.BossType);
            return accepted;
        }
        finally
        {
            runtime.ExitTransition();
        }
    }

    private TransitionResult ValidateRequest(in TransitionRequest request, BossState currentBossState, bool record)
    {
        if (!request.FromStateId.IsValid() || !request.ToStateId.IsValid())
        {
            return Reject(in request, currentBossState, ErrorCode.InvalidStateId, "State_Id の形式が不正です。", record);
        }

        if (request.EntityId == 0 || request.FrameUpdateNumber < 0 || request.Phase == Phase.Unknown || request.Reason.Kind == ReasonKind.Unknown || !request.Reason.HasValidThreshold)
        {
            return Reject(in request, currentBossState, ErrorCode.InvalidConfiguration, "遷移要求の必須値が不正です。", record);
        }

        bool fromRegistered = _registry.Get(request.FromStateId, out _);
        bool toRegistered = _registry.Get(request.ToStateId, out _);
        if (!fromRegistered || !toRegistered)
        {
            return Reject(in request, currentBossState, ErrorCode.UnregisteredState, "固定 map に対応する State_Id がありません。", record);
        }

        if (IsDeadState(request.FromStateId) && request.ToStateId != request.FromStateId)
        {
            return Reject(in request, currentBossState, ErrorCode.TerminalStateViolation, "Dead 状態からの復帰を拒否しました。", record);
        }

        if (_isAllowed is not null && !_isAllowed(request.FromStateId, request.ToStateId))
        {
            return Reject(in request, currentBossState, ErrorCode.TransitionNotAllowed, "許可されていない状態遷移です。", record);
        }

        if (request.FromStateId == request.ToStateId)
        {
            return Reject(in request, currentBossState, ErrorCode.TransitionCycle, "同一状態への即時循環を拒否しました。", record);
        }

        EnumCompletenessResult registryValidation = _registry.ValidateComplete();
        if (registryValidation.IsFailure)
        {
            return Reject(in request, currentBossState, registryValidation.ErrorCode, registryValidation.Message, record);
        }

        return TransitionResult.Accepted(in request, currentBossState, "遷移を検証しました。");
    }

    private TransitionResult Reject(in TransitionRequest request, BossState currentBossState, ErrorCode errorCode, string message, bool record)
    {
        BossState safeRecoveryBossState = request.SafeRecoveryStateId.IsValid()
            ? request.SafeRecoveryStateId
            : _defaultSafeRecoveryStateId;
        TransitionRequest effectiveRequest = new(
            request.FromStateId,
            request.ToStateId,
            request.Reason,
            request.Phase,
            request.EntityId,
            request.FrameUpdateNumber,
            safeRecoveryBossState);
        TransitionResult result = TransitionResult.Rejected(in effectiveRequest, currentBossState, errorCode, message);
        if (record)
        {
            RecordTransition(in request, result, BossType.Unknown);
        }

        return result;
    }

    private static bool IsDeadState(BossState state)
        => state is BossState.Boss1_Dead or BossState.Boss2_Dead;

    private void RecordTransition(in TransitionRequest request, TransitionResult result, BossType bossType)
    {
        if (_observation is null)
        {
            return;
        }

        DiagnosticSeverity severity = result.IsAccepted
            ? DiagnosticSeverity.Information
            : DiagnosticSeverity.TransitionRejected;
        ObservationEvent observation = new(
            request.EntityId,
            bossType,
            result.CurrentStateId,
            request.Phase,
            LifecycleOperation.Update,
            request.FrameUpdateNumber,
            0,
            request.Reason.Skill,
            AttackStage.None,

            request.Reason,
            severity,
            result.ErrorCode,
            result.Message,
            request.FromStateId,
            request.ToStateId);
        try
        {
            _observation.Record(in observation);
        }
        catch
        {
            // 観測失敗は状態処理を停止させません。
        }
    }

    private static LifecycleInvocation CreateInvocation(BossRuntimeSlot runtime, BossState stateId, LifecycleOperation operation, float deltaTime)
    {
        return new LifecycleInvocation(
            runtime.BossType,
            runtime.EntityId,
            stateId,
            operation,
            runtime.FrameUpdateNumber,
            deltaTime,
            runtime.Phase);
    }
}
