namespace AnimalBattle.Combat.Sync;

using System;
using System.Diagnostics;
using AnimalBattle.Combat;
using AnimalBattle.Combat.Components;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

/// <summary>
/// 練習用木桩（Dummy）設定変更時に、Flecs.NET ワールドの単例および活体木桩エンティティを同期・再水和する同期機構。
/// </summary>
public static class DummyConfigSync
{
    /// <summary>
    /// 木桩設定の変更をワールドおよび活体木桩エンティティへ反映します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="newConfig">更新後の DummyConfig 設定</param>
    public static void Sync(World world, DummyConfig newConfig)
    {
        Debug.Assert(world != default, "有効な Flecs ワールドが必要です。");
        Debug.Assert(newConfig != null, "DummyConfig は null にできません。");

        // 1. ワールド単例の更新
        world.Set(newConfig);

        // 2. 活体木桩エンティティの再水和
        using var query = world.QueryBuilder<Health, CollisionBox, Hurtbox, HitFlashSettings>()
            .With<DummyTag>()
            .Build();

        query.Each((ref Health health, ref CollisionBox col, ref Hurtbox hurt, ref HitFlashSettings flash) =>
        {
            var ratio = health.Max > 0.0f ? health.Current / health.Max : 1.0f;
            health.Max = newConfig.Health;
            health.Current = Math.Clamp(ratio * newConfig.Health, 0.0f, newConfig.Health);

            col.Size = newConfig.CollisionSize;
            hurt.Shape = new CollisionShape.Box(newConfig.CollisionSize);
            flash.Duration = newConfig.HitFlashDuration;
        });
    }
}
