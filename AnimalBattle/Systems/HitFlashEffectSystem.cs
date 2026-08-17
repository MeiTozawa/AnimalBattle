using Flecs.NET.Core;
using AnimalBattle.Components;

namespace AnimalBattle.Systems;

/// <summary>
/// 被弾時ホワイトフラッシュ演出の持続時間タイマーおよび点滅終了を制御する ECS System。
/// </summary>
public static class HitFlashEffectSystem
{
    /// <summary>
    /// 受撃フラッシュ演出システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
    {
        world.System<HitFlashEffect>("HitFlashEffectSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref HitFlashEffect flash) =>
             {
                 if (flash.IsFlashing)
                 {
                     flash.ElapsedTimer += it.DeltaTime();
                     if (flash.ElapsedTimer >= flash.Duration)
                     {
                         flash.IsFlashing = false;
                     }
                 }
             });
    }
}
