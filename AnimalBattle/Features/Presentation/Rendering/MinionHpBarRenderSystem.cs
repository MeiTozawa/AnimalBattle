namespace AnimalBattle.Presentation.Rendering;

using System.Drawing;

/// <summary>
///     雑魚敵の頭上に浮遊する HP バーを描画する ECS システム。
/// </summary>
public static class MinionHpBarRenderSystem
{
    /// <summary>
    ///     雑魚敵 HP バー描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddMinionHpBarRenderSystem(this World world)
    {
        world.System<Transform, CollisionBox, Health>("MinionHpBarRenderSystem")
            .With<MinionTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnStore)
            .Iter(ExecuteMinionHpBarRender);

        return world;
    }

    private static void ExecuteMinionHpBarRender(
        Iter it,
        Field<Transform> transforms,
        Field<CollisionBox> collisionBoxes,
        Field<Health> hps)
    {
        var count = it.Count();
        if (count == 0) return;

        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var minionCfg = world.Get<MinionConfig>();
        var barCfg = minionCfg.HpBar;
        if (barCfg.Width <= 0.0f || barCfg.Height <= 0.0f) return;

        for (var i = 0; i < count; i++)
        {
            ref readonly var trans = ref transforms[i];
            ref readonly var col = ref collisionBoxes[i];
            ref readonly var health = ref hps[i];

            var topY = trans.Position.Y - col.Size.Y * 0.5f;
            var barCenterX = (int)trans.Position.X;
            var barTopY = (int)(topY - barCfg.OffsetY - barCfg.Height);

            var p1 = new Vec2i(barCenterX - (int)(barCfg.Width * 0.5f), barTopY);
            var p2 = new Vec2i(p1.X + (int)barCfg.Width, barTopY + (int)barCfg.Height);

            // 背景と外枠の描画
            GameEngine.DrawBox(p1, p2, barCfg.BgColor, true);
            GameEngine.DrawBox(p1, p2, barCfg.BorderColor, false);

            // 残り HP の割合計算とゲージ描画
            var ratio = Math.Clamp(health.Current / Math.Max(1.0f, health.Max), 0.0f, 1.0f);
            var pad = barCfg.BorderPadding;
            var innerWidth = (p2.X - pad) - (p1.X + pad);
            var fillWidth = (int)Math.Round(innerWidth * ratio);

            if (fillWidth > 0)
            {
                var fillP1 = new Vec2i(p1.X + pad, p1.Y + pad);
                var fillP2 = new Vec2i(p1.X + pad + fillWidth, p2.Y - pad);
                var fillColor = LerpColor(barCfg.LowHpColor, barCfg.FillColor, ratio);
                GameEngine.DrawBox(fillP1, fillP2, fillColor, true);
            }
        }
    }

    /// <summary>
    ///     2 つのカラー間を指定した割合 (0.0 から 1.0) で線形補間します。
    /// </summary>
    /// <param name="from">割合 0.0 の基準カラー</param>
    /// <param name="to">割合 1.0 の目標カラー</param>
    /// <param name="t">補間割合 (0.0 から 1.0)</param>
    /// <returns>補間された <see cref="Color" /></returns>
    private static Color LerpColor(Color from, Color to, float t)
    {
        var clampedT = Math.Clamp(t, 0.0f, 1.0f);
        var a = (int)Math.Round(from.A + (to.A - from.A) * clampedT);
        var r = (int)Math.Round(from.R + (to.R - from.R) * clampedT);
        var g = (int)Math.Round(from.G + (to.G - from.G) * clampedT);
        var b = (int)Math.Round(from.B + (to.B - from.B) * clampedT);
        return Color.FromArgb(a, r, g, b);
    }
}
