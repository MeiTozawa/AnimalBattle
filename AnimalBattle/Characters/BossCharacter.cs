using System;
using AnimalBattle.Ai.Attack;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Ai.Dsl;
using AnimalBattle.Ai.Dsl.Modules;
using AnimalBattle.Ai.Ports;
using AnimalBattle.Ai.Runtime;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Physics;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
/// 多 Boss（Boss 1 / Boss 2）の生命周期、状態遷移、物理、弾幕および描画用データ同期を一元管理する領域モデルクラスです。
/// 従来の複雑な ECS 搬運層（MigrationAdapter / BossAiRuntimeSystem）から解耦され、直感的なオブジェクト操作を提供します。
/// </summary>
public sealed class BossCharacter
{
    private readonly Entity _entity;
    private readonly BossType _bossType;
    private readonly Sprite _sprite;
    private readonly Hurtbox _hurtbox;
    private readonly Hitbox _bodyHitbox;
    private readonly CollisionBox _collisionBox;

    private readonly BossAiRuntime _runtime;
    private readonly BossRuntimeSlot _slot;
    private readonly Boss1BehaviorState? _boss1State;
    private readonly Boss2BehaviorState? _boss2State;

    private float _x;
    private float _y;
    private float _vx;
    private float _vy;
    private float _currentHp;
    private float _maxHp;
    private float _hitFlashTimer;
    private bool _isGrounded;
    private long _frameNumber;

    /// <summary>Boss の種別を取得します。</summary>
    public BossType BossType => _bossType;

    /// <summary>ワールド X 座標を取得または設定します。</summary>
    public float X
    {
        get => _x;
        set => _x = value;
    }

    /// <summary>ワールド Y 座標を取得または設定します。</summary>
    public float Y
    {
        get => _y;
        set => _y = value;
    }

    /// <summary>X 軸移動速度を取得または設定します。</summary>
    public float Vx
    {
        get => _vx;
        set => _vx = value;
    }

    /// <summary>Y 軸移動速度を取得または設定します。</summary>
    public float Vy
    {
        get => _vy;
        set => _vy = value;
    }

    /// <summary>現在の HP を取得または設定します。</summary>
    public float CurrentHp
    {
        get => _currentHp;
        set => _currentHp = Math.Clamp(value, 0.0f, _maxHp);
    }

    /// <summary>最大 HP を取得します。</summary>
    public float MaxHp => _maxHp;

    /// <summary>Boss が生存中であるかを取得します。</summary>
    public bool IsAlive => _currentHp > 0.0f;

    /// <summary>現在の Phase を取得します。</summary>
    public Phase Phase => _slot.Phase;

    /// <summary>現在の Boss 状態を取得します。</summary>
    public BossState CurrentState => _slot.CurrentStateId;

    /// <summary>現在選択されている攻撃技能を取得します。</summary>
    public BossSkill CurrentSkill => _slot.AttackData.Skill;

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public float StateTimer => _slot.AiData.StateTimer;

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration => _slot.AiData.StateDuration;

    /// <summary>被弾時白閃タイマーを取得または設定します。</summary>
    public float HitFlashTimer
    {
        get => _hitFlashTimer;
        set => _hitFlashTimer = Math.Max(0.0f, value);
    }

    /// <summary>接地状態であるかを取得します。</summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity => _entity;

    /// <summary>
    /// Boss の Flecs エンティティ、種別およびスプライトハンドルを指定して BossCharacter を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="bossType">Boss 種別（Boss1 または Boss2）。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    public BossCharacter(Entity entity, BossType bossType, int spriteHandle)
    {
        _entity = entity;
        _bossType = bossType;

        ObservationService observation = new();
        ConfigProvider configProvider = ConfigProvider.FromConfigManager(observation: observation);

        PreallocatedCommandBuffer<MovementCommand> movementCommands = new(32);
        PreallocatedCommandBuffer<AttackCommand> attackCommands = new(32);
        PreallocatedCommandBuffer<EffectCommand> effectCommands = new(16);

        if (bossType == BossType.Boss1)
        {
            Boss1Config b1Cfg = ConfigManager.Boss1;
            _x = b1Cfg.BaseX;
            _y = b1Cfg.BaseY;
            _maxHp = b1Cfg.MaxHp;
            _currentHp = b1Cfg.MaxHp;

            _hurtbox = new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile);
            _bodyHitbox = new Hitbox(b1Cfg.GetHitboxShape(), damage: b1Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false);
            _collisionBox = default;
            _sprite = new Sprite(spriteHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192));

            _boss1State = new Boss1BehaviorState(b1Cfg.BaseX, b1Cfg.BaseY);
            _boss2State = null;

