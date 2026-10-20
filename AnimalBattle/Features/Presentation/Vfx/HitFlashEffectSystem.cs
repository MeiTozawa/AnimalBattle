using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     被弾時ホワイトフラッシュ（白閃）演出の持続時間タイマーおよび自動破棄を制御する ECS System。
/// </summary>
public static class HitFlashEffectSystem
{
    /// <summary>
    ///     受撃フラッシュ演出システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddHitFlashEffectSystem(this World world)
    {
        world.System<HitFlashEffect>("HitFlashEffectSystem")
            .Kind(Ecs.OnUpdate)
            .Each(UpdateHitFlashEffect);

        return world;
    }

    /// <summary>
    ///     受撃フラッシュ演出タイマー更新処理（名前付き静的メソッド）。
    /// </summary>
    public static void UpdateHitFlashEffect(Iter it, int i, ref HitFlashEffect flash)
    {
        flash.ElapsedTimer += it.DeltaTime();
        if (flash.IsFinished) it.Entity(i).Remove<HitFlashEffect>();
    }
}