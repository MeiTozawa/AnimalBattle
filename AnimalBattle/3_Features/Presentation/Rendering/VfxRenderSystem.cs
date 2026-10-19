using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     残影スプライト、環状衝撃波（RingEffect）、および弾丸破片パーティクルを描画する ECS エフェクト描画システム。
///     加算合成（BlendMode.Add）およびアルファ減衰（FadeEffect）を統合適用します。
/// </summary>
public static class VfxRenderSystem {
    /// <summary>
    ///     VFX 描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddVfxRenderSystem(this World world)
    {
        // 1. スプライト残影フェードエフェクト
        world.System<Transform, Sprite, FadeEffect>("SpriteFadeEffectRenderSystem")
            .With<VfxTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter((Iter it, Field<Transform> trans, Field<Sprite> sprites, Field<FadeEffect> fades) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ref var fade = ref fades[i];
                    var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
                    ref var sprite = ref sprites[i];
                    if (alpha <= 0 || !sprite.IsVisible || sprite.GraphHandle < 0) continue;

                    ref var t = ref trans[i];
                    GameEngine.SetDrawBright(sprite.Color.R, sprite.Color.G, sprite.Color.B);
                    GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
                    GameEngine.DrawRectRotaGraph(
                        (Vec2i)t.Position,
                        new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top),
                        new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height),
                        t.Scale,
                        t.Rotation,
                        sprite.GraphHandle,
                        true,
                        sprite.FlipX);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                    GameEngine.SetDrawBright(255, 255, 255);
                }
            });

        // 2. 環状拡散リングエフェクト
        world.System<Transform, RingEffect, FadeEffect>("RingEffectRenderSystem")
            .With<VfxTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter((Iter it, Field<Transform> trans, Field<RingEffect> rings, Field<FadeEffect> fades) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                var count = it.Count();
                for (var i = 0; i < count; i++)
                {
                    ref var fade = ref fades[i];
                    var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
                    if (alpha <= 0) continue;

                    ref var ring = ref rings[i];
                    var progress = Math.Clamp(fade.ElapsedTimer / Math.Max(0.001f, fade.Duration), 0.0f, 1.0f);
                    var currentRadius = ring.StartRadius + (ring.EndRadius - ring.StartRadius) * progress;
                    ref var t = ref trans[i];

                    GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
                    GameEngine.DrawCircle((Vec2i)t.Position, (int)currentRadius, ring.Color, ring.IsFilled);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                }
            });

        // 3. 破片パーティクルエフェクト
        world.System<Transform, Bullet, FadeEffect>("ParticleEffectRenderSystem")
            .With<VfxTag>()
            .Without<RecycleTag>()
            .Kind(Ecs.OnStore)
            .Iter((Iter it, Field<Transform> trans, Field<Bullet> bullets, Field<FadeEffect> fades) =>
            {
                var w = it.World();
                if (w.Get<GameFlow>().State != GameState.Battle) return;

                var count = it.Count();
                var bulletCfg = w.Get<RenderConfig>().Bullets;

                for (var i = 0; i < count; i++)
                {
                    ref var fade = ref fades[i];
                    var alpha = (int)Math.Clamp(fade.CurrentAlpha, 0.0f, 255.0f);
                    if (alpha <= 0) continue;

                    ref var b = ref bullets[i];
                    ref var t = ref trans[i];
                    var p = (Vec2i)t.Position;
                    var radius = (int)b.Radius;

                    GameEngine.SetDrawBlendMode(BlendMode.Add, alpha);
                    GameEngine.DrawCircle(p, radius + bulletCfg.ParticleOutlinePadding, b.OutlineColor, true);
                    GameEngine.DrawCircle(p, radius, b.FillColor, true);
                    GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
                }
            });

        return world;
    }
}
