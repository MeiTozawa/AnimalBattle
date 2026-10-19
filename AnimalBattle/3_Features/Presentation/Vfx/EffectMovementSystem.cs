using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     視覚演出エンティティ（VfxTag）の等速直線運動（位置更新）を一括処理する ECS System。
/// </summary>
public static class EffectMovementSystem {
    /// <summary>
    ///     エフェクト移動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddEffectMovementSystem(this World world)
    {
        world.System<Transform, Velocity>("EffectMovementSystem")
           .With<VfxTag>()
           .Kind(Ecs.OnUpdate)
           .Each((it, i, ref trans, ref vel) =>
            {
                var dt = it.DeltaTime();
                trans.Position += vel.Value * dt;
            });

        return world;
    }
}