using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     自機・首領・選人プレビューおよび近接攻撃等の汎用 Sprite 描画を行う ECS システム群。
///     被弾点滅、近接攻撃加算合成、およびスプライト基本描画を独立した専用クエリ・パスに分離し、
///     ホットループ内での entity.Has 分岐を完全に排除した純粋データ駆動型レンダラーです。
/// </summary>
public static class SpriteRenderSystem {
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
            .Iter((Iter it, Field<Transform> transforms, Field<Sprite> sprites) =>
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
                    var drawX = (int)(trans.X + sprite.JitterOffsetX);
                    var drawY = (int)(trans.Y + sprite.JitterOffsetY + sprite.SpringOffsetY);
                    var scaleX = trans.ScaleX * sprite.ScaleX;
                    var scaleY = trans.ScaleY * sprite.ScaleY;

                    GameEngine.DrawRectRotaGraph3(
                        drawX,
                        drawY,
                        sprite.SrcRect.Left,
                        sprite.SrcRect.Top,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        scaleX,
                        scaleY,
                        trans.Rotation,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);
                }
            });

        // 2. 近接攻撃スプライト加算合成描画パス
        world.System<Transform, Sprite>("MeleeSpriteRenderSystem")
            .With<PlayerMeleeTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter((Iter it, Field<Transform> transforms, Field<Sprite> sprites) =>
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
                    var drawX = (int)(trans.X + sprite.JitterOffsetX);
                    var drawY = (int)(trans.Y + sprite.JitterOffsetY + sprite.SpringOffsetY);
                    var scaleX = trans.ScaleX * sprite.ScaleX;
                    var scaleY = trans.ScaleY * sprite.ScaleY;

                    GameEngine.SetDrawBright(sprite.Color.R, sprite.Color.G, sprite.Color.B);
                    GameEngine.SetDrawBlendMode(BlendMode.Add, 255);

                    GameEngine.DrawRectRotaGraph3(
                        drawX,
                        drawY,
                        sprite.SrcRect.Left,
                        sprite.SrcRect.Top,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        scaleX,
                        scaleY,
                        trans.Rotation,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);

                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                    GameEngine.SetDrawBright(255, 255, 255);
                }
            });

        // 3. 被弾フラッシュ（白光オーバーレイ）描画パス
        world.System<Transform, Sprite, HitFlashEffect>("HitFlashRenderSystem")
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter((Iter it, Field<Transform> transforms, Field<Sprite> sprites, Field<HitFlashEffect> flashes) =>
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
                    var drawX = (int)(trans.X + sprite.JitterOffsetX);
                    var drawY = (int)(trans.Y + sprite.JitterOffsetY + sprite.SpringOffsetY);
                    var scaleX = trans.ScaleX * sprite.ScaleX;
                    var scaleY = trans.ScaleY * sprite.ScaleY;

                    GameEngine.SetDrawBright(flash.FlashColor.R, flash.FlashColor.G, flash.FlashColor.B);
                    GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);

                    GameEngine.DrawRectRotaGraph3(
                        drawX,
                        drawY,
                        sprite.SrcRect.Left,
                        sprite.SrcRect.Top,
                        sprite.SrcRect.Width,
                        sprite.SrcRect.Height,
                        scaleX,
                        scaleY,
                        trans.Rotation,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);

                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                    GameEngine.SetDrawBright(255, 255, 255);
                }
            });

        return world;
    }
}
