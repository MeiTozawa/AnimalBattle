using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Maps.Navigation;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
/// Boss2 が現在の足場でプレイヤーへ追跡する State_Module です。
/// </summary>
/// <param name="configuration">Boss2 設定境界。</param>
/// <param name="state">Boss2 共有実行時状態。</param>
/// <param name="navGraphProvider">ナビゲーショングラフを取得するためのプロバイダ（省略可能）。</param>
public sealed class Boss2PatrolChaseModule(
    IBoss2Configuration configuration,
    Boss2BehaviorState state,
    Func<PlatformNavigationGraph?>? navGraphProvider = null)
    : IBossStateModule, IStateModuleDependencyProvider
{
    private readonly IBoss2Configuration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly Boss2BehaviorState _state = state ?? throw new ArgumentNullException(nameof(state));
    private readonly Func<PlatformNavigationGraph?>? _navGraphProvider = navGraphProvider;

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

        PlatformNavigationGraph? navGraph = _navGraphProvider?.Invoke();
        NavPlan? plan = null;

        if (navGraph is not null && navGraph.Nodes.Count > 0)
        {
            float bossFootY = _state.CurrentPosition.Y + (_configuration.CombatSettings.CollisionHeight / 2.0f);
            float playerFootY = context.PlayerObservation.PositionY + (_configuration.CombatSettings.PlayerHitboxHeight / 2.0f);
            plan = navGraph.FindPath(
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
                        _state.JumpCooldown = _configuration.CombatSettings.JumpCooldownDuration;
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
                    if (MathF.Abs(takeoffVx) > _configuration.CombatSettings.TakeoffVelocityThreshold)
                    {
                        _state.FacingRight = takeoffVx > 0.0f;
                    }
                    _state.CurrentVelocity = new(takeoffVx, _state.CurrentVelocity.Y);
                    context.Movement.SetVelocity(takeoffVx, _state.CurrentVelocity.Y);
                    break;

                case NavStatus.DirectWalk:
                default:
                    if (MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold)
                    {
                        _state.FacingRight = deltaX > 0.0f;
                    }
                    float directVx = MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold ? MathF.Sign(deltaX) * settings.MoveSpeed : 0.0f;
                    _state.CurrentVelocity = new(directVx, _state.CurrentVelocity.Y);
                    context.Movement.SetVelocity(directVx, _state.CurrentVelocity.Y);
                    break;
            }
        }
        else
        {
            if (MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold)
            {
                _state.FacingRight = deltaX > 0.0f;
            }
            float velocityX = MathF.Abs(deltaX) > _configuration.CombatSettings.MoveThreshold ? MathF.Sign(deltaX) * settings.MoveSpeed : 0.0f;
            _state.CurrentVelocity = new(velocityX, _state.CurrentVelocity.Y);
            context.Movement.SetVelocity(velocityX, _state.CurrentVelocity.Y);
        }

        _state.StateElapsed += context.DeltaTime;
        _state.CooldownTracker.Update(context.DeltaTime);

        if (settings.Rules.Count > 0)
        {
            float distance = MathF.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            bool isSamePlatform = MathF.Abs(deltaY) <= _configuration.CombatSettings.SamePlatformToleranceY;
            bool isPlayerAirborne = MathF.Abs(context.PlayerObservation.VelocityY) > _configuration.CombatSettings.AirborneVelocityThreshold
                || deltaY < -_configuration.CombatSettings.AirborneHeightThreshold;
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
                float cooldown = GetSkillCooldown(chosenAttack, settings);
                _state.CooldownTracker.Trigger(chosenAttack, cooldown);
                _state.StateElapsed = 0.0f;
                ExecuteAttack(chosenAttack, ref context, in invocation);
                return;
            }

            // プレイヤーが長距離・別足場で逃げ続けている場合（追跡タイムアウト）、遠隔・急襲技能を発動
            if (_state.StateElapsed >= settings.ChaseTimeout)
            {
                BossSkill antiKiteSkill = SelectAntiKitingSkill(distance, isSamePlatform, settings);
                if (antiKiteSkill != BossSkill.None)
                {
                    float cooldown = GetSkillCooldown(antiKiteSkill, settings);
                    _state.CooldownTracker.Trigger(antiKiteSkill, cooldown);
                    _state.StateElapsed = 0.0f;
                    ExecuteAttack(antiKiteSkill, ref context, in invocation);
                    return;
                }
            }
        }
        else if (MathF.Abs(deltaX) <= _configuration.CombatSettings.MeleeTriggerRangeX && MathF.Abs(deltaY) <= _configuration.CombatSettings.MeleeTriggerRangeY)
        {
            _state.StateElapsed = 0.0f;
            SelectNextLegacyAttack(ref context, in invocation);
        }
    }

    /// <inheritdoc />
    public void Exit(ref StateContext context, in LifecycleInvocation invocation)
    {
        _state.StateElapsed = 0.0f;
    }

    /// <summary>
    /// 長時間プレイヤーを捕捉できない場合に、距離や足場状況に応じて適切な遠隔または急襲技能を選択します。
    /// </summary>
    /// <param name="distance">プレイヤーとの直線距離。</param>
    /// <param name="isSamePlatform">同一足場上にいるかどうか。</param>
    /// <param name="settings">現在のフェーズ設定。</param>
    /// <returns>発動すべき技能。発動可能な技能がない場合は BossSkill.None。</returns>
    private BossSkill SelectAntiKitingSkill(float distance, bool isSamePlatform, in Boss2PhaseSettings settings)
    {
        // 遠距離または別足場の場合：全画面落石（EarthquakeRoar）を最優先、次点に泰山圧頂（LeapSmash）
        if (!isSamePlatform || distance > _configuration.CombatSettings.AntiKitingRangedDistance)
        {
            if (_state.CooldownTracker.IsReady(BossSkill.EarthquakeRoar))
            {
                return BossSkill.EarthquakeRoar;
            }

            if (_state.CooldownTracker.IsReady(BossSkill.LeapSmash))
            {
                return BossSkill.LeapSmash;
            }
        }
        else if (distance >= _configuration.CombatSettings.AntiKitingMidDistance)
        {
            // 中距離の場合：泰山圧頂（LeapSmash）で急接近・破片弾幕を最優先、次点に全画面落石
            if (_state.CooldownTracker.IsReady(BossSkill.LeapSmash))
            {
                return BossSkill.LeapSmash;
            }

            if (_state.CooldownTracker.IsReady(BossSkill.EarthquakeRoar))
            {
                return BossSkill.EarthquakeRoar;
            }
        }
        else
        {
            // 近距離だが通常攻撃が不発（クールダウン等）：利用可能な技能を試行
            if (_state.CooldownTracker.IsReady(BossSkill.GroundStomp))
            {
                return BossSkill.GroundStomp;
            }

            if (_state.CooldownTracker.IsReady(BossSkill.EarthquakeRoar))
            {
                return BossSkill.EarthquakeRoar;
            }
        }

        // タイムアウト時間を大幅に超過しても両方クールダウン中の場合の強制発動セーフガード
        if (_state.StateElapsed >= settings.ChaseTimeout * _configuration.CombatSettings.AntiKitingEnforceMultiplier)
        {
            return (!isSamePlatform || distance > _configuration.CombatSettings.AntiKitingRangedDistance)
                ? BossSkill.EarthquakeRoar
                : BossSkill.LeapSmash;
        }

        return BossSkill.None;
    }

    private float GetSkillCooldown(BossSkill skill, in Boss2PhaseSettings settings)
    {
        for (int index = 0; index < settings.Rules.Count; index++)
        {
            AttackDecisionRule rule = settings.Rules[index];
            if (rule.Skill == skill && rule.Cooldown > 0.0f)
            {
                return rule.Cooldown;
            }
        }

        return _configuration.GetSkillDefaultCooldown(skill);
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

