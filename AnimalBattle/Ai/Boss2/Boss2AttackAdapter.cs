using System;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Core.Physics;
using ErrorOr;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 の攻撃ライフサイクルを汎用 DSL モジュール（Telegraph, Recovery）へ適合させるアダプターです。
/// </summary>
/// <remarks>
/// Boss2 の設定および共有状態を注入してアダプターを初期化します。
/// </remarks>
/// <param name="configuration">Boss2 設定境界。</param>
/// <param name="state">Boss2 実行時状態。</param>
public sealed class Boss2AttackAdapter(IBoss2Configuration configuration, Boss2BehaviorState state) : IBossAttackAdapter
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState TelegraphState => Boss2State.Telegraph;

    /// <inheritdoc />
    public BossState ExecuteState => Boss2State.Execute;

    /// <inheritdoc />
    public BossState RecoveryState => Boss2State.Recovery;

    /// <inheritdoc />
    public BossState DefaultState => Boss2State.Idle;

    /// <inheritdoc />
    public BossSkill CurrentSkill => _state.CurrentSkill;

    /// <inheritdoc />
    public ErrorOr<AttackDefinition> GetAttackDefinition(BossSkill skill)
    {
        if (_configuration.GetAttack(skill, out Boss2AttackPattern pattern))
        {
            return pattern.Definition;
        }

        return Error.NotFound("Boss2AttackAdapter.GetAttackDefinition", $"指定された技能 {skill} の攻撃定義が見つかりません。");
    }

    /// <inheritdoc />
    public void OnTelegraphEnter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.FacingRight = context.PlayerObservation.PositionX >= _state.CurrentPosition.X;
        _state.StateElapsed = 0.0f;
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

        // 泰山圧頂（LeapSmash）の易傷着地後隙中は移動を完全停止しプレイヤーの攻撃好機を維持
        if (_state.CurrentSkill == BossSkill.LeapSmash)
        {
            context.Movement.Stop();
        }
    }

    /// <inheritdoc />
    public void OnRecoveryCompleted()
    {
        _state.IsAttackActive = false;
        _state.RequestTransition(Boss2State.Idle, new TransitionReason(ReasonKind.AttackCompleted, skill: _state.CurrentSkill));
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
