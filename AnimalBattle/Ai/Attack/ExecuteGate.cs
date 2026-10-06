using System.Diagnostics;
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

    /// <summary>現在実行中の攻撃が持つアーマー種別を取得します。</summary>
    public ArmorType CurrentArmor => IsActive ? Definition.Armor : ArmorType.None;

    /// <summary>現在実行中の攻撃の靭性閾値を取得します。</summary>
    public float PoiseThreshold => IsActive ? Definition.PoiseThreshold : 0.0f;

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
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ExecuteGate 警告] 接地判定 component の Emit 中に例外が発生しました: {ex.Message}");
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
                    }
                }

                continue;
            }

            // 指定された発動時刻に達していないコンポーネントは保留（浮動小数点の累積誤差を考慮）
            if (Elapsed + 0.0001f < component.TriggerTime) continue;

            // 継続的移動コンポーネント（EasingMovement: 対象位置へのイージング平滑移動および滞空）の更新
            if (component.Kind == AttackComponentKind.EasingMovement)
            {
                // 後続の急降下（VelocityDive）や移動アクションが既に開始されている場合は MoveTo を停止
                if (HasLaterMovementStarted(component.TriggerTime)) continue;

                var peakY = component.Param0;
                var flyDuration = component.Param1;
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

            // 継続的移動コンポーネント（VelocityDive: 垂直・方向急降下等速運動）の更新
            if (component.Kind == AttackComponentKind.VelocityDive)
            {
                var descentSpeed = component.Param1;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExecuteGate 警告] 単発攻撃 component の Emit 中に例外が発生しました: {ex.Message}");
                acceptedOneShot = false;
            }

            if (acceptedOneShot)
            {
                emitted++;
            }
            else
            {
                failed++;
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
            AttackComponentKind.Bullet => EmitBullet(ref context, in component),
            AttackComponentKind.Hitbox or AttackComponentKind.OnGroundedHitbox => context.Attack.CreateHitbox(
                Definition.Skill, component.X, component.Y),
            AttackComponentKind.Movement => context.Movement.SetVelocity(component.X * _facingDirection, component.Y),
            AttackComponentKind.ScreenShake => context.Effect.TriggerScreenShake(component.Param0, component.Param1),
            AttackComponentKind.AudioCue => context.Effect.PlaySoundCue((int)component.Param0, component.Param1),
            _ => Error.Validation("ExecuteGate.Emit", "未知の AttackComponentKind です。")
        };
    }

    private ErrorOr<Success> EmitBullet(ref StateContext context, in AttackComponent component)
    {
        var speed = component.Param0;
        var count = (int)component.Param1;
        var spread = component.Param2;

        if (count <= 1)
            return context.Attack.EmitBullet(Definition.Skill, component.Param0, component.Param1);

        if (Definition.Skill == BossSkill.RingBurst)
        {
            var success = true;
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)count * MathF.Tau;
                if (context.Attack.EmitBullet(Definition.Skill, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed).IsError)
                    success = false;
            }

            return success ? Result.Success : Error.Failure("ExecuteGate.EmitBullet", "RingBurst 弾丸生成に失敗しました。");
        }

        var originX = !float.IsNaN(_startX) ? _startX : context.PlayerObservation.PositionX - 100.0f;
        var originY = !float.IsNaN(_startY) ? _startY : context.PlayerObservation.PositionY;
        var dx = context.PlayerObservation.PositionX - originX;
        var dy = context.PlayerObservation.PositionY - originY;
        if (MathF.Sqrt(dx * dx + dy * dy) <= 0.0001f)
        {
            dx = _facingDirection;
            dy = 0.0f;
        }

        var baseAngle = MathF.Atan2(dy, dx);
        var halfSpread = spread * (count - 1) * 0.5f;
        var allOk = true;
        for (var i = 0; i < count; i++)
        {
            var angle = count == 1 ? baseAngle : baseAngle - halfSpread + i * spread;
            if (context.Attack.EmitBullet(Definition.Skill, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed).IsError)
                allOk = false;
        }

        return allOk ? Result.Success : Error.Failure("ExecuteGate.EmitBullet", "弾幕生成に失敗しました。");
    }


    private bool HasLaterMovementStarted(float currentTriggerTime)
    {
        for (var i = 0; i < Definition.ComponentCount; i++)
            if (Definition.GetComponent(i, out var comp))
                if ((comp.Kind == AttackComponentKind.VelocityDive || comp.Kind == AttackComponentKind.Movement)
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