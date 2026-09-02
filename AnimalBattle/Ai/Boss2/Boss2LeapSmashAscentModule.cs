using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss 2: 泰山圧頂（LeapSmash）の上昇 Execute State_Module です。
/// 目標プレイヤー位置へ向かう跳躍初速と水平速度を設定して空高く跳び上がります。
/// </summary>
public sealed class Boss2LeapSmashAscentModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.LeapSmashAscent;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "ObservationService", "PlayerObservation"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2LeapSmashAscentModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsAttackActive = true;

        if (!Boss2ModuleSupport.GetPattern(_configuration, _state.CurrentAttackId, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2StateIds.Idle, new TransitionReason(ReasonKind.SafetyFallback, attackId: _state.CurrentAttackId, errorCode: ErrorCode.InvalidAttackId));
            return;
        }

        // 目標プレイヤー座標へ向けて跳躍力と水平速度を算出・適用
        float jumpForce = pattern.MoveSpeed > 0.0f ? pattern.MoveSpeed : 680.0f;
        float targetX = _state.AttackTargetPosition.X != 0.0f ? _state.AttackTargetPosition.X : context.PlayerObservation.PositionX;
        float currentX = _state.CurrentPosition.X;
        float horizontalDist = targetX - currentX;

        // 上昇所要時間（約 0.40秒）でプレイヤーの X 座標に到達する水平初速を計算
        float ascentDuration = 0.40f;
        float targetVx = Math.Clamp(horizontalDist / ascentDuration, -550.0f, 550.0f);

        _state.FacingRight = targetVx >= 0.0f;
        context.Movement.SetVelocity(targetVx, -jumpForce);
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead)
        {
            return;
        }

        _state.StateElapsed += context.DeltaTime;
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        context.Movement.ApplyGravity(settings.Gravity, settings.MaxFallSpeed);

        // 垂直速度が下向き（頂点到達）または一定時間経過で叩きつけ急降下（Descent）へ移行
        if (_state.CurrentVelocity.Y >= 0.0f || _state.StateElapsed >= 0.40f)
        {
            _state.RequestTransition(Boss2StateIds.LeapSmashDescent, new TransitionReason(ReasonKind.TimerElapsed, attackId: _state.CurrentAttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
    }
}

