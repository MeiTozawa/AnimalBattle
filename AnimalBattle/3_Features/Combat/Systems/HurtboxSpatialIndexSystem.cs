using AnimalBattle.Combat.Spatial;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     受撃ボックス（Hurtbox）の 2D 空間ハッシュグリッド登録システム。
///     毎フレーム衝突判定パイプラインの開始前に、有効な被弾エンティティを空間ハッシュへ一括挿入します。
/// </summary>
public static class HurtboxSpatialIndexSystem
{
    private static HurtboxSpatialIndex? _spatialIndex;
    private static Query<Transform, Hurtbox, Health, GameplayTagState> _hurtboxQuery;

    /// <summary>
    ///     現在アクティブな受撃ボックス空間ハッシュインデックスを取得します。
    /// </summary>
    public static HurtboxSpatialIndex SpatialIndex => _spatialIndex!;

    /// <summary>
    ///     空間ハッシュ登録システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHurtboxSpatialIndexSystem(this World world)
    {
        var config = ConfigManager.Arena.SpatialHash;
        _spatialIndex = new HurtboxSpatialIndex(config.CellSize, config.TableSize, config.InitialCapacity);
        _hurtboxQuery = world.QueryBuilder<Transform, Hurtbox, Health, GameplayTagState>().Build();

        world.System("HurtboxSpatialIndexSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(Execute);

        return world;
    }

    /// <summary>
    ///     空間ハッシュインデックスの毎フレーム更新処理（名前付き静的メソッド）。
    ///     インデックスをクリアし、有効な受撃ボックスを一括走査して登録します。
    /// </summary>
    public static void Execute(Iter it)
    {
        _spatialIndex!.Clear();
        _hurtboxQuery.Iter(CollectHurtboxes);
    }

    private static void CollectHurtboxes(
        Iter it,
        Field<Transform> trans,
        Field<Hurtbox> hurtboxes,
        Field<Health> hps,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        var index = _spatialIndex!;

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
