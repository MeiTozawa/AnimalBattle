using AnimalBattle.Abilities;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ワールド内の全 <see cref="AbilitySystemComponent" /> に対して時限タグの持続時間を推進・減衰するシステム。
/// </summary>
public static class TimedTagSystem
{
    public static World AddTimedTagSystem(this World world)
    {
        world.System<AbilitySystemComponent>("TimedTagSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<AbilitySystemComponent> ascs) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ascs[i].UpdateTimedTags(dt);
                }
            });

        return world;
    }
}
