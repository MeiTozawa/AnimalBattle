using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;
using AnimalBattle.Core.Input;
using AnimalBattle.Core.Physics;
using ErrorOr;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
/// プレイヤー自機の移動、跳躍、重力、射撃瞄準およびアニメーション状態を一元管理する領域モデルクラスです。
/// 従来の 8 元組 ECS システムから解耦され、直感的かつ高内聚なオブジェクト設計を提供します。
/// </summary>
public sealed class Player
{
    private readonly Entity _entity;
    private readonly Hurtbox _hurtbox;
    private Animation _animation;
    private CollisionBox _collisionBox;
    private Sprite _sprite;

    private float _x;
    private float _y;
    private float _vx;
    private float _vy;
    private float _currentHp;
    private float _maxHp;
    private float _invincibleTimer;
    private int _remainingJumps;
    private int _maxJumps;
    private bool _isGrounded;
    private float _shootCooldown;
    private float _scaleX;
    private float _scaleY;

    /// <summary>プレイヤーのワールド X 座標を取得または設定します。</summary>
    public float X
    {
        get => _x;
        set => _x = value;
    }

    /// <summary>プレイヤーのワールド Y 座標を取得または設定します。</summary>
    public float Y
    {
        get => _y;
        set => _y = value;
    }

    /// <summary>プレイヤーの X 軸移動速度を取得または設定します。</summary>
    public float Vx
    {
        get => _vx;
        set => _vx = value;
    }

    /// <summary>プレイヤーの Y 軸移動速度を取得または設定します。</summary>
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

    /// <summary>プレイヤーが生存中であるかを取得します。</summary>
    public bool IsAlive => _currentHp > 0.0f;

    /// <summary>無敵時間の残り秒数を取得または設定します。</summary>
    public float InvincibleTimer
    {
        get => _invincibleTimer;
        set => _invincibleTimer = Math.Max(0.0f, value);
    }

    /// <summary>接地状態であるかを取得または設定します。</summary>
    public bool IsGrounded
    {
        get => _isGrounded;
        set => _isGrounded = value;
    }

    /// <summary>残り跳躍可能回数を取得または設定します。</summary>
    public int RemainingJumps
    {
        get => _remainingJumps;
        set => _remainingJumps = value;
    }

    /// <summary>射撃クールダウンの残り秒数を取得または設定します。</summary>
    public float ShootCooldown
    {
        get => _shootCooldown;
        set => _shootCooldown = Math.Max(0.0f, value);
    }

    /// <summary>地形衝突判定ボックスを取得します。</summary>
    public CollisionBox CollisionBox => _collisionBox;

    /// <summary>被弾判定ボックスを取得します。</summary>
    public Hurtbox Hurtbox => _hurtbox;

    /// <summary>アニメーション制御インスタンスを取得します。</summary>
    public Animation Animation => _animation;

    /// <summary>スプライト描画設定インスタンスを取得します。</summary>
    public Sprite Sprite => _sprite;

    /// <summary>プレイヤーの描画スケール X を取得します。</summary>
    public float ScaleX => _scaleX;

    /// <summary>プレイヤーの描画スケール Y を取得します。</summary>
    public float ScaleY => _scaleY;

    /// <summary>互換性保持用の Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity => _entity;

    /// <summary>
    /// 設定情報と Flecs エンティティ句柄を指定して、プレイヤー自機を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライトの画像ハンドル。</param>
    public Player(Entity entity, PlayerConfig pCfg, int spriteHandle)
    {
        ArgumentNullException.ThrowIfNull(pCfg);
        _entity = entity;

        _x = pCfg.SpawnX;
        _y = pCfg.SpawnY;
        _vx = 0.0f;
        _vy = 0.0f;
        _maxHp = pCfg.MaxHp;
        _currentHp = pCfg.MaxHp;
        _invincibleTimer = 0.0f;
        _maxJumps = pCfg.MaxJumps;
        _remainingJumps = pCfg.MaxJumps;
        _isGrounded = false;
        _shootCooldown = 0.0f;
        _scaleX = pCfg.ScaleX;
        _scaleY = pCfg.ScaleY;

        _collisionBox = new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight);
        _hurtbox = new Hurtbox(pCfg.GetHurtboxShape(), Faction.Friendly);
        _animation = new Animation("Idle", CharacterType.Cat, isPlaying: true);
        _sprite = new Sprite(spriteHandle, flipX: false, isVisible: true, srcRect: new Rect<int>(0, 0, 64, 64));

