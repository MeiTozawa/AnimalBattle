using System.Diagnostics;
using AnimalBattle.Ai.Abilities;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Decision;
using AnimalBattle.Ai.Hfsm;
using AnimalBattle.Ai.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;
using ErrorOr;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss 2 の統合意思決定および行動制御ブレインです。
///     HFSM および AbilitySystemComponent を通じて足場移動、近接攻撃、気絶、状態遷移を一元管理します。
/// </summary>
public sealed class Boss2Brain : IBossBrain {
    private static readonly StringId StateRoot = "Root";
    private static readonly StringId StateCombat = "Combat";
    private static readonly StringId StateIdle = "Idle";
    private static readonly StringId StatePatrolChase = "PatrolChase";
    private static readonly StringId StatePlatformTraverse = "PlatformTraverse";
    private static readonly StringId StateAbility = "Ability";

    private static readonly StringId StateReaction = "Reaction";
    private static readonly StringId StateWallCrashStun = "WallCrashStun";
    private static readonly StringId StatePhaseTransition = "PhaseTransition";
    private static readonly StringId StateDead = "Dead";

    private enum TraverseStage : byte {
        Ascent,
        Descent,
        Landing
    }

    private readonly IBoss2Configuration _configuration;
    private readonly Func<PlatformNavigationGraph?>? _navGraphProvider;
    private readonly AbilitySystemComponent _asc = new();

    private readonly StateMachine _rootMachine = new(StateRoot);
    private readonly StateMachine _combatMachine = new(StateCombat);
    private readonly StateMachine _reactionMachine = new(StateReaction);

    private State _idleState = null!;
    private State _patrolChaseState = null!;
    private State _platformTraverseState = null!;
    private State _abilityState = null!;

    private State _wallCrashStunState = null!;
    private State _phaseTransitionState = null!;
    private State _deadState = null!;

    private TraverseStage _traverseStage = TraverseStage.Ascent;
    private float _stateTimer;
    private float _traverseTimer;
    private float _jumpCooldown;

    private bool _isDead;
    private bool _isWallStunned;
    private Phase? _pendingPhaseTransition;

    private Vec2f _currentPosition;
    private Vec2f _currentVelocity;
    private Vec2f _targetPosition;
    private bool _facingRight = true;

    /// <summary>Boss 2 ブレインを初期化します。</summary>
    /// <param name="spawnX">スポーン X 座標。</param>
    /// <param name="spawnY">スポーン Y 座標。</param>
    /// <param name="configuration">設定プロバイダ。</param>
    /// <param name="navGraphProvider">ナビゲーショングラフプロバイダ。</param>
    public Boss2Brain(
        float spawnX,
        float spawnY,
        IBoss2Configuration? configuration = null,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        _configuration = configuration ?? new Boss2DefaultConfiguration();
        _navGraphProvider = navGraphProvider;
        _currentPosition = new Vec2f(spawnX, spawnY);
        _targetPosition = new Vec2f(spawnX, spawnY);
        Phase = Phase.Phase1;

        InitializeAbilities();
        InitializeStateMachine();
        Reset(spawnX, spawnY);
    }

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>AI 目標地点座標を取得します。</summary>
    public Vec2f TargetPosition => _targetPosition;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => _targetPosition.X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => _targetPosition.Y;

    /// <summary>現在の Boss 移動速度を取得します。</summary>
    public Vec2f Velocity => _currentVelocity;

