using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// スラッシュ斬撃エフェクトの再生完了時の自動破棄を制御する ECS System。
/// </summary>
public static class SlashEffectSystem
{
    /// <summary>
    /// スラッシュ演出システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddSlashEffectSystem(this World world)
    {
        world.System<Animation>("SlashEffectSystem")
             .With<EffectTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Animation anim) =>
             {
                 if (anim.IsFinished)
                 {
                     it.Entity(i).Destruct();
                 }
             });

        return world;
    }
}
