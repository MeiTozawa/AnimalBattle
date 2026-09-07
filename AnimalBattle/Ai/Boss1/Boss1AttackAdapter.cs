using System;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Ai.Dsl.Modules;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;
using ErrorOr;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
/// Boss1 の攻撃ライフサイクルを汎用 DSL モジュール（Telegraph, Recovery）へ適合させるアダプターです。
/// </summary>
/// <remarks>
/// Boss1 の設定および共有状態を注入してアダプターを初期化します。
/// </remarks>
/// <param name="configuration">Boss1 設定境界。</param>
/// <param name="state">Boss1 実行時状態。</param>
public sealed class Boss1AttackAdapter(IBoss1Configuration configuration, Boss1BehaviorState state) : IBossAttackAdapter
{
    private readonly IBoss1Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss1BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState TelegraphState => Boss1State.Telegraph;

    /// <inheritdoc />
    public BossState ExecuteState => Boss1State.Execute;

    /// <inheritdoc />
    public BossState RecoveryState => Boss1State.Recovery;

    /// <inheritdoc />
    public BossState DefaultState => Boss1State.Hover;

    /// <inheritdoc />
    public BossSkill CurrentSkill => _state.CurrentSkill;

    /// <inheritdoc />
    public ErrorOr<AttackDefinition> GetAttackDefinition(BossSkill skill)
    {
        if (_configuration.GetAttack(skill, out Boss1AttackPattern pattern))
        {
            return pattern.Definition;
        }

        return Error.NotFound("Boss1AttackAdapter.GetAttackDefinition", $"指定された技能 {skill} の攻撃定義が見つかりません。");
    }

    /// <inheritdoc />
    public void OnTelegraphEnter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
    }

    /// <inheritdoc />
    public void OnRecoveryEnter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <inheritdoc />
    public void OnRecoveryUpdate(ref StateContext context, in LifecycleInvocation invocation, float elapsed)
    {
        _state.StateElapsed = elapsed;

        // 冲撞（DiveRam）の易傷・後隙期間中のみ地上で静止し、他技能の後隙では通常浮遊移動を維持
        if (_state.CurrentSkill == BossSkill.DiveRam)
        {
            context.Movement.MoveTo(_state.CurrentPosition.X, _state.CurrentPosition.Y);
        }
        else
        {
            DslBoss1Support.ApplyHoverMovement(ref context, in invocation, _configuration, _state, _state.CurrentSkill, AttackStage.Recovery);
            _state.AttackStartPosition = _state.CurrentPosition;
        }
    }

    /// <inheritdoc />
    public void OnRecoveryCompleted()
    {
        _state.IsAttackActive = false;
        _state.RequestTransition(Boss1State.Hover, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
    }

    /// <inheritdoc />
    public void RequestTransition(BossState targetState, TransitionReason reason)
    {
        _state.RequestTransition(targetState, in reason);
    }

    /// <inheritdoc />
    public void SetStateElapsed(float elapsed)
    {
        _state.StateElapsed = elapsed;
    }
}