        UpdateAnimation(0.0f);
        SyncToEntity();
    }

    /// <summary>
    /// フレームごとの入力、物理移動、跳躍、射撃、およびアニメーション更新を実行します。
    /// </summary>
    /// <param name="dt">デルタタイム（秒）。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="arenaCfg">アリーナ設定。</param>
    /// <param name="platformQuery">プラットフォーム検索クエリ。</param>
    /// <param name="world">エフェクト・弾丸生成用の Flecs ワールド。</param>
    public void Update(
        float dt,
        PlayerConfig pCfg,
        ArenaConfig arenaCfg,
        Query<Transform, CollisionBox> platformQuery,
        World world)
    {
        ArgumentNullException.ThrowIfNull(pCfg);
        ArgumentNullException.ThrowIfNull(arenaCfg);

        // 外部衝突システム（CombatCollisionSystem）による被弾ダメージの反映
        SyncFromEntity();

        if (_invincibleTimer > 0.0f)
        {
            _invincibleTimer = Math.Max(0.0f, _invincibleTimer - dt);
        }

        if (_shootCooldown > 0.0f)
        {
            _shootCooldown = Math.Max(0.0f, _shootCooldown - dt);
        }

        if (!IsAlive)
        {
            _vx = 0.0f;
            _vy = 0.0f;
            SyncToEntity();
            return;
        }

        // 1. 水平移動入力処理
        ProcessHorizontalInput(pCfg);

        // 2. 跳躍入力処理
        ProcessJumpInput(world, pCfg);

        // 3. 物理・地形衝突処理
        ApplyPhysicsAndCollisions(platformQuery, pCfg, arenaCfg, dt);

        // 4. アニメーション状態更新
        UpdateAnimation(dt);

        // 5. 手銃射撃処理
        ProcessShootInput(world, pCfg);

        // 6. 外部システム（描画・AI 観測・被弾判定）への同期
        SyncToEntity();
    }

    /// <summary>
    /// プレイヤーの状態を初期スポーン地点および初期パラメータにリセットします。
    /// </summary>
    /// <param name="pCfg">プレイヤー設定。</param>
    public void Reset(PlayerConfig pCfg)
    {
        ArgumentNullException.ThrowIfNull(pCfg);

        _x = pCfg.SpawnX;
        _y = pCfg.SpawnY;
        _vx = 0.0f;
        _vy = 0.0f;
        _currentHp = pCfg.MaxHp;
        _maxHp = pCfg.MaxHp;
        _invincibleTimer = 0.0f;
        _remainingJumps = pCfg.MaxJumps;
        _maxJumps = pCfg.MaxJumps;
        _isGrounded = false;
        _shootCooldown = 0.0f;
        _scaleX = pCfg.ScaleX;
        _scaleY = pCfg.ScaleY;

        _animation.Play("Idle");
        _sprite.FlipX = false;
        _sprite.IsVisible = true;
        UpdateAnimation(0.0f);

        SyncToEntity();
    }

    /// <summary>
    /// 被弾ダメージを適用し、無敵時間を設定します。
    /// </summary>
    /// <param name="amount">ダメージ量。</param>
    /// <param name="invincibleDuration">付与する無敵時間（秒）。</param>
    public void TakeDamage(float amount, float invincibleDuration)
    {
        if (_invincibleTimer > 0.0f || !IsAlive)
        {
            return;
        }

        CurrentHp = Math.Max(0.0f, _currentHp - amount);
        _invincibleTimer = invincibleDuration;
        if (!IsAlive)
        {
            _sprite.IsVisible = false;
        }

        SyncToEntity();
    }

    private void ProcessHorizontalInput(PlayerConfig pCfg)
    {
        bool isLeft = InputService.IsHolding(InputAction.Left);
        bool isRight = InputService.IsHolding(InputAction.Right);

        if (isLeft)
        {
            _vx = -pCfg.MoveSpeed;
            _sprite.FlipX = true;
        }
        else if (isRight)
        {
            _vx = pCfg.MoveSpeed;
            _sprite.FlipX = false;
        }
        else
        {
            _vx = 0.0f;
        }
    }

    private void ProcessJumpInput(World world, PlayerConfig pCfg)
    {
        bool jumpJustPressed = InputService.IsPressed(InputAction.Jump);
        if (!jumpJustPressed)
        {
            return;
        }

        if (_isGrounded)
        {
            _vy = pCfg.FirstJumpVelocity;
            _isGrounded = false;
            _remainingJumps = 1;
        }
        else if (_remainingJumps > 0)
        {
            _vy = pCfg.SecondJumpVelocity;
            _remainingJumps--;
            CombatEffectHelper.SpawnJumpRingEffect(world, _x, _y + _collisionBox.Height / 2.0f);
        }
    }

    private void ApplyPhysicsAndCollisions(
        Query<Transform, CollisionBox> platformQuery,
        PlayerConfig pCfg,
        ArenaConfig arenaCfg,
        float dt)
    {
        float currentVy = _vy;
        PhysicsService.ApplyGravity(ref currentVy, pCfg.Gravity, pCfg.MaxFallSpeed, dt);
        _vy = currentVy;

        float nextX = _x + _vx * dt;
        float nextY = _y + _vy * dt;

        nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

        bool isGrounded = false;
        float adjustedPosY = nextY;
        float adjustedVy = _vy;

        float currentY = _y;
        Box2D playerBox = new(nextX, nextY, _collisionBox.Width, _collisionBox.Height);

        platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
        {
            Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
            if (PhysicsService.CheckPlatformGround(
                currentY,
                nextY,
                playerBox,
                platformBox,
                out float groundY,
                out float vy))
            {
                isGrounded = true;
                adjustedPosY = groundY;
                adjustedVy = vy;
            }
        });

        float floorLimitY = 560.0f - _collisionBox.Height / 2.0f;
        if (adjustedPosY >= floorLimitY)
        {
            isGrounded = true;
            adjustedPosY = floorLimitY;
            adjustedVy = 0.0f;
        }

        if (isGrounded)
        {
            _remainingJumps = _maxJumps;
        }

        _x = nextX;
        _y = adjustedPosY;
        _vy = adjustedVy;
        _isGrounded = isGrounded;
    }

    private void UpdateAnimation(float dt)
    {
        if (!_isGrounded)
        {
            if (_vy < 0.0f)
            {
                _animation.Play("Jump");
            }
            else
            {
                _animation.Play("Fall");
            }
        }
        else if (Math.Abs(_vx) > 0.1f)
        {
            _animation.Play("Run");
        }
        else
        {
            _animation.Play("Idle");
        }

        if (_animation.IsPlaying)
        {
            _animation.Timer += dt;

            ErrorOr<AnimationConfig> charConfigRes = AssetManager.GetCharacterConfig(_animation.CharacterType);
            if (!charConfigRes.IsError && charConfigRes.Value != null)
            {
                AnimationConfig charConfig = charConfigRes.Value;
                ErrorOr<int> handleRes = AssetManager.GetCharacterSpriteHandle(_animation.CharacterType, _animation.CurrentClip);
                if (!handleRes.IsError && handleRes.Value >= 0)
                {
                    _sprite.GraphHandle = handleRes.Value;
                }

                _sprite.SrcRect = AssetManager.CalculateSrcRect(charConfig, _animation.CurrentClip, _animation.Timer, out bool isFinished);
                _animation.IsFinished = isFinished;
            }
        }
    }

    private void ProcessShootInput(World world, PlayerConfig pCfg)
    {
        bool shootJustPressed = InputService.IsPressed(InputAction.Attack);
        if (!shootJustPressed || _shootCooldown > 0.0f)
        {
            return;
        }

        CalculateAimDirection(out float dirX, out float dirY);
        SpawnBullet(world, pCfg, dirX, dirY);
        _shootCooldown = pCfg.ShootCooldown;
    }

    private void CalculateAimDirection(out float dirX, out float dirY)
    {
        bool isLeft = InputService.IsHolding(InputAction.Left);
        bool isRight = InputService.IsHolding(InputAction.Right);
        bool isUp = InputService.IsHolding(InputAction.Up);
        bool isDown = InputService.IsHolding(InputAction.Down);

        float inputX = (isRight ? 1.0f : 0.0f) - (isLeft ? 1.0f : 0.0f);
        float inputY = (isDown ? 1.0f : 0.0f) - (isUp ? 1.0f : 0.0f);

        if (Math.Abs(inputX) > 0.001f || Math.Abs(inputY) > 0.001f)
        {
            float length = MathF.Sqrt(inputX * inputX + inputY * inputY);
            dirX = inputX / length;
            dirY = inputY / length;

            if (inputX < 0.0f)
            {
                _sprite.FlipX = true;
            }
            else if (inputX > 0.0f)
            {
                _sprite.FlipX = false;
            }
        }
        else
        {
            dirX = _sprite.FlipX ? -1.0f : 1.0f;
            dirY = 0.0f;
        }
    }

    private void SpawnBullet(World world, PlayerConfig pCfg, float dirX, float dirY)
    {
        float spawnX = _x + dirX * pCfg.BulletSpawnOffset;
        float spawnY = _y + pCfg.BulletSpawnOffsetY + dirY * pCfg.BulletSpawnOffset;

        world.Entity()
             .Set(new Transform(spawnX, spawnY))
             .Set(new Velocity(dirX * pCfg.BulletSpeed, dirY * pCfg.BulletSpeed))
             .Set(new CollisionBox(pCfg.BulletWidth, pCfg.BulletHeight))
             .Set(new Hitbox(pCfg.GetBulletHitboxShape(), damage: pCfg.BulletDamage, faction: Faction.Friendly, destroyOnHit: true))
             .Add<PlayerBulletTag>();
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
            _invincibleTimer = health.InvincibleTimer;
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

        _entity.Set(new Transform(_x, _y, scaleX: _scaleX, scaleY: _scaleY));
        _entity.Set(new Velocity(_vx, _vy));

        _collisionBox.IsGrounded = _isGrounded;
        _entity.Set(_collisionBox);
        _entity.Set(_hurtbox);

        Health health = new(_maxHp);
        health.Current = _currentHp;
        health.InvincibleTimer = _invincibleTimer;
        _entity.Set(health);

        _entity.Set(_sprite);
        _entity.Set(_animation);

        if (!_entity.Has<PlayerTag>())
        {
            _entity.Add<PlayerTag>();
        }
    }
}
