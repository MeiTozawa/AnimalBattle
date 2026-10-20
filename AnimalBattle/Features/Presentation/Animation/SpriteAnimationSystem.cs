using System.Diagnostics;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Animation;

/// <summary>
///     全ての <see cref="Animation" /> と <see cref="Sprite" /> を持つエンティティのフレーム進行、
///     クリップタイマー更新、および UV 切り出し領域 (SrcRect) のリアルタイム計算を行う純粋な ECS System。
/// </summary>
public static class SpriteAnimationSystem
{
    /// <summary>
    ///     アニメーション駆動システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddSpriteAnimationSystem(this World world)
    {
        world.System<Components.Animation, Sprite>("SpriteAnimationSystem")
            .Kind(Ecs.OnUpdate)
            .Without<HitstopState>()
            .Each(UpdateSpriteAnimation);

        return world;
    }

    /// <summary>
    ///     スプライトアニメーション更新処理（名前付き静的メソッド）。
    /// </summary>
    public static void UpdateSpriteAnimation(Iter it, int i, ref Components.Animation anim, ref Sprite sprite)
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
            Debug.WriteLine($"[SpriteAnimationSystem 警告] スプライトハンドルの取得に失敗しました ({anim.CharacterType} / {anim.CurrentClip})。Idle にフォールバックします。");
            anim.Play("Idle");
            if (!AssetManager.TryGetCharacterSpriteHandle(anim.CharacterType, "Idle", out handle) || handle < 0)
            {
                return;
            }
        }

        sprite.GraphHandle = handle;
        sprite.SrcRect =
            AssetManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out var isFinished);
        anim.IsFinished = isFinished;
    }
}