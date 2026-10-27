using AnimalBattle.Logging;
using AnimalBattle.Abilities.Skills;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss 1 の統合意思決定・行動制御ブレインです。
///     フラット FSM および AbilitySystemComponentを通じてタイムライン・判定・状態遷移を一元管理します。
/// </summary>
public sealed class Boss1Brain : BaseBossBrain {
    private static readonly StringId StateHover = "Hover";
    private static readonly StringId StateAbility = "Ability";

    private readonly IBoss1Configuration _configuration;

    private State _hoverState = null!;
    private State _abilityState = null!;

    private float _hoverTimer;
    private int _attackIndex;

    private Vec2f _startPosition;

    /// <summary>Boss 1 ブレインを初期化します。</summary>
    /// <param name="world">対象の Flecs ワールド。</param>
    /// <param name="spawn">スポーン座標。</param>
    /// <param name="configuration">設定プロバイダー。</param>
    public Boss1Brain(World world, Vec2f spawn, IBoss1Configuration? configuration = null) : base(world)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        _configuration = configuration ?? new Boss1DefaultConfiguration(world);
        BaseX = spawn.X;
        _currentPosition = spawn;
        _startPosition = spawn;
        _targetPosition = spawn;
        Phase = Phase.Phase1;

        InitializeAbilities();
        InitializeStateMachine();
        Reset(spawn);
    }

    /// <inheritdoc />
    public override float BodyHeight => _world.Get<Boss1Config>().CollisionSize.Y;

    /// <summary>浮遊基準 X 座標を取得します。</summary>
    public float BaseX { get; set; }

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss1State State {
        get {
            if (_isDead) return Boss1State.Dead;
            if (_asc.HasTag(GameplayTag.Phasing)) return Boss1State.PhaseTransition;

            if (_stateMachine.CurrentState == _abilityState && _asc.IsCasting)
            {
                return CurrentSkillPhase switch {
                    SkillPhase.Telegraph => Boss1State.Telegraph,
                    SkillPhase.Active => Boss1State.Execute,
                    SkillPhase.Recovery => Boss1State.Recovery,
                    _ => Boss1State.Hover
                };
            }

            return Boss1State.Hover;
        }
    }

    /// <summary>浮遊アニメーション用の累積タイマーを取得します。</summary>
    public float HoverTimer => _hoverTimer;

    private void InitializeAbilities()
    {
        InitializeBaseBrain(_configuration.PhaseTransition);

        var patternCfg = _world.Get<Boss1PatternConfig>();
        var boss1Cfg = _world.Get<Boss1Config>();

        var skillNames = new HashSet<string>(boss1Cfg.Phase1.Skills);
        skillNames.UnionWith(boss1Cfg.Phase2.Skills);
        skillNames.UnionWith(boss1Cfg.Phase3.Skills);
        foreach (var rule in boss1Cfg.Phase1.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in boss1Cfg.Phase2.Rules) skillNames.Add(rule.Skill);
        foreach (var rule in boss1Cfg.Phase3.Rules) skillNames.Add(rule.Skill);

        foreach (var name in skillNames)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                RegisterSkill(new StringId(name), patternCfg);
            }
        }

        _asc.OnSkillCompleted += _ => OnAbilityCompleted();
    }

    private void RegisterSkill(StringId skill, Boss1PatternConfig config)
    {
        var cooldown = _configuration.GetSkillDefaultCooldown(skill);
        var buildResult = SkillBuilder.BuildAbility(skill, config, cooldown);
        if (buildResult.IsError)
        {
            Log.Error("Boss1Brain", $"Boss 1 のスキル構築に失敗しました: {skill} ({buildResult.FirstError.Description})");
            Debug.Assert(false, $"Boss 1 のスキルの構築に失敗しました: {skill}");
            return;
        }

        var ability = buildResult.Value;
        ability.ActionDispatcher = (step, dir) => HandleTimelineAction(step, dir, ability.AbilityTag);
        _asc.GiveAbility(ability);
    }

    private void InitializeStateMachine()
    {
        _hoverState = new State(StateHover, onEnter: () => _stateTimer = 0.0f);
        _abilityState = new State(StateAbility);

        _stateMachine.AddState(_hoverState);
        _stateMachine.AddState(_abilityState);
        _stateMachine.SetInitialState(_hoverState);
    }

    /// <summary>
    ///     ブレインの内部状態を初期位置へリセットします。
    /// </summary>
    public override void Reset(Vec2f pos)
    {
        _currentPosition = pos;
        _startPosition = pos;
        _targetPosition = pos;
        BaseX = pos.X;
        _stateTimer = 0.0f;
        _hoverTimer = 0.0f;
        _attackIndex = 0;
        _isDead = false;
        Phase = Phase.Phase1;

        _asc.InterruptCurrentAbility();
        _asc.ClearTags();
        _stateMachine.ChangeState(_hoverState);
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

        if (_asc.HasTag(GameplayTag.Phasing))
        {
            UpdatePhaseTransitionMotion(dt);
            return;
        }

        _stateMachine.Update(dt);

        if (_stateMachine.CurrentState == _hoverState)
        {
            UpdateHoverMotion(dt, playerPosition, isPlayerAlive);
        }
        else if (_stateMachine.CurrentState == _abilityState)
        {
            UpdateAbilityMotion(dt, playerPosition);
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        var b1Cfg = _world.Get<Boss1Config>();
        var p2Duration = _configuration.GetPhase(Phase.Phase2).TransitionDuration;
        var p3Duration = _configuration.GetPhase(Phase.Phase3).TransitionDuration;
        EvaluatePhaseTransition(currentHp, maxHp, b1Cfg.Phase2.HpThreshold, b1Cfg.Phase3.HpThreshold, p2Duration, p3Duration);
    }

    private void UpdatePhaseTransitionMotion(float dt)
    {
        _stateTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);

        var targetY = phaseCfg.HoverBaseY;
        var factor = 1.0f - MathF.Exp(-phaseCfg.TransitionFloatSpeed * dt);
        _currentPosition = _currentPosition with {
            Y = _currentPosition.Y + (targetY - _currentPosition.Y) * factor
        };
    }

    /// <inheritdoc />
    protected override void OnPhaseTransitionCompleted()
    {
        _stateTimer = 0.0f;
        _stateMachine.ChangeState(_hoverState);
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
            phaseCfg.HoverSpeed,
            phaseCfg.HoverAmp,
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

            if (_asc.TryActivateAbility(candidateSkill))
            {
                _startPosition = _currentPosition;
                _targetPosition = playerPos;
                _stateMachine.ChangeState(_abilityState);
                return;
            }
        }
    }

    private void UpdateAbilityMotion(float dt, Vec2f playerPos)
    {
        if (CurrentTimelineAttack?.Motion.Kind == SkillMotionKind.EaseToTarget)
        {
            if (CurrentSkillPhase == SkillPhase.Active)
            {
                var progress = CurrentTimelineAttack?.PhaseProgress ?? _asc.Progress;
                _currentPosition = SkillMotionDriver.EvaluateEaseToTarget(
                    _startPosition,
                    _targetPosition,
                    CurrentTimelineAttack!.Motion.EaseType,
                    progress);
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
                phaseCfg.HoverSpeed,
                phaseCfg.HoverAmp,
                phaseCfg.PlayerPursuitWeight,
                phaseCfg.HoverLerpSpeed,
                dt);
        }
    }

    /// <summary>
    ///     外力ノックバックによる微小変位を現在座標へ加算します。
    /// </summary>
    /// <param name="delta">フレーム変位量</param>
    public void ApplyKnockback(Vec2f delta)
    {
        _currentPosition += delta;
    }

    /// <summary>
    ///     現在座標を指定された矩形境界内に制限します。
    /// </summary>
    /// <param name="minX">最小 X 座標</param>
    /// <param name="maxX">最大 X 座標</param>
    /// <param name="minY">最小 Y 座標</param>
    /// <param name="maxY">最大 Y 座標</param>
    public void ClampPosition(float minX, float maxX, float minY, float maxY)
    {
        _currentPosition = new Vec2f(
            Math.Clamp(_currentPosition.X, minX, maxX),
            Math.Clamp(_currentPosition.Y, minY, maxY));
    }

    private void OnAbilityCompleted()
    {
        _stateTimer = 0.0f;
        _stateMachine.ChangeState(_hoverState);
    }
}
