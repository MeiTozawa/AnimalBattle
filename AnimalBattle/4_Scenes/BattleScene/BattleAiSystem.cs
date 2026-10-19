using AnimalBattle.Characters;
using AnimalBattle.Characters.Boss;
using AnimalBattle.Components;
using AnimalBattle.Core;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     戦闘シーンにおける首領（Boss）AI 意思決定および動的カメラ追従を推進する純粋 ECS システム。
///     GameState.Battle 状態時にのみアクティブとなり、OnUpdate フェーズで自機と首領の同期推論を実行します。
/// </summary>
public static class BattleAiSystem {
    /// <summary>
    ///     首領 AI 思考および動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleAiSystem(this World world)
    {
        var platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        var playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        var bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                PlayerPawn? player = null;
                playerQuery.Each((ref PlayerPawn p) => player = p);

                BaseBossAgent? boss = null;
                bossQuery.Each((ref BaseBossAgent b) => boss = b);

                if (player == null || boss == null) return;

                var dt = it.DeltaTime();

                ref readonly var pTrans = ref player.Transform;
                ref readonly var pVel = ref player.Velocity;
                ref readonly var bTrans = ref boss.Transform;

                boss.Update(
                    dt,
                    pTrans.Position,
                    pVel.Value,
                    w,
                    platformQuery);

                var camCfg = w.Get<AnimalBattle.Configs.CameraConfig>();
                CameraService.Update(
                    dt,
                    pTrans.X, pTrans.Y,
                    pVel.Vx, pVel.Vy,
                    bTrans.X, bTrans.Y,
                    camCfg);
            });

        return world;
    }
}
