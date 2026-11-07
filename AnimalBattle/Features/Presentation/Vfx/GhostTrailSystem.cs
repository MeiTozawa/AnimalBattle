using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Player.Components;
using AnimalBattle.Presentation.Rendering.Components;
using AnimalBattle.Presentation.Vfx.Components;

namespace AnimalBattle.Presentation.Vfx;

/// <summary>
///     自機ダッシュ状態を監視し、一定インターバルごとに速度残像 (Ghost Trail) を
///     オブジェクトプールから自動生成・配置する表現層 ECS システム。
///     物理・移動ロジックから視覚演出の完全な分離を提供します。
/// </summary>
public static class GhostTrailSystem
{
    /// <summary>
    ///     速度残像生成システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddGhostTrailSystem(this World world)
    {
        world.System<Transform, Sprite, DashState>("GhostTrailSystem")
            .Without<RecycleTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteGhostTrail);

        return world;
    }

    private static void ExecuteGhostTrail(
        Iter it,
        Field<Transform> transforms,
        Field<Sprite> sprites,
        Field<DashState> dashStates)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();
        var pCfg = world.GetActivePlayerConfig();
        var vfxColor = world.Get<PlayerVisualRenderConfig>().GhostTrailColor;
        var interval = pCfg.DashGhostTrailInterval;
        var duration = pCfg.DashGhostTrailDuration;
        var alpha = pCfg.DashGhostTrailAlpha;

        for (var i = 0; i < count; i++)
        {
            ref var dash = ref dashStates[i];
            if (!dash.IsDashing)
            {
                dash.GhostTrailTimer = 0.0f;
                continue;
            }

            dash.GhostTrailTimer += dt;
            if (dash.GhostTrailTimer >= interval)
            {
                dash.GhostTrailTimer = 0.0f;

                ref readonly var trans = ref transforms[i];
                ref readonly var sprite = ref sprites[i];

                CombatPoolService.RentAreaEffect()
                    .Set(new Transform(trans.Position, trans.Scale, trans.Rotation))
                    .Set(new Sprite(sprite.GraphHandle, sprite.FlipX, true, sprite.SrcRect, vfxColor))
                    .Set(new FadeEffect(duration, alpha, 0.0f));
            }
        }
    }
}
