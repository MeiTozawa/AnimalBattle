using AnimalBattle.Abilities;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     全エンティティの時限エフェクト減衰およびタグ状態の同期を推進する純粋な ECS システム。
///     インライン定長データコンポーネントを直接更新し、キャッシュ局所性と零 GC を維持します。
/// </summary>
public static class TimedTagSystem
{
    public static World AddTimedTagSystem(this World world)
    {
        world.System<TimedEffectsState, GameplayTagState>("TimedTagSystem")
            .Kind(Ecs.PreUpdate)
            .Iter(ExecuteTimedTagUpdate);

        return world;
    }

    private static void ExecuteTimedTagUpdate(
        Iter it,
        Field<TimedEffectsState> effects,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();

        for (var i = 0; i < count; i++)
        {
            ref var effect = ref effects[i];
            ref var tag = ref tags[i];
            var hasExpired = false;

            for (var s = 0; s < TimedEffectsState.MaxSlots; s++)
            {
                ref var slot = ref effect.GetSlotMut(s);
                if (!slot.IsActive) continue;

                slot.Remaining -= dt;
                if (slot.Remaining <= 0.0f)
                {
                    slot.IsActive = false;
                    slot.GrantedTags = GameplayTag.None;
                    slot.Remaining = 0.0f;
                    slot.TotalDuration = 0.0f;
                    slot.SourceHandle = 0;
                    hasExpired = true;
                }
            }

            // 失効したエフェクトが存在する場合のみタグビットマスクを再計算
            if (hasExpired)
            {
                TimedEffectService.RecalculateTagMask(ref effect, ref tag);
            }
        }
    }
}
