namespace AnimalBattle.Features.Items.Systems;

using System;
using AnimalBattle.Core;
using AnimalBattle.Features.Items.Components;
using AnimalBattle.Features.Items.Configs;
using Flecs.NET.Core;

/// <summary>
///     ドロップアイテム（回復アイテム）を DxLib 幾何プリミティブによって描画するシステム。
/// </summary>
public static class ItemRenderSystem
{
    /// <summary>
    ///     アイテム描画システムを Flecs ワールドへ登録します。
    /// </summary>
    public static World AddItemRenderSystem(this World world)
    {
        world.System<Transform, ItemDropPhysics, ItemLifetime, HealthPackTag>("ItemRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter(Execute);

        return world;
    }

    private static void Execute(
        Iter it,
        Field<Transform> transforms,
        Field<ItemDropPhysics> physics,
        Field<ItemLifetime> lifetimes,
        Field<HealthPackTag> _)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        ref readonly var cfg = ref world.Get<HealthPackRenderConfig>();

        for (var i = 0; i < count; i++)
        {
            ref readonly var tr = ref transforms[i];
            ref readonly var phys = ref physics[i];
            ref readonly var life = ref lifetimes[i];

            // 寿命警告点滅：残り時間が閾値以下の場合、周期的に非表示にして点滅
            if (life.Remaining <= life.BlinkThreshold)
            {
                var blinkPhase = (int)(life.Remaining * 10.0f) % 2;
                if (blinkPhase == 0) continue;
            }

            var center = new Vec2i((int)MathF.Round(tr.Position.X), (int)MathF.Round(tr.Position.Y));

            // 浮遊アニメーションに連動した呼吸パルス半径
            var pulseScale = 1.0f + 0.08f * MathF.Sin(phys.FloatTimer * 4.0f);
            var outerRadius = (int)(cfg.OuterRadius * pulseScale);
            var innerRadius = (int)(cfg.InnerRadius * pulseScale);

            // 1. 外周オーラリングの描画
            GameEngine.DrawCircle(center, outerRadius, cfg.AuraColor, false);

            // 2. 中心発光コアの描画
            GameEngine.DrawCircle(center, innerRadius, cfg.CoreColor, true);

            // 3. コア輪郭線の描画
            GameEngine.DrawCircle(center, innerRadius, cfg.OutlineColor, false);

            // 4. 中央クロス記号の描画
            var armLength = cfg.CrossArmLength;
            var armThick = cfg.CrossArmThickness;

            // 水平バー
            GameEngine.DrawBox(
                new Vec2i(center.X - armLength, center.Y - armThick),
                new Vec2i(center.X + armLength, center.Y + armThick),
                cfg.CrossColor,
                true);

            // 垂直バー
            GameEngine.DrawBox(
                new Vec2i(center.X - armThick, center.Y - armLength),
                new Vec2i(center.X + armThick, center.Y + armLength),
                cfg.CrossColor,
                true);
        }
    }
}
