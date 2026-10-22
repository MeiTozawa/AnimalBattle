using System;
using AnimalBattle.Combat.Spatial;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     World 単位で独立して保持される受撃ボックス空間ハッシュリソース。
/// </summary>
public sealed class HurtboxSpatialResource
{
    /// <summary>空間ハッシュグリッドの実体</summary>
    public HurtboxSpatialIndex Index { get; set; }

    /// <summary>受撃判定エンティティ抽出クエリ</summary>
    public Query<Transform, Hurtbox, Health, GameplayTagState> Query { get; init; }

    /// <summary>キャッシュされたセルサイズ</summary>
    public float CachedCellSize { get; set; }

    /// <summary>キャッシュされたハッシュテーブルサイズ</summary>
    public int CachedTableSize { get; set; }

    public HurtboxSpatialResource(
        HurtboxSpatialIndex index,
        Query<Transform, Hurtbox, Health, GameplayTagState> query,
        float cellSize,
        int tableSize)
    {
        Index = index;
        Query = query;
        CachedCellSize = cellSize;
        CachedTableSize = tableSize;
    }
}

/// <summary>
///     受撃ボックス：Hurtboxの 2D 空間ハッシュグリッド登録システム。
///     毎フレーム衝突判定パイプラインの開始前に、有効な被弾エンティティを空間ハッシュへ一括挿入します。
/// </summary>
public static class HurtboxSpatialIndexSystem
{
    /// <summary>
    ///     指定されたワールドから受撃ボックス空間ハッシュインデックスを取得します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>受撃ボックス空間ハッシュインデックス</returns>
    public static HurtboxSpatialIndex GetSpatialIndex(World world) => world.Get<HurtboxSpatialResource>().Index;

    /// <summary>
    ///     空間ハッシュ登録システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHurtboxSpatialIndexSystem(this World world)
    {
        var config = world.Get<ArenaConfig>().SpatialHash;
        var index = new HurtboxSpatialIndex(config.CellSize, config.TableSize, config.InitialCapacity);
        var query = world.QueryBuilder<Transform, Hurtbox, Health, GameplayTagState>().Build();

        world.Set(new HurtboxSpatialResource(index, query, config.CellSize, config.TableSize));

        world.System("HurtboxSpatialIndexSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(Execute);

        return world;
    }

    /// <summary>
    ///     空間ハッシュインデックスの毎フレーム更新処理：名前付き静的メソッド。
    ///     インデックスをクリアし、有効な受撃ボックスを一括走査して登録します。
    /// </summary>
    public static void Execute(Iter it)
    {
        var world = it.World();
        var res = world.Get<HurtboxSpatialResource>();
        ref readonly var arenaCfg = ref world.Get<ArenaConfig>();
        var hashCfg = arenaCfg.SpatialHash;

        // 熱再読み込み：ホットリロード検知：CellSize または TableSize が変更された場合は空間ハッシュを再構築
        if (MathF.Abs(res.CachedCellSize - hashCfg.CellSize) > 0.001f || res.CachedTableSize != hashCfg.TableSize)
        {
            res.Index = new HurtboxSpatialIndex(hashCfg.CellSize, hashCfg.TableSize, hashCfg.InitialCapacity);
            res.CachedCellSize = hashCfg.CellSize;
            res.CachedTableSize = hashCfg.TableSize;
        }

        res.Index.Clear();
        res.Query.Iter(CollectHurtboxes);
    }

    private static void CollectHurtboxes(
        Iter it,
        Field<Transform> trans,
        Field<Hurtbox> hurtboxes,
        Field<Health> hps,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        var index = it.World().Get<HurtboxSpatialResource>().Index;

        for (var i = 0; i < count; i++)
        {
            ref readonly var hp = ref hps[i];
            if (hp.Current <= 0.0f) continue;

            ref readonly var tag = ref tags[i];
            if (tag.HasTag(GameplayTag.Untargetable)) continue;

            var entity = it.Entity(i);
            ref readonly var t = ref trans[i];
            ref readonly var hb = ref hurtboxes[i];

            index.Insert(entity, hb.Faction, in t, in hb);
        }
    }
}
