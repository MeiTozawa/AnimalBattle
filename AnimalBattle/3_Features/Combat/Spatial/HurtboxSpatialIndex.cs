using System;
using System.Runtime.CompilerServices;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Spatial;

/// <summary>
///     受撃ボックス（Hurtbox）の空間キャッシュアイテム。
///     空間ハッシュセル内に登録され、粗判定および詳細幾何相交判定に供されます。
/// </summary>
public struct HurtboxItem
{
    public Entity Entity;
    public float CenterX;
    public float CenterY;
    public float HalfW;
    public float HalfH;
    public Faction Faction;
    public CollisionShape Shape;
    internal uint QueryId;
}

/// <summary>
///     受撃ボックス専用の高密度・零 GC 2D 空間ハッシュグリッド（Spatial Hash Grid）。
///     従来の全エンティティ線形二重ループ（O(N*M)）を空間近傍インデックス化により O(N) へ削減します。
/// </summary>
public sealed class HurtboxSpatialIndex
{
    private readonly float _cellSize;
    private readonly float _invCellSize;
    private readonly int[] _cellHeads;
    private readonly int _cellMask;

    private struct CellLink
    {
        public int ItemIndex;
        public int NextLink;
    }

    private HurtboxItem[] _items;
    private CellLink[] _links;
    private int _itemCount;
    private int _linkCount;
    private uint _currentQueryId;

    /// <summary>
    ///     現在登録されている有効受撃ボックスの総数。
    /// </summary>
    public int ItemCount => _itemCount;

    /// <summary>
    ///     空間ハッシュインデックスを初期化します。
    /// </summary>
    /// <param name="cellSize">正方形セルの幅・高さ（ピクセル）</param>
    /// <param name="tableSize">セルハッシュバケット数（2 の冪乗）</param>
    /// <param name="initialCapacity">初期許容 Hurtbox エンティティ数</param>
    public HurtboxSpatialIndex(float cellSize, int tableSize, int initialCapacity)
    {
        _cellSize = cellSize;
        _invCellSize = 1.0f / cellSize;

        var powerOfTwo = 1;
        while (powerOfTwo < tableSize) powerOfTwo <<= 1;
        _cellMask = powerOfTwo - 1;
        _cellHeads = new int[powerOfTwo];
        Array.Fill(_cellHeads, -1);

        _items = new HurtboxItem[initialCapacity];
        _links = new CellLink[initialCapacity * 4];
        _currentQueryId = 1;
    }

    /// <summary>
    ///     フレーム開始時に空間ハッシュ内の全登録データを高速消去（ゼロアロケーション）します。
    /// </summary>
    public void Clear()
    {
        Array.Fill(_cellHeads, -1);
        _itemCount = 0;
        _linkCount = 0;
    }

    /// <summary>
    ///     有効な受撃ボックスを空間ハッシュへ挿入します。
    /// </summary>
    public void Insert(Entity entity, Faction faction, in Transform trans, in Hurtbox hurtbox)
    {
        var cX = trans.X + hurtbox.Shape.OffsetX;
        var cY = trans.Y + hurtbox.Shape.OffsetY;
        var halfW = hurtbox.Shape.Type == CollisionShapeType.Box ? hurtbox.Shape.Width * 0.5f : hurtbox.Shape.Radius;
        var halfH = hurtbox.Shape.Type == CollisionShapeType.Box ? hurtbox.Shape.Height * 0.5f : hurtbox.Shape.Radius;

        if (_itemCount >= _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }

        var itemIdx = _itemCount++;
        _items[itemIdx] = new HurtboxItem
        {
            Entity = entity,
            CenterX = cX,
            CenterY = cY,
            HalfW = halfW,
            HalfH = halfH,
            Faction = faction,
            Shape = hurtbox.Shape,
            QueryId = 0
        };

        var minCx = (int)MathF.Floor((cX - halfW) * _invCellSize);
        var maxCx = (int)MathF.Floor((cX + halfW) * _invCellSize);
        var minCy = (int)MathF.Floor((cY - halfH) * _invCellSize);
        var maxCy = (int)MathF.Floor((cY + halfH) * _invCellSize);

        for (var x = minCx; x <= maxCx; x++)
        for (var y = minCy; y <= maxCy; y++)
        {
            if (_linkCount >= _links.Length)
            {
                Array.Resize(ref _links, _links.Length * 2);
            }

            var hash = HashCell(x, y);
            var linkIdx = _linkCount++;
            _links[linkIdx] = new CellLink
            {
                ItemIndex = itemIdx,
                NextLink = _cellHeads[hash]
            };
            _cellHeads[hash] = linkIdx;
        }
    }

    /// <summary>
    ///     指定の攻撃 AABB 境界と交差する空間セルから、敵対陣営の候補受撃ボックスを重複排除して高速抽出します。
    /// </summary>
    /// <param name="aCenterX">攻撃側中心 X</param>
    /// <param name="aCenterY">攻撃側中心 Y</param>
    /// <param name="aHalfW">攻撃側半幅</param>
    /// <param name="aHalfH">攻撃側半高</param>
    /// <param name="attackerFaction">攻撃側の陣営</param>
    /// <param name="outItemIndices">候補インデックス格納先 Span</param>
    /// <returns>マッチした候補数</returns>
    public int QueryCandidates(
        float aCenterX,
        float aCenterY,
        float aHalfW,
        float aHalfH,
        Faction attackerFaction,
        Span<int> outItemIndices)
    {
        if (_itemCount == 0) return 0;

        _currentQueryId++;
        if (_currentQueryId == 0)
        {
            // クエリ ID のオーバーフロー回帰防護
            for (var i = 0; i < _itemCount; i++) _items[i].QueryId = 0;
            _currentQueryId = 1;
        }

        var minCx = (int)MathF.Floor((aCenterX - aHalfW) * _invCellSize);
        var maxCx = (int)MathF.Floor((aCenterX + aHalfW) * _invCellSize);
        var minCy = (int)MathF.Floor((aCenterY - aHalfH) * _invCellSize);
        var maxCy = (int)MathF.Floor((aCenterY + aHalfH) * _invCellSize);

        var matchCount = 0;

        for (var x = minCx; x <= maxCx; x++)
        for (var y = minCy; y <= maxCy; y++)
        {
            var hash = HashCell(x, y);
            var linkIdx = _cellHeads[hash];

            while (linkIdx >= 0)
            {
                ref readonly var link = ref _links[linkIdx];
                var itemIdx = link.ItemIndex;
                ref var item = ref _items[itemIdx];

                if (item.QueryId != _currentQueryId)
                {
                    item.QueryId = _currentQueryId;

                    // 1. 陣営フィルタリング（同陣営同士はスキップ）
                    // 2. AABB Broadphase 初次粗判定
                    if (item.Faction != attackerFaction &&
                        MathF.Abs(aCenterX - item.CenterX) <= aHalfW + item.HalfW &&
                        MathF.Abs(aCenterY - item.CenterY) <= aHalfH + item.HalfH)
                    {
                        if (matchCount < outItemIndices.Length)
                        {
                            outItemIndices[matchCount++] = itemIdx;
                        }
                    }
                }

                linkIdx = link.NextLink;
            }
        }

        return matchCount;
    }

    /// <summary>
    ///     インデックスに対応する受撃ボックスアイテムを参照します。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly HurtboxItem GetItem(int itemIndex) => ref _items[itemIndex];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int HashCell(int cx, int cy)
    {
        return unchecked((int)(((uint)cx * 73856093u) ^ ((uint)cy * 19349663u))) & _cellMask;
    }
}
