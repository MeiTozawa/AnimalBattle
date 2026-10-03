using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Systems;
using Flecs.NET.Core;

using System.Drawing;

namespace AnimalBattle.Characters;

/// <summary>
///     多 Boss（Boss 1 / Boss 2）の生命周期、状態遷移、物理、弾幕および描画用データ同期を一元管理する領域モデルクラスです。
///     实体 Facade 原則に従い、ECS エンティティのコンポーネントを唯一の真実の源として直接操作します。
/// </summary>
public sealed class BossCharacter : IBossController, ICharacter {
    private readonly Boss1BehaviorState? _boss1State;
    private readonly Boss2BehaviorState? _boss2State;
    private readonly Entity _entity;
    private readonly BossAiRuntime _runtime;
    private readonly BossRuntimeSlot _slot;
    private int _aiErrorLoggedCount;
    private long _frameNumber;

    /// <summary>Boss のヘルス情報を取得します。</summary>
    public ref readonly Health Health => ref _entity.Get<Health>();

    /// <summary>
    ///     Boss の Flecs エンティティ、種別およびスプライトハンドルを指定して BossCharacter を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="bossType">Boss 種別（Boss1 または Boss2）。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">Boss2 プラットフォーム尋路用ナビゲーショングラフプロバイダー（省略可能）。</param>
    public BossCharacter(Entity entity, BossType bossType, int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        _entity = entity;
        BossType = bossType;

        ObservationService observation = new();
        var configProvider = ConfigProvider.FromConfigManager(observation: observation);

        PreallocatedCommandBuffer<MovementCommand> movementCommands = new(32);
        PreallocatedCommandBuffer<AttackCommand> attackCommands = new(32);
        PreallocatedCommandBuffer<EffectCommand> effectCommands = new(16);

        if (bossType == BossType.Boss1)
        {
            var b1Cfg = ConfigManager.Boss1;

            _boss1State = new Boss1BehaviorState(b1Cfg.SpawnX, b1Cfg.SpawnY);
            _boss2State = null;

            var moduleMap = CreateBoss1ModuleMap(_boss1State, observation);
            TransitionService transitionService = new(moduleMap, observation);
            _runtime = new BossAiRuntime(moduleMap, transitionService, observation, type => _boss1State);

            IMovementPort movement = new Boss1MovementPort(movementCommands);
            _slot = new BossRuntimeSlot(
                entity.Id,
                bossType,
                configProvider,
                movement,
                new BufferedAttackPort(attackCommands),
                new BufferedEffectPort(effectCommands),
                observation,
                new SystemRandomSource(),
                debugEnabled: ConfigManager.Render.Debug.Enabled,
                movementCommandBuffer: movementCommands,
                attackCommandBuffer: attackCommands,
                effectCommandBuffer: effectCommands);

            var initResult = _runtime.Initialize(_slot, BossState.Boss1_Hover);
            if (initResult.IsError)
                Console.WriteLine($"[BossCharacter] Boss1 初期化失敗: {initResult.FirstError.Description}");

            _entity.Set(new Health(b1Cfg.MaxHp))
               .Set(new InvincibleOnHit(0.0f))
               .Set(new HitFlashConfig(0.08f, Color.White))
               .Set(new Transform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY))
               .Set(new Velocity(0.0f))
               .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
               .Set(new Hitbox(b1Cfg.GetHitboxShape(), b1Cfg.BodyContactDamage, Faction.Hostile, false))
               .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
               .Set(new BossCombatState(IsVulnerable, Phase))
               .Add<BossTag>()
               .Add<Boss1Tag>();
        }
        else
        {
            var b2Cfg = ConfigManager.Boss2;

            _boss1State = null;
            _boss2State = new Boss2BehaviorState(b2Cfg.SpawnX, b2Cfg.SpawnY);

            var moduleMap = CreateBoss2ModuleMap(_boss2State, observation, navGraphProvider);
            TransitionService transitionService = new(moduleMap, observation);
            _runtime = new BossAiRuntime(moduleMap, transitionService, observation, type => _boss2State);

            IMovementPort movement = new Boss2MovementPort(movementCommands);
            _slot = new BossRuntimeSlot(
                entity.Id,
                bossType,
                configProvider,
                movement,
                new BufferedAttackPort(attackCommands),
                new BufferedEffectPort(effectCommands),
                observation,
                new SystemRandomSource(),
                debugEnabled: ConfigManager.Render.Debug.Enabled,
                movementCommandBuffer: movementCommands,
                attackCommandBuffer: attackCommands,
                effectCommandBuffer: effectCommands);

            var initResult = _runtime.Initialize(_slot, BossState.Boss2_Idle);
            if (initResult.IsError)
                Console.WriteLine($"[BossCharacter] Boss2 初期化失敗: {initResult.FirstError.Description}");

            _entity.Set(new Health(b2Cfg.MaxHp))
               .Set(new InvincibleOnHit(0.0f))
               .Set(new HitFlashConfig(0.08f, Color.White))
               .Set(new Transform(b2Cfg.SpawnX, b2Cfg.SpawnY, b2Cfg.ScaleX, b2Cfg.ScaleY))
               .Set(new Velocity(0.0f))
               .Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight))
               .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
               .Set(new Hitbox(b2Cfg.GetHitboxShape(), b2Cfg.BodyContactDamage, Faction.Hostile, false))
               .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
               .Set(new BossCombatState(IsVulnerable, Phase))
               .Add<BossTag>()
               .Add<Boss2Tag>();
        }

        UpdateCombatStateComponents();
    }

    /// <summary>Boss の種別を取得します。</summary>
    public BossType BossType { get; }

    /// <summary>Boss の名称を取得します。</summary>
    public string BossName => BossType == BossType.Boss1 ? ConfigManager.Boss1.BossName : ConfigManager.Boss2.BossName;

    /// <summary>現在の Phase を取得します。</summary>
    public Phase Phase => _slot.Phase;

    /// <summary>現在の Boss 状態を取得します。</summary>
    public BossState CurrentState => _slot.CurrentStateId;

    /// <summary>現在選択されている攻撃技能を取得します。</summary>
    public BossSkill CurrentSkill => BossType == BossType.Boss1
        ? _boss1State?.CurrentSkill ?? _slot.AttackData.Skill
        : _boss2State?.CurrentSkill ?? _slot.AttackData.Skill;

    /// <summary>現在選択されている攻撃技能名を取得します。</summary>
    public string SkillName => CurrentSkill != BossSkill.None ? CurrentSkill.ToString() : string.Empty;

    /// <summary>AI 目標地点 X 座標を取得します。</summary>
    public float TargetX => _slot.TargetPositionX;

    /// <summary>AI 目標地点 Y 座標を取得します。</summary>
    public float TargetY => _slot.TargetPositionY;

    /// <summary>Boss 1 の浮遊タイマー値を取得します。</summary>
    public float HoverTimer => _boss1State != null ? _boss1State.HoverTimer : 0.0f;

    /// <summary>Boss が右向きであるかを取得します。</summary>
    public bool FacingRight => _slot.MovementData.FacingRight;

    /// <summary>Boss が無敵状態であるかを取得します。</summary>
    public bool IsInvulnerable => _slot.AiData.IsInvulnerable || Health.InvincibleTimer > 0.0f;

    /// <summary>Boss が後隙による易傷（被ダメージ倍加）状態であるかを取得します。</summary>
    public bool IsVulnerable => BossType == BossType.Boss1
        ? _slot.CurrentStateId == BossState.Boss1_Recovery && CurrentSkill == BossSkill.DiveRam
        : _slot.CurrentStateId == BossState.Boss2_Recovery && CurrentSkill == BossSkill.LeapSmash;

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public float StateTimer => BossType == BossType.Boss1
        ? _boss1State?.StateElapsed ?? 0.0f
        : _boss2State?.StateElapsed ?? 0.0f;

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration => GetCurrentStateDuration();

    /// <summary>接地状態であるかを取得します（地上型 Boss 2 のみ有効）。</summary>
    public bool IsGrounded => BossType == BossType.Boss2 && _entity.Get<CollisionBox>().IsGrounded;

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity => _entity;

    /// <summary>ワールド X 座標を取得または設定します。</summary>
    public float X {
        get => _entity.Get<Transform>().X;
        set => _entity.GetMut<Transform>().X = value;
    }

    /// <summary>ワールド Y 座標を取得または設定します。</summary>
    public float Y {
        get => _entity.Get<Transform>().Y;
        set => _entity.GetMut<Transform>().Y = value;
    }

    /// <summary>X 軸移動速度を取得または設定します。</summary>
    public float Vx {
        get => _entity.Get<Velocity>().Vx;
        set => _entity.GetMut<Velocity>().Vx = value;
    }

    /// <summary>Y 軸移動速度を取得または設定します。</summary>
    public float Vy {
        get => _entity.Get<Velocity>().Vy;
        set => _entity.GetMut<Velocity>().Vy = value;
    }

    /// <summary>被ダメージ倍率を取得します（易傷状態時は倍加）。</summary>
    public float DamageMultiplier => IsVulnerable ? ConfigManager.Player.VulnerableDamageMultiplier : 1.0f;

    /// <summary>
    ///     フレームごとの Boss AI 評価、移動・弾幕・演出コマンド適用を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    /// <param name="playerPosition">プレイヤー自機の現在位置。</param>
    /// <param name="playerVelocity">プレイヤー自機の現在移動速度。</param>
    /// <param name="world">弾丸・エフェクト生成用の Flecs ワールド。</param>
    /// <param name="platformQuery">プラットフォーム足場検索クエリ。</param>
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        ref var health = ref _entity.GetMut<Health>();
        if (_slot.AiData.IsInvulnerable)
        {
            health.InvincibleTimer = MathF.Max(health.InvincibleTimer, 0.1f);
        }

        if (health.Current <= 0.0f) return;

        // 1. プレイヤー観測データの構築
        PlayerObservation playerObservation = new(
            1,
            true,
            playerPosition.X,
            playerPosition.Y,
            playerVelocity.X,
            playerVelocity.Y,
            _frameNumber);

        // 2. AI 状態機の更新（Phase -> 状態遷移 -> コマンド生成）
        _runtime.SynchronizeBossState(BossType, X, Y, Vx, Vy, IsGrounded, playerObservation.IsAlive);
        var updateResult =
            _runtime.Update(_slot, _frameNumber++, deltaTime, Phase, in playerObservation, Health.Current, Health.Max);
        if (updateResult.IsError && _aiErrorLoggedCount++ < 3)
            Console.WriteLine(
                $"[BossCharacter] AI 更新エラー (Frame {_frameNumber - 1}): {updateResult.FirstError.Description}");

        // 3. コマンド実行（物理移動・弾幕発射・画面演出）
        BossCommandExecutor.ExecuteMovement(world, _entity, _slot, deltaTime, platformQuery);
        BossCommandExecutor.ExecuteAttack(world, _entity, _slot);
        BossCommandExecutor.ExecuteEffects(world, _entity, _slot);

        // 4. 演出・描画用リアルタイム同期
        if (BossType == BossType.Boss1 && _boss1State != null)
        {
            ref var t = ref _entity.GetMut<Transform>();
            t.Rotation =
                (float)(MathF.Sin(_boss1State.HoverTimer * ConfigManager.Render.BossEffects.BossHoverAngleSpeed) *
                    ConfigManager.Render.BossEffects.BossHoverAngleAmp);
        }

        UpdateCombatStateComponents();
        _slot.AiData.StateTimer = StateTimer;
        _slot.AiData.StateDuration = StateDuration;

        ref var sprite = ref _entity.GetMut<Sprite>();
        sprite.FlipX = BossType != BossType.Boss1 && !_slot.MovementData.FacingRight;
    }

    /// <summary>
    ///     プレイヤー移動速度を省略した互換用更新メソッド。
    /// </summary>
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        Update(deltaTime, playerPosition, Vec2f.Zero, world, platformQuery);
    }

    /// <summary>
    ///     Boss の状態を初期位置および初期パラメータへリセットします。
    /// </summary>
    public void Reset()
    {
        _frameNumber = 0;

        if (BossType == BossType.Boss1)
        {
            var b1Cfg = ConfigManager.Boss1;

            ref var t = ref _entity.GetMut<Transform>();
            t.X = b1Cfg.SpawnX;
            t.Y = b1Cfg.SpawnY;
            t.ScaleX = b1Cfg.ScaleX;
            t.ScaleY = b1Cfg.ScaleY;
            t.Rotation = 0.0f;

            ref var h1 = ref _entity.GetMut<Health>();
            h1.Max = b1Cfg.MaxHp;
            h1.Current = b1Cfg.MaxHp;
            h1.InvincibleTimer = 0.0f;

            _boss1State?.Reset(b1Cfg.SpawnX, b1Cfg.SpawnY);

            var resetResult = _runtime.Reset(_slot, BossState.Boss1_Hover);
            if (resetResult.IsError)
                Console.WriteLine($"[BossCharacter] Boss1 リセット失敗: {resetResult.FirstError.Description}");
        }
        else
        {
            var b2Cfg = ConfigManager.Boss2;

            ref var t = ref _entity.GetMut<Transform>();
            t.X = b2Cfg.SpawnX;
            t.Y = b2Cfg.SpawnY;
            t.ScaleX = b2Cfg.ScaleX;
            t.ScaleY = b2Cfg.ScaleY;
            t.Rotation = 0.0f;

            ref var h2 = ref _entity.GetMut<Health>();
            h2.Max = b2Cfg.MaxHp;
            h2.Current = b2Cfg.MaxHp;
            h2.InvincibleTimer = 0.0f;

            ref var col = ref _entity.GetMut<CollisionBox>();
            col.IsGrounded = false;
            col.IsOnWall = false;

            _boss2State?.Reset(b2Cfg.SpawnX, b2Cfg.SpawnY);

            var resetResult = _runtime.Reset(_slot, BossState.Boss2_Idle);
            if (resetResult.IsError)
                Console.WriteLine($"[BossCharacter] Boss2 リセット失敗: {resetResult.FirstError.Description}");
        }

        ref var v = ref _entity.GetMut<Velocity>();
        v.Vx = 0.0f;
        v.Vy = 0.0f;

        ref var spr = ref _entity.GetMut<Sprite>();
        spr.IsVisible = true;
        spr.FlipX = false;

        if (_entity.IsAlive()) _entity.Remove<HitFlashEffect>();

        UpdateCombatStateComponents();
        _slot.AiData.StateTimer = StateTimer;
        _slot.AiData.StateDuration = StateDuration;
    }

    private void UpdateCombatStateComponents()
    {
        _entity.Set(new BossCombatState(IsVulnerable, Phase));
    }

    private float GetCurrentStateDuration()
    {
        return BossType == BossType.Boss1
            ? GetBoss1StateDuration()
            : GetBoss2StateDuration();
    }

    private float GetBoss1StateDuration()
    {
        var b1Cfg = ConfigManager.Boss1;
        return _slot.CurrentStateId.AsBoss1() switch {
            Boss1State.Hover => Phase switch {
                Phase.Phase2 => b1Cfg.Phase2.HoverDuration,
                Phase.Phase3 => b1Cfg.Phase3.HoverDuration,
                _ => b1Cfg.Phase1.HoverDuration
            },
            Boss1State.Telegraph => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var telegraphDef)
                ? telegraphDef.TelegraphDuration
                : 0.0f,
            Boss1State.Execute => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var execDef)
                ? execDef.ExecuteDuration
                : 0.0f,
            Boss1State.Recovery => Boss1PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var recDef)
                ? recDef.RecoveryDuration
                : 0.0f,
            Boss1State.PhaseTransition => Phase switch {
                Phase.Phase2 => b1Cfg.Phase2.TransitionDuration,
                Phase.Phase3 => b1Cfg.Phase3.TransitionDuration,
                _ => b1Cfg.Phase1.TransitionDuration
            },
            _ => 0.0f
        };
    }

    private float GetBoss2StateDuration()
    {
        var b2Cfg = ConfigManager.Boss2;
        return _slot.CurrentStateId.AsBoss2() switch {
            Boss2State.Idle => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.IdleDuration,
                Phase.Phase3 => b2Cfg.Phase3.IdleDuration,
                _ => b2Cfg.Phase1.IdleDuration
            },
            Boss2State.PatrolChase => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.ChaseTimeout,
                Phase.Phase3 => b2Cfg.Phase3.ChaseTimeout,
                _ => b2Cfg.Phase1.ChaseTimeout
            },
            Boss2State.MoveToTakeoff => b2Cfg.MoveToTakeoffTimeout,
            Boss2State.PlatformTraverse => b2Cfg.MaxAscentDuration + b2Cfg.LandingRecoveryDuration,
            Boss2State.Telegraph => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var telegraphDef)
                ? telegraphDef.TelegraphDuration
                : 0.0f,
            Boss2State.Execute => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var execDef)
                ? execDef.ExecuteDuration
                : 0.0f,
            Boss2State.WallCrashStun => b2Cfg.WallCrashStunDuration,
            Boss2State.Recovery => Boss2PipelineDefinition.Pipeline.TryGetAttack(CurrentSkill, out var recDef)
                ? recDef.RecoveryDuration
                : 0.0f,
            Boss2State.PhaseTransition => Phase switch {
                Phase.Phase2 => b2Cfg.Phase2.TransitionDuration,
                Phase.Phase3 => b2Cfg.Phase3.TransitionDuration,
                _ => b2Cfg.Phase1.TransitionDuration
            },
            _ => 0.0f
        };
    }

    private static EnumModuleMap CreateBoss1ModuleMap(
        Boss1BehaviorState boss1State,
        ObservationService observation)
    {
        Boss1DefaultConfiguration boss1Config = new();

        IBossStateModule[] modules = [
            new HoverMovementModule(boss1Config, boss1State),
            new Boss1AttackSelectionModule(boss1Config, boss1State),
            new Boss1TelegraphModule(boss1Config, boss1State),
            new Boss1ExecuteModule(boss1Config, boss1State),
            new Boss1RecoveryModule(boss1Config, boss1State),
            new Boss1PhaseTransitionModule(boss1Config, boss1State),
            new Boss1DeadModule(boss1State)
        ];

        string[] dependencies = [
            "AttackPort",
            "Boss1Configuration",
            "EffectPort",
            "MovementPort",
            "ObservationService",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies, observation);
    }

    private static EnumModuleMap CreateBoss2ModuleMap(
        Boss2BehaviorState boss2State,
        ObservationService observation,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
    {
        Boss2DefaultConfiguration boss2Config = new();

        IBossStateModule[] modules = [
            new Boss2IdleModule(boss2State),
            new Boss2PatrolChaseModule(boss2Config, boss2State, navGraphProvider),
            new Boss2MoveToTakeoffModule(boss2Config, boss2State),
            new Boss2PlatformTraverseModule(BossState.Boss2_PlatformTraverse, TraverseMode.Jump, boss2Config,
                boss2State),
            new Boss2TelegraphModule(boss2Config, boss2State),
            new Boss2ExecuteModule(boss2Config, boss2State),
            new Boss2WallCrashStunModule(boss2Config, boss2State),
            new Boss2RecoveryModule(boss2Config, boss2State),
            new Boss2PhaseTransitionModule(boss2Config, boss2State),
            new Boss2DeadModule(boss2State)
        ];

        string[] dependencies = [
            "AttackPort",
            "Boss2Configuration",
            "EffectPort",
            "MovementPort",
            "ObservationService",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies, observation);
    }

    private sealed class SystemRandomSource : IRandomSource {
        private readonly Random _random = new();

        public int NextInt(int exclusiveMax)
        {
            return _random.Next(exclusiveMax);
        }

        public float NextSingle()
        {
            return _random.NextSingle();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // マネージドリソースの追加解放なし
    }
}