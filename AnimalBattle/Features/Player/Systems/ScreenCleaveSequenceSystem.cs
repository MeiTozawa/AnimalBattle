using System.Drawing;
using AnimalBattle.Configs;
using AnimalBattle.Framework.Pooling;
using AnimalBattle.Player.Components;
using AnimalBattle.Presentation.Rendering.Components;
using AnimalBattle.Presentation.Vfx.Components;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     Bunny の特殊能力発動時、全画面にランダムな位置・方向・タイミングで無数の斬撃エフェクトを
///     段階的に展開するシーケンス実行 ECS システム。
/// </summary>
public static class ScreenCleaveSequenceSystem
{
    /// <summary>
    ///     全画面斬撃シーケンスシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddScreenCleaveSequenceSystem(this World world)
    {
        world.System<ScreenCleaveSequence>("ScreenCleaveSequenceSystem")
            .Without<RecycleTag>()
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteScreenCleaveSequence);

        return world;
    }

    private static void ExecuteScreenCleaveSequence(Iter it, Field<ScreenCleaveSequence> sequences)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var world = it.World();
        var arenaCfg = world.Get<ArenaConfig>();

        var minX = arenaCfg.BoundaryMinX + 40.0f;
        var maxX = arenaCfg.BoundaryMaxX - 40.0f;
        var minY = 60.0f;
        var maxY = arenaCfg.FloorLimitY + 20.0f;

        for (var i = 0; i < count; i++)
        {
            var entity = it.Entity(i);
            ref var seq = ref sequences[i];
            seq.ElapsedTimer += dt;

            var baseInterval = seq.TotalDuration / Math.Max(1, seq.TargetCount);

            // 異なるタイミングで斬撃エフェクトを順次生成（フレームあたり複数生成を正しく許容）
            while (seq.SpawnedCount < seq.TargetCount && seq.ElapsedTimer >= seq.NextSpawnTimer)
            {
                SpawnSingleBladeVfx(in seq, minX, maxX, minY, maxY);
                seq.SpawnedCount++;

                var jitter = (Random.Shared.NextSingle() - 0.5f) * 0.4f * baseInterval;
                seq.NextSpawnTimer += MathF.Max(0.0001f, baseInterval + jitter);
            }

            // 総持続時間満了：未生成分があればすべて放出し、シーケンスエンティティを回収
            if (seq.ElapsedTimer >= seq.TotalDuration)
            {
                while (seq.SpawnedCount < seq.TargetCount)
                {
                    SpawnSingleBladeVfx(in seq, minX, maxX, minY, maxY);
                    seq.SpawnedCount++;
                }

                entity.Add<RecycleTag>();
            }
        }
    }

    private static void SpawnSingleBladeVfx(in ScreenCleaveSequence seq, float minX, float maxX, float minY, float maxY)
    {
        if (seq.GraphHandle < 0) return;

        // 1. 全画面のランダムな位置
        var posX = minX + Random.Shared.NextSingle() * (maxX - minX);
        var posY = minY + Random.Shared.NextSingle() * (maxY - minY);
        var pos = new Vec2f(posX, posY);

        // 2. 任意のランダムな方向・角度（0 ～ 2π）
        var angle = Random.Shared.NextSingle() * MathF.PI * 2.0f;

        // 3. ランダムな拡縮スケール
        var scaleFactor = seq.ScaleMin + Random.Shared.NextSingle() * (seq.ScaleMax - seq.ScaleMin);
        var scale = new Vec2f(scaleFactor, scaleFactor);

        // 4. スプライトシートから行（4方向の斬撃バリエーション）をランダムに選択
        var rowIndex = Random.Shared.Next(0, 4);
        var srcRect = Rect.FromWidthHeight(seq.ColIndex * seq.CellSize, rowIndex * seq.CellSize, seq.CellSize, seq.CellSize);

        // 5. 左右反転のランダム性
        var flipX = Random.Shared.Next(0, 2) == 1;

        // 6. 前方への推進速度
        var vel = new Vec2f(MathF.Cos(angle) * seq.SlashSpeed, MathF.Sin(angle) * seq.SlashSpeed);

        // 7. エフェクトエンティティの生成と登録
        CombatPoolService.RentAreaEffect()
            .Set(new Transform(pos, scale, angle))
            .Set(new Velocity(vel))
            .Set(new Sprite(seq.GraphHandle, flipX, true, srcRect, seq.SlashColor))
            .Set(new FadeEffect(seq.SlashLifetime, 255.0f, 0.0f, EaseType.OutQuad));
    }
}
