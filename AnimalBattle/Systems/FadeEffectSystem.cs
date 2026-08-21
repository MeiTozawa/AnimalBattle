using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// イージング関数に基づく透明度フェードイン・フェードアウトの進行を制御し、持続時間満了時にエンティティを自動破棄する ECS System。
/// 近接斬撃判定（Melee Slash Hitbox）や各種パーティクルの残留を防ぎます。
/// </summary>
public static class FadeEffectSystem
{
    /// <summary>
    /// フェード演出および自動寿命破棄システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddFadeEffectSystem(this World world)
    {
        world.System<FadeEffect>("FadeEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref FadeEffect fade) =>
             {
                 fade.ElapsedTimer += it.DeltaTime();
                 if (fade.ElapsedTimer >= fade.Duration)
                 {
                     it.Entity(i).Destruct();
                     return;
                 }

                 float progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
                 float factor = EasingMath.Evaluate(fade.Ease, progress);
                 fade.CurrentAlpha = fade.StartAlpha + (fade.EndAlpha - fade.StartAlpha) * factor;

                 Entity entity = it.Entity(i);
                 if (entity.Has<Velocity>() && entity.Has<Transform>())
                 {
                     ref Transform trans = ref entity.GetMut<Transform>();
                     ref readonly Velocity vel = ref entity.Get<Velocity>();
                     trans.X += vel.Vx * it.DeltaTime();
                     trans.Y += vel.Vy * it.DeltaTime();
                 }
             });

        return world;
    }
}
