using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Decision;
using AnimalBattle.Ai.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;

namespace AnimalBattle.Ai.Boss2;

/// <summary>
///     Boss2（Ground Crusher）の統合意思決定・行動制御ブレインです。
///     足場巡回追跡、高低差跳躍・着地、近接斬撃・下砸・咆哮、および気絶硬直を一元管理します。
/// </summary>
public sealed class Boss2Brain {
    private enum MacroState : byte {
        Idle,
        PatrolChase,
        PlatformTraverse,
        Casting,
        WallCrashStun,
        PhaseTransition,
        Dead
    }

    private enum TraverseStage : byte {
        Ascent,
        Descent,
        Landing
    }

    private readonly IBoss2Configuration _configuration;
    private readonly Func<PlatformNavigationGraph?>? _navGraphProvider;
    private readonly SkillRunner _skillRunner = new();
    private readonly BossCooldownTracker _cooldownTracker = new();

    private MacroState _macroState = MacroState.Idle;
    private TraverseStage _traverseStage = TraverseStage.Ascent;

    private float _stateTimer;
    private float _traverseTimer;
    private float _jumpCooldown;

    private Vec2f _currentPosition;
    private Vec2f _currentVelocity;
    private bool _facingRight = true;

    /// <summary>Boss2 ブレインを初期化します。</summary>
    /// <param name="spawnX">スポーン X 座標。</param>
    /// <param name="spawnY">スポーン Y 座標。</param>
    /// <param name="configuration">設定プロバイダ（省略時は既定）。</param>
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
        Phase = Phase.Phase1;
        Reset(spawnX, spawnY);
    }

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>現在の Boss 移動速度を取得します。</summary>
    public Vec2f Velocity => _currentVelocity;

    /// <summary>現在の右向きフラグを取得します。</summary>
    public bool FacingRight => _facingRight;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; private set; }

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss2State State => _macroState switch {
        MacroState.Casting => _skillRunner.CurrentPhase switch {
            SkillPhase.Telegraph => Boss2State.Telegraph,
            SkillPhase.Active => Boss2State.Execute,
            SkillPhase.Recovery => Boss2State.Recovery,
            _ => Boss2State.Idle
        },
        MacroState.Idle => Boss2State.Idle,
        MacroState.PatrolChase => Boss2State.PatrolChase,
        MacroState.PlatformTraverse => Boss2State.PlatformTraverse,
        MacroState.WallCrashStun => Boss2State.WallCrashStun,
        MacroState.PhaseTransition => Boss2State.PhaseTransition,
        MacroState.Dead => Boss2State.Dead,
        _ => Boss2State.Idle
    };

    /// <summary>現在選択中の技能を取得します。</summary>
    public BossSkill CurrentSkill { get; private set; } = BossSkill.None;

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public float StateTimer => _macroState == MacroState.Casting ? _skillRunner.PhaseElapsed : _stateTimer;

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration {
        get {
            var phaseCfg = _configuration.GetPhase(Phase);
            return _macroState switch {
                MacroState.Idle => phaseCfg.IdleDuration,
                MacroState.PatrolChase => phaseCfg.ChaseTimeout,
                MacroState.PhaseTransition => phaseCfg.TransitionDuration,
                MacroState.WallCrashStun => _configuration.CombatSettings.WallCrashStunDuration,
                MacroState.Casting => _skillRunner.PhaseDuration,
                _ => 0.0f
            };
        }
    }

    /// <summary>後隙または気絶による易傷状態かどうかを取得します。</summary>
    public bool IsVulnerable => State is Boss2State.Recovery or Boss2State.WallCrashStun;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _macroState == MacroState.Dead;

    /// <summary>近接攻撃判定生成イベント（技能種別, オフセットX, オフセットY）。</summary>
    public event Action<BossSkill, float, float>? OnCreateHitbox;

    /// <summary>弾幕・衝撃波発射イベント（技能種別, 速度X, 速度Y）。</summary>
    public event Action<BossSkill, float, float>? OnEmitBullet;

    /// <summary>全弾幕消去イベント。</summary>
    public event Action? OnClearBullets;

    /// <summary>音効再生イベント（音効ID, 音量）。</summary>
    public event Action<int, float>? OnPlayAudio;

    /// <summary>画面振動イベント（振動強度, 持続時間）。</summary>
    public event Action<float, float>? OnScreenShake;

    /// <summary>
    ///     ブレインの内部状態を初期位置へリセットします。
    /// </summary>
    public void Reset(float x, float y)
    {
        _currentPosition = new Vec2f(x, y);
        _currentVelocity = Vec2f.Zero;
        _facingRight = true;
        _macroState = MacroState.Idle;
        _stateTimer = 0.0f;
        _traverseTimer = 0.0f;
        _jumpCooldown = 0.0f;
        Phase = Phase.Phase1;
        CurrentSkill = BossSkill.None;
        _cooldownTracker.Reset();
        _skillRunner.Reset();
    }

    /// <summary>
    ///     死亡状態へ遷移します。
    /// </summary>
    public void Kill()
    {
        _macroState = MacroState.Dead;
        _currentVelocity = Vec2f.Zero;
        _skillRunner.Reset();
        CurrentSkill = BossSkill.None;
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
        if (_macroState == MacroState.Dead || dt <= 0.0f) return;

        try
        {
            // クールダウン減算
            if (_jumpCooldown > 0.0f) _jumpCooldown = MathF.Max(0.0f, _jumpCooldown - dt);
            _cooldownTracker.Update(dt);

            // フェーズ遷移評価（HP 割合判定）
            EvaluatePhaseTransition(currentHp, maxHp);

            switch (_macroState)
            {
                case MacroState.Idle:
                    UpdateIdle(dt, playerPosition, isPlayerAlive, isGrounded);
                    break;

                case MacroState.PatrolChase:
                    UpdatePatrolChase(dt, playerPosition, playerVelocity, isPlayerAlive, isGrounded);
                    break;

                case MacroState.PlatformTraverse:
                    UpdatePlatformTraverse(dt, isGrounded);
                    break;

                case MacroState.Casting:
                    UpdateCasting(dt, isGrounded, isOnWall);
                    break;

                case MacroState.WallCrashStun:
                    UpdateWallCrashStun(dt);
                    break;

                case MacroState.PhaseTransition:
                    UpdatePhaseTransition(dt);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Boss2Brain 警告] Update 処理中に例外が発生しました: {ex.Message}");
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        if (maxHp <= 0.0f || _macroState == MacroState.PhaseTransition) return;

        var hpRatio = currentHp / maxHp;
        var b2Cfg = ConfigManager.Boss2;

        if (Phase == Phase.Phase1 && hpRatio <= b2Cfg.Phase2.HpThreshold)
        {
            StartPhaseTransition(Phase.Phase2);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= b2Cfg.Phase3.HpThreshold)
        {
            StartPhaseTransition(Phase.Phase3);
        }
    }

    private void StartPhaseTransition(Phase nextPhase)
    {
        Phase = nextPhase;
        _macroState = MacroState.PhaseTransition;
        _stateTimer = 0.0f;
        _currentVelocity = Vec2f.Zero;
        _skillRunner.Reset();
        OnClearBullets?.Invoke();
    }

    private void UpdatePhaseTransition(float dt)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;
        var phaseCfg = _configuration.GetPhase(Phase);

        if (_stateTimer >= phaseCfg.TransitionDuration)
        {
            _macroState = MacroState.Idle;
            _stateTimer = 0.0f;
        }
    }

    private void UpdateIdle(float dt, Vec2f playerPos, bool isPlayerAlive, bool isGrounded)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;

        var dx = playerPos.X - _currentPosition.X;
        if (MathF.Abs(dx) > _configuration.CombatSettings.MoveThreshold)
        {
            _facingRight = dx > 0.0f;
        }

        var phaseCfg = _configuration.GetPhase(Phase);
        if (_stateTimer >= phaseCfg.IdleDuration && isPlayerAlive && isGrounded)
        {
            _macroState = MacroState.PatrolChase;
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
            // 足場から踏み外した場合は落下機動へ遷移
            _macroState = MacroState.PlatformTraverse;
            _traverseStage = TraverseStage.Descent;
            _traverseTimer = 0.0f;
            return;
        }

        _stateTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);
        var dx = playerPos.X - _currentPosition.X;
        var dy = playerPos.Y - _currentPosition.Y;

        // ナビゲーショングラフによる経路探索
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
            // 跳躍機動へ移行
            var jumpVy = plan.RequiredVy != 0.0f ? plan.RequiredVy : -phaseCfg.JumpForce;
            var jumpVx = plan.RequiredVx;
            _facingRight = jumpVx >= 0.0f;
            _currentVelocity = new Vec2f(jumpVx, jumpVy);
            _jumpCooldown = _configuration.CombatSettings.JumpCooldownDuration;
            _macroState = MacroState.PlatformTraverse;
            _traverseStage = TraverseStage.Ascent;
            _traverseTimer = 0.0f;
            return;
        }

        // 地上移動速度の算出
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

        // 技能発動評価
        if (isPlayerAlive && phaseCfg.Rules.Count > 0)
        {
            EvaluateSkillExecution(dx, dy, playerVel, phaseCfg);
        }
    }

    private void EvaluateSkillExecution(float dx, float dy, Vec2f playerVel, Boss2PhaseSettings phaseCfg)
    {
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        var isSamePlatform = MathF.Abs(dy) <= _configuration.CombatSettings.SamePlatformToleranceY;
        var isPlayerAirborne = MathF.Abs(playerVel.Y) > _configuration.CombatSettings.AirborneVelocityThreshold
            || dy < -_configuration.CombatSettings.AirborneHeightThreshold;
        var isPlayerGrounded = !isPlayerAirborne;

        AttackDecisionContext decisionContext = new(distance, isSamePlatform, isPlayerGrounded, isPlayerAirborne);
        var chosenSkill = BossDecisionEvaluator.EvaluateWeighted(phaseCfg.Rules, _cooldownTracker, in decisionContext);

        if (chosenSkill != BossSkill.None)
        {
            StartSkill(chosenSkill, phaseCfg);
            return;
        }

        // 追跡タイムアウト時の急襲/遠隔技能選択
        if (_stateTimer >= phaseCfg.ChaseTimeout)
        {
            var antiKiteSkill = isSamePlatform ? BossSkill.GroundStomp : BossSkill.LeapSmash;
            if (Boss2PipelineDefinition.Pipeline.TryGetAttack(antiKiteSkill, out _))
            {
                StartSkill(antiKiteSkill, phaseCfg);
            }
        }
    }

    private void StartSkill(BossSkill skill, Boss2PhaseSettings phaseCfg)
    {
        if (!Boss2PipelineDefinition.Pipeline.TryGetAttack(skill, out var def))
        {
            Debug.Fail($"[Boss2Brain 警告] Boss2 技能定義が見つかりません: {skill}");
            return;
        }

        var cooldown = GetSkillCooldown(skill, phaseCfg);
        _cooldownTracker.Trigger(skill, cooldown);

        CurrentSkill = skill;
        _macroState = MacroState.Casting;
        _stateTimer = 0.0f;
        _currentVelocity = Vec2f.Zero;

        _skillRunner.Start(def, _facingRight ? 1.0f : -1.0f);
    }

    private float GetSkillCooldown(BossSkill skill, in Boss2PhaseSettings settings)
    {
        for (var index = 0; index < settings.Rules.Count; index++)
        {
            var rule = settings.Rules[index];
            if (rule.Skill == skill && rule.Cooldown > 0.0f) return rule.Cooldown;
        }

        return _configuration.GetSkillDefaultCooldown(skill);
    }

    private void UpdatePlatformTraverse(float dt, bool isGrounded)
    {
        _stateTimer += dt;
        _traverseTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);

        switch (_traverseStage)
        {
            case TraverseStage.Ascent:
                // 重力適用
                var newVy = MathF.Min(phaseCfg.MaxFallSpeed, _currentVelocity.Y + phaseCfg.Gravity * dt);
                _currentVelocity = new Vec2f(_currentVelocity.X, newVy);

                if (_currentVelocity.Y >= 0.0f || _traverseTimer >= phaseCfg.MaxAscentDuration)
                {
                    _traverseStage = TraverseStage.Descent;
                    _traverseTimer = 0.0f;
                }
                break;

            case TraverseStage.Descent:
                // 落下重力適用
                var fallVy = MathF.Min(phaseCfg.MaxFallSpeed, _currentVelocity.Y + phaseCfg.Gravity * dt);
                _currentVelocity = new Vec2f(_currentVelocity.X, fallVy);

                if (isGrounded && _traverseTimer >= 0.05f)
                {
                    _traverseStage = TraverseStage.Landing;
                    _traverseTimer = 0.0f;
                    _currentVelocity = Vec2f.Zero;
                    OnScreenShake?.Invoke(2.0f, 0.15f);
                }
                break;

            case TraverseStage.Landing:
                _currentVelocity = Vec2f.Zero;
                if (_traverseTimer >= phaseCfg.LandingRecoveryDuration)
                {
                    _macroState = MacroState.PatrolChase;
                    _stateTimer = 0.0f;
                }
                break;
        }
    }

    private void UpdateCasting(float dt, bool isGrounded, bool isOnWall)
    {
        // 技能タイムライン更新
        _skillRunner.Update(dt, isGrounded, (comp, dir) => ExecuteComponent(comp, dir));

        // 突進中の壁激突判定
        if (_skillRunner.CurrentPhase == SkillPhase.Active && isOnWall && MathF.Abs(_currentVelocity.X) > 10.0f)
        {
            _macroState = MacroState.WallCrashStun;
            _stateTimer = 0.0f;
            _currentVelocity = Vec2f.Zero;
            _skillRunner.Reset();
            OnScreenShake?.Invoke(8.0f, 0.4f);
            return;
        }

        // 技能完了評価
        if (_skillRunner.IsComplete)
        {
            _macroState = MacroState.Idle;
            _stateTimer = 0.0f;
            CurrentSkill = BossSkill.None;
            _currentVelocity = Vec2f.Zero;
            _skillRunner.Reset();
        }
    }

    private void UpdateWallCrashStun(float dt)
    {
        _stateTimer += dt;
        _currentVelocity = Vec2f.Zero;

        if (_stateTimer >= _configuration.CombatSettings.WallCrashStunDuration)
        {
            _macroState = MacroState.Idle;
            _stateTimer = 0.0f;
            CurrentSkill = BossSkill.None;
        }
    }

    private void ExecuteComponent(AttackComponent comp, float dir)
    {
        switch (comp.Kind)
        {
            case AttackComponentKind.Movement:
            case AttackComponentKind.VelocityDive:
                _currentVelocity = new Vec2f(comp.Param0 * dir, comp.Param1);
                break;

            case AttackComponentKind.Hitbox:
            case AttackComponentKind.OnGroundedHitbox:
                OnCreateHitbox?.Invoke(CurrentSkill, comp.Param0 * dir, comp.Param1);
                break;

            case AttackComponentKind.Bullet:
                OnEmitBullet?.Invoke(CurrentSkill, comp.Param0 * dir, comp.Param1);
                break;

            case AttackComponentKind.AudioCue:
                OnPlayAudio?.Invoke((int)comp.Param0, comp.Param1);
                break;

            case AttackComponentKind.ScreenShake:
                OnScreenShake?.Invoke(comp.Param0, comp.Param1);
                break;
        }
    }
}
