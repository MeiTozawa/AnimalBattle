using System;
using AnimalBattle.Configs;
using ErrorOr;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
/// プレイヤー自機の移動、跳躍、重力、射撃瞄準およびアニメーション状態を一元管理する領域モデルクラスです。
/// 实体 Facade 原則に従い、ECS エンティティのコンポーネントを唯一の真実の源として直接操作します。
/// </summary>
public sealed class Player
{
    private static readonly StringId ClipIdle = "Idle";
    private static readonly StringId ClipRun = "Run";
    private static readonly StringId ClipJump = "Jump";
    private static readonly StringId ClipFall = "Fall";

    private readonly Entity _entity;
    private readonly Hurtbox _hurtbox;

    private int _remainingJumps;
    private int _maxJumps;
    private float _shootCooldown;

    /// <summary>プレイヤーのワールド X 座標を取得または設定します。</summary>
    public float X
    {
        get => _entity.Has<Transform>() ? _entity.Get<Transform>().X : 0.0f;
        set
        {
            if (_entity.Has<Transform>())
            {
                _entity.GetMut<Transform>().X = value;
            }
        }
    }

    /// <summary>プレイヤーのワールド Y 座標を取得または設定します。</summary>
    public float Y
    {
        get => _entity.Has<Transform>() ? _entity.Get<Transform>().Y : 0.0f;
        set
        {
            if (_entity.Has<Transform>())
            {
                _entity.GetMut<Transform>().Y = value;
            }
        }
    }

    /// <summary>プレイヤーの X 軸移動速度を取得または設定します。</summary>
    public float Vx
    {
        get => _entity.Has<Velocity>() ? _entity.Get<Velocity>().Vx : 0.0f;
        set
        {
            if (_entity.Has<Velocity>())
            {
                _entity.GetMut<Velocity>().Vx = value;
            }
        }
    }

    /// <summary>プレイヤーの Y 軸移動速度を取得または設定します。</summary>
    public float Vy
    {
        get => _entity.Has<Velocity>() ? _entity.Get<Velocity>().Vy : 0.0f;
        set
        {
            if (_entity.Has<Velocity>())
            {
                _entity.GetMut<Velocity>().Vy = value;
            }
        }
    }

    /// <summary>現在の HP を取得または設定します。</summary>
    public float CurrentHp
    {
        get => _entity.Has<Health>() ? _entity.Get<Health>().Current : 0.0f;
        set
        {
            if (_entity.Has<Health>())
            {
                ref Health health = ref _entity.GetMut<Health>();
                health.Current = Math.Clamp(value, 0.0f, health.Max);
            }
        }
    }

    /// <summary>最大 HP を取得します。</summary>
    public float MaxHp => _entity.Has<Health>() ? _entity.Get<Health>().Max : 0.0f;

    /// <summary>プレイヤーが生存中であるかを取得します。</summary>
    public bool IsAlive => CurrentHp > 0.0f;

    /// <summary>無敵時間の残り秒数を取得または設定します。</summary>
    public float InvincibleTimer
    {
        get => _entity.Has<Health>() ? _entity.Get<Health>().InvincibleTimer : 0.0f;
        set
        {
            if (_entity.Has<Health>())
            {
                _entity.GetMut<Health>().InvincibleTimer = Math.Max(0.0f, value);
            }
        }
    }

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    public bool IsFlashing => _entity.IsAlive() && _entity.Has<HitFlashEffect>();

