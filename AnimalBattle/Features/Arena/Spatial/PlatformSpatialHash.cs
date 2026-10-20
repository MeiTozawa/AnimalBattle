using AnimalBattle.Core;

namespace AnimalBattle.Maps.Spatial;

/// <summary>
///     プラットフォーム足場用の軽量 2D 空間ハッシュグリッド。
///     毎フレーム全プラットフォームを線形探索（O(N)）するオーバーヘッドを排除し、
///     局所グリッドセル内の候補プラットフォームのみを O(1) で高速抽出します。
/// </summary>
public sealed class PlatformSpatialHash(float cellSize = 256.0f)
{
    private readonly float _cellSize = cellSize > 0.0f ? cellSize : 256.0f;
    private readonly Dictionary<long, List<int>> _grid = new();
    private readonly Dictionary<int, Box2D> _boxes = new();
    private readonly Stack<List<int>> _listPool = new();
    private readonly HashSet<int> _visitedQueryIds = [];

    /// <summary>
    ///     登録済みプラットフォーム総数を取得します。
    /// </summary>
    public int Count => _boxes.Count;

    /// <summary>
    ///     空間グリッドの 1 セルあたりのサイズ（ピクセル）を取得します。
    /// </summary>
    public float CellSize => _cellSize;

    private static long GetKey(int cx, int cy) => ((long)cx << 32) | (uint)cy;

    /// <summary>
    ///     空間ハッシュ内の全登録データを消去します。
    ///     セル内の List は破弃せずプールへ回収して再利用し、GC プレッシャーを排除します。
    /// </summary>
    public void Clear()
    {
        foreach (var list in _grid.Values)
        {
            list.Clear();
            _listPool.Push(list);
        }
        _grid.Clear();
        _boxes.Clear();
    }

    /// <summary>
    ///     指定されたプラットフォーム ID とその境界矩形を空間ハッシュへ登録します。
    /// </summary>
    /// <param name="id">プラットフォーム識別 ID</param>
    /// <param name="box">足場境界矩形</param>
    public void Insert(int id, Box2D box)
    {
        _boxes[id] = box;
        var minX = (int)MathF.Floor(box.Left / _cellSize);
        var maxX = (int)MathF.Floor(box.Right / _cellSize);
        var minY = (int)MathF.Floor(box.Top / _cellSize);
        var maxY = (int)MathF.Floor(box.Bottom / _cellSize);

        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        {
            var key = GetKey(x, y);
            if (!_grid.TryGetValue(key, out var list))
            {
                list = _listPool.Count > 0 ? _listPool.Pop() : [];
                _grid[key] = list;
            }
            list.Add(id);
        }
    }

    /// <summary>
    ///     指定矩形領域と交差する可能性のあるプラットフォーム ID 群を抽出します。
    ///     重複除外は HashSet による O(1) 判定を行い、List.Contains による O(N) 線形探索退化と GC アロケーションを防ぎます。
    /// </summary>
    /// <param name="queryBox">判定対象矩形</param>
    /// <param name="outCandidates">結果格納先リスト（呼び出し側で事前確保・再利用推奨）</param>
    public void Query(Box2D queryBox, List<int> outCandidates)
    {
        outCandidates.Clear();
        _visitedQueryIds.Clear();

        var minX = (int)MathF.Floor(queryBox.Left / _cellSize);
        var maxX = (int)MathF.Floor(queryBox.Right / _cellSize);
        var minY = (int)MathF.Floor(queryBox.Top / _cellSize);
        var maxY = (int)MathF.Floor(queryBox.Bottom / _cellSize);

        for (var x = minX; x <= maxX; x++)
        for (var y = minY; y <= maxY; y++)
        {
            var key = GetKey(x, y);
            if (_grid.TryGetValue(key, out var list))
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var id = list[i];
                    if (_visitedQueryIds.Add(id))
                    {
                        outCandidates.Add(id);
                    }
                }
            }
        }
    }

    /// <summary>
    ///     登録済みプラットフォームの境界矩形を取得します。
    /// </summary>
    /// <param name="id">プラットフォーム ID</param>
    /// <param name="box">取得された矩形</param>
    /// <returns>存在する場合は true</returns>
    public bool TryGetBox(int id, out Box2D box) => _boxes.TryGetValue(id, out box);
}
