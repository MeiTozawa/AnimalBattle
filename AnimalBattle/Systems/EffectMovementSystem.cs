using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// 視覚演出エンティティ（EffectTag）の等速直線運動（位置更新）を一括処理する ECS System。
/// </summary>
public static class EffectMovementSystem
{
    /// <summary>
    /// エフェクト移動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddEffectMovementSystem(this World world)
    {
        world.System<Transform, Velocity>("EffectMovementSystem")
             .With<EffectTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Velocity vel) =>
             {
                 float dt = it.DeltaTime();
                 trans.X += vel.Vx * dt;
                 trans.Y += vel.Vy * dt;
             });

        return world;
    }
}
