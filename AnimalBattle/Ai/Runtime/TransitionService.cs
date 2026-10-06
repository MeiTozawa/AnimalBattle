using System.Diagnostics;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;

namespace AnimalBattle.Ai.Runtime;

/// <summary>
///     遷移検証、即時循環防止、ライフサイクル適用を一元管理します。
/// </summary>
public sealed class TransitionService : ITransitionService {
    private readonly BossState _defaultSafeRecoveryStateId;
    private readonly Func<BossState, BossState, bool>? _isAllowed;
    private readonly IEnumModuleMap _registry;

    /// <summary>遷移サービスを生成します。</summary>
    public TransitionService(
        IEnumModuleMap registry,
        Func<BossState, BossState, bool>? isAllowed = null,
        BossState defaultSafeRecoveryStateId = default)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _isAllowed = isAllowed;
        _defaultSafeRecoveryStateId = defaultSafeRecoveryStateId;
    }

    /// <summary>検証順序に従って遷移要求を検証します。</summary>
    public TransitionResult Request(in TransitionRequest request)
    {
        return ValidateRequest(in request, request.FromStateId);
    }

    /// <summary>合法な要求を Exit、data commit、Enter の順に適用します。</summary>
    public TransitionResult Apply(ref BossRuntimeSlot runtime, in TransitionRequest request)
    {
        var validation = ValidateRequest(in request, runtime.CurrentStateId);
        if (validation.IsRejected)
        {
            return validation;
        }

        if (runtime.CurrentStateId != request.FromStateId)
        {
            return Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionNotAllowed,
                "現在状態と遷移要求の State_Id が一致しません。");
        }

        if (request.EntityId != runtime.EntityId || request.FrameUpdateNumber != runtime.FrameUpdateNumber ||
            request.Phase != runtime.Phase)
        {
            return Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionNotAllowed,
                "遷移要求の Entity または Frame_Update が一致しません。");
        }

        if (runtime.EnterTransition().IsError)
        {
            return Reject(in request, runtime.CurrentStateId, ErrorCode.TransitionCycle, "即時遷移の再帰を拒否しました。");
        }

        try
        {
            if (!_registry.Get(request.FromStateId, out var currentModule)
             || !_registry.Get(request.ToStateId, out var nextModule))
            {
                return Reject(in request, runtime.CurrentStateId, ErrorCode.UnregisteredState,
                    "固定 map に対応する State_Module がありません。");
            }

            ref var context = ref runtime.Context;
            if (runtime.StateEntered)
            {
                var exitInvocation = CreateInvocation(runtime, request.FromStateId, LifecycleOperation.Exit,
                    runtime.DeltaTime);
                try
                {
                    currentModule.Exit(ref context, in exitInvocation);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[TransitionService 警告] 現在 State_Module の Exit に失敗しました: {ex.Message}");
                    runtime.MarkModuleStopped();
                    return TransitionResult.Rejected(in request, runtime.CurrentStateId,
                        ErrorCode.ModuleExecutionFailed, "現在 State_Module の Exit に失敗しました。");
                }
            }

            if (request.ToStateId == BossState.Boss1_AttackSelection)
                runtime.ApplyPendingConfigAtAttackSelection();
            else
                runtime.ApplyPendingConfigAtTransition();

            var previousReason = runtime.CurrentTransitionReason;
            runtime.SetCurrentState(request.ToStateId, request.Reason);
            var enterInvocation =
                CreateInvocation(runtime, request.ToStateId, LifecycleOperation.Enter, runtime.DeltaTime);
            try
            {
                nextModule.Enter(ref context, in enterInvocation);
                runtime.MarkStateEntered();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TransitionService 警告] 遷移先 State_Module の Enter に失敗しました: {ex.Message}");
                runtime.SetCurrentState(request.FromStateId, previousReason);
                runtime.MarkModuleStopped();
                return TransitionResult.Rejected(in request, runtime.CurrentStateId,
                    ErrorCode.ModuleExecutionFailed, "遷移先 State_Module の Enter に失敗しました。");
            }

            return TransitionResult.Accepted(in request, runtime.CurrentStateId);
        }
        finally
        {
            runtime.ExitTransition();
        }
    }

    private TransitionResult ValidateRequest(in TransitionRequest request, BossState currentBossState)
    {
        if (!request.FromStateId.IsValid() || !request.ToStateId.IsValid())
            return Reject(in request, currentBossState, ErrorCode.InvalidStateId, "State_Id の形式が不正です。");

        if (request.EntityId == 0 || request.FrameUpdateNumber < 0 || request.Phase == Phase.Unknown ||
            request.Reason.Kind == ReasonKind.Unknown || !request.Reason.HasValidThreshold)
            return Reject(in request, currentBossState, ErrorCode.InvalidConfiguration, "遷移要求の必須値が不正です。");

        var fromRegistered = _registry.Get(request.FromStateId, out _);
        var toRegistered = _registry.Get(request.ToStateId, out _);
        if (!fromRegistered || !toRegistered)
            return Reject(in request, currentBossState, ErrorCode.UnregisteredState, "固定 map に対応する State_Id がありません。");

        if (IsDeadState(request.FromStateId) && request.ToStateId != request.FromStateId)
            return Reject(in request, currentBossState, ErrorCode.TerminalStateViolation, "Dead 状態からの復帰を拒否しました。");

        if (_isAllowed is not null && !_isAllowed(request.FromStateId, request.ToStateId))
            return Reject(in request, currentBossState, ErrorCode.TransitionNotAllowed, "許可されていない状態遷移です。");

        if (request.FromStateId == request.ToStateId)
            return Reject(in request, currentBossState, ErrorCode.TransitionCycle, "同一状態への即時循環を拒否しました。");

        var registryValidation = _registry.ValidateComplete();
        if (registryValidation.IsFailure)
            return Reject(in request, currentBossState, registryValidation.ErrorCode, registryValidation.Message);

        return TransitionResult.Accepted(in request, currentBossState, "遷移を検証しました。");
    }

    private TransitionResult Reject(in TransitionRequest request, BossState currentBossState, ErrorCode errorCode,
        string message)
    {
        var safeRecoveryBossState = request.SafeRecoveryStateId.IsValid()
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
        return TransitionResult.Rejected(in effectiveRequest, currentBossState, errorCode, message);
    }

    private static bool IsDeadState(BossState state)
    {
        return state.IsDead;
    }

    private static LifecycleInvocation CreateInvocation(BossRuntimeSlot runtime, BossState stateId,
        LifecycleOperation operation, float deltaTime)
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