using AnimalBattle.Combat.Spawners;
using AnimalBattle.Enemies;
using AnimalBattle.Enemies.Services;
using AnimalBattle.Features.Tutorial.Components;
using AnimalBattle.Features.Tutorial.Configs;

namespace AnimalBattle.Features.Tutorial.Handlers;

/// <summary>
///     小怪戦闘フェーズの生成・全滅判定ハンドラー。
/// </summary>
public sealed class CombatPhaseHandler : ITutorialPhaseHandler
{
    /// <inheritdoc />
    public void Setup(World world, Entity root, TutorialPhaseConfig config, TutorialConfig globalConfig)
    {
        Debug.Assert(config != null, "フェーズ設定が null です。");
        if (config == null) return;

        ref var state = ref world.GetMut<TutorialState>();
        state.TargetsRemaining = config.Minions.Length;

        foreach (var item in config.Minions)
        {
            if (item.MinionType.Equals("Flying", StringComparison.OrdinalIgnoreCase))
            {
                var pos = item.Pos != Vec2f.Zero ? item.Pos : MinionSpawnService.SelectBestSpawnPoint(world, MinionType.Flying);
                MinionSpawner.SpawnFlyingMinion(world, root, pos);
            }
            else if (item.MinionType.Equals("Ground", StringComparison.OrdinalIgnoreCase))
            {
                var pos = item.Pos != Vec2f.Zero ? item.Pos : MinionSpawnService.SelectBestSpawnPoint(world, MinionType.Ground);
                MinionSpawner.SpawnGroundMinion(world, root, pos);
            }
            else
            {
                Debug.Assert(false, $"未知の小怪種別です: {item.MinionType}");
            }
        }
    }

    /// <inheritdoc />
    public bool UpdateAndCheckCompletion(World world, ref TutorialState state, TutorialPhaseConfig config, TutorialConfig globalConfig, float dt)
    {
        var aliveMinions = 0;
        using var minionQuery = world.QueryBuilder().With<MinionTag>().Build();
        minionQuery.Each(_ => aliveMinions++);

        state.TargetsRemaining = aliveMinions;

        if (aliveMinions == 0 && !state.IsPhaseCompleted)
        {
            state.IsPhaseCompleted = true;
            state.CompletionTimer = globalConfig.Timing.MinionDefeatDelay;
        }

        if (state.IsPhaseCompleted)
        {
            state.CompletionTimer -= dt;
            if (state.CompletionTimer <= 0f)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Teardown(World world, Entity root)
    {
        using var enemyQuery = world.QueryBuilder().With<EnemyTag>().Build();
        enemyQuery.Each(e => e.Destruct());

        BulletSpawner.ClearAllBossBullets(world);
    }
}
