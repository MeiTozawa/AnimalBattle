namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     World 単位で独立して保持される動的カメラ追従システム用コンテキスト。
/// </summary>
internal sealed class CameraFollowContext : IDisposable
{
    public required Query<Transform, Velocity> PlayerQuery { get; init; }
    public required Query<Transform> BossQuery { get; init; }

    public Vec2f CachedPlayerPos;
    public Vec2f CachedPlayerVel;
    public bool FoundPlayer;

    public Vec2f CachedBossPos;
    public bool FoundBoss;

    public void Dispose()
    {
        PlayerQuery.Dispose();
        BossQuery.Dispose();
    }
}

/// <summary>
///     ゲームプレイ中（戦闘シーンおよびチュートリアルシーン）におけるプレイヤー・ボス間の動的カメラ追従と画面振動減衰を統括する ECS システム。
///     AI 思考システムやチュートリアル進行システムからカメラ更新責務を分離します。
/// </summary>
public static class CameraFollowSystem
{
    /// <summary>
    ///     動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddCameraFollowSystem(this World world)
    {
        var playerQuery = world.QueryBuilder<Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        var bossQuery = world.QueryBuilder<Transform>()
            .With<BossTag>()
            .Without<DeadTag>()
            .Build();

        world.SetDisposable(new CameraFollowContext
        {
            PlayerQuery = playerQuery,
            BossQuery = bossQuery
        });

        world.System("CameraFollowSystem")
            .Kind(Ecs.PostUpdate)
            .Iter(ExecuteCameraFollow);

        return world;
    }

    private static void ExecuteCameraFollow(Iter it)
    {
        var world = it.World();
        ref readonly var flow = ref world.Get<GameFlow>();
        if (!flow.IsGameplay) return;

        var ctx = world.Get<CameraFollowContext>();
        ctx.FoundPlayer = false;
        ctx.PlayerQuery.Iter(CollectPlayer);

        if (!ctx.FoundPlayer) return;

        ctx.FoundBoss = false;
        if (flow.State == GameState.Battle)
        {
            ctx.BossQuery.Iter(CollectBoss);
        }

        var dt = it.DeltaTime();
        var camCfg = world.Get<CameraConfig>();
        var targetBossPos = ctx.FoundBoss ? ctx.CachedBossPos : ctx.CachedPlayerPos;

        CameraService.Update(
            dt,
            ctx.CachedPlayerPos,
            ctx.CachedPlayerVel,
            targetBossPos,
            camCfg);
    }

    private static void CollectPlayer(Iter it, Field<Transform> transforms, Field<Velocity> velocities)
    {
        var count = it.Count();
        if (count == 0) return;

        var ctx = it.World().Get<CameraFollowContext>();
        var idx = count - 1;
        ctx.FoundPlayer = true;
        ctx.CachedPlayerPos = transforms[idx].Position;
        ctx.CachedPlayerVel = velocities[idx].Value;
    }

    private static void CollectBoss(Iter it, Field<Transform> transforms)
    {
        var count = it.Count();
        if (count == 0) return;

        var ctx = it.World().Get<CameraFollowContext>();
        var idx = count - 1;
        ctx.FoundBoss = true;
        ctx.CachedBossPos = transforms[idx].Position;
    }
}
