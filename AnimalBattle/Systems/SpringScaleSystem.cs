using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     二階減衰振動スプリングに基づくスケール変形（スカッシュ＆ストレッチ）更新システム。
///     ヒットストップ（<see cref="HitstopState" />）中のエンティティは更新をスキップします。
/// </summary>
public static class SpringScaleSystem
{
    /// <summary>
    ///     ECS ワールドにスプリングスケール更新システムを登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddSpringScaleSystem(this World world)
    {
        world.System<SpringScale>()
            .Without<HitstopState>()
            .Each((Iter it, int i, ref SpringScale spring) =>
            {
                var dt = it.DeltaTime();
                if (dt <= 0.0f) return;

                var omega = MathF.Tau * spring.Frequency;
                var k = omega * omega;
                var c = 2.0f * spring.DampingRatio * omega;

                var accelX = -k * (spring.CurrentX - spring.TargetX) - c * spring.VelX;
                var accelY = -k * (spring.CurrentY - spring.TargetY) - c * spring.VelY;

                spring.VelX += accelX * dt;
                spring.CurrentX += spring.VelX * dt;

                spring.VelY += accelY * dt;
                spring.CurrentY += spring.VelY * dt;
            });

        return world;
    }
}
