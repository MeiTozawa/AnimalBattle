using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using ErrorOr;

namespace AnimalBattle.Ai.Attack;

/// <summary>攻撃 component を lifecycle 内で一回だけ port へ発行する Execute gate です。</summary>
public sealed class ExecuteGate {
    private bool _completed;
    private float _facingDirection;
    private bool _hasLeftGround;
    private float _lockedApexX = float.NaN;
    private float _startX = float.NaN;
    private float _startY = float.NaN;

    /// <summary>攻撃定義と向き、one-shot guard を指定して Execute gate を生成します。</summary>
    /// <param name="definition">攻撃定義。</param>
    /// <param name="facingDirection">キャラクターの向き（正: 右向き 1.0f、負: 左向き -1.0f）。</param>
    /// <param name="oneShot">多重発火防止ガード。</param>
    public ExecuteGate(AttackDefinition definition, float facingDirection = 1.0f, OneShotEventGuard? oneShot = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        _facingDirection = facingDirection >= 0.0f ? 1.0f : -1.0f;
        OneShot = oneShot ?? new OneShotEventGuard(Math.Max(1, definition.ComponentCount));
    }

    /// <summary>Execute が有効かどうかを取得します。</summary>
    public bool IsActive { get; private set; }

    /// <summary>攻撃定義を取得します。</summary>
    public AttackDefinition Definition { get; private set; }

    /// <summary>Execute 完了後かどうかを取得します。</summary>
    public bool IsComplete => !IsActive && (_completed || Elapsed > 0);

    /// <summary>Execute の経過時間を取得します。</summary>
    public float Elapsed { get; private set; }

    /// <summary>component 発行を抑止する one-shot guard を取得します。</summary>
    private OneShotEventGuard OneShot { get; }

    /// <summary>Execute を開始し、component 発行 key を新しい lifecycle に切り替えます。</summary>
    public ErrorOr<Success> Start(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (IsActive) return Result.Success;

        if (_completed) return Error.Failure("ExecuteGate.Start", "既に Execute は完了しています。");

        if (!Definition.IsValid || !invocation.IsValid ||
            OneShot.Begin(Definition.Skill, invocation.FrameUpdateNumber).IsError)
        {
            _completed = true;
            Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration,
                "攻撃時間が不正なため Execute を開始せず、安全な非攻撃行動へ移行します。", 0, -1);
            return Error.Validation("ExecuteGate.Start", "攻撃設定またはフレーム番号が不正です。");
        }

