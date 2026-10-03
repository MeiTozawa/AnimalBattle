using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     プレイヤー自機の移動、跳躍、重力、射撃瞄準およびアニメーション状態を一元管理する領域モデルクラスです。
///     实体 Facade 原則に従い、ECS エンティティのコンポーネントを唯一の真実の源として直接操作します。
/// </summary>
public sealed class Player : ICharacter {
    private static readonly StringId ClipIdle = "Idle";
    private static readonly StringId ClipRun = "Run";
    private static readonly StringId ClipJump = "Jump";
    private static readonly StringId ClipFall = "Fall";

    private Entity _entity;
    private int _maxJumps;

    private float _shootCooldown;

    /// <summary>
    ///     設定情報と Flecs エンティティ句柄を指定して、プレイヤー自機を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライトの画像ハンドル。</param>
    public Player(Entity entity, PlayerConfig pCfg, int spriteHandle)
    {
        ArgumentNullException.ThrowIfNull(pCfg);
        _entity = entity;

        _maxJumps = pCfg.MaxJumps;
        RemainingJumps = pCfg.MaxJumps;
        _shootCooldown = 0.0f;
        Hurtbox = new Hurtbox(pCfg.GetHurtboxShape());

        _entity.Set(new Transform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY))
           .Set(new Velocity(0.0f))
           .Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight))
           .Set(Hurtbox)
           .Set(new Health(pCfg.MaxHp))
           .Set(new Animation("Idle"))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 64, 64)))
           .Set(new CharacterMovementIntent())
           .Set(new CharacterWeaponIntent())
           .Set(JumpCapability.FromPlayerConfig(pCfg))
           .Add<PlayerTag>();

        UpdateAnimation(0.0f);
    }

    /// <summary>プレイヤーの現在の高レベル動作状態を取得します。</summary>
    public PlayerState CurrentState {
        get
        {
            if (!IsAlive) return PlayerState.Dead;

            if (IsFlashing || InvincibleTimer > 0.0f) return PlayerState.Hurt;

            if (IsGrounded) return PlayerState.Grounded;

            return PlayerState.Airborne;
        }
    }

    /// <summary>無敵時間の残り秒数を取得または設定します。</summary>
    public float InvincibleTimer {
        get => _entity.Has<Health>() ? _entity.Get<Health>().InvincibleTimer : 0.0f;
        set
        {
            if (_entity.Has<Health>()) _entity.GetMut<Health>().InvincibleTimer = Math.Max(0.0f, value);
        }
    }

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    public bool IsFlashing => _entity.IsAlive() && _entity.Has<HitFlashEffect>();

    /// <summary>接地状態であるかを取得または設定します。</summary>
    public bool IsGrounded {
        get => _entity.Has<CollisionBox>() && _entity.Get<CollisionBox>().IsGrounded;
        set
        {
            if (_entity.Has<CollisionBox>()) _entity.GetMut<CollisionBox>().IsGrounded = value;
        }
    }

    /// <summary>残り跳躍可能回数を取得または設定します。</summary>
    public int RemainingJumps {
        get => _entity.Has<JumpCapability>() ? _entity.Get<JumpCapability>().RemainingJumps : _remainingJumps;
        set
        {
            _remainingJumps = value;
            if (_entity.Has<JumpCapability>())
            {
                _entity.GetMut<JumpCapability>().RemainingJumps = value;
            }
        }
    }
    private int _remainingJumps;

    /// <summary>射撃クールダウンの残り秒数を取得または設定します。</summary>
    public float ShootCooldown {
        get => _shootCooldown;
        set => _shootCooldown = Math.Max(0.0f, value);
    }

    /// <summary>地形衝突判定ボックスを取得します。</summary>
    public CollisionBox CollisionBox => _entity.Has<CollisionBox>() ? _entity.Get<CollisionBox>() : default;

    /// <summary>被弾判定ボックスを取得します。</summary>
    private Hurtbox Hurtbox { get; }

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

    /// <summary>対応する Flecs エンティティ ID を取得します。</summary>
    public ulong EntityId => _entity.Id;

    /// <summary>プレイヤーの所属陣営（常に味方）を取得します。</summary>
    public Faction Faction => Faction.Friendly;

    /// <summary>プレイヤーが無敵状態であるかを取得します。</summary>
    public bool IsInvincible => InvincibleTimer > 0.0f;

    /// <summary>プレイヤーのワールド X 座標を取得または設定します。</summary>
    public float X {
        get => _entity.Has<Transform>() ? _entity.Get<Transform>().X : 0.0f;
        set
        {
            if (_entity.Has<Transform>()) _entity.GetMut<Transform>().X = value;
        }
    }

    /// <summary>プレイヤーのワールド Y 座標を取得または設定します。</summary>
    public float Y {
        get => _entity.Has<Transform>() ? _entity.Get<Transform>().Y : 0.0f;
        set
        {
            if (_entity.Has<Transform>()) _entity.GetMut<Transform>().Y = value;
        }
    }

    /// <summary>プレイヤーの X 軸移動速度を取得または設定します。</summary>
    public float Vx {
        get => _entity.Has<Velocity>() ? _entity.Get<Velocity>().Vx : 0.0f;
        set
        {
            if (_entity.Has<Velocity>()) _entity.GetMut<Velocity>().Vx = value;
        }
    }

    /// <summary>プレイヤーの Y 軸移動速度を取得または設定します。</summary>
    public float Vy {
        get => _entity.Has<Velocity>() ? _entity.Get<Velocity>().Vy : 0.0f;
        set
        {
            if (_entity.Has<Velocity>()) _entity.GetMut<Velocity>().Vy = value;
        }
    }

    /// <summary>現在の HP を取得または設定します。</summary>
    public float CurrentHp {
        get => _entity.Has<Health>() ? _entity.Get<Health>().Current : 0.0f;
        set
        {
            if (_entity.Has<Health>())
            {
                ref var health = ref _entity.GetMut<Health>();
                health.Current = Math.Clamp(value, 0.0f, health.Max);
            }
        }
    }

    /// <summary>最大 HP を取得します。</summary>
    public float MaxHp => _entity.Has<Health>() ? _entity.Get<Health>().Max : 0.0f;

    /// <summary>プレイヤーが生存中であるかを取得します。</summary>
    public bool IsAlive => CurrentHp > 0.0f;

    /// <summary>
    ///     プレイヤーにダメージを与えます。
    /// </summary>
    /// <param name="damage">受けるダメージ量。</param>
    /// <param name="ignoreInvincible">無敵時間を無視するかどうか。</param>
    public void TakeDamage(float damage, bool ignoreInvincible = false)
    {
        if (damage <= 0.0f || !IsAlive) return;

        if (!ignoreInvincible && IsInvincible) return;

        CurrentHp -= damage;
    }

    /// <summary>
    ///     フレームごとの入力、物理移動、跳躍、射撃、およびアニメーション更新を実行します。
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
            ref var health = ref _entity.GetMut<Health>();
            if (health.InvincibleTimer > 0.0f) health.InvincibleTimer = Math.Max(0.0f, health.InvincibleTimer - dt);
        }

        if (_shootCooldown > 0.0f) _shootCooldown = Math.Max(0.0f, _shootCooldown - dt);

        if (!IsAlive)
        {
            if (_entity.Has<Velocity>())
            {
                ref var v = ref _entity.GetMut<Velocity>();
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
        ApplyPhysicsAndCollisions(platformQuery, pCfg, arenaCfg, dt, world);

        // 4. アニメーション状態更新
        UpdateAnimation(dt);

        // 5. 手銃射撃処理
        ProcessShootInput(world, pCfg);
    }

    /// <summary>
    ///     プレイヤーの状態を初期スポーン地点および初期パラメータにリセットします。
    /// </summary>
    /// <param name="pCfg">プレイヤー設定。</param>
    public void Reset(PlayerConfig pCfg)
    {
        ArgumentNullException.ThrowIfNull(pCfg);

        if (_entity.Has<Transform>())
        {
            ref var t = ref _entity.GetMut<Transform>();
            t.X = pCfg.SpawnX;
            t.Y = pCfg.SpawnY;
            t.ScaleX = pCfg.ScaleX;
            t.ScaleY = pCfg.ScaleY;
        }

        if (_entity.Has<Velocity>())
        {
            ref var v = ref _entity.GetMut<Velocity>();
            v.Vx = 0.0f;
            v.Vy = 0.0f;
        }

        if (_entity.Has<Health>())
        {
            ref var h = ref _entity.GetMut<Health>();
            h.Max = pCfg.MaxHp;
            h.Current = pCfg.MaxHp;
            h.InvincibleTimer = 0.0f;
        }

        if (_entity.Has<CollisionBox>())
        {
            ref var c = ref _entity.GetMut<CollisionBox>();
            c.IsGrounded = false;
            c.IsOnWall = false;
        }

        RemainingJumps = pCfg.MaxJumps;
        _maxJumps = pCfg.MaxJumps;
        _shootCooldown = 0.0f;

        if (_entity.Has<JumpCapability>())
        {
            _entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        }

        if (_entity.Has<CharacterMovementIntent>())
        {
            _entity.GetMut<CharacterMovementIntent>().Reset();
        }

        if (_entity.Has<CharacterWeaponIntent>())
        {
            _entity.GetMut<CharacterWeaponIntent>().Reset();
        }

        if (_entity.Has<Animation>())
        {
            ref var anim = ref _entity.GetMut<Animation>();
            anim.Play(ClipIdle);
        }

        if (_entity.Has<Sprite>())
        {
            ref var spr = ref _entity.GetMut<Sprite>();
            spr.FlipX = false;
            spr.IsVisible = true;
        }

        UpdateAnimation(0.0f);

        if (_entity.IsAlive() && _entity.Has<HitFlashEffect>()) _entity.Remove<HitFlashEffect>();
    }

    private void ProcessHorizontalInput(PlayerConfig pCfg)
    {
        var isLeft = InputService.IsHolding(InputAction.Left);
        var isRight = InputService.IsHolding(InputAction.Right);

        var moveX = 0.0f;
        if (isLeft) moveX = -1.0f;
        else if (isRight) moveX = 1.0f;

        if (_entity.Has<CharacterMovementIntent>())
        {
            ref var intent = ref _entity.GetMut<CharacterMovementIntent>();
            intent.MoveX = moveX;
        }

        ref var vel = ref _entity.GetMut<Velocity>();
        ref var spr = ref _entity.GetMut<Sprite>();

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
        var jumpJustPressed = InputService.IsPressed(InputAction.Jump);
        var jumpHolding = InputService.IsHolding(InputAction.Jump);

        if (_entity.Has<CharacterMovementIntent>())
        {
            ref var intent = ref _entity.GetMut<CharacterMovementIntent>();
            intent.JumpTriggered = jumpJustPressed;
            intent.JumpHolding = jumpHolding;
        }
    }

    private void ApplyPhysicsAndCollisions(
        Query<Transform, CollisionBox> platformQuery,
        PlayerConfig pCfg,
        ArenaConfig arenaCfg,
        float dt,
        World world)
    {
        KinematicMovementSystem.UpdateEntity(_entity, dt, platformQuery, arenaCfg, world);
    }

    private void UpdateAnimation(float dt)
    {
        if (!_entity.Has<Animation>()) return;

        ref var anim = ref _entity.GetMut<Animation>();
        ref readonly var vel = ref _entity.Get<Velocity>();
        ref readonly var col = ref _entity.Get<CollisionBox>();

        if (!col.IsGrounded)
        {
            anim.Play(vel.Vy < 0.0f ? ClipJump : ClipFall);
        }
        else if (Math.Abs(vel.Vx) > ConfigManager.Player.WalkAnimVelocityThreshold)
        {
            anim.Play(ClipRun);
        }
        else
        {
            anim.Play(ClipIdle);
        }

        if (!anim.IsPlaying) return;
        anim.Timer += dt;

        var charConfigRes = AssetManager.GetCharacterConfig(anim.CharacterType);
        if (!charConfigRes.IsError)
        {
            var charConfig = charConfigRes.Value;
            ref var spr = ref _entity.GetMut<Sprite>();
            var handleRes = AssetManager.GetCharacterSpriteHandle(anim.CharacterType, anim.CurrentClip);
            if (handleRes is { IsError: false, Value: >= 0 }) spr.GraphHandle = handleRes.Value;

            spr.SrcRect =
                AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out var isFinished);
            anim.IsFinished = isFinished;
        }
    }

    private void ProcessShootInput(World world, PlayerConfig pCfg)
    {
        var shootJustPressed = InputService.IsPressed(InputAction.Attack);
        if (!shootJustPressed || _shootCooldown > 0.0f) return;

        CalculateAimDirection(out var dirX, out var dirY);
        SpawnBullet(world, pCfg, dirX, dirY);
        _shootCooldown = pCfg.ShootCooldown;
    }

    private void CalculateAimDirection(out float dirX, out float dirY)
    {
        var isLeft = InputService.IsHolding(InputAction.Left);
        var isRight = InputService.IsHolding(InputAction.Right);
        var isUp = InputService.IsHolding(InputAction.Up);
        var isDown = InputService.IsHolding(InputAction.Down);

        var inputX = (isRight ? 1.0f : 0.0f) - (isLeft ? 1.0f : 0.0f);
        var inputY = (isDown ? 1.0f : 0.0f) - (isUp ? 1.0f : 0.0f);

        if (Math.Abs(inputX) > 0.001f || Math.Abs(inputY) > 0.001f)
        {
            var len = MathF.Sqrt(inputX * inputX + inputY * inputY);
            dirX = inputX / len;
            dirY = inputY / len;

            if (_entity.Has<Sprite>())
            {
                ref var spr = ref _entity.GetMut<Sprite>();
                if (inputX < 0.0f)
                    spr.FlipX = true;
                else if (inputX > 0.0f) spr.FlipX = false;
            }
        }
        else
        {
            ref readonly var spr = ref _entity.Get<Sprite>();
            dirX = spr.FlipX ? -1.0f : 1.0f;
            dirY = 0.0f;
        }
    }

    private void SpawnBullet(World world, PlayerConfig pCfg, float dirX, float dirY)
    {
        ref readonly var trans = ref _entity.Get<Transform>();

        var spawnX = trans.X + dirX * pCfg.BulletSpawnOffset;
        var spawnY = trans.Y + pCfg.BulletSpawnOffsetY + dirY * pCfg.BulletSpawnOffset;
        var vx = dirX * pCfg.BulletSpeed;
        var vy = dirY * pCfg.BulletSpeed;

        // 事前確保されたエンティティプールから通常弾を貸し出し
        CombatEntityPoolHelper.RentPlayerBullet(world)
           .Set(new Transform(spawnX, spawnY))
           .Set(new Velocity(vx, vy))
           .Set(new CollisionBox(pCfg.BulletWidth, pCfg.BulletHeight))
           .Set(new Hitbox(pCfg.GetBulletHitboxShape(), pCfg.BulletDamage))
           .Add<PlayerBulletTag>();
    }
}

/// <summary>
///     プレイヤー自機の高レベル動作状態を表す状態マシン列挙型。
/// </summary>
public enum PlayerState {
    /// <summary>
    ///     死亡状態（HPは0以下）
    /// </summary>
    Dead,

    /// <summary>被弾硬直・無敵点滅状態。</summary>
    Hurt,

    /// <summary>接地状態（立ち・歩行・射撃など）。</summary>
    Grounded,

    /// <summary>空中状態（上昇・落下など）。</summary>
    Airborne
}