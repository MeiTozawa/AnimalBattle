using System.Diagnostics;
using AnimalBattle.Ai.Abilities;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Hfsm;
using AnimalBattle.Ai.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using ErrorOr;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss 1 の統合意思決定・行動制御ブレインです。
///     HFSM および AbilitySystemComponent を通じて時序・判定・状態遷移を一元管理します。
/// </summary>
public sealed class Boss1Brain : IBossBrain {
    private static readonly StringId StateRoot = "Root";
    private static readonly StringId StateCombat = "Combat";
    private static readonly StringId StateHover = "Hover";
    private static readonly StringId StateAbility = "Ability";
    private static readonly StringId StateReaction = "Reaction";
    private static readonly StringId StatePhaseTransition = "PhaseTransition";
    private static readonly StringId StateDead = "Dead";

    private readonly IBoss1Configuration _configuration;
    private readonly AbilitySystemComponent _asc = new();
    private readonly StateMachine _rootMachine = new(StateRoot);
    private readonly StateMachine _combatMachine = new(StateCombat);
    private readonly StateMachine _reactionMachine = new(StateReaction);
    private State _hoverState = null!;
    private State _abilityState = null!;
    private State _phaseTransitionState = null!;
    private State _deadState = null!;

    private float _stateTimer;
    private float _hoverTimer;
    private int _attackIndex;
    private bool _isDead;
    private Phase? _pendingPhaseTransition;

    private Vec2f _currentPosition;
    private Vec2f _startPosition;
    private Vec2f _targetPosition;

    /// <summary>Boss 1 ブレインを初期化します。</summary>
    /// <param name="spawnX">スポーン X 座標。</param>
    /// <param name="spawnY">スポーン Y 座標。</param>
    /// <param name="configuration">設定プロバイダ。</param>
    public Boss1Brain(float spawnX, float spawnY, IBoss1Configuration? configuration = null)
    {
        _configuration = configuration ?? new Boss1DefaultConfiguration();
        BaseX = spawnX;
        _currentPosition = new Vec2f(spawnX, spawnY);
        _startPosition = new Vec2f(spawnX, spawnY);
        _targetPosition = new Vec2f(spawnX, spawnY);
        Phase = Phase.Phase1;

        InitializeAbilities();
        InitializeStateMachine();
        Reset(spawnX, spawnY);
    }