    /// <summary>現在の右向きフラグを取得します。</summary>
    public bool FacingRight => _facingRight;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; private set; }

    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    public AbilitySystemComponent AbilitySystem => _asc;

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss2State State {
        get {
            if (_rootMachine.CurrentState == _reactionMachine)
            {
                if (_reactionMachine.CurrentState == _deadState) return Boss2State.Dead;
                if (_reactionMachine.CurrentState == _phaseTransitionState) return Boss2State.PhaseTransition;
                if (_reactionMachine.CurrentState == _wallCrashStunState) return Boss2State.WallCrashStun;
            }

            if (_combatMachine.CurrentState == _abilityState && _asc.CurrentAbility != null)
            {
                return _asc.SkillRunner.CurrentPhase switch {
                    SkillPhase.Telegraph => Boss2State.Telegraph,
                    SkillPhase.Active => Boss2State.Execute,
                    SkillPhase.Recovery => Boss2State.Recovery,
                    _ => Boss2State.Idle
                };
            }

            if (_combatMachine.CurrentState == _idleState) return Boss2State.Idle;
            if (_combatMachine.CurrentState == _patrolChaseState) return Boss2State.PatrolChase;
            if (_combatMachine.CurrentState == _platformTraverseState) return Boss2State.PlatformTraverse;

            return Boss2State.Idle;
        }
    }

    /// <summary>現在の Boss 統一マクロ状態を取得します。</summary>
    public BossState CurrentState => BossState.From(State);

    /// <summary>現在発動中の技能を取得します。</summary>
    public BossSkill CurrentSkill => _asc.CurrentAbility?.SkillId ?? BossSkill.None;

    /// <summary>現在状態の経過時間を取得します。</summary>
    public float StateTimer => _asc.IsCasting ? _asc.SkillRunner.PhaseElapsed : _stateTimer;

    /// <summary>現在状態の目標持続時間を取得します。</summary>
    public float StateDuration {
        get {
            var phaseCfg = _configuration.GetPhase(Phase);
            if (_rootMachine.CurrentState == _reactionMachine)
            {
                if (_reactionMachine.CurrentState == _wallCrashStunState)
                    return _configuration.CombatSettings.WallCrashStunDuration;
                if (_reactionMachine.CurrentState == _phaseTransitionState)
                    return phaseCfg.TransitionDuration;
                return 0.0f;
            }

            if (_combatMachine.CurrentState == _abilityState && _asc.CurrentAbility != null)
            {
                return _asc.SkillRunner.PhaseDuration;
            }

            if (_combatMachine.CurrentState == _idleState) return phaseCfg.IdleDuration;
            if (_combatMachine.CurrentState == _patrolChaseState) return phaseCfg.ChaseTimeout;

            return 0.0f;
        }
    }

    /// <summary>後隙または気絶による易傷状態かどうかを取得します。</summary>
    public bool IsVulnerable => State is Boss2State.Recovery or Boss2State.WallCrashStun;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _isDead;

    /// <summary>近接攻撃判定生成イベント。</summary>
    public event Action<BossSkill, float, float>? OnCreateHitbox;

    /// <summary>弾幕波次発射イベント。</summary>
    public event Action<BossSkill, Vec2f, Vec2f>? OnEmitBarrageWave;

    /// <summary>全弾幕消去イベント。</summary>
    public event Action? OnClearBullets;


    private void InitializeAbilities()
    {
        var patternCfg = ConfigManager.Boss2Patterns;

        RegisterSkill(BossSkill.HeavySlash, patternCfg);
        RegisterSkill(BossSkill.GroundStomp, patternCfg);
        RegisterSkill(BossSkill.LeapSmash, patternCfg);
        RegisterSkill(BossSkill.DoubleCleave, patternCfg);
        RegisterSkill(BossSkill.FrenzyCharge, patternCfg);
        RegisterSkill(BossSkill.EarthquakeRoar, patternCfg);

        _asc.OnExecuteMovement += (vx, vy) => _currentVelocity = new Vec2f(vx, vy);
        _asc.OnEmitBullet += (skill, _, _) => OnEmitBarrageWave?.Invoke(skill, _currentPosition, _targetPosition);
        _asc.OnCreateHitbox += (skill, ox, oy) => OnCreateHitbox?.Invoke(skill, ox, oy);
        _asc.OnSkillCompleted += _ => OnAbilityCompleted();
    }

    private void RegisterSkill(BossSkill skill, Boss2PatternConfig config)
    {
        var buildResult = SkillConfigLoader.BuildFromBoss2(skill, config);
        if (buildResult.IsError)
        {
            Debug.Assert(false, $"Boss 2 技能時序の構築に失敗しました: {skill}");
            return;
        }

        var cooldown = _configuration.GetSkillDefaultCooldown(skill);

        var ability = new GameplayAbility {
            SkillId = skill,
            Cooldown = cooldown,
            Timeline = buildResult.Value,
            GrantedTags = GameplayTag.Casting
        };

        _asc.RegisterAbility(ability);
    }

    private void SyncPhaseAbilitiesCooldown(Boss2PhaseSettings phaseCfg)
    {
        foreach (var rule in phaseCfg.Rules)
        {
            if (rule.Cooldown > 0.0f && _asc.TryGetAbility(rule.Skill, out var ability) && ability is not null)
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

        _combatMachine.AddSubState(_idleState);
        _combatMachine.AddSubState(_patrolChaseState);
        _combatMachine.AddSubState(_platformTraverseState);
        _combatMachine.AddSubState(_abilityState);
        _combatMachine.SetInitialState(_idleState);

        _wallCrashStunState = new State(StateWallCrashStun, onEnter: () => {
            _stateTimer = 0.0f;
            _currentVelocity = Vec2f.Zero;
            _asc.InterruptCurrentAbility();
        });

        _phaseTransitionState = new State(StatePhaseTransition, onEnter: () => {
            _stateTimer = 0.0f;
            _currentVelocity = Vec2f.Zero;
            _asc.InterruptCurrentAbility();
            OnClearBullets?.Invoke();
            if (_pendingPhaseTransition.HasValue)
            {
                Phase = _pendingPhaseTransition.Value;
                _pendingPhaseTransition = null;
                SyncPhaseAbilitiesCooldown(_configuration.GetPhase(Phase));
            }
        });

        _deadState = new State(StateDead, onEnter: () => {
            _currentVelocity = Vec2f.Zero;
            _asc.InterruptCurrentAbility();
            OnClearBullets?.Invoke();
        });

        _reactionMachine.AddSubState(_wallCrashStunState);
        _reactionMachine.AddSubState(_phaseTransitionState);
        _reactionMachine.AddSubState(_deadState);

        _rootMachine.AddSubState(_combatMachine);
        _rootMachine.AddSubState(_reactionMachine);
        _rootMachine.SetInitialState(_combatMachine);

        _rootMachine.AddGlobalTransition(() => _isDead, _reactionMachine);
        _rootMachine.AddGlobalTransition(() => _pendingPhaseTransition.HasValue, _reactionMachine);
        _rootMachine.AddGlobalTransition(() => _isWallStunned, _reactionMachine);
    }

    /// <summary>
    ///     ブレインの内部状態を初期位置へリセットします。
    /// </summary>
    public void Reset(float x, float y)
    {
        _currentPosition = new Vec2f(x, y);
        _targetPosition = new Vec2f(x, y);
        _currentVelocity = Vec2f.Zero;
        _facingRight = true;
        _stateTimer = 0.0f;
        _traverseTimer = 0.0f;
        _jumpCooldown = 0.0f;
        _isDead = false;
        _isWallStunned = false;
        _pendingPhaseTransition = null;
        Phase = Phase.Phase1;

        _asc.InterruptCurrentAbility();
        SyncPhaseAbilitiesCooldown(_configuration.GetPhase(Phase));
        _combatMachine.ChangeState(_idleState);
        _rootMachine.ChangeState(_combatMachine);
    }

    /// <summary>
    ///     死亡状態へ遷移します。
    /// </summary>
    public void Kill()
    {
        _isDead = true;
        _reactionMachine.ChangeState(_deadState);
        _rootMachine.ChangeState(_reactionMachine);
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
        bool isOnWall,
        float currentHp,
        float maxHp)
    {
        if (_isDead || dt <= 0.0f) return;

        if (_jumpCooldown > 0.0f) _jumpCooldown = MathF.Max(0.0f, _jumpCooldown - dt);

        EvaluatePhaseTransition(currentHp, maxHp);

        _asc.Update(dt, isGrounded);
        _rootMachine.Update(dt);

        if (_rootMachine.CurrentState == _combatMachine)
        {
            if (_combatMachine.CurrentState == _idleState)
            {
                UpdateIdle(dt, playerPosition, isPlayerAlive, isGrounded);
            }
            else if (_combatMachine.CurrentState == _patrolChaseState)
            {
                UpdatePatrolChase(dt, playerPosition, playerVelocity, isPlayerAlive, isGrounded);
            }
            else if (_combatMachine.CurrentState == _platformTraverseState)
            {
                UpdatePlatformTraverse(dt, isGrounded);
            }
            else if (_combatMachine.CurrentState == _abilityState)
            {
                UpdateAbility(isOnWall);
            }
        }
        else if (_rootMachine.CurrentState == _reactionMachine)
        {
            if (_reactionMachine.CurrentState == _wallCrashStunState)
            {
                UpdateWallCrashStun(dt);
            }
            else if (_reactionMachine.CurrentState == _phaseTransitionState)
            {
                UpdatePhaseTransition(dt);
            }
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        if (maxHp <= 0.0f || _pendingPhaseTransition.HasValue) return;
        if (_rootMachine.CurrentState == _reactionMachine) return;

        var hpRatio = currentHp / maxHp;
        var b2Cfg = ConfigManager.Boss2;

        if (Phase == Phase.Phase1 && hpRatio <= b2Cfg.Phase2.HpThreshold)
        {
            _pendingPhaseTransition = Phase.Phase2;
            _reactionMachine.ChangeState(_phaseTransitionState);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= b2Cfg.Phase3.HpThreshold)
        {
            _pendingPhaseTransition = Phase.Phase3;
            _reactionMachine.ChangeState(_phaseTransitionState);
        }
    }

    private void UpdatePhaseTransition(float dt)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;
        var phaseCfg = _configuration.GetPhase(Phase);

        if (_stateTimer >= phaseCfg.TransitionDuration)
        {
            _combatMachine.ChangeState(_idleState);
            _rootMachine.ChangeState(_combatMachine);
            _stateTimer = 0.0f;
        }
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
            _combatMachine.ChangeState(_patrolChaseState);
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
            _combatMachine.ChangeState(_platformTraverseState);
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
            var playerFootY = playerPos.Y + _configuration.CombatSettings.PlayerHitboxHeight / 2.0f;
            plan = navGraph.FindPath(_currentPosition.X, bossFootY, playerPos.X, playerFootY, isGrounded, phaseCfg.MoveSpeed);
        }

        if (plan is not null && plan.Status == NavStatus.ExecuteJump && _jumpCooldown <= 0.0f)
        {
            var jumpVy = plan.RequiredVy != 0.0f ? plan.RequiredVy : -phaseCfg.JumpForce;
            var jumpVx = plan.RequiredVx;
            _facingRight = jumpVx >= 0.0f;
            _currentVelocity = new Vec2f(jumpVx, jumpVy);
            _jumpCooldown = _configuration.CombatSettings.JumpCooldownDuration;
            _combatMachine.ChangeState(_platformTraverseState);
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
        var chosenSkill = BossDecisionEvaluator.EvaluateWeighted(phaseCfg.Rules, _asc.CooldownTracker, in decisionContext);

        var dir = _facingRight ? 1.0f : -1.0f;

        if (chosenSkill != BossSkill.None)
        {
            var result = _asc.TryActivateAbility(chosenSkill, dir);
            if (!result.IsError)
            {
                _targetPosition = new Vec2f(_currentPosition.X + dx, _currentPosition.Y + dy);
                _currentVelocity = Vec2f.Zero;
                _combatMachine.ChangeState(_abilityState);
                return;
            }
        }

        if (_stateTimer >= phaseCfg.ChaseTimeout)
        {
            var antiKiteSkill = isSamePlatform ? BossSkill.GroundStomp : BossSkill.LeapSmash;
            var result = _asc.TryActivateAbility(antiKiteSkill, dir);
            if (!result.IsError)
            {
                _currentVelocity = Vec2f.Zero;
                _combatMachine.ChangeState(_abilityState);
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
                    _combatMachine.ChangeState(_patrolChaseState);
                    _stateTimer = 0.0f;
                }
                break;
        }
    }

    private void UpdateAbility(bool isOnWall)
    {
        if (CurrentSkill == BossSkill.FrenzyCharge &&
            _asc.SkillRunner.CurrentPhase == SkillPhase.Active &&
            isOnWall &&
            MathF.Abs(_currentVelocity.X) > 10.0f)
        {
            _isWallStunned = true;
            _reactionMachine.ChangeState(_wallCrashStunState);
            _rootMachine.ChangeState(_reactionMachine);
        }
    }

    private void UpdateWallCrashStun(float dt)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;

        if (_stateTimer >= _configuration.CombatSettings.WallCrashStunDuration)
        {
            _isWallStunned = false;
            _combatMachine.ChangeState(_idleState);
            _rootMachine.ChangeState(_combatMachine);
            _stateTimer = 0.0f;
        }
    }

    private void OnAbilityCompleted()
    {
        _stateTimer = 0.0f;
        _currentVelocity = Vec2f.Zero;
        _combatMachine.ChangeState(_idleState);
    }
}
