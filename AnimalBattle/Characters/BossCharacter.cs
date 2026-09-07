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
using ErrorOr;
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
    private Sprite _sprite;
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
    private float _scaleX = 1.0f;
    private float _scaleY = 1.0f;
    private bool _isGrounded;
    private long _frameNumber;

    /// <summary>Boss の種別を取得します。</summary>
    public BossType BossType => _bossType;

    /// <summary>Boss の名称を取得します。</summary>
    public string BossName => _bossType == BossType.Boss1 ? ConfigManager.Boss1.BossName : ConfigManager.Boss2.BossName;

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
    public BossSkill CurrentSkill => _bossType == BossType.Boss1
        ? (_boss1State?.CurrentSkill ?? _slot.AttackData.Skill)
        : (_boss2State?.CurrentSkill ?? _slot.AttackData.Skill);

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
    public bool IsInvulnerable => _slot.AiData.IsInvulnerable;

    /// <summary>Boss が後隙による易傷（被ダメージ倍加）状態であるかを取得します。</summary>
    public bool IsVulnerable => _bossType == BossType.Boss1
        ? (_slot.CurrentStateId == BossState.Boss1_Recovery && CurrentSkill == BossSkill.DiveRam)
        : (_slot.CurrentStateId == BossState.Boss2_Recovery && CurrentSkill == BossSkill.LeapSmash);

    /// <summary>描画スケール X を取得します。</summary>
    public float ScaleX => _scaleX;

    /// <summary>描画スケール Y を取得します。</summary>
    public float ScaleY => _scaleY;

    /// <summary>スプライト表示情報を取得します。</summary>
    public Sprite Sprite => _sprite;

    /// <summary>現在状態の経過時間（秒）を取得します。</summary>
    public float StateTimer => _slot.AiData.StateTimer;

    /// <summary>現在状態の目標持続時間（秒）を取得します。</summary>
    public float StateDuration => _slot.AiData.StateDuration;

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    public bool IsFlashing => _entity.IsAlive() && _entity.Has<HitFlashEffect>();

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

            _hurtbox = new(b1Cfg.GetHurtboxShape(), Faction.Hostile);
            _bodyHitbox = new(b1Cfg.GetHitboxShape(), damage: b1Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false);
            _collisionBox = default;
            _sprite = new(spriteHandle, flipX: false, isVisible: true, srcRect: new(0, 0, 192, 192));

            _boss1State = new(b1Cfg.BaseX, b1Cfg.BaseY);
            _boss2State = new(0.0f, 0.0f);

            EnumModuleMap moduleMap = CreateCompleteModuleMap(_boss1State, _boss2State, observation);
            TransitionService transitionService = new(moduleMap, observation);
            _runtime = new(moduleMap, transitionService, observation, type => _boss1State);

            IMovementPort movement = new Boss1MovementPort(movementCommands);
            _slot = new(
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

            ErrorOr<Success> initResult = _runtime.Initialize(_slot, BossState.Boss1_Hover);
            if (initResult.IsError)
            {
                Console.WriteLine($"[BossCharacter] Boss1 初期化失敗: {initResult.FirstError.Description}");
            }

            InitializeEntityComponents(b1Cfg.ScaleX, b1Cfg.ScaleY);
        }
        else
        {
            Boss2Config b2Cfg = ConfigManager.Boss2;
            _x = b2Cfg.SpawnX;
            _y = b2Cfg.SpawnY;
            _maxHp = b2Cfg.MaxHp;
            _currentHp = b2Cfg.MaxHp;

            _collisionBox = new(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight);
            _hurtbox = new(b2Cfg.GetHurtboxShape(), Faction.Hostile);
            _bodyHitbox = new(b2Cfg.GetHitboxShape(), damage: b2Cfg.BodyContactDamage, faction: Faction.Hostile, destroyOnHit: false);
            _sprite = new(spriteHandle, flipX: false, isVisible: true, srcRect: new(0, 0, 192, 192));

            _boss1State = new(0.0f, 0.0f);
            _boss2State = new(b2Cfg.SpawnX, b2Cfg.SpawnY);

            EnumModuleMap moduleMap = CreateCompleteModuleMap(_boss1State, _boss2State, observation);
            TransitionService transitionService = new(moduleMap, observation);
            _runtime = new(moduleMap, transitionService, observation, type => _boss2State);

            IMovementPort movement = new Boss2MovementPort(movementCommands);
            _slot = new(
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

            ErrorOr<Success> initResult = _runtime.Initialize(_slot, BossState.Boss2_Idle);
            if (initResult.IsError)
            {
                Console.WriteLine($"[BossCharacter] Boss2 初期化失敗: {initResult.FirstError.Description}");
            }

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
        // 1. 被弾判定システム（CombatCollisionSystem）による HP の反映
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
        ErrorOr<Success> updateResult = _runtime.Update(_slot, _frameNumber++, deltaTime, Phase, in playerObservation, _currentHp, _maxHp);
        if (updateResult.IsError && _frameNumber <= 5)
        {
            Console.WriteLine($"[BossCharacter] AI 更新エラー (Frame {_frameNumber - 1}): {updateResult.FirstError.Description}");
        }

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
            _isGrounded = false;

            if (_boss1State != null)
            {
                _boss1State.CurrentPosition = new(_x, _y);
                _boss1State.CurrentVelocity = Vec2f.Zero;
                _boss1State.HoverTimer = 0.0f;
                _boss1State.AttackSubTimer = 0.0f;
                _boss1State.AngleOffset = 0.0f;
            }

            ErrorOr<Success> initResult = _runtime.Initialize(_slot, BossState.Boss1_Hover);
            if (initResult.IsError)
            {
                Console.WriteLine($"[BossCharacter] Boss1 リセット失敗: {initResult.FirstError.Description}");
            }
        }
        else
        {
            Boss2Config b2Cfg = ConfigManager.Boss2;
            _x = b2Cfg.SpawnX;
            _y = b2Cfg.SpawnY;
            _vx = 0.0f;
            _vy = 0.0f;
            _currentHp = b2Cfg.MaxHp;
            _isGrounded = false;

            if (_boss2State != null)
            {
                _boss2State.CurrentPosition = new(_x, _y);
                _boss2State.CurrentVelocity = Vec2f.Zero;
                _boss2State.StateElapsed = 0.0f;
                _boss2State.JumpCooldown = 0.0f;
            }

            ErrorOr<Success> initResult = _runtime.Initialize(_slot, BossState.Boss2_Idle);
            if (initResult.IsError)
            {
                Console.WriteLine($"[BossCharacter] Boss2 リセット失敗: {initResult.FirstError.Description}");
            }
        }

        _sprite.IsVisible = true;
        if (_entity.IsAlive() && _entity.Has<HitFlashEffect>())
        {
            _entity.Remove<HitFlashEffect>();
        }

        SyncToEntity();
    }

    private void InitializeEntityComponents(float scaleX, float scaleY)
    {
        _scaleX = scaleX;
        _scaleY = scaleY;

        _entity.Set(new Transform(_x, _y, scaleX: scaleX, scaleY: scaleY))
               .Set(new Velocity())
               .Set(_hurtbox)
               .Set(_bodyHitbox)
               .Set(new Health(_maxHp, _currentHp))
               .Set(_sprite)
               .Set(new BossCombatState(IsInvulnerable, IsVulnerable, Phase))
               .Add<BossTag>();

        if (_bossType == BossType.Boss1)
        {
            _entity.Add<Boss1Tag>();
        }
        else
        {
            _entity.Set(_collisionBox)
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

        if (_entity.Has<Sprite>())
        {
            ref Sprite spr = ref _entity.GetMut<Sprite>();
            _sprite.IsVisible = spr.IsVisible;
        }
    }

    private void SyncToEntity()
    {
        if (!_entity.IsAlive())
        {
            return;
        }

        float rotation = _bossType == BossType.Boss1 && _boss1State != null
            ? (float)(MathF.Sin(_boss1State.HoverTimer * ConfigManager.Render.BossEffects.BossHoverAngleSpeed) * ConfigManager.Render.BossEffects.BossHoverAngleAmp)
            : 0.0f;
        _entity.Set(new Transform(_x, _y, scaleX: _scaleX, scaleY: _scaleY, rotation: rotation));

        Health health = new(_maxHp, _currentHp);
        _entity.Set(health);

        _entity.Set(new BossCombatState(IsInvulnerable, IsVulnerable, Phase));

        if (_entity.Has<Sprite>())
        {
            ref Sprite sprite = ref _entity.GetMut<Sprite>();
            sprite.IsVisible = _sprite.IsVisible;
            sprite.FlipX = _bossType == BossType.Boss1 ? false : !_slot.MovementData.FacingRight;
        }
    }

    private static EnumModuleMap CreateCompleteModuleMap(
        Boss1BehaviorState boss1State,
        Boss2BehaviorState boss2State,
        ObservationService observation)
    {
        Boss1DefaultConfiguration boss1Config = new();
        Boss2DefaultConfiguration boss2Config = new();
        Boss1AttackAdapter adapter1 = new(boss1Config, boss1State);
        Boss2AttackAdapter adapter2 = new(boss2Config, boss2State);

        IBossStateModule[] modules =
        [
            new HoverMovementModule(boss1Config, boss1State),
            new Boss1AttackSelectionModule(boss1Config, boss1State),
            new DslTelegraphModule(adapter1),
            new DslBoss1ExecuteModule(boss1Config, boss1State),
            new DslRecoveryModule(adapter1),
            new Boss1PhaseTransitionModule(boss1Config, boss1State),
            new Boss1DeadModule(boss1State),

            new Boss2IdleModule(boss2State),
            new Boss2PatrolChaseModule(boss2Config, boss2State),
            new Boss2MoveToTakeoffModule(boss2Config, boss2State),
            new DslPlatformTraverseModule(BossState.Boss2_PlatformTraverse, TraverseMode.Jump, boss2Config, boss2State),
            new DslTelegraphModule(adapter2),
            new DslBoss2ExecuteModule(boss2Config, boss2State),
            new Boss2WallCrashStunModule(boss2State),
            new DslRecoveryModule(adapter2),
            new Boss2PhaseTransitionModule(boss2Config, boss2State),
            new Boss2DeadModule(boss2State)
        ];

        string[] dependencies =
        [
            "AttackPort",
            "Boss1Configuration",
            "Boss2Configuration",
            "EffectPort",
            "MovementPort",
            "ObservationService",
            "PlayerObservation",
            "RandomSource"
        ];

        return new EnumModuleMap(modules, dependencies, observation);
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
