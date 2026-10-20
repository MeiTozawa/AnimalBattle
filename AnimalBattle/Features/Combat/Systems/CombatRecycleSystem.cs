using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     <see cref="RecycleTag" /> が付与されたエンティティをフレーム末尾（<see cref="Ecs.PostUpdate" />）で
///     一括回収する単一責任クリーンアップシステム。
///     各シミュレーションシステムが実行中に即時回収することによる時序競合やデータ破壊を防止し、
///     確定論的な一括返却（Batch Recycle）を実行します。
/// </summary>
public static class CombatRecycleSystem {
    /// <summary>
    ///     戦闘エンティティ遅延回収システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World" />。</returns>
    public static World AddCombatRecycleSystem(this World world)
    {
        world.System("CombatRecycleSystem")
           .With<RecycleTag>()
           .Kind(Ecs.PostUpdate)
           .Iter(ExecuteCombatRecycle);

        return world;
    }

    private static void ExecuteCombatRecycle(Iter it)
    {
        var count = it.Count();
        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            entity.Remove<RecycleTag>();
            CombatPoolService.Recycle(entity);
        }
    }
}
