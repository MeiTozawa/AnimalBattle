using System.Diagnostics;
using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Skills;

using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss 2 の統合意思決定および行動制御ブレインです。
///     フラット FSM および AbilitySystemComponentを通じて足場移動、近接攻撃、気絶、状態遷移を一元管理します。
/// </summary>
public sealed class Boss2Brain : BaseBossBrain {
    private static readonly StringId StateIdle = "Idle";
    private static readonly StringId StatePatrolChase = "PatrolChase";
    private static readonly StringId StatePlatformTraverse = "PlatformTraverse";
    private static readonly StringId StateAbility = "Ability";

    private enum TraverseStage : byte {
        Ascent,
        Descent,
        Landing
    }

    private readonly World _world;
    private readonly IBoss2Configuration _configuration;
    private readonly Func<PlatformNavigationGraph?>? _navGraphProvider;

    private State _idleState = null!;
    private State _patrolChaseState = null!;
    private State _platformTraverseState = null!;
    private State _abilityState = null!;

    private TraverseStage _traverseStage = TraverseStage.Ascent;
    private float _traverseTimer;
    private float _jumpCooldown;

    private Vec2f _currentVelocity;
    private Vec2f _leapStartPosition;
    private float _targetLandingSurfaceY;
    private bool _facingRight = true;

    /// <summary>Boss 2 ブレインを初期化します。</summary>
    /// <param name="world">対象の Flecs ワールド。</param>
    /// <param name="spawn">スポーン座標。</param>
    /// <param name="configuration">設定プロバイダ。</param>
    /// <param name="navGraphProvider">ナビゲーショングラフプロバイダ。</param>
    public Boss2Brain(
        World world,
        Vec2f spawn,
        IBoss2Configuration? configuration = null,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        _world = world;
        _configuration = configuration ?? new Boss2DefaultConfiguration(world);
        _navGraphProvider = navGraphProvider;
        _currentPosition = spawn;
        _targetPosition = spawn;
        Phase = Phase.Phase1;

        InitializeAbilities();
        InitializeStateMachine();
        Reset(spawn);
    }

    /// <summary>現在の Boss 移動速度を取得します。</summary>
    public Vec2f Velocity => _currentVelocity;

    /// <summary>泰山圧頂技能の目標着地サーフェス Y 座標（足場上面または床面）を取得します。</summary>
    public float TargetLandingSurfaceY => _targetLandingSurfaceY;

    /// <inheritdoc />
    public override bool FacingRight => _facingRight;

    /// <summary>Boss 2 の設定プロバイダを取得します。</summary>
    public IBoss2Configuration Configuration => _configuration;

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss2State State {
        get {
            if (_isDead) return Boss2State.Dead;
            if (_asc.HasTag(GameplayTag.Phasing)) return Boss2State.PhaseTransition;
            if (_asc.HasTag(GameplayTag.Stunned)) return Boss2State.WallCrashStun;

            if (_stateMachine.CurrentState == _abilityState && _asc.IsCasting)
            {
                return _asc.CurrentPhase switch {
                    SkillPhase.Telegraph => Boss2State.Telegraph,
                    SkillPhase.Active => Boss2State.Execute,
                    SkillPhase.Recovery => Boss2State.Recovery,
                    _ => Boss2State.Idle
                };
            }

            if (_stateMachine.CurrentState == _idleState) return Boss2State.Idle;
            if (_stateMachine.CurrentState == _patrolChaseState) return Boss2State.PatrolChase;
            if (_stateMachine.CurrentState == _platformTraverseState) return Boss2State.PlatformTraverse;

            return Boss2State.Idle;
        }
    }

    /// <inheritdoc />
    public override BossState CurrentState => BossState.From(State);

    /// <summary>近接攻撃判定生成イベント。</summary>
    public event Action<StringId, float, float>? OnCreateHitbox;

