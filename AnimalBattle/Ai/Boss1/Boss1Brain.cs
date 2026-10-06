using System.Diagnostics;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Skills;
using AnimalBattle.Configs;

namespace AnimalBattle.Ai.Boss1;

/// <summary>
///     Boss1（Ancient Cat Overlord）の統合意思決定・行動制御ブレインです。
///     浮遊移動、弾幕生成、急降下突進、およびフェーズ遷移を一元管理します。
/// </summary>
public sealed class Boss1Brain {
    private enum MacroState : byte {
        Hover,
        Casting,
        PhaseTransition,
        Dead
    }

    private readonly IBoss1Configuration _configuration;
    private readonly SkillRunner _skillRunner = new();
    private readonly Random _random = new();

    private MacroState _macroState = MacroState.Hover;
    private float _stateTimer;
    private float _hoverTimer;
    private float _angleOffset;
    private int _attackIndex;

    private Vec2f _currentPosition;
    private Vec2f _startPosition;
    private Vec2f _targetPosition;

    /// <summary>Boss1 ブレインを初期化します。</summary>
    /// <param name="spawnX">スポーン X 座標。</param>
    /// <param name="spawnY">スポーン Y 座標。</param>
    /// <param name="configuration">設定プロバイダ（省略時は既定）。</param>
    public Boss1Brain(float spawnX, float spawnY, IBoss1Configuration? configuration = null)
    {
        _configuration = configuration ?? new Boss1DefaultConfiguration();
        BaseX = spawnX;
        _currentPosition = new Vec2f(spawnX, spawnY);
        Phase = Phase.Phase1;
        Reset(spawnX, spawnY);
    }

    /// <summary>浮遊基準 X 座標を取得します。</summary>
    public float BaseX { get; set; }

    /// <summary>現在の Boss 座標を取得します。</summary>
    public Vec2f Position => _currentPosition;

    /// <summary>現在の戦闘フェーズを取得します。</summary>
    public Phase Phase { get; private set; }

