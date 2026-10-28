namespace AnimalBattle.Enemies;

/// <summary>
/// 雑魚敵のプレハブおよびシステムを一括管理する Flecs モジュール。
/// </summary>
public readonly struct EnemyModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<EnemyModule>();

        MinionPrefabs.Register(world);
    }
}
