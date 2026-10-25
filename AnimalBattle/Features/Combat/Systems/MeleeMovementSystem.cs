using Flecs.NET.Core;

namespace AnimalBattle.Combat.Systems;

/// <summary>
///     移動を伴う近接攻撃判定の等速直線移動を処理する ECS システム。
/// </summary>
public static class MeleeMovementSystem
{
    /// <summary>
    ///     近接攻撃判定移動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddMeleeMovementSystem(this World world)
    {
        world.System<Transform, Velocity>("MeleeMovementSystem")
            .With<BossMeleeTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(UpdateMeleeMovement);

        return world;
    }

    /// <summary>
    ///     近接攻撃判定の座標更新パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void UpdateMeleeMovement(Iter it, Field<Transform> trans, Field<Velocity> vel)
    {
        var count = it.Count();
        if (count <= 0) return;

        var dt = it.DeltaTime();
        for (var i = 0; i < count; i++)
        {
            trans[i].Position += vel[i].Value * dt;
        }
    }
}
