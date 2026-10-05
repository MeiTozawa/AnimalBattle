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
        if (h.Current <= 0.0f)
        {
            ref var v = ref _entity.GetMut<Velocity>();
            v.Vx = 0.0f;
            v.Vy = 0.0f;
            return;
        }
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
        anim.Play("Idle");

        ref var spr = ref _entity.GetMut<Sprite>();
        spr.FlipX = false;
        spr.IsVisible = true;

        if (_entity.IsAlive()) _entity.Remove<HitFlashEffect>();
    }
}