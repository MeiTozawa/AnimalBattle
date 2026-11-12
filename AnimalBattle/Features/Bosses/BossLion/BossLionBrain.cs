using AnimalBattle.Logging;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Maps.Navigation;

namespace AnimalBattle.Ai.BossLion;

/// <summary>
///     BossLion の統合意思決定および行動制御ブレインです。
///     フラット FSM および AbilitySystemComponentを通じて足場移動、近接攻撃、気絶、状態遷移を一元管理します。
/// </summary>
public sealed class BossLionBrain : BaseBossBrain {
    public enum BrainSubState : byte {
        Idle,
        PatrolChase,
        PlatformTraverse,
        Ability
    }

    private enum TraverseStage : byte {
        Ascent,
        Descent,
        Landing
    }

    private readonly IBossLionConfiguration _configuration;
    private readonly PlatformNavigationGraph _navGraph = new();
    public PlatformNavigationGraph NavGraph => _navGraph;

    /// <inheritdoc />
    public override PlatformNavigationGraph? NavigationGraph => _navGraph;
    private BrainSubState _subState = BrainSubState.Idle;

    private TraverseStage _traverseStage = TraverseStage.Ascent;
    private float _traverseTimer;
    private float _jumpCooldown;
    private float _repathCooldown;
    private Vec2f _lastRepathPlayerPos;
    private NavPlan? _cachedNavPlan;
    private PlatformLink? _activeNavLink;

    private Vec2f _currentVelocity;
    private Vec2f _leapStartPosition;
    private float _targetLandingSurfaceY;
    private bool _facingRight = true;

    /// <summary>BossLion ブレインを初期化します。</summary>
    /// <param name="world">対象の Flecs ワールド。</param>
    /// <param name="spawn">スポーン座標。</param>
    /// <param name="configuration">設定プロバイダ。</param>
    /// <param name="navGraphProvider">ナビゲーショングラフプロバイダ。</param>
    public BossLionBrain(
        World world,
        Vec2f spawn,
        IBossLionConfiguration? configuration = null,
        IReadOnlyList<PlatformNode>? platformNodes = null) : base(world)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        _configuration = configuration ?? new BossLionDefaultConfiguration(world);
        _currentPosition = spawn;
        _targetPosition = spawn;
        Phase = Phase.Phase1;

        if (platformNodes != null && platformNodes.Count > 0 && world.Has<ArenaConfig>())
        {
            var arenaCfg = world.Get<ArenaConfig>();
            var p1 = _configuration.GetPhase(Phase.Phase1);
            _navGraph.Build(platformNodes, p1.JumpForce, p1.Gravity, p1.MoveSpeed, arenaCfg.NavigationGraph);
        }