    private void InitializeAbilities()
    {
        InitializeBaseBrain(_configuration.PhaseTransition);

        var patternCfg = _world.Get<Boss2PatternConfig>();
        var boss2Cfg = _world.Get<Boss2Config>();

        var skillNames = new HashSet<string>(boss2Cfg.Phase1.Skills);
        skillNames.UnionWith(boss2Cfg.Phase2.Skills);
        skillNames.UnionWith(boss2Cfg.Phase3.Skills);
        foreach (var rule in boss2Cfg.Phase1.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in boss2Cfg.Phase2.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in boss2Cfg.Phase3.Rules) skillNames.Add(rule.Skill);

        foreach (var name in skillNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                RegisterSkill(new StringId(name), patternCfg);
            }
        }

        _asc.OnExecuteMovement += (vx, vy) => _currentVelocity = new Vec2f(vx, vy);
        _asc.OnEmitBullet += (skill, _, _) => RaiseEmitBulletWave(skill, _currentPosition, _targetPosition);
        _asc.OnCreateHitbox += (skill, ox, oy) => OnCreateHitbox?.Invoke(skill, ox, oy);
        _asc.OnSkillCompleted += _ => OnAbilityCompleted();
    }

    private void RegisterSkill(StringId skill, Boss2PatternConfig config)
    {
        var cooldown = _configuration.GetSkillDefaultCooldown(skill);
        var buildResult = BossPatternLoader.BuildAbility(skill, config, cooldown);
        if (buildResult.IsError)
        {
            Debug.Assert(false, $"Boss 2 技能時序の構築に失敗しました: {skill}");
            return;
        }

        _asc.GiveAbility(buildResult.Value);
    }

    private void SyncPhaseAbilitiesCooldown(Boss2PhaseSettings phaseCfg)
    {
        foreach (var rule in phaseCfg.Rules)
        {
            if (rule.Cooldown > 0.0f && _asc.TryGetAbility(rule.SkillTag, out var ability) && ability is not null)
            {
                ability.Cooldown = rule.Cooldown;
            }
        }
    }

    private void InitializeStateMachine()
    {
        _idleState = new State(StateIdle, onEnter: () => {
            _stateTimer = 0.0f;
            _currentVelocity = Vec2f.Zero;
        });

        _patrolChaseState = new State(StatePatrolChase, onEnter: () => {
            _stateTimer = 0.0f;
        });

        _platformTraverseState = new State(StatePlatformTraverse);

        _abilityState = new State(StateAbility, onEnter: () => {
            _stateTimer = 0.0f;
            _currentVelocity = Vec2f.Zero;
        });

        _stateMachine.AddState(_idleState);
        _stateMachine.AddState(_patrolChaseState);
        _stateMachine.AddState(_platformTraverseState);
        _stateMachine.AddState(_abilityState);
        _stateMachine.SetInitialState(_idleState);
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
        _stateTimer = 0.0f;
        _currentVelocity = Vec2f.Zero;
        _stateMachine.ChangeState(_idleState);
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
    public void Reset(Vec2f pos)
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
        _stateMachine.ChangeState(_idleState);
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
            _currentVelocity = Vec2f.Zero;
            return;
        }

        if (_asc.HasTag(GameplayTag.Stunned))
        {
            _currentVelocity = Vec2f.Zero;
            return;
        }

        _stateMachine.Update(dt);

        if (_stateMachine.CurrentState == _idleState)
        {
            UpdateIdle(dt, playerPosition, isPlayerAlive, isGrounded);
        }
        else if (_stateMachine.CurrentState == _patrolChaseState)
        {
            UpdatePatrolChase(dt, playerPosition, playerVelocity, isPlayerAlive, isGrounded);
        }
        else if (_stateMachine.CurrentState == _platformTraverseState)
        {
            UpdatePlatformTraverse(dt, isGrounded);
        }
        else if (_stateMachine.CurrentState == _abilityState)
        {
            UpdateAbility(dt, isGrounded, playerPosition);
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        var b2Cfg = _world.Get<Boss2Config>();
        var p2Duration = _configuration.GetPhase(Phase.Phase2).TransitionDuration;
        var p3Duration = _configuration.GetPhase(Phase.Phase3).TransitionDuration;
        EvaluatePhaseTransition(currentHp, maxHp, b2Cfg.Phase2.HpThreshold, b2Cfg.Phase3.HpThreshold, p2Duration, p3Duration);
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
            _stateMachine.ChangeState(_patrolChaseState);
            _stateTimer = 0.0f;
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
            _stateMachine.ChangeState(_platformTraverseState);
            _traverseStage = TraverseStage.Descent;
            _traverseTimer = 0.0f;
            return;
        }

        _stateTimer += dt;
        _targetPosition = playerPos;
        var phaseCfg = _configuration.GetPhase(Phase);
        var dx = playerPos.X - _currentPosition.X;
        var dy = playerPos.Y - _currentPosition.Y;

        var navGraph = _navGraphProvider?.Invoke();
        NavPlan? plan = null;
        if (navGraph is not null && navGraph.Nodes.Count > 0)
        {
            var bossFootY = _currentPosition.Y + _configuration.CombatSettings.CollisionHeight / 2.0f;
            var playerFootY = playerPos.Y + _configuration.CombatSettings.PlayerCollisionHeight / 2.0f;
            plan = navGraph.FindPath(_currentPosition.X, bossFootY, playerPos.X, playerFootY, isGrounded, phaseCfg.MoveSpeed);
        }

        if (plan is not null && plan.Status == NavStatus.ExecuteJump && _jumpCooldown <= 0.0f)
        {
            var jumpVy = plan.RequiredVy != 0.0f ? plan.RequiredVy : -phaseCfg.JumpForce;
            var jumpVx = plan.RequiredVx;
            _facingRight = jumpVx >= 0.0f;
            _currentVelocity = new Vec2f(jumpVx, jumpVy);
            _jumpCooldown = _configuration.CombatSettings.JumpCooldownDuration;
            _stateMachine.ChangeState(_platformTraverseState);
            _traverseStage = TraverseStage.Ascent;
            _traverseTimer = 0.0f;
            return;
        }

        float walkVx;
        if (plan is not null)
        {
            walkVx = plan.RequiredVx;
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

    private void CastNextAbility(float dx, float dy, Vec2f playerVel, Boss2PhaseSettings phaseCfg)
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
                if (chosenSkill == _configuration.TargetLockSkillTag)
                {
                    _leapStartPosition = _currentPosition;
                    LockLeapSmashTarget(_targetPosition.X, _targetPosition.Y);
                }
                _currentVelocity = Vec2f.Zero;
                _stateMachine.ChangeState(_abilityState);
                return;
            }
        }

        if (_stateTimer >= phaseCfg.ChaseTimeout)
        {
            StringId antiKiteSkill = isSamePlatform ? _configuration.AntiKitingMeleeSkillTag : _configuration.AntiKitingRangedSkillTag;
            if (_asc.TryActivateAbility(antiKiteSkill, dir))
            {
                _targetPosition = new Vec2f(_currentPosition.X + dx, _currentPosition.Y + dy);
                if (antiKiteSkill == _configuration.TargetLockSkillTag)
                {
                    _leapStartPosition = _currentPosition;
                    LockLeapSmashTarget(_targetPosition.X, _targetPosition.Y);
                }
                _currentVelocity = Vec2f.Zero;
                _stateMachine.ChangeState(_abilityState);
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
                    _stateMachine.ChangeState(_patrolChaseState);
                    _stateTimer = 0.0f;
                }
            break;
        }
    }

    private void UpdateAbility(float dt, bool isGrounded, Vec2f playerPos)
    {
        if (CurrentSkillTag == _configuration.TargetLockSkillTag)
        {
            UpdateLeapSmashMotion(dt, isGrounded, playerPos);
        }
        else if (_asc.CurrentPhase == SkillPhase.Recovery)
        {
            _currentVelocity = Vec2f.Zero;
        }
    }

    private void UpdateLeapSmashMotion(float dt, bool isGrounded, Vec2f playerPos)
    {
        if (!_asc.IsCasting) return;

        var patternCfg = _world.Get<Boss2PatternConfig>().LeapSmash;
        var flyDuration = patternCfg.FlyDuration;
        var peakY = patternCfg.PeakAltitudeY;
        var hangDuration = patternCfg.ApexHangDuration;
        var descentSpeed = patternCfg.DescentSpeed;

        if (_asc.CurrentPhase == SkillPhase.Telegraph)
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

        if (_asc.CurrentPhase == SkillPhase.Active)
        {
            var elapsed = _asc.PhaseElapsed;

            // 1. 上昇飛翔フェーズ (0.0 ～ FlyDuration)
            if (elapsed < flyDuration)
            {
                var progress = Math.Clamp(elapsed / flyDuration, 0.0f, 1.0f);
                var easeProgress = MathF.Sin(progress * MathF.PI * 0.5f); // Smooth Sine EaseOut

                var desiredX = _leapStartPosition.X + (_targetPosition.X - _leapStartPosition.X) * easeProgress;
                var desiredY = _leapStartPosition.Y + (peakY - _leapStartPosition.Y) * easeProgress;

                var vx = dt > 0.0f ? (desiredX - _currentPosition.X) / dt : 0.0f;
                var vy = dt > 0.0f ? (desiredY - _currentPosition.Y) / dt : 0.0f;
                _currentVelocity = new Vec2f(vx, vy);

                var dx = _targetPosition.X - _currentPosition.X;
                if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
                    _facingRight = dx > 0.0f;
            }
            // 2. 最高点懸停フェーズ (FlyDuration ～ FlyDuration + ApexHangDuration)
            else if (elapsed < flyDuration + hangDuration)
            {
                var desiredX = _targetPosition.X;
                var desiredY = peakY;
                var vx = dt > 0.0f ? (desiredX - _currentPosition.X) / dt : 0.0f;
                var vy = dt > 0.0f ? (desiredY - _currentPosition.Y) / dt : 0.0f;
                _currentVelocity = new Vec2f(vx, vy);
            }
            // 3. 垂直急降下叩きつけフェーズ
            else
            {
                if (isGrounded && elapsed > flyDuration + hangDuration + 0.05f)
                {
                    _currentVelocity = Vec2f.Zero;
                }
                else
                {
                    _currentVelocity = new Vec2f(0.0f, descentSpeed);
                }
            }
            return;
        }

        if (_asc.CurrentPhase == SkillPhase.Recovery)
        {
            _currentVelocity = Vec2f.Zero;
        }
    }

    private void OnAbilityCompleted()
    {
        _stateTimer = 0.0f;
        _currentVelocity = Vec2f.Zero;
        _targetLandingSurfaceY = 0.0f;
        _stateMachine.ChangeState(_idleState);
    }

    private void LockLeapSmashTarget(float playerX, float playerCenterY)
    {
        var playerCollisionHeight = _configuration.CombatSettings.PlayerCollisionHeight > 0.0f
            ? _configuration.CombatSettings.PlayerCollisionHeight
            : 48.0f;
        var playerFootY = playerCenterY + playerCollisionHeight * 0.5f;

        _targetLandingSurfaceY = CalculateTargetLandingSurfaceY(playerX, playerFootY);
        _targetPosition = new Vec2f(playerX, _targetLandingSurfaceY - playerCollisionHeight * 0.5f);
    }

    private float CalculateTargetLandingSurfaceY(float targetX, float playerFootY)
    {
        var arenaCfg = _world.Get<ArenaConfig>();
        var floorY = arenaCfg.FloorLimitY;
        var navGraph = _navGraphProvider?.Invoke();

        var bestSurfaceY = floorY;

        if (navGraph is not null && navGraph.Nodes.Count > 0)
        {
            const float tolerance = 5.0f;

            foreach (var node in navGraph.Nodes)
            {
                if (node.ContainsX(targetX, 15.0f))
                {
                    if (node.TopY >= playerFootY - tolerance)
                    {
                        if (node.TopY < bestSurfaceY)
                        {
                            bestSurfaceY = node.TopY;
                        }
                    }
                }
            }
        }

        return bestSurfaceY;
    }
}
