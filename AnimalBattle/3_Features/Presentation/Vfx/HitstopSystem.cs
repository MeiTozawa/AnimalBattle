using AnimalBattle.Components;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     ヒットストップ（時間凍結）のタイマー減衰および解除を制御する ECS システム。
/// </summary>
public static class HitstopSystem
{
    /// <summary>
    ///     ヒットストップシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHitstopSystem(this World world)
    {
        world.System<HitstopState, Sprite>("HitstopStateSystem")
            .Kind(Ecs.PreUpdate)
            .Each((Iter it, int i, ref HitstopState hitstop, ref Sprite sprite) =>
            {
                hitstop.RemainingSeconds -= it.DeltaTime();
                if (hitstop.RemainingSeconds <= 0.0f)
                {
                    var entity = it.Entity(i);
                    entity.Remove<HitstopState>();
                    entity.Remove<SpriteJitter>();
                    sprite.JitterOffset = Vec2f.Zero;
                }
            });

        return world;
    }
}
