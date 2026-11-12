namespace AnimalBattle.Enemies.Systems;

using AnimalBattle.Abilities;
using AnimalBattle.Abilities.Skills;
using AnimalBattle.Configs;
using AnimalBattle.Presentation.Rendering;

/// <summary>
///     空中浮遊弾幕怪の攻撃予兆線（Telegraph）をワールド空間へ描画する ECS システム。
///     RenderModule 内の CameraOffsetApplySystem 実行後に呼び出され、カメラ振動・追従オフセットを正しく反映します。
/// </summary>
public static class FlyingMinionTelegraphRenderSystem
{
    /// <summary>
    ///     空中浮遊弾幕怪の予兆線描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddFlyingMinionTelegraphRenderSystem(this World world)
    {
        world.System("FlyingMinionTelegraphRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(ExecuteFlyingMinionTelegraph);

        return world;
    }

    private static void ExecuteFlyingMinionTelegraph(Iter it)
    {
        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (!flow.IsGameplay) return;

        var ctx = world.Get<FlyingMinionContext>();
        FlyingMinionSystem.RefreshPlayerPosition(ctx);

        var bossFx = world.Get<BossEffectsRenderConfig>();
        var arenaCfg = world.Get<ArenaConfig>();

        ctx.MinionQuery.Iter((Iter mIt, Field<Transform> trans, Field<Sprite> sprites, Field<CollisionBox> cols, Field<FlyingMinionComponent> minions) =>
        {
            var count = mIt.Count();
            if (count == 0) return;

            var fCtx = mIt.World().Get<FlyingMinionContext>();
            var fallbackTarget = fCtx.HasPlayer ? fCtx.CachedPlayerPos : Vec2f.Zero;

            for (var i = 0; i < count; i++)
            {
                ref readonly var m = ref minions[i];
                if (m.Asc == null || !m.Asc.HasTag(GameplayTag.Telegraph)) continue;

                var currentAbility = m.Asc.CurrentAbility;
                if (currentAbility == null) continue;

                ref readonly var t = ref trans[i];
                ref readonly var s = ref sprites[i];
                ref readonly var c = ref cols[i];

                var finalTarget = m.TargetPos != Vec2f.Zero ? m.TargetPos : fallbackTarget;

                AbilityTelegraphContext tCtx = new(
                    t.Position,
                    finalTarget,
                    !s.FlipX,
                    m.Asc.Progress,
                    m.HoverTimer,
                    c.Size.Y,
                    bossFx,
                    arenaCfg);

                currentAbility.DrawTelegraph(in tCtx);
            }
        });
    }
}
