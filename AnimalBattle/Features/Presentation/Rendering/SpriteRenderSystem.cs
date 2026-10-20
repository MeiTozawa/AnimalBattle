using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     自機・首領・選人プレビューおよび近接攻撃等の汎用 Sprite 描画を行う ECS システム群。
///     被弾点滅、近接攻撃加算合成、およびスプライト基本描画を独立した専用クエリ・パスに分離し、
///     ホットループ内での entity.Has 分岐を完全に排除した純粋データ駆動型レンダラーです。
/// </summary>
public static class SpriteRenderSystem
{
    /// <summary>
    ///     汎用スプライト描画システム群（基本、近接加算、被弾フラッシュ）を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddSpriteRenderSystem(this World world)
    {
        // 1. 通常スプライト描画パス（近接加算・残影・回収タグを除外）
        world.System<Transform, Sprite>("SpriteRenderSystem")
            .Without<PlayerMeleeTag>()
            .Without<FadeEffect>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawStandardSprites);

        // 2. 近接攻撃スプライト加算合成描画パス
        world.System<Transform, Sprite>("MeleeSpriteRenderSystem")
            .With<PlayerMeleeTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawMeleeSprites);

        // 3. 被弾フラッシュ（白光オーバーレイ）描画パス
        world.System<Transform, Sprite, HitFlashEffect>("HitFlashRenderSystem")
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawHitFlashSprites);

        return world;
    }

    /// <summary>
    ///     通常スプライト描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void DrawStandardSprites(Iter it, Field<Transform> transforms, Field<Sprite> sprites)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var count = it.Count();
        for (var i = 0; i < count; i++)
        {
            ref var sprite = ref sprites[i];
            if (!sprite.IsVisible || sprite.GraphHandle < 0) continue;

            ref var trans = ref transforms[i];
            var drawPos = (trans.Position + sprite.JitterOffset).ToVec2i() + new Vec2i(0, (int)sprite.SpringOffsetY);
            var srcPos = new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top);
            var srcSize = new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height);
            var scale = new Vec2f(trans.Scale.X * sprite.Scale.X, trans.Scale.Y * sprite.Scale.Y);

            GameEngine.DrawRectRotaGraph(
                drawPos,
                srcPos,
                srcSize,
                scale,
                trans.Rotation,
                sprite.GraphHandle,
                true,
                sprite.FlipX);
        }
    }

    /// <summary>
    ///     近接攻撃スプライト加算合成描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void DrawMeleeSprites(Iter it, Field<Transform> transforms, Field<Sprite> sprites)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var count = it.Count();
        if (count == 0) return;

        for (var i = 0; i < count; i++)
        {
            ref var sprite = ref sprites[i];
            if (!sprite.IsVisible || sprite.GraphHandle < 0) continue;

            ref var trans = ref transforms[i];
            var drawPos = (trans.Position + sprite.JitterOffset).ToVec2i() + new Vec2i(0, (int)sprite.SpringOffsetY);
            var srcPos = new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top);
            var srcSize = new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height);
            var scale = new Vec2f(trans.Scale.X * sprite.Scale.X, trans.Scale.Y * sprite.Scale.Y);

            GameEngine.SetDrawBright(sprite.Color.R, sprite.Color.G, sprite.Color.B);
            GameEngine.SetDrawBlendMode(BlendMode.Add, 255);

            GameEngine.DrawRectRotaGraph(
                drawPos,
                srcPos,
                srcSize,
                scale,
                trans.Rotation,
                sprite.GraphHandle,
                true,
                sprite.FlipX);

            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
            GameEngine.SetDrawBright(255, 255, 255);
        }
    }

    /// <summary>
    ///     被弾白閃オーバーレイ描画パイプライン（名前付き静的メソッド）。
    /// </summary>
    public static void DrawHitFlashSprites(Iter it, Field<Transform> transforms, Field<Sprite> sprites, Field<HitFlashEffect> flashes)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var count = it.Count();
        if (count == 0) return;

        for (var i = 0; i < count; i++)
        {
            ref readonly var flash = ref flashes[i];
            var alpha = (int)(255 * flash.Intensity);
            if (alpha <= 0) continue;

            ref var sprite = ref sprites[i];
            if (!sprite.IsVisible || sprite.GraphHandle < 0) continue;

            ref var trans = ref transforms[i];
            var drawPos = (trans.Position + sprite.JitterOffset).ToVec2i() + new Vec2i(0, (int)sprite.SpringOffsetY);
            var srcPos = new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top);
            var srcSize = new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height);
            var scale = new Vec2f(trans.Scale.X * sprite.Scale.X, trans.Scale.Y * sprite.Scale.Y);

            GameEngine.SetDrawBright(flash.FlashColor.R, flash.FlashColor.G, flash.FlashColor.B);
            GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);

            GameEngine.DrawRectRotaGraph(
                drawPos,
                srcPos,
                srcSize,
                scale,
                trans.Rotation,
                sprite.GraphHandle,
                true,
                sprite.FlipX);

            GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
            GameEngine.SetDrawBright(255, 255, 255);
        }
    }
}
