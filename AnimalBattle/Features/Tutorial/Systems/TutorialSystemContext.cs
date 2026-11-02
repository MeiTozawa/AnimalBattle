using AnimalBattle.Features.Tutorial.Components;

namespace AnimalBattle.Features.Tutorial.Systems;

/// <summary>
///     チュートリアル各システムが共有する事前ビルド済み ECS クエリとフレームキャッシュ。
///     毎フレームの QueryBuilder 破棄およびヒープ割り当てを完全に排除します。
/// </summary>
public sealed class TutorialSystemContext : IDisposable
{
    public Query<Health, Transform, Velocity> PlayerHealthQuery { get; }
    public Query<TutorialTarget, Transform> TargetQuery { get; }
    public Query<TutorialDoor, Transform> DoorQuery { get; }
    public Query MinionQuery { get; }

    public Vec2f CachedPlayerPos;
    public Vec2f CachedPlayerVel;
    public bool HasPlayer;
    public bool PlayerDied;

    public TutorialSystemContext(World world)
    {
        PlayerHealthQuery = world.QueryBuilder<Health, Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        TargetQuery = world.QueryBuilder<TutorialTarget, Transform>().Build();
        DoorQuery = world.QueryBuilder<TutorialDoor, Transform>().Build();
        MinionQuery = world.QueryBuilder().With<MinionTag>().Without<DeadTag>().Build();
    }

    public void Dispose()
    {
        PlayerHealthQuery.Dispose();
        TargetQuery.Dispose();
        DoorQuery.Dispose();
        MinionQuery.Dispose();
    }
}
