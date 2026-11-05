namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     出現バッファ状態にあるエンティティ（Boss・小怪）専用の半透明呼吸描画システム。
///     DxLib の BlendMode.Alpha を用い、設定ファイル駆動の正弦波による滑らかな半透明呼吸点滅を表現します。
/// </summary>
public static class SpawnBufferRenderSystem
{
    /// <summary>
    ///     出現バッファ描画システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddSpawnBufferRenderSystem(this World world)
    {
        world.System<Transform, Sprite, SpawnBufferState>("SpawnBufferRenderSystem")
            .Without<RecycleTag>()
            .Without<DeadTag>()
            .Kind(Ecs.OnStore)
            .Iter(DrawSpawnBufferSprites);

        return world;
    }

    /// <summary>
    ///     出現バッファ専用描画パイプライン：名前付き静的メソッド。
    /// </summary>
    public static void DrawSpawnBufferSprites(
        Iter it,
        Field<Transform> transforms,
        Field<Sprite> sprites,
        Field<SpawnBufferState> buffers)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (!flow.IsGameplay) return;

        var count = it.Count();
        if (count == 0) return;

        var spawnBufferCfg = w.Get<SpawnBufferRenderConfig>();
        var baseAlpha = spawnBufferCfg.BaseAlpha;
        var pulseAlpha = spawnBufferCfg.PulseAlpha;
        var frequency = spawnBufferCfg.Frequency;

        for (var i = 0; i < count; i++)
        {
            ref var sprite = ref sprites[i];
            if (!sprite.IsVisible || sprite.GraphHandle < 0) continue;

            ref readonly var buffer = ref buffers[i];
            ref var trans = ref transforms[i];

            // 設定ファイル駆動の呼吸周波数および振幅アルファ変調
            var alpha = (int)(baseAlpha + pulseAlpha * MathF.Sin(buffer.RemainingTime * frequency));
            alpha = Math.Clamp(alpha, 0, 255);

            var drawPos = (trans.Position + sprite.JitterOffset).ToVec2i() + new Vec2i(0, (int)sprite.SpringOffsetY);
            var srcPos = new Vec2i(sprite.SrcRect.Left, sprite.SrcRect.Top);
            var srcSize = new Vec2i(sprite.SrcRect.Width, sprite.SrcRect.Height);
            var scale = new Vec2f(trans.Scale.X * sprite.Scale.X, trans.Scale.Y * sprite.Scale.Y);

            GameEngine.SetDrawBright(sprite.Color.R, sprite.Color.G, sprite.Color.B);
            GameEngine.SetDrawBlendMode(BlendMode.Alpha, alpha);

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

        // 描画ステートの初期化復元
        GameEngine.SetDrawBlendMode(BlendMode.NoBlend, 0);
        GameEngine.SetDrawBright(255, 255, 255);
    }
}
