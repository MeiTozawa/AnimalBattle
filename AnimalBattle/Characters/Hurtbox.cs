using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Characters;

/// <summary>
/// キャラクターの被弾・喰らい判定領域（Hurtbox）を表すクラス。
/// </summary>
/// <remarks>
/// 指定されたパラメータで新しい <see cref="Hurtbox"/> インスタンスを生成します。
/// </remarks>
/// <param name="ownerIndex">所有者のプレイヤーインデックス</param>
/// <param name="offsetX">相対 X オフセット</param>
/// <param name="offsetY">相対 Y オフセット</param>
/// <param name="width">幅</param>
/// <param name="height">高さ</param>
/// <param name="teamId">チーム ID（省略時は ownerIndex）</param>
public class Hurtbox(int ownerIndex, float offsetX, float offsetY, float width, float height, int teamId = -1)
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
    /// 被弾判定領域の衝突フィルタ情報を取得または設定します。
    /// </summary>
    public CollisionFilter Filter { get; set; } = new(CollisionCategory.CharacterBody, CollisionCategory.AttackHitbox | CollisionCategory.Projectile, ownerIndex, teamId >= 0 ? teamId : ownerIndex);

    /// <summary>
    /// 所有者の現在位置および向きに基づくワールド座標空間での判定矩形を取得します。
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
}