        Elapsed = 0;
        IsActive = true;
        _hasLeftGround = false;
        _startX = float.NaN;
        _startY = float.NaN;
        _lockedApexX = float.NaN;
        return Result.Success;
    }

    /// <summary>現在 frame の Execute を更新し、未発行 component を port へ渡します。</summary>
    /// <param name="context">状態コンテキスト。</param>
    /// <param name="invocation">ライフサイクル呼び出し情報。</param>
    /// <param name="isGrounded">現在キャラクターが接地しているかどうか。</param>
    /// <param name="currentX">キャラクターの現在 X 座標。</param>
    /// <param name="currentY">キャラクターの現在 Y 座標。</param>
    public ExecuteResult Execute(
        ref StateContext context,
        in LifecycleInvocation invocation,
        bool isGrounded = false,
        float currentX = float.NaN,
        float currentY = float.NaN)
    {
        if (!IsActive && Start(ref context, in invocation).IsError) return ExecuteResult.Rejected;

        if (float.IsNaN(_startX) && !float.IsNaN(currentX))
        {
            _startX = currentX;
            _startY = currentY;
        }

        if (!isGrounded) _hasLeftGround = true;

        var emitted = 0;
        var failed = 0;
        var suppressed = 0;
        for (var index = 0; index < Definition.ComponentCount; index++)
        {
            if (!Definition.GetComponent(index, out var component)) continue;

            // 接地時イベント駆動コンポーネントの処理
            if (component.Kind == AttackComponentKind.OnGroundedHitbox)
            {
                // 跳躍前の接地状態による誤発火を防ぐため、一度空中に離脱した後の接地のみ判定
                var minTime = MathF.Max(component.TriggerTime, 0.05f);
                if (_hasLeftGround && isGrounded && Elapsed >= minTime)
                {
                    if (OneShot.Reserve(component.Index).IsError)
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
                        // 接地判定が成功したら、即座に Execute を完了して Recovery へ遷移
                        IsActive = false;
                        _completed = true;
                    }
                    else
                    {
                        failed++;
                        Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed,
                            "接地判定 component の実行に失敗しました。", Elapsed, component.Index);
                    }
                }

                continue;
            }

            // 指定された発動時刻に達していないコンポーネントは保留（浮動小数点の累積誤差を考慮）
            if (Elapsed + 0.0001f < component.TriggerTime) continue;

            // 継続的移動コンポーネント（FlyToTarget: 対象頭上へのイージング飛翔）の更新
            if (component.Kind == AttackComponentKind.FlyToTarget)
            {
                // 後続の急降下（Dive）や移動アクションが既に開始されている場合は MoveTo を停止
                if (HasLaterMovementStarted(component.TriggerTime)) continue;

                var peakY = component.X;
                var flyDuration = component.Y;
                var t = flyDuration > 0.0f
                    ? Math.Clamp((Elapsed - component.TriggerTime) / flyDuration, 0.0f, 1.0f)
                    : 1.0f;

                float curX;
                float curY;
                if (t >= 1.0f)
                {
                    // 最高点到達後は滞空（ApexHang）中であるため、X 座標を固定してプレイヤーの回避余地を保証
                    if (float.IsNaN(_lockedApexX))
                    {
                        var targetX = context.PlayerObservation.PositionX;
                        _lockedApexX = targetX;
                    }

                    curX = _lockedApexX;
                    curY = peakY;
                }
                else
                {
                    var easeT = MathF.Sin(t * MathF.PI * 0.5f);
                    var targetX = context.PlayerObservation.PositionX;
                    var startX = !float.IsNaN(_startX) ? _startX : targetX;
                    var startY = !float.IsNaN(_startY) ? _startY : peakY;
                    curX = startX + (targetX - startX) * easeT;
                    curY = startY + (peakY - startY) * easeT;
                }

                context.Movement.MoveTo(curX, curY);
                continue;
            }

            // 継続的移動コンポーネント（Dive: 垂直急降下）の更新
            if (component.Kind == AttackComponentKind.Dive)
            {
                var descentSpeed = component.Y;
                context.Movement.SetVelocity(0.0f, descentSpeed);
                continue;
            }

            if (OneShot.Reserve(component.Index).IsError)
            {
                suppressed++;
                continue;
            }

            bool acceptedOneShot;
            try
            {
                acceptedOneShot = !Emit(ref context, in component).IsError;
            }
            catch
            {
                acceptedOneShot = false;
            }

            if (acceptedOneShot)
            {
                emitted++;
            }
            else
            {
                failed++;
                Record(in context, in invocation, DiagnosticSeverity.ExecutionError, ErrorCode.ExecutionFailed,
                    "攻撃 component の実行に失敗しました。再発動は行いません。", Elapsed, component.Index);
            }
        }

        if (IsActive)
        {
            Elapsed = MathF.Min(Definition.ExecuteDuration, Elapsed + context.DeltaTime);
            if (Elapsed >= Definition.ExecuteDuration)
            {
                IsActive = false;
                _completed = true;
            }
        }

        return new ExecuteResult(true, IsActive, emitted, failed, suppressed);
    }

    /// <summary>Execute の別名として lifecycle update から呼び出します。</summary>
    public ExecuteResult Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        return Execute(ref context, in invocation);
    }

    /// <summary>Execute を終了し、次の攻撃の one-shot key を受け付けられる状態にします。</summary>
    public void Reset()
    {
        IsActive = false;
        _completed = false;
        _hasLeftGround = false;
        Elapsed = 0;
        _startX = float.NaN;
        _startY = float.NaN;
        _lockedApexX = float.NaN;
        OneShot.Reset();
    }

    /// <summary>新しい攻撃定義と向きを指定して Execute gate をリセットし再利用します。</summary>
    public void Reset(AttackDefinition definition, float forwardDir = 1.0f)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        _facingDirection = forwardDir >= 0.0f ? 1.0f : -1.0f;
        IsActive = false;
        _completed = false;
        _hasLeftGround = false;
        Elapsed = 0;
        _startX = float.NaN;
        _startY = float.NaN;
        _lockedApexX = float.NaN;
        OneShot.Reset();
    }

    private ErrorOr<Success> Emit(ref StateContext context, in AttackComponent component)
    {
        return component.Kind switch {
            AttackComponentKind.Bullet => context.Attack.EmitBullet(Definition.Skill, component.X, component.Y),
            AttackComponentKind.Hitbox or AttackComponentKind.OnGroundedHitbox => context.Attack.CreateHitbox(
                Definition.Skill, component.X, component.Y),
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
            Definition.Skill,
            AttackStage.Execute,
            new TransitionReason(
                ReasonKind.AttackCompleted,
                skill: Definition.Skill,
                errorCode: errorCode,
                originalInput: componentIndex),
            severity,
            errorCode,
            message,
            configuredDuration: Definition.ExecuteDuration,
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

    private bool HasLaterMovementStarted(float currentTriggerTime)
    {
        for (var i = 0; i < Definition.ComponentCount; i++)
            if (Definition.GetComponent(i, out var comp))
                if ((comp.Kind == AttackComponentKind.Dive || comp.Kind == AttackComponentKind.Movement)
                 && comp.TriggerTime > currentTriggerTime
                 && Elapsed + 0.0001f >= comp.TriggerTime)
                    return true;

        return false;
    }
}

/// <summary>一回の Execute 更新で発行した component 数と段階状態です。</summary>
public readonly record struct ExecuteResult(
    bool Started,
    bool IsRunning,
    int EmittedCount,
    int FailedCount,
    int SuppressedCount) {
    /// <summary>開始できなかった結果です。</summary>
    public static ExecuteResult Rejected => new(false, false, 0, 0, 0);

    /// <summary>component 実行失敗があったかどうかを取得します。</summary>
    public bool HasFailure => FailedCount > 0;

    /// <summary>Execute が完了したかどうかを取得します。</summary>
    public bool IsComplete => Started && !IsRunning;
}