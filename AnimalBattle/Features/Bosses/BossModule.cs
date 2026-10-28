namespace AnimalBattle.Bosses;

/// <summary>
///     Boss の Prefab プレハブおよび登録を一括管理する Flecs モジュール。
/// </summary>
public readonly struct BossModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<BossModule>();

        BossPrefabs.Register(world);
    }
}
