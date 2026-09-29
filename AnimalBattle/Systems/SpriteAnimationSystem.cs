using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     全ての <see cref="Animation" /> と <see cref="Sprite" /> を持つエンティティのフレーム進行、
///     クリップタイマー更新、および UV 切り出し領域 (SrcRect) のリアルタイム計算を行う純粋な ECS System。
/// </summary>
public static class SpriteAnimationSystem {
    /// <summary>
    ///     アニメーション駆動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddSpriteAnimationSystem(this World world)
    {
        world.System<Animation, Sprite>("SpriteAnimationSystem")
           .Without<PlayerTag>()
           .Kind(Ecs.OnUpdate)
           .Each((it, i, ref anim, ref sprite) =>
            {
                if (!anim.IsPlaying) return;

                anim.Timer += it.DeltaTime();

                var charConfigRes = AssetManager.GetCharacterConfig(anim.CharacterType);
                if (!charConfigRes.IsError && charConfigRes.Value != null)
                {
                    var charConfig = charConfigRes.Value;
                    var handleRes = AssetManager.GetCharacterSpriteHandle(anim.CharacterType, anim.CurrentClip);
                    sprite.GraphHandle = !handleRes.IsError ? handleRes.Value : -1;
                    sprite.SrcRect =
                        AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out var isFinished);
                    anim.IsFinished = isFinished;
                }
            });

        return world;
    }
}