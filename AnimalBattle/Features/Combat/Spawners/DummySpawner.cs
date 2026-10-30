using AnimalBattle.Combat.Components;

namespace AnimalBattle.Combat.Spawners;

/// <summary>
///     操作手触り検証用の木桩（Training Dummy）エンティティを生成・配置するスポナークラス。
/// </summary>
public static class DummySpawner
{
    public const string DummyName = "TrainingDummy";

    /// <summary>
    ///     戦闘シーンルート配下にテスト用木桩を生成します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <param name="root">戦闘シーンのルートエンティティ</param>
    /// <returns>生成された木桩エンティティ</returns>
    public static Entity Spawn(World world, Entity root)
    {
        var assets = world.Get<IAssetManager>();
        // トレーニング用木桩（Dummy）の専用テクスチャを取得
        var texPath = ResourcePath.Characters.Character016_3Png;
        assets.TryGetTexture(texPath, out var dummyHandle);

        var cfg = world.Get<DummyConfig>();

        var dummy = world.Entity(DummyName)
            .ChildOf(root)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<EnemyTag>()
            .Add<DummyTag>()
            .Set(new Transform(cfg.Spawn, new Vec2f(0.5f, 0.5f)))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new CollisionBox(cfg.CollisionSize))
            .Set(new Health(cfg.Health))
            .Set(CombatProfile.ForDummy())
            .Set(new Hurtbox(new CollisionShape.Box(cfg.CollisionSize), Faction.Hostile))
            .Set(new HitFlashSettings(cfg.HitFlashDuration))
            .Set(new Sprite(dummyHandle, false, true, new Rect(0, 0, 192, 192)));

        return dummy;
    }
}
