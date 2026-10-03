using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Characters;

/// <summary>
///     戦闘空間におけるすべての受控エンティティ統一の【純粋な読み取り専用】領域観察契約。
///     高頻度呼び出しパスでのオーバーヘッドを排除するため、
///     Entity.Has&lt;T&gt; 検査を行わず、ComponentRequirementsModule で保証されたコンポーネントへ直接アクセスします。
/// </summary>
public interface ICharacter
{
    /// <summary>対応する Flecs エンティティ句柄を取得します。</summary>
    Entity Entity { get; }

    /// <summary>エンティティ ID を取得します。</summary>
    ulong EntityId => Entity.Id;

    /// <summary>ワールド X 座標を取得します。</summary>
    float X => Entity.Get<Transform>().X;

    /// <summary>ワールド Y 座標を取得します。</summary>
    float Y => Entity.Get<Transform>().Y;

    /// <summary>X 軸移動速度を取得します。</summary>
    float Vx => Entity.Get<Velocity>().Vx;

    /// <summary>Y 軸移動速度を取得します。</summary>
    float Vy => Entity.Get<Velocity>().Vy;

    /// <summary>現在の HP を取得します。</summary>
    float CurrentHp => Entity.Get<Health>().Current;

    /// <summary>最大 HP を取得します。</summary>
    float MaxHp => Entity.Get<Health>().Max;

    /// <summary>生存中であるかを取得します。</summary>
    bool IsAlive => Entity.Get<Health>().Current > 0.0f;

    /// <summary>所属陣営を取得します。</summary>
    Faction Faction => Entity.Get<Hurtbox>().Faction;

    /// <summary>無敵保護状態であるかを取得します。</summary>
    bool IsInvincible => Entity.Get<Health>().InvincibleTimer > 0.0f;

    /// <summary>スプライト表示情報を取得します。</summary>
    Sprite Sprite => Entity.Get<Sprite>();

    /// <summary>描画スケール X を取得します。</summary>
    float ScaleX => Entity.Get<Transform>().ScaleX;

    /// <summary>描画スケール Y を取得します。</summary>
    float ScaleY => Entity.Get<Transform>().ScaleY;

    /// <summary>被弾時白閃演出中であるかを取得します。</summary>
    bool IsFlashing => Entity.IsAlive() && Entity.Has<HitFlashEffect>();
}
