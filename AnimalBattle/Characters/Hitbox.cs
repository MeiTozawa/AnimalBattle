using System;
using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Characters;

/// <summary>
/// キャラクターの攻撃判定領域（Hitbox）を表すクラス。
/// 被弾判定領域（Hurtbox）との AABB 交差判定機能を提供します。
/// </summary>
/// <remarks>
/// 指定されたパラメータで新しい <see cref="Hitbox"/> インスタンスを生成します。
/// </remarks>
/// <param name="ownerIndex">所有者のプレイヤーインデックス</param>
/// <param name="offsetX">相対 X オフセット</param>
/// <param name="offsetY">相対 Y オフセット</param>
/// <param name="width">幅</param>
/// <param name="height">高さ</param>
/// <param name="damage">ダメージ数</param>
/// <param name="knockbackForce">吹っ飛び力</param>
/// <param name="knockbackAngle">吹っ飛び角度（省略時は 45度）</param>
/// <param name="teamId">チーム ID（省略時は ownerIndex）</param>
public class Hitbox(int ownerIndex, float offsetX, float offsetY, float width, float height, float damage = 10.0f, float knockbackForce = 400.0f, float knockbackAngle = (float)(Math.PI / 4.0), int teamId = -1)
{
    /// <summary>
    /// 所有者プレイヤーのインデックスを取得または設定します。
    /// </summary>
    public int OwnerIndex
    {
        get => Filter.OwnerId;
        set => Filter = new CollisionFilter(Filter.Category, Filter.Mask, value, Filter.TeamId);
    }

    /// <summary>
    /// 所有者の位置に対する相対 X オフセットを取得または設定します。
    /// </summary>
    public float OffsetX { get; set; } = offsetX;

    /// <summary>
    /// 所有者の位置に対する相対 Y オフセットを取得または設定します。
    /// </summary>
    public float OffsetY { get; set; } = offsetY;

    /// <summary>
    /// 判定矩形の幅を取得または設定します。
    /// </summary>
    public float Width { get; set; } = width;

    /// <summary>
    /// 判定矩形の高さを取得または設定します。
    /// </summary>
    public float Height { get; set; } = height;

    /// <summary>
    /// 攻撃が与えるダメージ数を取得または設定します。
    /// </summary>
    public float Damage { get; set; } = damage;

    /// <summary>
    /// 吹っ飛び力の大きさを取得または設定します。
    /// </summary>
    public float KnockbackForce { get; set; } = knockbackForce;

    /// <summary>
    /// 吹っ飛び方向の角度（ラジアン）を取得または設定します。
    /// </summary>
    public float KnockbackAngle { get; set; } = knockbackAngle;

    /// <summary>
    /// 現在攻撃判定がアクティブ有効であるかを取得または設定します。
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// 攻撃判定領域の衝突フィルタ情報を取得または設定します。
    /// </summary>
    public CollisionFilter Filter { get; set; } = new(CollisionCategory.AttackHitbox, CollisionCategory.CharacterBody | CollisionCategory.Projectile, ownerIndex, teamId >= 0 ? teamId : ownerIndex);

    /// <summary>
    /// この Hitbox が現在の攻撃判定で既に命中させたターゲットの OwnerId 集合を取得します。
    /// </summary>
    public HashSet<int> HitTargetOwnerIds { get; } = [];

    /// <summary>
    /// 現在の攻撃によるヒット履歴（命中させたターゲット一覧）をリセットします。
    /// </summary>
    public void ResetHitTargets()
    {
        HitTargetOwnerIds.Clear();
    }

    /// <summary>
    /// 指定されたターゲットへのヒットを記録します。
    /// </summary>
    /// <param name="targetOwnerId">命中したターゲットの所有者 ID</param>
    public void RegisterHit(int targetOwnerId)
    {
        HitTargetOwnerIds.Add(targetOwnerId);
    }

    /// <summary>
    /// 所有者の現在位置および向きに基づくワールド座標空間での攻撃判定矩形を取得します。
    /// </summary>
    /// <param name="ownerX">所有者のワールド X 座標</param>
    /// <param name="ownerY">所有者のワールド Y 座標（足元）</param>
    /// <param name="facingLeft">所有者が左を向いているか</param>
    /// <returns>ワールド座標系における判定矩形</returns>
    public Rect<float> GetWorldBounds(float ownerX, float ownerY, bool facingLeft)
    {
        float actualOffsetX = facingLeft ? -OffsetX - Width : OffsetX;
        float x1 = ownerX + actualOffsetX;
        float y1 = ownerY + OffsetY - Height;
        float x2 = x1 + Width;
        float y2 = y1 + Height;

        return new Rect<float>(x1, y1, x2, y2);
    }

    /// <summary>
    /// ターゲットの Hurtbox との AABB 交差判定を行います。
    /// </summary>
    /// <param name="targetHurtbox">対象の Hurtbox インスタンス</param>
    /// <param name="ownerX">所有者のワールド X 座標</param>
    /// <param name="ownerY">所有者のワールド Y 座標</param>
    /// <param name="ownerFacingLeft">所有者の向き</param>
    /// <param name="targetX">対象のワールド X 座標</param>
    /// <param name="targetY">対象のワールド Y 座標</param>
    /// <param name="targetFacingLeft">対象の向き</param>
    /// <returns>衝突（交差）している場合は true</returns>
    public bool Intersects(Hurtbox targetHurtbox, float ownerX, float ownerY, bool ownerFacingLeft, float targetX, float targetY, bool targetFacingLeft)
    {
        if (!IsActive || targetHurtbox == null || !Filter.CanHit(targetHurtbox.Filter))
        {
            return false;
        }

        if (HitTargetOwnerIds.Contains(targetHurtbox.OwnerIndex))
        {
            return false;
        }

        Rect<float> hitRect = GetWorldBounds(ownerX, ownerY, ownerFacingLeft);
        Rect<float> hurtRect = targetHurtbox.GetWorldBounds(targetX, targetY, targetFacingLeft);

        return hitRect.Intersects(hurtRect);
    }
}
