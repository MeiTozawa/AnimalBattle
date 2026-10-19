using AnimalBattle.Abilities;
using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     戦闘空間内に存在する全操作対象エンティティ（自機 Pawn および首領 Agent）の共通基底クラス。
///     唯一の真実の源である Flecs エンティティ句柄を保持し、
///     コンポーネントの読み取り専用参照（ref readonly）および意図的アトミック操作を提供します。
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

    /// <summary>空間変換情報の読み取り専用参照を取得します。</summary>
    public ref readonly Transform Transform => ref Entity.Get<Transform>();

    /// <summary>移動速度情報の読み取り専用参照を取得します。</summary>
    public ref readonly Velocity Velocity => ref Entity.Get<Velocity>();

    /// <summary>現在位置座標を取得します。</summary>
    public Vec2f Position => Entity.Get<Transform>().Position;

    /// <summary>ワールド位置ベクトルを一括設定します。</summary>
    public void SetPosition(Vec2f pos)
    {
        Entity.GetMut<Transform>().Position = pos;
    }

    /// <summary>移動速度ベクトルを一括設定します。</summary>
    public void SetVelocity(Vec2f vel)
    {
        Entity.Set(new Velocity(vel));
    }

    /// <summary>空間変換パラメータ（座標・スケール）を一括設定します。</summary>
    public void SetTransform(Vec2f pos, Vec2f scale)
    {
        Entity.Set(new Transform(pos, scale));
    }

    #endregion

    #region Health & Combat

    /// <summary>ヘルス情報の読み取り専用参照を取得します。</summary>
    public ref readonly Health Health => ref Entity.Get<Health>();

    /// <summary>生存中であるかを取得します。</summary>
    public bool IsAlive => Entity.Get<Health>().Current > 0.0f;

    /// <summary>無敵保護または不可命中状態であるかを取得します（ホットパスでの entity.Has なし）。</summary>
    public bool IsInvincible => Entity.Get<GameplayTagState>().HasAnyTag(GameplayTag.Invincible | GameplayTag.Untargetable);

    /// <summary>所属陣営を取得します。</summary>
    public Faction Faction => Entity.Get<Hurtbox>().Faction;

    /// <summary>受撃判定ボックスの読み取り専用参照を取得します。</summary>
    public ref readonly Hurtbox Hurtbox => ref Entity.Get<Hurtbox>();

    /// <summary>ヘルスを全回復・再初期化します。</summary>
    public void ResetHealth(float maxHp)
    {
        Entity.Set(new Health(maxHp));
    }

    #endregion

    #region Collision & Physics

    /// <summary>地形衝突判定ボックスの読み取り専用参照を取得します。</summary>
    public ref readonly CollisionBox CollisionBox => ref Entity.Get<CollisionBox>();

    #endregion

    #region Visual & Animation

    /// <summary>スプライト表示情報の読み取り専用参照を取得します。</summary>
    public ref readonly Sprite Sprite => ref Entity.Get<Sprite>();

    /// <summary>スプライトの左右反転フラグを設定します。</summary>
    public void SetFlipX(bool flipX)
    {
        Entity.GetMut<Sprite>().FlipX = flipX;
    }

    /// <summary>スプライトの表示・非表示フラグを設定します。</summary>
    public void SetVisible(bool isVisible)
    {
        Entity.GetMut<Sprite>().IsVisible = isVisible;
    }

    /// <summary>指定されたアニメーションクリップを再生します。</summary>
    public void PlayAnimation(string clipName)
    {
        if (Entity.Has<Animation>())
            Entity.GetMut<Animation>().Play(clipName);
    }

    /// <summary>白閃や死亡タグなどの瞬態エフェクト・状態フラグをクリアします。</summary>
    public void ClearTransientEffects()
    {
        if (Entity.IsAlive())
        {
            if (Entity.Has<HitFlashEffect>()) Entity.Remove<HitFlashEffect>();
            if (Entity.Has<DeadTag>()) Entity.Remove<DeadTag>();
        }
    }

    #endregion
}
