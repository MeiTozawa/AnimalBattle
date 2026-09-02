using System;
using System.Collections.Generic;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss 2: 泰山圧頂（LeapSmash）の高速垂直急降下・叩きつけ Execute State_Module です。
/// 空中頭上から真下へ向けて超高速で垂直急降下し、着地時に衝撃波と破片弾幕を放ちます。
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

        LeapSmashConfig leapCfg = ConfigManager.Pattern.LeapSmash;
        float descentSpeed = leapCfg.DescentSpeed > 0.0f ? leapCfg.DescentSpeed : 1200.0f;

        // 水平速度を 0 にして純粋な超高速垂直急降下（真下叩きつけ）
        context.Movement.SetVelocity(0.0f, descentSpeed);
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
        LeapSmashConfig leapCfg = ConfigManager.Pattern.LeapSmash;
        float descentSpeed = leapCfg.DescentSpeed > 0.0f ? leapCfg.DescentSpeed : 1200.0f;

        // 水平移動せず真下へ高速落下
        context.Movement.SetVelocity(0.0f, descentSpeed);
        context.Movement.ApplyGravity(settings.Gravity * 2.0f, 1500.0f);


        // 地面または足場に着地（激突）した時
        if (_state.IsGrounded && _state.StateElapsed > 0.03f)
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

