namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     視覚演出エンティティ：VfxTagの等速直線運動：位置更新を一括処理する ECS System。
/// </summary>
public static class EffectMovementSystem
{
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
            .Each(UpdateEffectMovement);

        return world;
    }

    /// <summary>
    ///     エフェクト位置更新処理：名前付き静的メソッド。
    /// </summary>
    public static void UpdateEffectMovement(Iter it, int i, ref Transform trans, ref Velocity vel)
    {
        var dt = it.DeltaTime();
        trans.Position += vel.Value * dt;
    }
}