using AnimalBattle.Combat.Components;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     出現バッファ状態のタイマー減衰および終了時の解除を管理する ECS システム。
///     バッファ終了時に接触攻撃判定を再有効化し、状態コンポーネントを除去します。
/// </summary>
public static class SpawnBufferUpdateSystem
{
    /// <summary>
    ///     出現バッファ更新システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddSpawnBufferUpdateSystem(this World world)
    {
        world.System<Hitbox, SpawnBufferState>("SpawnBufferUpdateSystem")
            .Without<DeadTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteSpawnBufferUpdate);

        return world;
    }

    private static void ExecuteSpawnBufferUpdate(Iter it, Field<Hitbox> hitboxes, Field<SpawnBufferState> buffers)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        using (new DeferScope(it.World()))
        {
            for (var i = 0; i < count; i++)
            {
                ref var buffer = ref buffers[i];
                ref var hitbox = ref hitboxes[i];

                buffer.RemainingTime -= dt;
                if (buffer.RemainingTime <= 0.0f)
                {
                    hitbox.IsActive = true;
                    it.Entity(i).Remove<SpawnBufferState>();
                }
            }
        }
    }
}
