using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss 2: 泰山圧頂（LeapSmash）の落下・叩きつけ Execute State_Module です。
/// 空中頂点から目標プレイヤー位置へ向けて高速急降下し、着地時に衝撃波と破片弾幕を放ちます。
/// </summary>
public sealed class Boss2LeapSmashDescentModule : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration;
    private readonly Boss2BehaviorState _state;

    /// <inheritdoc />
    public BossState Id => Boss2StateIds.LeapSmashDescent;

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "AttackPort", "ObservationService", "PlayerObservation"];

    /// <summary>Boss2 設定と共有実行値を注入します。</summary>
    public Boss2LeapSmashDescentModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;

        // 頂点からプレイヤーの足元へ向けて高速叩きつけ急降下（下方向猛烈加速）
        float diveVy = 900.0f;
        float targetX = _state.AttackTargetPosition.X != 0.0f ? _state.AttackTargetPosition.X : context.PlayerObservation.PositionX;
        float currentX = _state.CurrentPosition.X;
        float horizontalDist = targetX - currentX;
        float diveVx = Math.Clamp(horizontalDist / 0.25f, -450.0f, 450.0f);

        _state.FacingRight = diveVx >= 0.0f;
        context.Movement.SetVelocity(diveVx, diveVy);
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
        context.Movement.ApplyGravity(settings.Gravity * 1.5f, 1300.0f);

        // 地面または足場に着地（激突）した時
        if (_state.IsGrounded && _state.StateElapsed > 0.05f)
        {
            // 着地激突衝撃波＆破片弾幕を生成
            context.Attack.CreateHitbox(Boss2AttackIds.LeapSmash, _state.CurrentPosition.X, _state.CurrentPosition.Y);

            _state.IsAttackActive = false;
            _state.RequestTransition(Boss2StateIds.Recovery, new TransitionReason(ReasonKind.AttackCompleted, attackId: _state.CurrentAttackId));
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
    }
}

