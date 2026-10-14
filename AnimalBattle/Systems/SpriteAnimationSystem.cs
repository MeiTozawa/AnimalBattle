using System.Diagnostics;
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
           .Kind(Ecs.OnUpdate)
           .Without<HitstopState>()
           .Each((it, i, ref anim, ref sprite) =>
            {
                if (!anim.IsPlaying) return;

                anim.Timer += it.DeltaTime();

                if (!AssetManager.TryGetCharacterConfig(anim.CharacterType, out var charConfig))
                {
                    Debug.Fail($"キャラクターのアニメーション設定が登録されていません: {anim.CharacterType}");
                    return;
                }

                var hasHandle = AssetManager.TryGetCharacterSpriteHandle(anim.CharacterType, anim.CurrentClip, out var handle);
                if (!hasHandle || handle < 0)
                {
                    Debug.Fail($"[SpriteAnimationSystem 警告] スプライトハンドルの取得に失敗しました ({anim.CharacterType} / {anim.CurrentClip})");
                    return;
                }
                sprite.GraphHandle = handle;
                sprite.SrcRect =
                    AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out var isFinished);
                anim.IsFinished = isFinished;
            });

        return world;
    }
}