    /// <summary>浮遊基準 X 座標を取得します。</summary>
    public float BaseX { get; set; }

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>AI 目標地点座標を取得します。</summary>
    public Vec2f TargetPosition => _targetPosition;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => _targetPosition.X;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => _targetPosition.Y;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; private set; }

    /// <summary>現在アクティブな能力システムコンポーネントを取得します。</summary>
    public AbilitySystemComponent AbilitySystem => _asc;

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss1State State {
        get {
            if (_rootMachine.CurrentState == _reactionMachine)
            {
                if (_reactionMachine.CurrentState == _deadState) return Boss1State.Dead;
                if (_reactionMachine.CurrentState == _phaseTransitionState) return Boss1State.PhaseTransition;
            }

            if (_combatMachine.CurrentState == _abilityState && _asc.CurrentAbility != null)
            {
                return _asc.SkillRunner.CurrentPhase switch {
                    SkillPhase.Telegraph => Boss1State.Telegraph,
                    SkillPhase.Active => Boss1State.Execute,
                    SkillPhase.Recovery => Boss1State.Recovery,
                    _ => Boss1State.Hover
                };
            }

            return Boss1State.Hover;
        }
    }

    /// <summary>現在の Boss 統一マクロ状態を取得します。</summary>
    public BossState CurrentState => BossState.From(State);

    /// <summary>現在発動中の技能を取得します。</summary>
    public BossSkill CurrentSkill => _asc.CurrentAbility?.SkillId ?? BossSkill.None;

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public bool FacingRight => true;

    /// <summary>現在状態の経過時間を取得します。</summary>
    public float StateTimer => _asc.IsCasting ? _asc.SkillRunner.PhaseElapsed : _stateTimer;

    /// <summary>現在状態の目標持続時間を取得します。</summary>
    public float StateDuration {
        get {
            var phaseCfg = _configuration.GetPhase(Phase);
            if (_rootMachine.CurrentState == _reactionMachine)
            {
                return phaseCfg.TransitionDuration;
            }

            if (_combatMachine.CurrentState == _abilityState && _asc.CurrentAbility != null)
            {
                return _asc.SkillRunner.PhaseDuration;
            }

            return phaseCfg.HoverDuration;
        }
    }

    /// <summary>浮遊アニメーション用の累積タイマーを取得します。</summary>
    public float HoverTimer => _hoverTimer;

    /// <summary>急降下突進の後隙による易傷状態かどうかを取得します。</summary>
    public bool IsVulnerable => State == Boss1State.Recovery && CurrentSkill == BossSkill.DiveRam;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _isDead;

    /// <summary>弾幕波次発射イベント。</summary>
    public event Action<BossSkill, Vec2f, Vec2f>? OnEmitBarrageWave;

    /// <summary>全弾幕消去イベント。</summary>
    public event Action? OnClearBullets;

    private void InitializeAbilities()
    {
        var patternCfg = ConfigManager.Boss1Patterns;

        RegisterSkill(BossSkill.DiveRam, patternCfg);
        RegisterSkill(BossSkill.Aimed3Shot, patternCfg);
        RegisterSkill(BossSkill.RingBurst, patternCfg);
        RegisterSkill(BossSkill.DoubleSpiral, patternCfg);
        RegisterSkill(BossSkill.RainDrop, patternCfg);
        RegisterSkill(BossSkill.CrossChaos, patternCfg);
        RegisterSkill(BossSkill.FastSniper, patternCfg);

        _asc.OnEmitBullet += (skill, _, _) => OnEmitBarrageWave?.Invoke(skill, _currentPosition, _targetPosition);
        _asc.OnSkillCompleted += _ => OnAbilityCompleted();
    }

    private void RegisterSkill(BossSkill skill, Boss1PatternConfig config)
    {
        var buildResult = SkillConfigLoader.BuildFromBoss1(skill, config);
        if (buildResult.IsError)
        {
            Debug.Assert(false, $"Boss 1 技能時序の構築に失敗しました: {skill}");
            return;
        }

        var cooldown = skill switch {
            BossSkill.DiveRam => 5.0f,
            BossSkill.Aimed3Shot => 3.0f,
            BossSkill.RingBurst => 4.0f,
            BossSkill.DoubleSpiral => 4.5f,
            BossSkill.RainDrop => 6.0f,
            BossSkill.CrossChaos => 5.0f,
            BossSkill.FastSniper => 3.5f,
            _ => 2.0f
        };

        var ability = new GameplayAbility {
            SkillId = skill,
            Cooldown = cooldown,
            Timeline = buildResult.Value,
            GrantedTags = GameplayTag.Casting
        };

        _asc.RegisterAbility(ability);
    }

    private void InitializeStateMachine()
    {
        _hoverState = new State(StateHover, onEnter: () => _stateTimer = 0.0f);
        _abilityState = new State(StateAbility);
        _combatMachine.AddSubState(_hoverState);
        _combatMachine.AddSubState(_abilityState);
        _combatMachine.SetInitialState(_hoverState);

        _phaseTransitionState = new State(StatePhaseTransition, onEnter: () => {
            _stateTimer = 0.0f;
            _asc.InterruptCurrentAbility();
            OnClearBullets?.Invoke();
            if (_pendingPhaseTransition.HasValue)
            {
                Phase = _pendingPhaseTransition.Value;
                _pendingPhaseTransition = null;
            }
        });

        _deadState = new State(StateDead, onEnter: () => {
            _asc.InterruptCurrentAbility();
            OnClearBullets?.Invoke();
        });

        _reactionMachine.AddSubState(_phaseTransitionState);
        _reactionMachine.AddSubState(_deadState);

        _rootMachine.AddSubState(_combatMachine);
        _rootMachine.AddSubState(_reactionMachine);
        _rootMachine.SetInitialState(_combatMachine);

        _rootMachine.AddGlobalTransition(() => _isDead, _reactionMachine);
        _rootMachine.AddGlobalTransition(() => _pendingPhaseTransition.HasValue, _reactionMachine);
    }

    /// <summary>
    ///     ブレインの内部状態を初期位置へリセットします。
    /// </summary>
    public void Reset(float x, float y)
    {
        _currentPosition = new Vec2f(x, y);
        _startPosition = new Vec2f(x, y);
        _targetPosition = new Vec2f(x, y);
        _stateTimer = 0.0f;
        _hoverTimer = 0.0f;
        _attackIndex = 0;
        _isDead = false;
        _pendingPhaseTransition = null;
        Phase = Phase.Phase1;

        _asc.InterruptCurrentAbility();
        _reactionMachine.ChangeState(_phaseTransitionState);
        _combatMachine.ChangeState(_hoverState);
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
        ulong playerEntityId,
        bool isPlayerAlive,
        float currentHp,
        float maxHp)
    {
        if (_isDead || dt <= 0.0f) return;

        EvaluatePhaseTransition(currentHp, maxHp);

        _asc.Update(dt);
        _rootMachine.Update(dt);

        if (_rootMachine.CurrentState == _combatMachine)
        {
            if (_combatMachine.CurrentState == _hoverState)
            {
                UpdateHoverMotion(dt, playerPosition, isPlayerAlive);
            }
            else if (_combatMachine.CurrentState == _abilityState)
            {
                UpdateAbilityMotion(dt, playerPosition);
            }
        }
        else if (_rootMachine.CurrentState == _reactionMachine)
        {
            if (_reactionMachine.CurrentState == _phaseTransitionState)
            {
                UpdatePhaseTransitionMotion(dt);
            }
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        if (maxHp <= 0.0f || _pendingPhaseTransition.HasValue) return;
        if (_rootMachine.CurrentState == _reactionMachine) return;

        var hpRatio = currentHp / maxHp;
        var b1Cfg = ConfigManager.Boss1;

        if (Phase == Phase.Phase1 && hpRatio <= b1Cfg.Phase2.HpThreshold)
        {
            _pendingPhaseTransition = Phase.Phase2;
            _reactionMachine.ChangeState(_phaseTransitionState);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= b1Cfg.Phase3.HpThreshold)
        {
            _pendingPhaseTransition = Phase.Phase3;
            _reactionMachine.ChangeState(_phaseTransitionState);
        }
    }

    private void UpdatePhaseTransitionMotion(float dt)
    {
        _stateTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);

        var targetY = phaseCfg.HoverBaseY;
        _currentPosition = new Vec2f(
            _currentPosition.X,
            _currentPosition.Y + (targetY - _currentPosition.Y) * Math.Clamp(dt * phaseCfg.TransitionFloatSpeed, 0.0f, 1.0f));

        if (_stateTimer >= phaseCfg.TransitionDuration)
        {
            _combatMachine.ChangeState(_hoverState);
            _rootMachine.ChangeState(_combatMachine);
            _stateTimer = 0.0f;
        }
    }

    private void UpdateHoverMotion(float dt, Vec2f playerPos, bool isPlayerAlive)
    {
        _stateTimer += dt;
        _hoverTimer += dt;
        _targetPosition = playerPos;

        var phaseCfg = _configuration.GetPhase(Phase);
        Vec2f basePos = new(BaseX, phaseCfg.HoverBaseY);

        _currentPosition = PhysicsService.CalculateBossHoverPosition(
            _currentPosition,
            basePos,
            playerPos,
            _hoverTimer,
            phaseCfg.HoverSpeedX,
            phaseCfg.HoverSpeedY,
            phaseCfg.HoverAmpX,
            phaseCfg.HoverAmpY,
            phaseCfg.PlayerPursuitWeight,
            phaseCfg.HoverLerpSpeed,
            dt);

        if (_stateTimer >= phaseCfg.HoverDuration && isPlayerAlive && phaseCfg.AttackOrder.Count > 0)
        {
            CastNextAbility(playerPos);
        }
    }

    private void CastNextAbility(Vec2f playerPos)
    {
        var phaseCfg = _configuration.GetPhase(Phase);
        var attempts = phaseCfg.AttackOrder.Count;

        for (var i = 0; i < attempts; i++)
        {
            _attackIndex = (_attackIndex + 1) % phaseCfg.AttackOrder.Count;
            var candidateSkill = phaseCfg.AttackOrder[_attackIndex];

            var result = _asc.TryActivateAbility(candidateSkill);
            if (!result.IsError)
            {
                _startPosition = _currentPosition;
                _targetPosition = playerPos;
                _combatMachine.ChangeState(_abilityState);
                return;
            }
        }
    }

    private void UpdateAbilityMotion(float dt, Vec2f playerPos)
    {
        if (CurrentSkill == BossSkill.DiveRam)
        {
            if (_asc.SkillRunner.CurrentPhase == SkillPhase.Active)
            {
                var duration = Math.Max(0.01f, _asc.SkillRunner.PhaseDuration);
                var progress = Math.Clamp(_asc.SkillRunner.PhaseElapsed / duration, 0.0f, 1.0f);
                var easeProgress = EasingMath.Evaluate(EaseType.InQuad, progress);

                _currentPosition = new Vec2f(
                    _startPosition.X + (_targetPosition.X - _startPosition.X) * easeProgress,
                    _startPosition.Y + (_targetPosition.Y - _startPosition.Y) * easeProgress);
            }
        }
        else
        {
            _hoverTimer += dt;
            var phaseCfg = _configuration.GetPhase(Phase);
            Vec2f basePos = new(BaseX, phaseCfg.HoverBaseY);

            _currentPosition = PhysicsService.CalculateBossHoverPosition(
                _currentPosition,
                basePos,
                playerPos,
                _hoverTimer,
                phaseCfg.HoverSpeedX,
                phaseCfg.HoverSpeedY,
                phaseCfg.HoverAmpX,
                phaseCfg.HoverAmpY,
                phaseCfg.PlayerPursuitWeight,
                phaseCfg.HoverLerpSpeed,
                dt);
        }
    }

    private void OnAbilityCompleted()
    {
        _stateTimer = 0.0f;
        _combatMachine.ChangeState(_hoverState);
    }
}
