using AnimalBattle.Abilities;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     戦闘空間内に存在する全操作対象エンティティ（自機 Pawn および首領 Agent）の共通基底クラス。
///     唯一の真実の源である Flecs エンティティ句柄を保持し、
///     空間座標、線速度、生命値、衝突判定、スプライトおよびアニメーションの共通操作を一元的に提供します。
/// </summary>
public abstract class Pawn
{
    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    public Entity Entity { get; }

    /// <summary>
    ///     対象の Flecs エンティティを指定して <see cref="Pawn" /> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    protected Pawn(Entity entity)
    {
        Entity = entity;
    }

    /// <summary>エンティティ ID を取得します。</summary>
    public ulong EntityId => Entity.Id;

    #region Transform & Velocity

    /// <summary>ワールド X 座標を取得または設定します。</summary>
    public float X
    {
        get => Entity.Get<Transform>().X;
        set => Entity.GetMut<Transform>().X = value;
    }

    /// <summary>ワールド Y 座標を取得または設定します。</summary>
    public float Y
    {
        get => Entity.Get<Transform>().Y;
        set => Entity.GetMut<Transform>().Y = value;
    }

    /// <summary>X 軸移動速度を取得または設定します。</summary>
    public float Vx
    {
        get => Entity.Get<Velocity>().Vx;
        set => Entity.GetMut<Velocity>().Vx = value;
    }

    /// <summary>Y 軸移動速度を取得または設定します。</summary>
    public float Vy
    {
        get => Entity.Get<Velocity>().Vy;
        set => Entity.GetMut<Velocity>().Vy = value;
    }

    /// <summary>描画スケール X を取得または設定します。</summary>
    public float ScaleX
    {
        get => Entity.Get<Transform>().ScaleX;
        set => Entity.GetMut<Transform>().ScaleX = value;
    }

    /// <summary>描画スケール Y を取得または設定します。</summary>
    public float ScaleY
    {
        get => Entity.Get<Transform>().ScaleY;
        set => Entity.GetMut<Transform>().ScaleY = value;
    }

    /// <summary>ワールド座標を一括設定します。</summary>
    public void SetPosition(float x, float y)
    {
        ref var t = ref Entity.GetMut<Transform>();
        t.X = x;
        t.Y = y;
    }

    /// <summary>移動速度を一括設定します。</summary>
    protected void SetVelocity(float vx, float vy)
    {
        ref var v = ref Entity.GetMut<Velocity>();
        v.Vx = vx;
        v.Vy = vy;
    }

    /// <summary>空間変換パラメータ（座標・スケール）を一括設定します。</summary>
    protected void SetTransform(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f)
    {
        Entity.Set(new Transform(x, y, scaleX, scaleY));
    }

    #endregion

    #region Health & Combat

    /// <summary>ヘルス情報の参照を取得します。</summary>
    public ref readonly Health Health => ref Entity.Get<Health>();

    /// <summary>現在の HP を取得します。</summary>
    public float CurrentHp => Entity.Get<Health>().Current;

    /// <summary>最大 HP を取得します。</summary>
    public float MaxHp => Entity.Get<Health>().Max;

    /// <summary>アビリティシステムコンポーネントを取得します。</summary>
    public virtual AbilitySystemComponent AbilitySystem => Entity.Get<AbilitySystemComponent>();

    /// <summary>生存中であるかを取得します。</summary>
    public bool IsAlive => Entity.Get<Health>().Current > 0.0f;

    /// <summary>無敵保護または不可命中状態であるかを取得します。</summary>
    public bool IsInvincible => AbilitySystem.HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable);

    /// <summary>所属陣営を取得します。</summary>
    public Faction Faction => Entity.Get<Hurtbox>().Faction;

    /// <summary>受撃判定ボックスを取得します。</summary>
    public Hurtbox Hurtbox => Entity.Get<Hurtbox>();

    /// <summary>ヘルスを全回復・再初期化します。</summary>
    protected void ResetHealth(float maxHp)
    {
        Entity.Set(new Health(maxHp));
    }

    #endregion

    #region Collision & Physics

    /// <summary>地形衝突判定ボックスを取得します。</summary>
    public CollisionBox CollisionBox => Entity.Get<CollisionBox>();

    /// <summary>接地状態であるかを取得または設定します。</summary>
    public virtual bool IsGrounded
    {
        get => Entity.Get<CollisionBox>().IsGrounded;
        set => Entity.GetMut<CollisionBox>().IsGrounded = value;
    }

    #endregion

    #region Visual & Animation

    /// <summary>スプライト表示情報を取得します。</summary>
    public Sprite Sprite => Entity.Get<Sprite>();

    /// <summary>スプライトの水平反転フラグを取得または設定します。</summary>
    public bool FlipX
    {
        get => Entity.Get<Sprite>().FlipX;
        set => Entity.GetMut<Sprite>().FlipX = value;
    }

    /// <summary>スプライトの表示・非表示フラグを取得または設定します。</summary>
    public bool IsVisible
    {
        get => Entity.Get<Sprite>().IsVisible;
        set => Entity.GetMut<Sprite>().IsVisible = value;
    }

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    public bool IsFlashing => Entity.IsAlive() && Entity.Has<HitFlashEffect>();

    /// <summary>現在再生中のアニメーションクリップ名を取得します。</summary>
    public string CurrentAnimation => Entity.Has<Animation>() ? Entity.Get<Animation>().CurrentClip.ToString() : string.Empty;

    /// <summary>指定されたアニメーションクリップを再生します。</summary>
    protected void PlayAnimation(string clipName)
    {
        if (Entity.Has<Animation>())
            Entity.GetMut<Animation>().Play(clipName);
    }

    /// <summary>白閃や死亡タグなどの瞬態エフェクト・状態フラグをクリアします。</summary>
    protected void ClearTransientEffects()
    {
        if (Entity.IsAlive())
        {
            if (Entity.Has<HitFlashEffect>()) Entity.Remove<HitFlashEffect>();
            if (Entity.Has<DeadTag>()) Entity.Remove<DeadTag>();
        }
    }

    #endregion
}