        InitializeAbilities();
        Reset(spawn);
    }

    /// <summary>現在の Boss 移動速度を取得します。</summary>
    /// <inheritdoc />
    public override Vec2f Velocity => _currentVelocity;

    /// <inheritdoc />
    public override float BodyHeight => _world.Get<BossLionConfig>().CollisionSize.Y;

    /// <summary>ヒップドロップ LeapSmash スキルの着地目標サーフェス Y 座標：足場上面または床面を取得します。</summary>
    public float TargetLandingSurfaceY => _targetLandingSurfaceY;

    /// <inheritdoc />
    public override bool FacingRight => _facingRight;

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public BossLionState State {
        get {
            if (_isDead) return BossLionState.Dead;
            if (_asc.HasTag(GameplayTag.Phasing)) return BossLionState.PhaseTransition;
            if (_asc.HasTag(GameplayTag.Stunned)) return BossLionState.Stunned;

            if (_subState == BrainSubState.Ability && _asc.IsCasting)
            {
                return CurrentSkillPhase switch {
                    SkillPhase.Telegraph => BossLionState.Telegraph,
                    SkillPhase.Active => BossLionState.Execute,
                    SkillPhase.Recovery => BossLionState.Recovery,
                    _ => BossLionState.Idle
                };
            }

            return _subState switch {
                BrainSubState.Idle => BossLionState.Idle,
                BrainSubState.PatrolChase => BossLionState.PatrolChase,
                BrainSubState.PlatformTraverse => BossLionState.PlatformTraverse,
                _ => BossLionState.Idle
            };
        }
    }

    /// <inheritdoc />
    public override void WriteSubStateDescription(Span<char> destination, out int charsWritten, string nextAbilityName)
    {
        switch (State)
        {
            case BossLionState.Idle:
                destination.TryWrite($"[STATE: IDLE / ALERT]", out charsWritten);
                break;
            case BossLionState.PatrolChase:
                destination.TryWrite($"[STATE: CHASE (NEXT: {nextAbilityName})]", out charsWritten);
                break;
            case BossLionState.PlatformTraverse:
                destination.TryWrite($"[STATE: PLATFORM TRAVERSE (AIRBORNE)]", out charsWritten);
                break;
            default:
                destination.TryWrite($"[STATE: UNKNOWN]", out charsWritten);
                break;
        }
    }

    protected override void ApplyMovementAction(float vx, float vy)
    {
        _currentVelocity = new Vec2f(vx, vy);
    }

    private void InitializeAbilities()
    {
        InitializeBaseBrain(_configuration.PhaseTransition);

        var patternCfg = _world.Get<BossLionPatternConfig>();
        ReloadSkills(patternCfg);

        _asc.OnSkillCompleted += tag =>
        {
            if (tag != _phaseTransitionAbility.AbilityTag)
            {
                OnAbilityCompleted();
            }
        };
    }

    /// <summary>
    /// 指定された技能設定から BossLion の技能群を再構築・登録します。
    /// </summary>
    /// <param name="patternCfg">最新の技能設定</param>
    public void ReloadSkills(BossLionPatternConfig patternCfg)
    {
        var bossLionCfg = _world.Get<BossLionConfig>();

        var skillNames = new HashSet<string>(bossLionCfg.Phase1.Skills);
        skillNames.UnionWith(bossLionCfg.Phase2.Skills);
        skillNames.UnionWith(bossLionCfg.Phase3.Skills);
        foreach (var rule in bossLionCfg.Phase1.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in bossLionCfg.Phase2.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in bossLionCfg.Phase3.Rules) skillNames.Add(rule.Skill);
        if (!string.IsNullOrWhiteSpace(bossLionCfg.AntiKitingMeleeSkill)) skillNames.Add(bossLionCfg.AntiKitingMeleeSkill);
        if (!string.IsNullOrWhiteSpace(bossLionCfg.AntiKitingRangedSkill)) skillNames.Add(bossLionCfg.AntiKitingRangedSkill);

        foreach (var name in skillNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                RegisterSkill(new StringId(name), patternCfg);
            }
        }
    }

    private void RegisterSkill(StringId skill, BossLionPatternConfig config)
    {
        var cooldown = _configuration.GetSkillDefaultCooldown(skill);
        var buildResult = SkillBuilder.BuildAbility(skill, config, cooldown);
        if (buildResult.IsError)
        {
            Log.Error("BossLionBrain", $"BossLion のスキル構築に失敗しました: {skill} ({buildResult.FirstError.Description})");
            Debug.Assert(false, $"BossLion 技能時系列の構築に失敗しました: {skill}");
            return;
        }

        var ability = buildResult.Value;
        ability.ActionDispatcher = (step, dir) => HandleTimelineAction(step, dir, ability.AbilityTag);
        _asc.GiveAbility(ability);
    }

    private void SyncPhaseAbilitiesCooldown(BossLionPhaseSettings phaseCfg)
    {
        foreach (var rule in phaseCfg.Rules)
        {
            if (rule.Cooldown > 0.0f && _asc.TryGetAbility(rule.SkillTag, out var ability) && ability is not null)
            {
                ability.Cooldown = rule.Cooldown;
            }
        }
    }

    private void TransitionTo(BrainSubState nextState)
    {
        if (_subState == nextState) return;
        _subState = nextState;
        _stateTimer = 0.0f;
        if (nextState is BrainSubState.Idle or BrainSubState.Ability)
        {
            _currentVelocity = Vec2f.Zero;
        }
    }

    /// <inheritdoc />
    protected override void OnPhaseTransitionEntered(Phase newPhase)
    {
        _currentVelocity = Vec2f.Zero;
        _targetLandingSurfaceY = 0.0f;
        SyncPhaseAbilitiesCooldown(_configuration.GetPhase(newPhase));
    }

    /// <inheritdoc />
    protected override void OnPhaseTransitionCompleted()
    {
        TransitionTo(BrainSubState.Idle);
    }

    /// <inheritdoc />
    protected override void OnDeadEntered()
    {
        _currentVelocity = Vec2f.Zero;
        _targetLandingSurfaceY = 0.0f;
    }

    /// <summary>
    ///     ブレインの内部状態を初期位置へリセットします。
    /// </summary>
    public override void Reset(Vec2f pos)
    {
        _currentPosition = pos;
        _targetPosition = pos;
        _leapStartPosition = pos;
        _targetLandingSurfaceY = 0.0f;
        _currentVelocity = Vec2f.Zero;
        _facingRight = true;
        _stateTimer = 0.0f;
        _traverseTimer = 0.0f;
        _jumpCooldown = 0.0f;
        _isDead = false;
        Phase = Phase.Phase1;

        _asc.InterruptCurrentAbility();
        _asc.ClearTags();
        SyncPhaseAbilitiesCooldown(_configuration.GetPhase(Phase));
        _subState = BrainSubState.Idle;
    }

    /// <summary>
    ///     毎フレームの意思決定および運動・技能更新を実行します。
    /// </summary>
    public void Update(
        float dt,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        bool isPlayerAlive,
        bool isGrounded,
        float currentHp,
        float maxHp,
        Vec2f? bossPosition = null)
    {
        if (_isDead || dt <= 0.0f) return;

        if (bossPosition.HasValue)
        {
            _currentPosition = bossPosition.Value;
        }

        if (_jumpCooldown > 0.0f) _jumpCooldown = MathF.Max(0.0f, _jumpCooldown - dt);

        EvaluatePhaseTransition(currentHp, maxHp);

        _asc.Update(dt, isGrounded);

        if (_asc.HasTag(GameplayTag.Phasing))
        {
            _stateTimer += dt;
            _currentVelocity = Vec2f.Zero;
            return;
        }

        if (_asc.HasTag(GameplayTag.Stunned))
        {
            _currentVelocity = Vec2f.Zero;
            return;
        }

        switch (_subState)
        {
            case BrainSubState.Idle:
                UpdateIdle(dt, playerPosition, isPlayerAlive, isGrounded);
                break;
            case BrainSubState.PatrolChase:
                UpdatePatrolChase(dt, playerPosition, playerVelocity, isPlayerAlive, isGrounded);
                break;
            case BrainSubState.PlatformTraverse:
                UpdatePlatformTraverse(dt, isGrounded);
                break;
            case BrainSubState.Ability:
                UpdateAbility(dt, isGrounded, playerPosition);
                break;
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        var bLionCfg = _world.Get<BossLionConfig>();
        var p2Duration = _configuration.GetPhase(Phase.Phase2).TransitionDuration;
        var p3Duration = _configuration.GetPhase(Phase.Phase3).TransitionDuration;
        EvaluatePhaseTransition(currentHp, maxHp, bLionCfg.Phase2.HpThreshold, bLionCfg.Phase3.HpThreshold, p2Duration, p3Duration);
    }

    private void UpdateIdle(float dt, Vec2f playerPos, bool isPlayerAlive, bool isGrounded)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;
        _targetPosition = playerPos;

        var dx = playerPos.X - _currentPosition.X;
        if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
        {
            _facingRight = dx > 0.0f;
        }

        var phaseCfg = _configuration.GetPhase(Phase);
        if (_stateTimer >= phaseCfg.IdleDuration && isPlayerAlive && isGrounded)
        {
            TransitionTo(BrainSubState.PatrolChase);
        }
    }

    private void UpdatePatrolChase(
        float dt,
        Vec2f playerPos,
        Vec2f playerVel,
        bool isPlayerAlive,
        bool isGrounded)
    {
        if (!isGrounded)
        {
            TransitionTo(BrainSubState.PlatformTraverse);
            _traverseStage = TraverseStage.Descent;
            _traverseTimer = 0.0f;
            return;
        }

        _stateTimer += dt;
        _repathCooldown -= dt;
        _targetPosition = playerPos;
        var phaseCfg = _configuration.GetPhase(Phase);
        var dx = playerPos.X - _currentPosition.X;
        var dy = playerPos.Y - _currentPosition.Y;

        var isSamePlatform = MathF.Abs(dy) <= _configuration.CombatSettings.SamePlatformToleranceY;

        NavPlan? plan = null;
        if (isSamePlatform)
        {
            // 同一足場上の場合は A* グラフ探索を短絡し、直接水平移動
            _cachedNavPlan = null;
        _activeNavLink = null;
        }
        else
        {
            var playerMovedDistSq = (playerPos - _lastRepathPlayerPos).LengthSquared();
            // 探索クールダウンが終了しているか、プレイヤーが 64px 以上大きく移動した場合のみ A* 経路を再探索
            if (_cachedNavPlan == null || _repathCooldown <= 0.0f || playerMovedDistSq > 64.0f * 64.0f)
            {
                var navGraph = _navGraph;
                if (navGraph is not null && navGraph.Nodes.Count > 0)
                {
                    var bossFootY = _currentPosition.Y + _configuration.CombatSettings.CollisionHeight / 2.0f;
                    var playerFootY = playerPos.Y + _configuration.CombatSettings.PlayerCollisionHeight / 2.0f;
                    _cachedNavPlan = navGraph.FindPath(_currentPosition.X, bossFootY, playerPos.X, playerFootY, isGrounded, phaseCfg.MoveSpeed, _activeNavLink);
                    if (_cachedNavPlan.HasValue && _cachedNavPlan.Value.NextLink != null) _activeNavLink = _cachedNavPlan.Value.NextLink;
                    _lastRepathPlayerPos = playerPos;
                    _repathCooldown = 0.25f; // 250ms (毎秒 4 回以下に制限)
                }
            }

            plan = _cachedNavPlan;
        }

        if (plan is { Status: NavStatus.ExecuteJump } jumpPlan && _jumpCooldown <= 0.0f)
        {
            var jumpVy = jumpPlan.RequiredVy != 0.0f ? jumpPlan.RequiredVy : -phaseCfg.JumpForce;
            var jumpVx = jumpPlan.RequiredVx;
            _facingRight = jumpVx >= 0.0f;
            _currentVelocity = new Vec2f(jumpVx, jumpVy);
            _jumpCooldown = _configuration.CombatSettings.JumpCooldownDuration;
            _cachedNavPlan = null; // 跳躍実行によりプラン消費
            TransitionTo(BrainSubState.PlatformTraverse);
            _traverseStage = TraverseStage.Ascent;
            _traverseTimer = 0.0f;
            return;
        }

        float walkVx;
        if (plan is { } validPlan)
        {
            walkVx = validPlan.RequiredVx;
            if (MathF.Abs(walkVx) > _configuration.CombatSettings.MoveThreshold)
                _facingRight = walkVx > 0.0f;
        }
        else
        {
            if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
                _facingRight = dx > 0.0f;
            walkVx = MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold
                ? MathF.Sign(dx) * phaseCfg.MoveSpeed
                : 0.0f;
        }
        _currentVelocity = new Vec2f(walkVx, _currentVelocity.Y);

        if (isPlayerAlive && phaseCfg.Rules.Count > 0)
        {
            CastNextAbility(dx, dy, playerVel, phaseCfg);
        }
    }

    private void CastNextAbility(float dx, float dy, Vec2f playerVel, BossLionPhaseSettings phaseCfg)
    {
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        var isSamePlatform = MathF.Abs(dy) <= _configuration.CombatSettings.SamePlatformToleranceY;
        var isPlayerAirborne = MathF.Abs(playerVel.Y) > _configuration.CombatSettings.AirborneVelocityThreshold
            || dy < -_configuration.CombatSettings.AirborneHeightThreshold;
        var isPlayerGrounded = !isPlayerAirborne;

        AttackDecisionContext decisionContext = new(distance, isSamePlatform, isPlayerGrounded, isPlayerAirborne);
        var chosenSkill = BossDecisionEvaluator.EvaluateWeighted(phaseCfg.Rules, _asc.Cooldowns, in decisionContext);

        var dir = _facingRight ? 1.0f : -1.0f;

        if (!chosenSkill.IsEmpty)
        {
            if (_asc.TryActivateAbility(chosenSkill, dir))
            {
                _targetPosition = new Vec2f(_currentPosition.X + dx, _currentPosition.Y + dy);
                if (CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.LeapSmash)
                {
                    _leapStartPosition = _currentPosition;
                    LockLeapSmashTarget(_targetPosition.X, _targetPosition.Y);
                }
                _currentVelocity = Vec2f.Zero;
                TransitionTo(BrainSubState.Ability);
                return;
            }
        }

        if (_stateTimer >= phaseCfg.ChaseTimeout)
        {
            StringId antiKiteSkill = isSamePlatform ? _configuration.AntiKitingMeleeSkillTag : _configuration.AntiKitingRangedSkillTag;
            if (_asc.TryActivateAbility(antiKiteSkill, dir))
            {
                _targetPosition = new Vec2f(_currentPosition.X + dx, _currentPosition.Y + dy);
                if (CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.LeapSmash)
                {
                    _leapStartPosition = _currentPosition;
                    LockLeapSmashTarget(_targetPosition.X, _targetPosition.Y);
                }
                _currentVelocity = Vec2f.Zero;
                TransitionTo(BrainSubState.Ability);
            }
        }
    }

    private void UpdatePlatformTraverse(float dt, bool isGrounded)
    {
        _stateTimer += dt;
        _traverseTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);

        switch (_traverseStage)
        {
            case TraverseStage.Ascent:
                var newVy = MathF.Min(phaseCfg.MaxFallSpeed, _currentVelocity.Y + phaseCfg.Gravity * dt);
                _currentVelocity = new Vec2f(_currentVelocity.X, newVy);

                if (_currentVelocity.Y >= 0.0f || _traverseTimer >= phaseCfg.MaxAscentDuration)
                {
                    _traverseStage = TraverseStage.Descent;
                    _traverseTimer = 0.0f;
                }
            break;

            case TraverseStage.Descent:
                var fallVy = MathF.Min(phaseCfg.MaxFallSpeed, _currentVelocity.Y + phaseCfg.Gravity * dt);
                _currentVelocity = new Vec2f(_currentVelocity.X, fallVy);

                if (isGrounded && _traverseTimer >= 0.05f)
                {
                    _traverseStage = TraverseStage.Landing;
                    _traverseTimer = 0.0f;
                    _currentVelocity = Vec2f.Zero;
                }
            break;

            case TraverseStage.Landing:
                _currentVelocity = Vec2f.Zero;
                if (_traverseTimer >= phaseCfg.LandingRecoveryDuration)
                {
                    TransitionTo(BrainSubState.PatrolChase);
                }
            break;
        }
    }

    private void UpdateAbility(float dt, bool isGrounded, Vec2f playerPos)
    {
        if (CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.LeapSmash)
        {
            UpdateLeapSmashMotion(dt, isGrounded, playerPos);
        }
        else if (CurrentSkillPhase == SkillPhase.Recovery)
        {
            _currentVelocity = Vec2f.Zero;
        }
    }

    private void UpdateLeapSmashMotion(float dt, bool isGrounded, Vec2f playerPos)
    {
        if (!_asc.IsCasting || CurrentTimelineAttack == null) return;

        var motion = CurrentTimelineAttack.Motion;

        if (CurrentSkillPhase == SkillPhase.Telegraph)
        {
            _currentVelocity = Vec2f.Zero;
            LockLeapSmashTarget(playerPos.X, playerPos.Y);
            _leapStartPosition = _currentPosition;
            var dx = _targetPosition.X - _currentPosition.X;
            if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
            {
                _facingRight = dx > 0.0f;
            }
            return;
        }

        if (CurrentSkillPhase == SkillPhase.Active)
        {
            var elapsed = PhaseElapsed;
            _currentVelocity = SkillMotionDriver.EvaluateLeapSmashVelocity(
                elapsed,
                dt,
                _leapStartPosition,
                _targetPosition,
                _currentPosition,
                motion.FlyDuration,
                motion.PeakAltitudeY,
                motion.ApexHangDuration,
                motion.DescentSpeed,
                isGrounded);

            if (elapsed < motion.FlyDuration)
            {
                var dx = _targetPosition.X - _currentPosition.X;
                if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
                    _facingRight = dx > 0.0f;
            }
            return;
        }

        if (CurrentSkillPhase == SkillPhase.Recovery)
        {
            _currentVelocity = Vec2f.Zero;
        }
    }

    private void OnAbilityCompleted()
    {
        _targetLandingSurfaceY = 0.0f;
        TransitionTo(BrainSubState.Idle);
    }

    private void LockLeapSmashTarget(float playerX, float playerCenterY)
    {
        var playerCollisionHeight = _configuration.CombatSettings.PlayerCollisionHeight > 0.0f
            ? _configuration.CombatSettings.PlayerCollisionHeight
            : 48.0f;
        var playerFootY = playerCenterY + playerCollisionHeight * 0.5f;

        _targetLandingSurfaceY = SkillMotionDriver.CalculateTargetLandingSurfaceY(
            _world,
            _navGraph,
            playerX,
            playerFootY);
        _targetPosition = new Vec2f(playerX, _targetLandingSurfaceY - playerCollisionHeight * 0.5f);
    }
}