            Boss1DefaultConfiguration boss1Config = new();
            Boss1AttackAdapter adapter = new(boss1Config, _boss1State);

            IBossStateModule[] modules =
            [
                new HoverMovementModule(boss1Config, _boss1State),
                new Boss1AttackSelectionModule(boss1Config, _boss1State),
                new DslTelegraphModule(adapter),
                new DslBoss1ExecuteModule(boss1Config, _boss1State),
                new DslRecoveryModule(adapter),
                new Boss1PhaseTransitionModule(boss1Config, _boss1State),
                new Boss1DeadModule(_boss1State)
            ];

            string[] dependencies = ["AttackPort", "Boss1Configuration", "EffectPort", "MovementPort", "ObservationService", "PlayerObservation", "RandomSource"];
            EnumModuleMap moduleMap = new(modules, dependencies, observation);
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

            _runtime.Initialize(_slot, BossState.Boss1_Hover);
            InitializeEntityComponents(b1Cfg.ScaleX, b1Cfg.ScaleY);
        }
        else
        {
            Boss2Config b2Cfg = ConfigManager.Boss2;
            _x = b2Cfg.SpawnX;
            _y = b2Cfg.SpawnY;
            _maxHp = b2Cfg.MaxHp;
            _currentHp = b2Cfg.MaxHp;

            _collisionBox = new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight);
            _hurtbox = new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile);
            _bodyHitbox = new Hitbox(b2Cfg.GetHitboxShape(), damage: b2Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false);
            _sprite = new Sprite(spriteHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 192, 192));

            _boss1State = null;
            _boss2State = new Boss2BehaviorState(b2Cfg.SpawnX, b2Cfg.SpawnY);

            Boss2DefaultConfiguration boss2Config = new();
            Boss2AttackAdapter adapter = new(boss2Config, _boss2State);

            IBossStateModule[] modules =
            [
                new Boss2IdleModule(_boss2State),
                new Boss2PatrolChaseModule(boss2Config, _boss2State),
                new Boss2MoveToTakeoffModule(boss2Config, _boss2State),
                new DslPlatformTraverseModule(BossState.Boss2_PlatformTraverse, TraverseMode.Jump, boss2Config, _boss2State),
                new DslTelegraphModule(adapter),
                new DslBoss2ExecuteModule(boss2Config, _boss2State),
                new Boss2WallCrashStunModule(_boss2State),
                new DslRecoveryModule(adapter),
                new Boss2PhaseTransitionModule(boss2Config, _boss2State),
                new Boss2DeadModule(_boss2State)
            ];

            string[] dependencies = ["AttackPort", "Boss2Configuration", "EffectPort", "MovementPort", "ObservationService", "PlayerObservation", "RandomSource"];
            EnumModuleMap moduleMap = new(modules, dependencies, observation);
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

            _runtime.Initialize(_slot, BossState.Boss2_Idle);
            InitializeEntityComponents(b2Cfg.ScaleX, b2Cfg.ScaleY);
        }

        SyncToEntity();
    }

    /// <summary>
    /// フレームごとの Boss AI 評価、移動・弾幕・演出コマンド適用および Entity 同期を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    /// <param name="playerPosition">プレイヤー自機の現在位置。</param>
    /// <param name="world">弾丸・エフェクト生成用の Flecs ワールド。</param>
    /// <param name="platformQuery">プラットフォーム足場検索クエリ。</param>
    public void Update(
        float deltaTime,
        Vec2f playerPosition,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        // 1. 被弾判定システム（CombatCollisionSystem）による HP・HitFlashTimer の反映
        SyncFromEntity();

        if (!IsAlive)
        {
            SyncToEntity();
            return;
        }

        // 2. プレイヤー観測データの構築
        PlayerObservation playerObservation = new(
            entityId: 1,
            isAlive: true,
            positionX: playerPosition.X,
            positionY: playerPosition.Y,
            velocityX: 0.0f,
            velocityY: 0.0f,
            frameUpdateNumber: _frameNumber);

        // 3. AI 状態機の更新（Phase -> 状態遷移 -> コマンド生成）
        _runtime.SynchronizeBossState(_bossType, _x, _y, _vx, _vy, _isGrounded, playerObservation.IsAlive);
        _runtime.Update(_slot, _frameNumber++, deltaTime, Phase, in playerObservation, _currentHp, _maxHp);

        // 4. コマンド実行（物理移動・弾幕発射・画面演出）
        BossCommandExecutor.ExecuteMovement(world, _entity, _slot, deltaTime, platformQuery);
        BossCommandExecutor.ExecuteAttack(world, _entity, _slot);
        BossCommandExecutor.ExecuteEffects(world, _entity, _slot);

        // 5. 実行後の座標・速度をローカルに反映
        if (_entity.Has<Transform>())
        {
            ref readonly Transform t = ref _entity.Get<Transform>();
            _x = t.X;
            _y = t.Y;
        }

        if (_entity.Has<Velocity>())
        {
            ref readonly Velocity v = ref _entity.Get<Velocity>();
            _vx = v.Vx;
            _vy = v.Vy;
        }

        if (_entity.Has<CollisionBox>())
        {
            _isGrounded = _entity.Get<CollisionBox>().IsGrounded;
        }

        // 6. 描画・HUD システム（WorldRenderSystem / HudRenderSystem）への直伝同期
        SyncToEntity();
    }

    /// <summary>
    /// Boss の状態を初期位置および初期パラメータへリセットします。
    /// </summary>
    public void Reset()
    {
        if (_bossType == BossType.Boss1)
        {
            Boss1Config b1Cfg = ConfigManager.Boss1;
            _x = b1Cfg.BaseX;
            _y = b1Cfg.BaseY;
            _vx = 0.0f;
            _vy = 0.0f;
            _currentHp = b1Cfg.MaxHp;
            _hitFlashTimer = 0.0f;
            _isGrounded = false;

            if (_boss1State != null)
            {
                _boss1State.CurrentPosition = new Vec2f(_x, _y);
                _boss1State.CurrentVelocity = Vec2f.Zero;
                _boss1State.HoverTimer = 0.0f;
                _boss1State.AttackSubTimer = 0.0f;
                _boss1State.AngleOffset = 0.0f;
            }

            _runtime.Initialize(_slot, BossState.Boss1_Hover);
        }
        else
        {
            Boss2Config b2Cfg = ConfigManager.Boss2;
            _x = b2Cfg.SpawnX;
            _y = b2Cfg.SpawnY;
            _vx = 0.0f;
            _vy = 0.0f;
            _currentHp = b2Cfg.MaxHp;
            _hitFlashTimer = 0.0f;
            _isGrounded = false;

            if (_boss2State != null)
            {
                _boss2State.CurrentPosition = new Vec2f(_x, _y);
                _boss2State.CurrentVelocity = Vec2f.Zero;
                _boss2State.StateElapsed = 0.0f;
                _boss2State.JumpCooldown = 0.0f;
            }

            _runtime.Initialize(_slot, BossState.Boss2_Idle);
        }

        SyncToEntity();
    }

    private void InitializeEntityComponents(float scaleX, float scaleY)
    {
        _entity.Set(new Transform(_x, _y, scaleX: scaleX, scaleY: scaleY))
               .Set(new Velocity())
               .Set(_hurtbox)
               .Set(_bodyHitbox)
               .Set(new Health(_maxHp))
               .Set(_sprite)
               .Add<BossTag>();

        if (_bossType == BossType.Boss1)
        {
            _entity.Set(new Boss1Stats(baseX: _x, baseY: _y))
                   .Set(new Boss1StateMachine())
                   .Add<Boss1Tag>();
        }
        else
        {
            _entity.Set(_collisionBox)
                   .Set(new Boss2Stats(spawnX: _x, spawnY: _y))
                   .Set(new Boss2StateMachine())
                   .Add<Boss2Tag>();
        }
    }

    private void SyncFromEntity()
    {
        if (!_entity.IsAlive())
        {
            return;
        }

        if (_entity.Has<Health>())
        {
            ref Health health = ref _entity.GetMut<Health>();
            _currentHp = health.Current;
        }

        if (_bossType == BossType.Boss1 && _entity.Has<Boss1Stats>())
        {
            ref Boss1Stats stats = ref _entity.GetMut<Boss1Stats>();
            _hitFlashTimer = stats.HitFlashTimer;
        }
        else if (_bossType == BossType.Boss2 && _entity.Has<Boss2Stats>())
        {
            ref Boss2Stats stats = ref _entity.GetMut<Boss2Stats>();
            _hitFlashTimer = stats.HitFlashTimer;
        }
    }

    private void SyncToEntity()
    {
        if (!_entity.IsAlive())
        {
            return;
        }

        ref Health health = ref _entity.GetMut<Health>();
        health.Current = _currentHp;
        health.Max = _maxHp;

        BossCompatibilityData compatibility = _slot.CompatibilityView;

        if (_bossType == BossType.Boss1)
        {
            ref Boss1StateMachine fsm = ref _entity.GetMut<Boss1StateMachine>();
            fsm.CurrentState = MapToLegacyBoss1Kind(_slot.CurrentStateId);
            fsm.CurrentSkillName = _slot.AttackData.Skill != BossSkill.None ? _slot.AttackData.Skill.ToString() : string.Empty;
            fsm.StateTimer = _slot.AiData.StateTimer;
            fsm.StateDuration = _slot.AiData.StateDuration;
            fsm.TargetX = compatibility.LegacyTargetX;
            fsm.TargetY = compatibility.LegacyTargetY;
            fsm.IsInvulnerable = _slot.AiData.IsInvulnerable;

            ref Boss1Stats stats = ref _entity.GetMut<Boss1Stats>();
            stats.HoverTimer = _boss1State != null ? _boss1State.HoverTimer : 0.0f;
            stats.HitFlashTimer = _hitFlashTimer;
            stats.Phase = (int)_slot.Phase;
        }
        else
        {
            ref Boss2StateMachine fsm = ref _entity.GetMut<Boss2StateMachine>();
            fsm.CurrentState = MapToLegacyBoss2Kind(_slot.CurrentStateId, _slot.AttackData.Skill);
            fsm.CurrentSkillName = _slot.AttackData.Skill != BossSkill.None ? _slot.AttackData.Skill.ToString() : string.Empty;
            fsm.StateTimer = _slot.AiData.StateTimer;
            fsm.StateDuration = _slot.AiData.StateDuration;
            fsm.TargetX = compatibility.LegacyTargetX;
            fsm.TargetY = compatibility.LegacyTargetY;
            fsm.IsInvulnerable = _slot.AiData.IsInvulnerable;

            ref Boss2Stats stats = ref _entity.GetMut<Boss2Stats>();
            stats.HitFlashTimer = _hitFlashTimer;
            stats.Phase = (int)_slot.Phase;
            stats.FacingRight = _slot.MovementData.FacingRight;

            if (_entity.Has<Sprite>())
            {
                ref Sprite sprite = ref _entity.GetMut<Sprite>();
                sprite.FlipX = !_slot.MovementData.FacingRight;
            }
        }

        _entity.Set(compatibility);
    }

    private static Boss1StateKind MapToLegacyBoss1Kind(BossState state)
    {
        return state.AsBoss1() switch
        {
            Boss1State.Hover => Boss1StateKind.Hovering,
            Boss1State.AttackSelection => Boss1StateKind.Hovering,
            Boss1State.Telegraph => Boss1StateKind.Telegraphing,
            Boss1State.Execute => Boss1StateKind.Casting,
            Boss1State.Recovery => Boss1StateKind.Recovering,
            Boss1State.PhaseTransition => Boss1StateKind.PhaseTransition,
            _ => Boss1StateKind.Hovering
        };
    }

    private static Boss2StateKind MapToLegacyBoss2Kind(BossState state, BossSkill skill)
    {
        return state.AsBoss2() switch
        {
            Boss2State.Idle => Boss2StateKind.Idle,
            Boss2State.PatrolChase => Boss2StateKind.PatrolChase,
            Boss2State.MoveToTakeoff => Boss2StateKind.MoveToTakeoff,
            Boss2State.PlatformTraverse => Boss2StateKind.PlatformJumping,
            Boss2State.Telegraph => skill switch
            {
                BossSkill.HeavySlash => Boss2StateKind.TelegraphHeavySlash,
                BossSkill.DoubleCleave => Boss2StateKind.TelegraphDoubleCleave,
                BossSkill.GroundStomp => Boss2StateKind.TelegraphGroundStomp,
                BossSkill.LeapSmash => Boss2StateKind.TelegraphLeapSmash,
                BossSkill.FrenzyCharge => Boss2StateKind.TelegraphFrenzyCharge,
                BossSkill.EarthquakeRoar => Boss2StateKind.TelegraphEarthquakeRoar,
                _ => Boss2StateKind.TelegraphHeavySlash
            },
            Boss2State.Execute => skill switch
            {
                BossSkill.HeavySlash => Boss2StateKind.ExecutingHeavySlash,
                BossSkill.DoubleCleave => Boss2StateKind.ExecutingDoubleCleave,
                BossSkill.GroundStomp => Boss2StateKind.ExecutingGroundStomp,
                BossSkill.LeapSmash => Boss2StateKind.ExecutingLeapAscent,
                BossSkill.FrenzyCharge => Boss2StateKind.ExecutingFrenzyCharge,
                BossSkill.EarthquakeRoar => Boss2StateKind.ExecutingEarthquakeRoar,
                _ => Boss2StateKind.ExecutingHeavySlash
            },
            Boss2State.WallCrashStun => Boss2StateKind.WallCrashStun,
            Boss2State.Recovery => Boss2StateKind.SkillRecovery,
            Boss2State.PhaseTransition => Boss2StateKind.PhaseTransition,
            Boss2State.Dead => Boss2StateKind.Dead,
            _ => Boss2StateKind.Idle
        };
    }

    private sealed class SystemRandomSource : IRandomSource
    {
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
}
