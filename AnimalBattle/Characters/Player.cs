using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;

using System.Drawing;

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

    private readonly Entity _entity;

    /// <summary>プレイヤーのヘルス情報を取得します。</summary>
    public ref readonly Health Health => ref _entity.Get<Health>();

    /// <summary>
    ///     設定情報と Flecs エンティティ句柄を指定して、プレイヤー自機を初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="pCfg">プレイヤー設定。</param>
    /// <param name="spriteHandle">プレイヤースプライトの画像ハンドル。</param>
    public Player(Entity entity, PlayerConfig pCfg, int spriteHandle)
    {
        System.Diagnostics.Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");
        _entity = entity;

        _entity.Set(new Health(pCfg.MaxHp))
           .Set(new InvincibleOnHit(pCfg.InvincibleDuration))
           .Set(new HitFlashConfig(pCfg.HitFlashDuration, Color.White))
           .Set(new Transform(pCfg.SpawnX, pCfg.SpawnY, pCfg.ScaleX, pCfg.ScaleY))
           .Set(new Velocity(0.0f))
           .Set(new CollisionBox(pCfg.HitboxWidth, pCfg.HitboxHeight))
           .Set(new Hurtbox(pCfg.GetHurtboxShape(), Faction.Friendly))
           .Set(new Animation("Idle"))
           .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 64, 64)))
           .Set(new CharacterMovementIntent())
           .Set(new CharacterWeaponIntent())
           .Set(JumpCapability.FromPlayerConfig(pCfg))
           .Set(WeaponState.FromPlayerConfig(pCfg))
           .Add<PlayerTag>();

        UpdateAnimation(0.0f);
    }

    /// <summary>プレイヤーの現在の高レベル動作状態を取得します。</summary>
    public PlayerState CurrentState {
        get
        {
            ref readonly var hp = ref _entity.Get<Health>();
            if (hp.Current <= 0.0f) return PlayerState.Dead;

            if (hp.InvincibleTimer > 0.0f) return PlayerState.Hurt;

            if (IsGrounded) return PlayerState.Grounded;

            return PlayerState.Airborne;
        }
    }

    /// <summary>接地状態であるかを取得または設定します。</summary>
    public bool IsGrounded {
        get => _entity.Get<CollisionBox>().IsGrounded;
        set => _entity.GetMut<CollisionBox>().IsGrounded = value;
    }

    /// <summary>残り跳躍可能回数を取得または設定します。</summary>
    public int RemainingJumps {
        get => _entity.Get<JumpCapability>().RemainingJumps;
        set => _entity.GetMut<JumpCapability>().RemainingJumps = value;
    }

    /// <summary>射撃クールダウンの残り秒数を取得または設定します。</summary>
    public float ShootCooldown {
        get => _entity.Get<WeaponState>().CooldownRemaining;
        set => _entity.GetMut<WeaponState>().CooldownRemaining = Math.Max(0.0f, value);
    }

    /// <summary>地形衝突判定ボックスを取得します。</summary>
    public CollisionBox CollisionBox => _entity.Get<CollisionBox>();

    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity => _entity;

    /// <summary>プレイヤーのワールド X 座標を取得または設定します。</summary>
    public float X {
        get => _entity.Get<Transform>().X;
        set => _entity.GetMut<Transform>().X = value;
    }

    /// <summary>プレイヤーのワールド Y 座標を取得または設定します。</summary>
    public float Y {
        get => _entity.Get<Transform>().Y;
        set => _entity.GetMut<Transform>().Y = value;
    }

    /// <summary>プレイヤーの X 軸移動速度を取得または設定します。</summary>
    public float Vx {
        get => _entity.Get<Velocity>().Vx;
        set => _entity.GetMut<Velocity>().Vx = value;
    }

    /// <summary>プレイヤーの Y 軸移動速度を取得または設定します。</summary>
    public float Vy {
        get => _entity.Get<Velocity>().Vy;
        set => _entity.GetMut<Velocity>().Vy = value;
    }

    /// <summary>
    ///     被弾成功時フック。設定された無敵時間をプレイヤーの体力状態に付与します。
    /// </summary>
    /// <param name="actualDamage">実際に適用されたダメージ量。</param>
    public void OnDamaged(float actualDamage)
    {
        _entity.GetMut<Health>().InvincibleTimer = ConfigManager.Player.InvincibleDuration;
    }

    /// <summary>
    ///     フレームごとの物理移動、跳躍、射撃、およびアニメーション更新を実行します。
    /// </summary>
    /// <param name="dt">デルタタイム（秒）。</param>
    /// <param name="arenaCfg">アリーナ設定。</param>
    /// <param name="platformQuery">プラットフォーム検索クエリ。</param>
    /// <param name="world">エフェクト・弾丸生成用の Flecs ワールド。</param>
    public void Update(
        float dt,
        ArenaConfig arenaCfg,
        Query<Transform, CollisionBox> platformQuery,
        World world)
    {
        System.Diagnostics.Debug.Assert(arenaCfg != null, "ArenaConfig は null であってはなりません。");

        ref var h = ref _entity.GetMut<Health>();
        if (h.InvincibleTimer > 0.0f)
        {
            h.InvincibleTimer = Math.Max(0.0f, h.InvincibleTimer - dt);
        }

        if (h.Current <= 0.0f)
        {
            ref var v = ref _entity.GetMut<Velocity>();
            v.Vx = 0.0f;
            v.Vy = 0.0f;
            return;
        }

        // 1. 物理・地形衝突処理（意図コンポーネントを消費）
        KinematicMovementSystem.UpdateEntity(_entity, dt, platformQuery, arenaCfg, world);

        // 2. アニメーション状態更新
        UpdateAnimation(dt);

        // 3. 武器射撃処理（意図コンポーネントを消費）
        CharacterWeaponSystem.UpdateEntity(_entity, dt, world);
    }

    /// <summary>
    ///     プレイヤーの状態を初期スポーン地点および初期パラメータにリセットします。
    /// </summary>
    /// <param name="pCfg">プレイヤー設定。</param>
    public void Reset(PlayerConfig pCfg)
    {
        System.Diagnostics.Debug.Assert(pCfg != null, "PlayerConfig は null であってはなりません。");

        ref var t = ref _entity.GetMut<Transform>();
        t.X = pCfg.SpawnX;
        t.Y = pCfg.SpawnY;
        t.ScaleX = pCfg.ScaleX;
        t.ScaleY = pCfg.ScaleY;

        ref var v = ref _entity.GetMut<Velocity>();
        v.Vx = 0.0f;
        v.Vy = 0.0f;

        ref var h = ref _entity.GetMut<Health>();
        h.Max = pCfg.MaxHp;
        h.Current = pCfg.MaxHp;
        h.InvincibleTimer = 0.0f;

        ref var c = ref _entity.GetMut<CollisionBox>();
        c.IsGrounded = false;
        c.IsOnWall = false;

        _entity.Set(JumpCapability.FromPlayerConfig(pCfg));
        _entity.Set(WeaponState.FromPlayerConfig(pCfg));
        _entity.GetMut<CharacterMovementIntent>().Reset();
        _entity.GetMut<CharacterWeaponIntent>().Reset();

        ref var anim = ref _entity.GetMut<Animation>();
        anim.Play(ClipIdle);

        ref var spr = ref _entity.GetMut<Sprite>();
        spr.FlipX = false;
        spr.IsVisible = true;

        UpdateAnimation(0.0f);

        if (_entity.IsAlive()) _entity.Remove<HitFlashEffect>();
    }

    private void UpdateAnimation(float dt)
    {
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
}