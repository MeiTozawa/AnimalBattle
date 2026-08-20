using Flecs.NET.Core;
using AnimalBattle.Components;

namespace AnimalBattle.Plugins;

/// <summary>
/// Flecs.NET の <c>With</c> 関係を用いて、特定コンポーネント/タグ（PlayerTag, BossTag 等）に必須となる
/// 依存コンポーネントを自動補完するコアプラグイン。
/// </summary>
public class RequirePlugin : IPlugin
{
    /// <summary>
    /// 全ての必須コンポーネント依存関係を Flecs.NET ワールドへ明示的に登録します。
    /// </summary>
    /// <param name="app">構築対象の App コンテナ</param>
    public void Build(App app)
    {
        World world = app.World;

        // PlayerTag に必須なコンポーネント群を自動補完
        world.Component<PlayerTag>().Entity
             .Add(Ecs.With, world.Component<Transform>().Entity)
             .Add(Ecs.With, world.Component<Velocity>().Entity)
             .Add(Ecs.With, world.Component<CollisionBox>().Entity)
             .Add(Ecs.With, world.Component<Health>().Entity)
             .Add(Ecs.With, world.Component<DoubleJump>().Entity)
             .Add(Ecs.With, world.Component<PlayerState>().Entity)
             .Add(Ecs.With, world.Component<Sprite>().Entity)
             .Add(Ecs.With, world.Component<Animation>().Entity);

        // BossTag に必須なコンポーネント群を自動補完
        world.Component<BossTag>().Entity
             .Add(Ecs.With, world.Component<Transform>().Entity)
             .Add(Ecs.With, world.Component<Velocity>().Entity)
             .Add(Ecs.With, world.Component<Health>().Entity)
             .Add(Ecs.With, world.Component<BossStats>().Entity)
             .Add(Ecs.With, world.Component<BossStateMachine>().Entity);
    }
}