    /// <summary>接地状態であるかを取得または設定します。</summary>
    public bool IsGrounded
    {
        get => _entity.Has<CollisionBox>() && _entity.Get<CollisionBox>().IsGrounded;
        set
        {
            if (_entity.Has<CollisionBox>())
            {
                _entity.GetMut<CollisionBox>().IsGrounded = value;
            }
        }
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
    public CollisionBox CollisionBox => _entity.Has<CollisionBox>() ? _entity.Get<CollisionBox>() : default;

    /// <summary>被弾判定ボックスを取得します。</summary>
    public Hurtbox Hurtbox => _hurtbox;

    /// <summary>アニメーション制御インスタンスを取得します。</summary>
    public Animation Animation => _entity.Has<Animation>() ? _entity.Get<Animation>() : default;

    /// <summary>スプライト描画設定インスタンスを取得します。</summary>
    public Sprite Sprite => _entity.Has<Sprite>() ? _entity.Get<Sprite>() : default;

    /// <summary>プレイヤーの描画スケール X を取得します。</summary>
    public float ScaleX => _entity.Has<Transform>() ? _entity.Get<Transform>().ScaleX : 1.0f;

    /// <summary>プレイヤーの描画スケール Y を取得します。</summary>
    public float ScaleY => _entity.Has<Transform>() ? _entity.Get<Transform>().ScaleY : 1.0f;

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
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

        _maxJumps = pCfg.MaxJumps;
        _remainingJumps = pCfg.MaxJumps;
        _shootCooldown = 0.0f;
        _hurtbox = new(pCfg.GetHurtboxShape(), Faction.Friendly);

        _entity.Set(new Transform(pCfg.SpawnX, pCfg.SpawnY, scaleX: pCfg.ScaleX, scaleY: pCfg.ScaleY))
               .Set(new Velocity(0.0f, 0.0f))
               .Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight))
               .Set(_hurtbox)
               .Set(new Health(pCfg.MaxHp))
               .Set(new Animation("Idle", CharacterType.Cat, isPlaying: true))
               .Set(new Sprite(spriteHandle, flipX: false, isVisible: true, srcRect: new(0, 0, 64, 64)))
               .Add<PlayerTag>();

        UpdateAnimation(0.0f);
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

        if (_entity.Has<Health>())
        {
            ref Health health = ref _entity.GetMut<Health>();
            if (health.InvincibleTimer > 0.0f)
            {
                health.InvincibleTimer = Math.Max(0.0f, health.InvincibleTimer - dt);
            }
        }

        if (_shootCooldown > 0.0f)
        {
            _shootCooldown = Math.Max(0.0f, _shootCooldown - dt);
        }

        if (!IsAlive)
        {
            if (_entity.Has<Velocity>())
            {
                ref Velocity v = ref _entity.GetMut<Velocity>();
                v.Vx = 0.0f;
                v.Vy = 0.0f;
            }
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
    }

    /// <summary>
    /// プレイヤーの状態を初期スポーン地点および初期パラメータにリセットします。
    /// </summary>
    /// <param name="pCfg">プレイヤー設定。</param>
    public void Reset(PlayerConfig pCfg)
    {
        ArgumentNullException.ThrowIfNull(pCfg);

        if (_entity.Has<Transform>())
        {
            ref Transform t = ref _entity.GetMut<Transform>();
            t.X = pCfg.SpawnX;
            t.Y = pCfg.SpawnY;
            t.ScaleX = pCfg.ScaleX;
            t.ScaleY = pCfg.ScaleY;
        }

        if (_entity.Has<Velocity>())
        {
            ref Velocity v = ref _entity.GetMut<Velocity>();
            v.Vx = 0.0f;
            v.Vy = 0.0f;
        }

        if (_entity.Has<Health>())
        {
            ref Health h = ref _entity.GetMut<Health>();
            h.Max = pCfg.MaxHp;
            h.Current = pCfg.MaxHp;
            h.InvincibleTimer = 0.0f;
        }

        if (_entity.Has<CollisionBox>())
        {
            ref CollisionBox c = ref _entity.GetMut<CollisionBox>();
            c.IsGrounded = false;
            c.IsOnWall = false;
        }

        _remainingJumps = pCfg.MaxJumps;
        _maxJumps = pCfg.MaxJumps;
        _shootCooldown = 0.0f;

        if (_entity.Has<Animation>())
        {
            ref Animation anim = ref _entity.GetMut<Animation>();
            anim.Play(ClipIdle);
        }

        if (_entity.Has<Sprite>())
        {
            ref Sprite spr = ref _entity.GetMut<Sprite>();
            spr.FlipX = false;
            spr.IsVisible = true;
        }

        UpdateAnimation(0.0f);

        if (_entity.IsAlive() && _entity.Has<HitFlashEffect>())
        {
            _entity.Remove<HitFlashEffect>();
        }
    }

    private void ProcessHorizontalInput(PlayerConfig pCfg)
    {
        bool isLeft = InputService.IsHolding(InputAction.Left);
        bool isRight = InputService.IsHolding(InputAction.Right);

        ref Velocity vel = ref _entity.GetMut<Velocity>();
        ref Sprite spr = ref _entity.GetMut<Sprite>();

        if (isLeft)
        {
            vel.Vx = -pCfg.MoveSpeed;
            spr.FlipX = true;
        }
        else if (isRight)
        {
            vel.Vx = pCfg.MoveSpeed;
            spr.FlipX = false;
        }
        else
        {
            vel.Vx = 0.0f;
        }
    }

    private void ProcessJumpInput(World world, PlayerConfig pCfg)
    {
        bool jumpJustPressed = InputService.IsPressed(InputAction.Jump);
        if (!jumpJustPressed)
        {
            return;
        }

        ref Velocity vel = ref _entity.GetMut<Velocity>();
        ref CollisionBox col = ref _entity.GetMut<CollisionBox>();
        ref Transform trans = ref _entity.GetMut<Transform>();

        if (col.IsGrounded)
        {
            vel.Vy = pCfg.FirstJumpVelocity;
            col.IsGrounded = false;
            _remainingJumps = 1;
        }
        else if (_remainingJumps > 0)
        {
            vel.Vy = pCfg.SecondJumpVelocity;
            _remainingJumps--;
            CombatEffectHelper.SpawnJumpRingEffect(world, trans.X, trans.Y + (col.Height / 2.0f));
        }
    }

    private void ApplyPhysicsAndCollisions(
        Query<Transform, CollisionBox> platformQuery,
        PlayerConfig pCfg,
        ArenaConfig arenaCfg,
        float dt)
    {
        ref Transform trans = ref _entity.GetMut<Transform>();
        ref Velocity vel = ref _entity.GetMut<Velocity>();
        ref CollisionBox col = ref _entity.GetMut<CollisionBox>();

        float currentVy = vel.Vy;
        PhysicsService.ApplyGravity(ref currentVy, pCfg.Gravity, pCfg.MaxFallSpeed, dt);
        vel.Vy = currentVy;

        float nextX = trans.X + (vel.Vx * dt);
        float nextY = trans.Y + (vel.Vy * dt);

        nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

        float currentY = trans.Y;
        Box2D playerBox = new(nextX, nextY, col.Width, col.Height);

        bool hitPlatform = false;
        float platformGroundY = float.MaxValue;
        float platformVy = 0.0f;

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
                if (!hitPlatform || groundY < platformGroundY)
                {
                    hitPlatform = true;
                    platformGroundY = groundY;
                    platformVy = vy;
                }
            }
        });

        bool isGrounded = hitPlatform;
        float adjustedPosY = hitPlatform ? platformGroundY : nextY;
        float adjustedVy = hitPlatform ? platformVy : vel.Vy;

        float floorLimitY = ConfigManager.Arena.FloorLimitY - (col.Height / 2.0f);
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

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;
        col.IsGrounded = isGrounded;
    }

    private void UpdateAnimation(float dt)
    {
        if (!_entity.Has<Animation>())
        {
            return;
        }

        ref Animation anim = ref _entity.GetMut<Animation>();
        ref readonly Velocity vel = ref _entity.Get<Velocity>();
        ref readonly CollisionBox col = ref _entity.Get<CollisionBox>();

        if (!col.IsGrounded)
        {
            if (vel.Vy < 0.0f)
            {
                anim.Play(ClipJump);
            }
            else
            {
                anim.Play(ClipFall);
            }
        }
        else if (Math.Abs(vel.Vx) > 0.1f)
        {
            anim.Play(ClipRun);
        }
        else
        {
            anim.Play(ClipIdle);
        }

        if (anim.IsPlaying)
        {
            anim.Timer += dt;

            ErrorOr<AnimationConfig> charConfigRes = AssetManager.GetCharacterConfig(anim.CharacterType);
            if (!charConfigRes.IsError && charConfigRes.Value != null)
            {
                AnimationConfig charConfig = charConfigRes.Value;
                ref Sprite spr = ref _entity.GetMut<Sprite>();
                ErrorOr<int> handleRes = AssetManager.GetCharacterSpriteHandle(anim.CharacterType, anim.CurrentClip);
                if (!handleRes.IsError && handleRes.Value >= 0)
                {
                    spr.GraphHandle = handleRes.Value;
                }

                spr.SrcRect = AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out bool isFinished);
                anim.IsFinished = isFinished;
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
            float len = MathF.Sqrt((inputX * inputX) + (inputY * inputY));
            dirX = inputX / len;
            dirY = inputY / len;

            if (_entity.Has<Sprite>())
            {
                ref Sprite spr = ref _entity.GetMut<Sprite>();
                if (inputX < 0.0f)
                {
                    spr.FlipX = true;
                }
                else if (inputX > 0.0f)
                {
                    spr.FlipX = false;
                }
            }
        }
        else
        {
            ref readonly Sprite spr = ref _entity.Get<Sprite>();
            dirX = spr.FlipX ? -1.0f : 1.0f;
            dirY = 0.0f;
        }
    }

    private void SpawnBullet(World world, PlayerConfig pCfg, float dirX, float dirY)
    {
        ref readonly Transform trans = ref _entity.Get<Transform>();

        float spawnX = trans.X + (dirX * pCfg.BulletSpawnOffset);
        float spawnY = trans.Y + pCfg.BulletSpawnOffsetY + (dirY * pCfg.BulletSpawnOffset);
        float vx = dirX * pCfg.BulletSpeed;
        float vy = dirY * pCfg.BulletSpeed;

        // 事前確保されたエンティティプールから通常弾を貸し出し
        CombatEntityPoolHelper.RentPlayerBullet(world)
             .Set(new Transform(spawnX, spawnY))
             .Set(new Velocity(vx, vy))
             .Set(new CollisionBox(pCfg.BulletWidth, pCfg.BulletHeight))
             .Set(new Hitbox(pCfg.GetBulletHitboxShape(), damage: pCfg.BulletDamage, faction: Faction.Friendly, destroyOnHit: true))
             .Add<PlayerBulletTag>();
    }
}
