namespace AnimalBattle.Combat;

using AnimalBattle.Combat.Spawners;
using AnimalBattle.Enemies;
using AnimalBattle.Features.Items.Spawners;

/// <summary>
///     戦闘リセット・フェーズ遷移・シーン破棄時における残存弾幕、近接判定、雑魚敵、ドロップアイテム、および VFX の一括クリーンアップを統括するドメインサービス。
/// </summary>
public static class CombatCleanupService
{
    /// <summary>
    ///     自機弾幕および VFX エンティティをすべてオブジェクトプールへ即時返却します。
    /// </summary>
    public static void RecyclePlayerProjectilesAndVfx(World world)
    {
        using (var playerBullets = world.QueryBuilder().With<PlayerBulletTag>().Build())
        {
            playerBullets.Each(CombatPoolService.Recycle);
        }

        using (var vfx = world.QueryBuilder().With<VfxTag>().Build())
        {
            vfx.Each(CombatPoolService.Recycle);
        }
    }

    /// <summary>
    ///     戦闘リセット時（自機エンティティが存続する場合）の動的戦闘オブジェクト一括クリーンアップを実行します。
    ///     自機に装備された永続近接武器エンティティは破棄せず、安全に無効化および履歴初期化のみを行います。
    /// </summary>
    public static void ClearTransientCombatEntitiesForReset(World world)
    {
        BulletSpawner.ClearAllEnemyBulletsImmediate(world);
        MeleeSpawner.ClearAllEnemyMeleeImmediate(world);
        MinionSpawner.ClearAllMinionsImmediate(world);
        ItemSpawner.ResetItems(world);
        RecyclePlayerProjectilesAndVfx(world);

        using var playerMelee = world.QueryBuilder().With<PlayerMeleeTag>().Build();
        playerMelee.Each(static e => e.Disable());

        using var meleeHitHistory = world.QueryBuilder<HitHistory>().With<PlayerMeleeTag>().Build();
        meleeHitHistory.Each(static (ref HitHistory h) => h = new HitHistory());
    }

    /// <summary>
    ///     シーン破棄時（自機を含むシーン全エンティティを破棄する場合）の戦闘オブジェクト一括回収・破棄を実行します。
    /// </summary>
    public static void ClearAllCombatEntitiesForTeardown(World world)
    {
        BulletSpawner.ClearAllEnemyBulletsImmediate(world);
        MeleeSpawner.ClearAllEnemyMeleeImmediate(world);
        MinionSpawner.ClearAllMinionsImmediate(world);
        ItemSpawner.ResetItems(world);
        RecyclePlayerProjectilesAndVfx(world);

        using var playerMelee = world.QueryBuilder().With<PlayerMeleeTag>().Build();
        playerMelee.Each(static e =>
        {
            e.Disable();
            e.Destruct();
        });
    }
}
