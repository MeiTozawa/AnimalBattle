using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 component を lifecycle 内で一回だけ port へ発行する Execute gate です。</summary>
public sealed class ExecuteGate
{
    private readonly AttackDefinition _definition;
    private readonly OneShotEventGuard _oneShot;
    private readonly float _facingDirection;
    private bool _active;
    private bool _completed;
    private float _elapsed;

    /// <summary>Execute が有効かどうかを取得します。</summary>
    public bool IsActive => _active;
    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition => _definition;
    /// <summary>Execute 完了後かどうかを取得します。</summary>
    public bool IsComplete => !_active && _elapsed > 0;
    /// <summary>Execute の経過時間を取得します。</summary>
    public float Elapsed => _elapsed;
    /// <summary>component 発行を抑止する one-shot guard を取得します。</summary>
    public OneShotEventGuard OneShot => _oneShot;

    /// <summary>攻撃定義と向き、one-shot guard を指定して Execute gate を生成します。</summary>
    /// <param name="definition">攻撃定義。</param>
    /// <param name="facingDirection">キャラクターの向き（正: 右向き 1.0f、負: 左向き -1.0f）。</param>
    /// <param name="oneShot">多重発火防止ガード。</param>
    public ExecuteGate(AttackDefinition definition, float facingDirection = 1.0f, OneShotEventGuard? oneShot = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        _facingDirection = facingDirection >= 0.0f ? 1.0f : -1.0f;
        _oneShot = oneShot ?? new OneShotEventGuard(Math.Max(1, definition.ComponentCount));
    }

    /// <summary>Execute を開始し、component 発行 key を新しい lifecycle に切り替えます。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_active)
        {
            return Result.Success;
        }

        if (_completed)
        {
            return Error.Failure("ExecuteGate.Start", "既に Execute は完了しています。");
        }

        if (!_definition.IsValid || !invocation.IsValid || _oneShot.Begin(_definition.Skill, invocation.FrameUpdateNumber).IsError)
        {
            _completed = true;
            Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "攻撃時間が不正なため Execute を開始せず、安全な非攻撃行動へ移行します。", 0, -1);
            return Error.Validation("ExecuteGate.Start", "攻撃設定またはフレーム番号が不正です。");
        }

        _elapsed = 0;
        _active = true;
        return Result.Success;
    }

    /// <summary>現在 frame の Execute を更新し、未発行 component を port へ一回だけ渡します。</summary>
    public ExecuteResult Execute(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!_active && Start(ref context, in invocation).IsError)
        {
            return ExecuteResult.Rejected;
        }

        int emitted = 0;
        int failed = 0;
        int suppressed = 0;
        for (int index = 0; index < _definition.ComponentCount; index++)
        {
            if (!_definition.GetComponent(index, out AttackComponent component))
            {
                continue;
            }

            // 指定された発動時刻に達していないコンポーネントは保留（浮動小数点の累積誤差を考慮）
            if (_elapsed + 0.0001f < component.TriggerTime)
            {
                continue;
            }

            if (_oneShot.Reserve(component.Index).IsError)
            {
                suppressed++;
                continue;
            }

            bool accepted;
            try
            {
                accepted = !Emit(ref context, in component).IsError;
            }
            catch
            {
                accepted = false;
            }
            if (accepted)
            {
                emitted++;
            }
            else
            {
                failed++;
                Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed, "攻撃 component の実行に失敗しました。再発動は行いません。", _elapsed, component.Index);
            }
        }

        _elapsed = MathF.Min(_definition.ExecuteDuration, _elapsed + context.DeltaTime);
        if (_elapsed >= _definition.ExecuteDuration)
        {
            _active = false;
            _completed = true;
        }

        return new ExecuteResult(true, _active, emitted, failed, suppressed);
    }

    /// <summary>Execute の別名として lifecycle update から呼び出します。</summary>
    public ExecuteResult Update(ref StateContext context, in LifecycleInvocation invocation)
        => Execute(ref context, in invocation);

    /// <summary>Execute を終了し、次の攻撃の one-shot key を受け付けられる状態にします。</summary>
    public void Reset()
    {
        _active = false;
        _completed = false;
        _elapsed = 0;
        _oneShot.Reset();
    }

    private ErrorOr<Success> Emit(ref StateContext context, in AttackComponent component)
    {
        return component.Kind switch
        {
            AttackComponentKind.Bullet => context.Attack.EmitBullet(_definition.Skill, component.X, component.Y),
            AttackComponentKind.Hitbox => context.Attack.CreateHitbox(_definition.Skill, component.X, component.Y),
            AttackComponentKind.Movement => context.Movement.SetVelocity(component.X * _facingDirection, component.Y),
            _ => Error.Validation("ExecuteGate.Emit", "未知の AttackComponentKind です。")
        };
    }

    private void Record(
        in StateContext context,
        in LifecycleInvocation invocation,
        DiagnosticSeverity severity,
        ErrorCode errorCode,
        string message,
        float realDuration,
        int componentIndex)
    {
        ObservationEvent observation = new(
            context.EntityId,
            context.BossType,
            invocation.State,
            context.Phase,
            invocation.Operation,
            context.FrameUpdateNumber,
            context.DeltaTime,
            _definition.Skill,
            AttackStage.Execute,
            new TransitionReason(
                ReasonKind.AttackCompleted,
                skill: _definition.Skill,
                errorCode: errorCode,
                originalInput: componentIndex),
            severity,
            errorCode,
            message,
            configuredDuration: _definition.ExecuteDuration,
            realDuration: realDuration);

        try
        {
            context.Observation.Record(in observation);
        }
        catch
        {
            // 観測失敗だけを理由に Execute の残り component を停止しません。
        }
    }
}

/// <summary>一回の Execute 更新で発行した component 数と段階状態です。</summary>
public readonly record struct ExecuteResult(
    bool Started,
    bool IsRunning,
    int EmittedCount,
    int FailedCount,
    int SuppressedCount)
{
    /// <summary>開始できなかった結果です。</summary>
    public static ExecuteResult Rejected => new(false, false, 0, 0, 0);
    /// <summary>component 実行失敗があったかどうかを取得します。</summary>
    public bool HasFailure => FailedCount > 0;
    /// <summary>Execute が完了したかどうかを取得します。</summary>
    public bool IsComplete => Started && !IsRunning;
}
