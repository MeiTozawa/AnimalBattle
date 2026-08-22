using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Asset;

namespace AnimalBattle.Systems;

/// <summary>
/// 全ての <see cref="Animation"/> と <see cref="Sprite"/> を持つエンティティのフレーム進行、
/// クリップタイマー更新、および UV 切り出し領域 (SrcRect) のリアルタイム計算を行う純粋な ECS System。
/// </summary>
public static class SpriteAnimationSystem
{
    /// <summary>
    /// アニメーション駆動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddSpriteAnimationSystem(this World world)
    {
        world.System<Animation, Sprite>("SpriteAnimationSystem")
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Animation anim, ref Sprite sprite) =>
             {
                 if (!anim.IsPlaying)
                 {
                     return;
                 }

                 anim.Timer += it.DeltaTime();

                 Result<AnimationConfig> charConfigRes = AssetManager.GetCharacterConfig(anim.CharacterType);
                 if (charConfigRes.IsOk && charConfigRes.Value != null)
                 {
                     AnimationConfig charConfig = charConfigRes.Value;
                     sprite.GraphHandle = AssetManager.GetCharacterSpriteHandle(anim.CharacterType, anim.CurrentClip).ValueOrDefault(-1);
                     sprite.SrcRect = AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out bool isFinished);
                     anim.IsFinished = isFinished;
                 }
             });

        return world;
    }
}
