using System;
using System.Runtime.InteropServices;

namespace AnimalBattle.Core;
/// <summary>
/// Hitbox / Hurtbox の衝突対象フィルタリング条件を定義する構造体。
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CollisionFilter
{
    /// <summary>
    /// 自オブジェクトの衝突カテゴリ。
    /// </summary>
    public CollisionCategory Category;

    /// <summary>
    /// 衝突を判定する対象カテゴリのマスク。
    /// </summary>
    public CollisionCategory Mask;

    /// <summary>
    /// オブジェクトの所有者 ID (プレイヤーID または キャラクターID)。
    /// </summary>
    public int OwnerId;

    /// <summary>
    /// チーム ID (同チームへのフレンドリーファイアを防止するために使用)。
    /// </summary>
    public int TeamId;

    /// <summary>
    /// カテゴリ、マスク、所有者ID、およびチームIDを指定して <see cref="CollisionFilter"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="category">自オブジェクトのカテゴリ</param>
    /// <param name="mask">衝突判定対象マスク</param>
    /// <param name="ownerId">所有者 ID (任意)</param>
    /// <param name="teamId">チーム ID (任意)</param>
    public CollisionFilter(CollisionCategory category, CollisionCategory mask, int ownerId = -1, int teamId = -1)
    {
        Category = category;
        Mask = mask;
        OwnerId = ownerId;
        TeamId = teamId;
    }

    /// <summary>
    /// 相手の <see cref="CollisionFilter"/> とヒット判定を行うべきか検証します。
    /// </summary>
    /// <param name="other">判定対象の相手フィルタ</param>
    /// <returns>ヒット可能であれば true</returns>
    public readonly bool CanHit(in CollisionFilter other) => ShouldCollide(in other);

    /// <summary>
    /// 相手の <see cref="CollisionFilter"/> と衝突判定を行うべきか検証します。
    /// </summary>
    /// <param name="other">判定対象の相手フィルタ</param>
    /// <returns>衝突対象として有効な場合は true</returns>
    public readonly bool ShouldCollide(in CollisionFilter other)
    {
        if (OwnerId >= 0 && OwnerId == other.OwnerId)
        {
            return false;
        }

        if (TeamId >= 0 && TeamId == other.TeamId)
        {
            return false;
        }

        bool selfMatchesOtherMask = (Category & other.Mask) != CollisionCategory.None;
        bool otherMatchesSelfMask = (other.Category & Mask) != CollisionCategory.None;

        return selfMatchesOtherMask && otherMatchesSelfMask;
    }
}
