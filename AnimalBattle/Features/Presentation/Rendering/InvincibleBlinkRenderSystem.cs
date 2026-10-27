namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     無敵点滅スプライト専用の独立レンダーパス ECS システム。
///     論理可視性 Sprite.IsVisible を一切改変せず、点滅の滅位相にあるフレームのみ DrawCall をスキップします。
/// </summary>
public static class InvincibleBlinkRenderSystem
{
    /// <summary>
    ///     無敵点滅描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddInvincibleBlinkRenderSystem(this World world)
    {
        world.System<Transform, Sprite, InvincibleBlinkEffect>("InvincibleBlinkRenderSystem")
            .Without<PlayerMeleeTag>()
            .Without<BossMeleeTag>()
            .Without<FadeEffect>()
            .Without<RecycleTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawInvincibleBlinkSprites);

        return world;
    }

    /// <summary>
    ///     無敵点滅スプライト専用描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void DrawInvincibleBlinkSprites(
        Iter it,
        Field<Transform> transforms,
        Field<Sprite> sprites,
        Field<InvincibleBlinkEffect> blinks)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var count = it.Count();
        if (count == 0) return;

        for (var i = 0; i < count; i++)
        {
            ref var sprite = ref sprites[i];
            // 論理的に非表示（ステルス・死亡演出等）なら描画しない
            if (!sprite.IsVisible || sprite.GraphHandle < 0) continue;

            ref readonly var blink = ref blinks[i];
            // 点滅位相の判定：滅位相フレームは DrawCall のみをスキップ（sprite.IsVisible は完全不変）
            if (blink.IsBlinkHidden) continue;

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
}
