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
public static class BattleAiSystem
{
    private static Query<Transform, CollisionBox> _platformQuery;
    private static Query<PlayerPawn> _playerQuery;
    private static Query<BaseBossAgent> _bossQuery;

    private static PlayerPawn? _cachedPlayer;
    private static BaseBossAgent? _cachedBoss;

    /// <summary>
    ///     首領 AI 思考および動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleAiSystem(this World world)
    {
        _platformQuery = world.QueryBuilder<Transform, CollisionBox>()
            .With<PlatformTag>()
            .Build();

        _playerQuery = world.QueryBuilder<PlayerPawn>().Build();
        _bossQuery = world.QueryBuilder<BaseBossAgent>().Build();

        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteBattleAi);

        return world;
    }

    private static void ExecuteBattleAi(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        _cachedPlayer = null;
        _cachedBoss = null;

        _playerQuery.Each(FindPlayer);
        _bossQuery.Each(FindBoss);

        var player = _cachedPlayer;
        var boss = _cachedBoss;
        _cachedPlayer = null;
        _cachedBoss = null;

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
            _platformQuery);

        var camCfg = w.Get<AnimalBattle.Configs.CameraConfig>();
        CameraService.Update(
            dt,
            pTrans.Position,
            pVel.Value,
            bTrans.Position,
            camCfg);
    }

    private static void FindPlayer(ref PlayerPawn p) => _cachedPlayer = p;
    private static void FindBoss(ref BaseBossAgent b) => _cachedBoss = b;
}