    /// <summary>現在のマクロ・ミクロ統合状態を取得します。</summary>
    public Boss1State State => _macroState switch {
        MacroState.Casting => _skillRunner.CurrentPhase switch {
            SkillPhase.Telegraph => Boss1State.Telegraph,
            SkillPhase.Active => Boss1State.Execute,
            SkillPhase.Recovery => Boss1State.Recovery,
            _ => Boss1State.Hover
        },
        MacroState.Hover => Boss1State.Hover,
        MacroState.PhaseTransition => Boss1State.PhaseTransition,
        MacroState.Dead => Boss1State.Dead,
        _ => Boss1State.Hover
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
                MacroState.Hover => phaseCfg.HoverDuration,
                MacroState.PhaseTransition => phaseCfg.TransitionDuration,
                MacroState.Casting => _skillRunner.PhaseDuration,
                _ => 0.0f
            };
        }
    }

    /// <summary>浮遊アニメーション用の累積タイマー（秒）を取得します。</summary>
    public float HoverTimer => _hoverTimer;

    /// <summary>急降下突進の後隙による易傷状態かどうかを取得します。</summary>
    public bool IsVulnerable => State == Boss1State.Recovery && CurrentSkill == BossSkill.DiveRam;

    /// <summary>死亡状態かどうかを取得します。</summary>
    public bool IsDead => _macroState == MacroState.Dead;

    /// <summary>弾幕発射イベント（技能種別, 速度X, 速度Y）。</summary>
    public event Action<BossSkill, float, float>? OnEmitBullet;

    /// <summary>近接攻撃判定生成イベント（技能種別, オフセットX, オフセットY）。</summary>
    public event Action<BossSkill, float, float>? OnCreateHitbox;

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
        _macroState = MacroState.Hover;
        _stateTimer = 0.0f;
        _hoverTimer = 0.0f;
        _angleOffset = 0.0f;
        _attackIndex = 0;
        Phase = Phase.Phase1;
        CurrentSkill = BossSkill.None;
        _skillRunner.Reset();
    }

    /// <summary>
    ///     死亡状態へ遷移します。
    /// </summary>
    public void Kill()
    {
        _macroState = MacroState.Dead;
        _skillRunner.Reset();
        CurrentSkill = BossSkill.None;
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
        if (_macroState == MacroState.Dead || dt <= 0.0f) return;

        try
        {
            // フェーズ遷移評価（HP 割合判定）
            EvaluatePhaseTransition(currentHp, maxHp);

            switch (_macroState)
            {
                case MacroState.Hover:
                    UpdateHover(dt, playerPosition, isPlayerAlive);
                    break;

                case MacroState.Casting:
                    UpdateCasting(dt, playerPosition);
                    break;

                case MacroState.PhaseTransition:
                    UpdatePhaseTransition(dt);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Boss1Brain 警告] Update 処理中に例外が発生しました: {ex.Message}");
        }
    }

    private void EvaluatePhaseTransition(float currentHp, float maxHp)
    {
        if (maxHp <= 0.0f || _macroState == MacroState.PhaseTransition) return;

        var hpRatio = currentHp / maxHp;
        var b1Cfg = ConfigManager.Boss1;

        if (Phase == Phase.Phase1 && hpRatio <= b1Cfg.Phase2.HpThreshold)
        {
            StartPhaseTransition(Phase.Phase2);
        }
        else if (Phase == Phase.Phase2 && hpRatio <= b1Cfg.Phase3.HpThreshold)
        {
            StartPhaseTransition(Phase.Phase3);
        }
    }

    private void StartPhaseTransition(Phase nextPhase)
    {
        Phase = nextPhase;
        _macroState = MacroState.PhaseTransition;
        _stateTimer = 0.0f;
        _skillRunner.Reset();
        OnClearBullets?.Invoke();
    }

    private void UpdatePhaseTransition(float dt)
    {
        _stateTimer += dt;
        var phaseCfg = _configuration.GetPhase(Phase);

        // 基準高度へ滑らかに帰還
        var targetY = phaseCfg.HoverBaseY;
        _currentPosition = new Vec2f(
            _currentPosition.X,
            _currentPosition.Y + (targetY - _currentPosition.Y) * Math.Clamp(dt * phaseCfg.TransitionFloatSpeed, 0.0f, 1.0f));

        if (_stateTimer >= phaseCfg.TransitionDuration)
        {
            _macroState = MacroState.Hover;
            _stateTimer = 0.0f;
        }
    }

    private void UpdateHover(float dt, Vec2f playerPos, bool isPlayerAlive)
    {
        _stateTimer += dt;
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

        if (_stateTimer >= phaseCfg.HoverDuration && isPlayerAlive && phaseCfg.AttackOrder.Count > 0)
        {
            SelectNextAttack(playerPos);
        }
    }

    private void SelectNextAttack(Vec2f playerPos)
    {
        var phaseCfg = _configuration.GetPhase(Phase);
        _attackIndex = (_attackIndex + 1) % phaseCfg.AttackOrder.Count;
        var skill = phaseCfg.AttackOrder[_attackIndex];

        if (!Boss1PipelineDefinition.Pipeline.TryGetAttack(skill, out var def))
        {
            Debug.Fail($"[Boss1Brain 警告] 技能定義が見つかりません: {skill}");
            return;
        }

        CurrentSkill = skill;
        _macroState = MacroState.Casting;
        _startPosition = _currentPosition;
        _targetPosition = playerPos;
        _angleOffset = 0.0f;

        _skillRunner.Start(def);
    }

    private void UpdateCasting(float dt, Vec2f playerPos)
    {
        if (!_configuration.GetAttack(CurrentSkill, out var pattern))
        {
            Debug.WriteLine($"[Boss1Brain 警告] 攻撃パターン未定義: {CurrentSkill}");
            _macroState = MacroState.Hover;
            _stateTimer = 0.0f;
            return;
        }

        // 技能タイムラインを更新
        _skillRunner.Update(dt, false, (comp, _) => ExecuteComponent(comp, pattern));

        // 技能固有の機体運動
        if (CurrentSkill == BossSkill.DiveRam)
        {
            UpdateDiveRamMotion(pattern);
        }
        else
        {
            // 弾幕詠唱中の浮遊運動維持
            UpdateBarrageHoverMotion(dt, playerPos);
        }

        // 技能完了評価
        if (_skillRunner.IsComplete)
        {
            _macroState = MacroState.Hover;
            _stateTimer = 0.0f;
            CurrentSkill = BossSkill.None;
            _skillRunner.Reset();
        }
    }

    private void UpdateDiveRamMotion(Boss1AttackPattern pattern)
    {
        var phase = _skillRunner.CurrentPhase;
        if (phase == SkillPhase.Active)
        {
            var duration = Math.Max(0.01f, pattern.Definition.ExecuteDuration);
            var progress = Math.Clamp(_skillRunner.PhaseElapsed / duration, 0.0f, 1.0f);
            var easeProgress = EasingMath.Evaluate(pattern.EaseType, progress);

            _currentPosition = new Vec2f(
                _startPosition.X + (_targetPosition.X - _startPosition.X) * easeProgress,
                _startPosition.Y + (_targetPosition.Y - _startPosition.Y) * easeProgress);
        }
    }

    private void UpdateBarrageHoverMotion(float dt, Vec2f playerPos)
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

    private void ExecuteComponent(AttackComponent comp, Boss1AttackPattern pattern)
    {
        switch (comp.Kind)
        {
            case AttackComponentKind.Bullet:
                EmitBarrageWave(pattern);
                break;

            case AttackComponentKind.Hitbox:
                OnCreateHitbox?.Invoke(pattern.Skill, comp.Param0, comp.Param1);
                break;

            case AttackComponentKind.AudioCue:
                OnPlayAudio?.Invoke((int)comp.Param0, comp.Param1);
                break;

            case AttackComponentKind.ScreenShake:
                OnScreenShake?.Invoke(comp.Param0, comp.Param1);
                break;
        }
    }

    private void EmitBarrageWave(Boss1AttackPattern pattern)
    {
        switch (pattern.Skill)
        {
            case BossSkill.Aimed3Shot:
                EmitAimed3Shot(pattern);
                break;
            case BossSkill.RingBurst:
                EmitRingBurst(pattern);
                break;
            case BossSkill.DoubleSpiral:
                EmitDoubleSpiral(pattern);
                break;
            case BossSkill.RainDrop:
                EmitRainDrop(pattern);
                break;
            case BossSkill.CrossChaos:
                EmitCrossChaos(pattern);
                break;
            case BossSkill.FastSniper:
                EmitFastSniper(pattern);
                break;
            default:
                Debug.WriteLine($"[Boss1Brain 警告] 未知の弾幕スキル: {pattern.Skill}");
                break;
        }
    }

    private void EmitAimed3Shot(Boss1AttackPattern pattern)
    {
        var dx = _targetPosition.X - _currentPosition.X;
        var dy = _targetPosition.Y - _currentPosition.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        var baseAngle = len > 0.0001f ? MathF.Atan2(dy, dx) : 0.0f;

        for (var i = -1; i <= 1; i++)
        {
            var angle = baseAngle + i * pattern.AngleStep;
            OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed);
        }
    }

    private void EmitRingBurst(Boss1AttackPattern pattern)
    {
        var count = Math.Max(4, pattern.BulletCount);
        for (var i = 0; i < count; i++)
        {
            var angle = i / (float)count * MathF.Tau + _angleOffset;
            OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed);
        }
        _angleOffset += pattern.AngleStep;
    }

    private void EmitDoubleSpiral(Boss1AttackPattern pattern)
    {
        var a1 = _angleOffset;
        var a2 = a1 + MathF.PI;
        OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(a1) * pattern.BulletSpeed, MathF.Sin(a1) * pattern.BulletSpeed);
        OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(a2) * pattern.BulletSpeed, MathF.Sin(a2) * pattern.BulletSpeed);
        _angleOffset += pattern.AngleStep;
    }

    private void EmitRainDrop(Boss1AttackPattern pattern)
    {
        var range = pattern.MaxX - pattern.MinX;
        var x1 = pattern.MinX + _random.NextSingle() * range;
        var x2 = pattern.MinX + _random.NextSingle() * range;
        OnEmitBullet?.Invoke(pattern.Skill, x1, pattern.BulletSpeed);
        OnEmitBullet?.Invoke(pattern.Skill, x2, pattern.BulletSpeed);
    }

    private void EmitCrossChaos(Boss1AttackPattern pattern)
    {
        for (var i = 0; i < 4; i++)
        {
            var angle = _angleOffset + i * (MathF.PI / 2.0f);
            OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed);
        }
        _angleOffset += pattern.AngleStep;
    }

    private void EmitFastSniper(Boss1AttackPattern pattern)
    {
        var dx = _targetPosition.X - _currentPosition.X;
        var dy = _targetPosition.Y - _currentPosition.Y;
        var angle = MathF.Atan2(dy, dx);
        OnEmitBullet?.Invoke(pattern.Skill, MathF.Cos(angle) * pattern.BulletSpeed, MathF.Sin(angle) * pattern.BulletSpeed);
    }
}
