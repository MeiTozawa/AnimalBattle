using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃生成と次遷移評価を終了条件まで抑止する共通 Recovery State_Module です。</summary>
public sealed class RecoveryModule : IBossStateModule
{
    private readonly AttackDefinition _definition;
    private bool _active;
    private float _elapsed;

    /// <summary>Recovery の State_Id を取得します。</summary>
    public BossState Id { get; }
    /// <summary>現在の攻撃 ID を取得します。</summary>
    public AttackId AttackId => _definition.AttackId;
    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition => _definition;
    public bool IsActive => _active;
    /// <summary>Recovery 完了後に次遷移を評価できるかどうかを取得します。</summary>
    public bool CanEvaluateNextTransition => !_active && _elapsed > 0;
    /// <summary>Recovery の経過時間を取得します。</summary>
    public float Elapsed => _elapsed;

    /// <summary>攻撃定義と State_Id を指定して Recovery を生成します。</summary>
    public RecoveryModule(BossState stateId, AttackDefinition definition)
    {
        if (!stateId.IsValid()) throw new ArgumentOutOfRangeException(nameof(stateId));
        ArgumentNullException.ThrowIfNull(definition);
        Id = stateId;
        _definition = definition;
    }

    /// <summary>Recovery を開始します。開始に失敗した場合は安全な非攻撃行動を選択します。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_active)
        {
            return Result.Success;
        }

        if (!_definition.IsValid || !invocation.IsValid)
        {
            Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "攻撃時間が不正なため Recovery を開始せず、安全な非攻撃行動へ移行します。", 0);
            return Error.Validation("RecoveryModule.Start", "攻撃定義またはフレーム呼び出しが不正です。");
        }

        _elapsed = 0;
        _active = true;
        Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "攻撃の Recovery を開始しました。", 0);
        return Result.Success;
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        Start(ref context, in invocation);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_active)
        {
            return;
        }

        _elapsed = MathF.Min(_definition.RecoveryDuration, _elapsed + context.DeltaTime);
        Record(in context, in invocation, DiagnosticSeverity.Information, ErrorCode.None, "攻撃の Recovery を更新しました。", _elapsed);
        if (_elapsed >= _definition.RecoveryDuration)
        {
            _active = false;
        }
    }

    /// <summary>Recovery 完了後だけ next transition evaluator を実行します。</summary>
    public bool EvaluateNextTransition(Func<bool> transitionEvaluator)
    {
        ArgumentNullException.ThrowIfNull(transitionEvaluator);
        return CanEvaluateNextTransition && transitionEvaluator();
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _active = false;
    }

    private void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        float realDuration)
    {
        ObservationEvent observation = new(
            context.EntityId,
            context.BossType,
            invocation.State,
            context.Phase,
            invocation.Operation,
            context.FrameUpdateNumber,
            context.DeltaTime,
            _definition.AttackId,
            AttackStage.Recovery,
            new TransitionReason(ReasonKind.AttackCompleted, attackId: _definition.AttackId),
            severity,
            errorCode,
            message,
            configuredDuration: _definition.RecoveryDuration,
            realDuration: realDuration);
        try
        {
            context.Observation.Record(in observation);
        }
        catch
        {
            // 観測失敗だけを理由に Recovery を停止しません。
        }
    }
}
