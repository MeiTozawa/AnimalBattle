namespace AnimalBattle.Common;

/// <summary>
///     全ゲーム領域共通の基底コンポーネント・基底 Prefab を初期登録する Flecs モジュール。
/// </summary>
public struct CommonModule : IFlecsModule
{
    public void InitModule(World world)
    {
        world.Module<CommonModule>();
        CommonPrefabs.Register(world);
    }
}
