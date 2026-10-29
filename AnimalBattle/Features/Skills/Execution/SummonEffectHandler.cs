namespace AnimalBattle.Skills;

using AnimalBattle.Enemies;
using AnimalBattle.Enemies.Services;
using AnimalBattle.Scenes.Battle;

/// <summary>
/// 雑魚敵をワールドに動的召喚する技能エフェクトハンドラー。
/// </summary>
public sealed class SummonEffectHandler : ISkillEffectHandler
{
    /// <inheritdoc />
    public SkillEffectKind Kind => SkillEffectKind.Summon;

    /// <inheritdoc />
    public void Execute(in SkillExecutionContext ctx, SkillEffectConfig effect, ref float angleOffset)
    {
        var cfg = effect.Summon;
        if (cfg == null) return;

        var world = ctx.World;
        if (world == default) return;

        var aliveCount = CountAliveMinions(world, cfg.Type);
        if (aliveCount >= cfg.MaxAliveCount) return;

        var root = world.Lookup(BattleSceneSpawner.RootName);
        if (root == default || !root.IsAlive())
        {
            root = world.Entity(BattleSceneSpawner.RootName);
        }

        var spawnPos = MinionSpawnService.SelectBestSpawnPoint(world, cfg.Type);
        if (cfg.Type == MinionType.Flying)
        {
            MinionSpawner.SpawnFlyingMinion(world, root, spawnPos);
        }
        else
        {
            MinionSpawner.SpawnGroundMinion(world, root, spawnPos);
        }
    }

    private static int CountAliveMinions(World world, MinionType type)
    {
        var count = 0;
        if (type == MinionType.Flying)
        {
            using var query = world.QueryBuilder().With<FlyingMinionTag>().Without<DeadTag>().Build();
            query.Each((Entity _) => count++);
        }
        else
        {
            using var query = world.QueryBuilder().With<GroundMinionTag>().Without<DeadTag>().Build();
            query.Each((Entity _) => count++);
        }

        return count;
    }
}
