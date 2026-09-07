using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;
using AnimalBattle.Maps;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Scenes;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 が現在の足場でプレイヤーへ追跡する State_Module です。
/// </summary>
/// <param name="configuration">Boss2 設定境界。</param>
/// <param name="state">Boss2 共有実行時状態。</param>
public sealed class Boss2PatrolChaseModule(IBoss2Configuration configuration, Boss2BehaviorState state)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));

    /// <inheritdoc />
    public BossState HandledState => Boss2State.PatrolChase;
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDependencies { get; } = ["Boss2Configuration", "MovementPort", "PlayerObservation"];

    /// <inheritdoc />
    public void Enter(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
        _state.IsInvulnerable = false;
    }

    /// <inheritdoc />
    public void Update(ref StateContext context, in LifecycleInvocation invocation)
    {
        if (_state.IsDead || !_state.IsGrounded)
        {
            if (!_state.IsGrounded)
            {
                _state.RequestTransition(Boss2State.PlatformTraverse, new TransitionReason(ReasonKind.PlayerObservation));
            }
            return;
        }

        if (_state.JumpCooldown > 0.0f)
        {
            _state.JumpCooldown = MathF.Max(0.0f, _state.JumpCooldown - context.DeltaTime);
        }

        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        float deltaX = context.PlayerObservation.PositionX - _state.CurrentPosition.X;
        float deltaY = context.PlayerObservation.PositionY - _state.CurrentPosition.Y;

        TileMap? tileMap = BattleScene.CurrentTileMap;
        NavPlan? plan = null;

        if (tileMap is not null && tileMap.NavGraph.Nodes.Count > 0)
        {
            float bossFootY = _state.CurrentPosition.Y + (ConfigManager.Boss2.CollisionHeight / 2.0f);
            float playerFootY = context.PlayerObservation.PositionY + (ConfigManager.Player.HitboxHeight / 2.0f);
            plan = tileMap.NavGraph.FindPath(
                _state.CurrentPosition.X,
                bossFootY,
                context.PlayerObservation.PositionX,
                playerFootY,
                _state.IsGrounded,
                settings.MoveSpeed);
        }

        if (plan is not null)
        {
            switch (plan.Status)
            {
                case NavStatus.ExecuteJump:
                    if (_state.JumpCooldown <= 0.0f)
                    {
                        float jumpVy = plan.RequiredVy != 0.0f ? plan.RequiredVy : -settings.JumpForce;
                        float jumpVx = plan.RequiredVx;
                        _state.FacingRight = jumpVx >= 0.0f;
                        _state.IsGrounded = false;
                        _state.JumpCooldown = 1.0f;
                        _state.CurrentVelocity = new(jumpVx, jumpVy);
                        context.Movement.SetVelocity(jumpVx, jumpVy);
                        _state.RequestTransition(Boss2State.PlatformTraverse, new(ReasonKind.PlayerObservation));
                        return;
                    }
                    else
                    {
                        _state.FacingRight = plan.TargetX >= _state.CurrentPosition.X;
                        _state.CurrentVelocity = new(0.0f, _state.CurrentVelocity.Y);
                        context.Movement.SetVelocity(0.0f, _state.CurrentVelocity.Y);
                    }
                    break;

                case NavStatus.MoveToTakeoff:
                case NavStatus.ExecuteDrop:
                case NavStatus.InAir:
                    float takeoffVx = plan.RequiredVx;
                    if (MathF.Abs(takeoffVx) > 5.0f)
                    {
                        _state.FacingRight = takeoffVx > 0.0f;
                    }
                    _state.CurrentVelocity = new(takeoffVx, _state.CurrentVelocity.Y);
                    context.Movement.SetVelocity(takeoffVx, _state.CurrentVelocity.Y);
                    break;

                case NavStatus.DirectWalk:
                default:
                    if (MathF.Abs(deltaX) > 6.0f)
                    {
                        _state.FacingRight = deltaX > 0.0f;
                    }
                    float directVx = MathF.Abs(deltaX) > 6.0f ? MathF.Sign(deltaX) * settings.MoveSpeed : 0.0f;
                    _state.CurrentVelocity = new(directVx, _state.CurrentVelocity.Y);
                    context.Movement.SetVelocity(directVx, _state.CurrentVelocity.Y);
                    break;
            }
        }
        else
        {
            if (MathF.Abs(deltaX) > 6.0f)
            {
                _state.FacingRight = deltaX > 0.0f;
            }
            float velocityX = MathF.Abs(deltaX) > 6.0f ? MathF.Sign(deltaX) * settings.MoveSpeed : 0.0f;
            _state.CurrentVelocity = new(velocityX, _state.CurrentVelocity.Y);
            context.Movement.SetVelocity(velocityX, _state.CurrentVelocity.Y);
        }

        _state.CooldownTracker.Update(context.DeltaTime);

        if (settings.Rules.Count > 0)
        {
            float distance = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            bool isSamePlatform = MathF.Abs(deltaY) <= 60.0f;
            bool isPlayerAirborne = MathF.Abs(context.PlayerObservation.VelocityY) > 15.0f || deltaY < -50.0f;
            bool isPlayerGrounded = !isPlayerAirborne;

            AttackDecisionContext decisionContext = new(
                distance,
                isSamePlatform,
                isPlayerGrounded,
                isPlayerAirborne);

            BossSkill chosenAttack = BossDecisionEvaluator.EvaluateWeighted(
                settings.Rules,
                _state.CooldownTracker,
                in decisionContext);


            if (chosenAttack != BossSkill.None)
            {
                // 選択された技能の冷却時間をトリガー
                for (int index = 0; index < settings.Rules.Count; index++)
                {
                    AttackDecisionRule rule = settings.Rules[index];
                    if (rule.Skill == chosenAttack)
                    {
                        _state.CooldownTracker.Trigger(chosenAttack, rule.Cooldown);
                        break;
                    }
                }

                ExecuteAttack(chosenAttack, ref context, in invocation);
                return;
            }
        }
        else if (MathF.Abs(deltaX) <= 175.0f && MathF.Abs(deltaY) <= 75.0f)
        {
            SelectNextLegacyAttack(ref context, in invocation);
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    private void ExecuteAttack(BossSkill skill, ref StateContext context, in LifecycleInvocation invocation)
    {
        if (!Boss2ModuleSupport.GetPattern(_configuration, skill, in context, in invocation, out Boss2AttackPattern pattern))
        {
            _state.RequestTransition(Boss2State.Idle, new(ReasonKind.SafetyFallback, skill: skill, errorCode: ErrorCode.InvalidBossSkill));
            return;
        }


        _state.CurrentSkill = pattern.Skill;
        _state.AttackStartPosition = _state.CurrentPosition;
        _state.AttackTargetPosition = new(context.PlayerObservation.PositionX, context.PlayerObservation.PositionY);
        _state.AttackTargetEntityId = context.PlayerObservation.EntityId;
        _state.IsAttackActive = true;
        _state.RequestTransition(Boss2State.Telegraph, new(ReasonKind.PlayerObservation, skill: skill));
    }

    private void SelectNextLegacyAttack(ref StateContext context, in LifecycleInvocation invocation)
    {
        Boss2PhaseSettings settings = _configuration.GetPhase(context.Phase);
        if (settings.AttackOrder.Count == 0)
        {
            Boss2ModuleSupport.Record(in context, in invocation, DiagnosticSeverity.Warning, ErrorCode.InvalidConfiguration, "Boss2 の攻撃順が空です。");
            return;
        }

        int index = _state.CurrentAttackIndex % settings.AttackOrder.Count;
        _state.CurrentAttackIndex++;
        BossSkill skill = settings.AttackOrder[index];
        ExecuteAttack(skill, ref context, in invocation);
    }
}

