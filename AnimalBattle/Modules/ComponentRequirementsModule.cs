using Flecs.NET.Core;

namespace AnimalBattle.Modules;

/// <summary>
///     Flecs.NET の <c>With</c> 関係を用いて、特定コンポーネント/タグ（PlayerTag, BossTag, Boss1Tag, Boss2Tag 等）に必須となる
///     依存コンポーネントを自動補完する Flecs.NET ネイティブモジュール。
/// </summary>
public readonly struct ComponentRequirementsModule : IFlecsModule {
    /// <summary>
    ///     全ての必須コンポーネント依存関係を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    public void InitModule(World world)
    {
        world.Module<ComponentRequirementsModule>();

        // PlayerTag に必須なコンポーネント群を自動補完
        world.Component<PlayerTag>().Entity
           .Add(Ecs.With, world.Component<Transform>().Entity)
           .Add(Ecs.With, world.Component<Velocity>().Entity)
           .Add(Ecs.With, world.Component<CollisionBox>().Entity)
           .Add(Ecs.With, world.Component<Sprite>().Entity)
           .Add(Ecs.With, world.Component<Animation>().Entity);

        // BossTag に共通して必須なコンポーネント群
        world.Component<BossTag>().Entity
           .Add(Ecs.With, world.Component<Transform>().Entity)
           .Add(Ecs.With, world.Component<Velocity>().Entity)
           .Add(Ecs.With, world.Component<BossCombatState>().Entity);

        // Boss 2 (Ground Crusher) 専用コンポーネント群
        world.Component<Boss2Tag>().Entity
           .Add(Ecs.With, world.Component<CollisionBox>().Entity);
    }
}