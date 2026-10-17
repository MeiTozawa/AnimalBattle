using AnimalBattle.Components;
using AnimalBattle.Framework.Pooling;
using Flecs.NET.Core;

namespace AnimalBattle.Common;

/// <summary>
///     全ゲーム領域で共有される基底抽象 Prefab（原型）の登録と管理。
///     インスタンス化時の Archetype 安定化（AutoOverride）を保証します。
/// </summary>
public static class CommonPrefabs
{
    public const string BaseProjectileName = "BaseProjectilePrefab";
    public const string BaseMeleeHitboxName = "BaseMeleeHitboxPrefab";
    public const string BaseVfxName = "BaseVfxPrefab";
    public const string BaseCharacterName = "BaseCharacterPrefab";

    public static Entity BaseProjectile { get; private set; }
    public static Entity BaseMeleeHitbox { get; private set; }
    public static Entity BaseVfx { get; private set; }
    public static Entity BaseCharacter { get; private set; }

    /// <summary>
    ///     全基底抽象 Prefab を Flecs ワールドに登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド。</param>
    public static void Register(World world)
    {
        // PoolLeaseState に AutoOverride を付与し、インスタンス化時の Archetype 跨表搬遷を完全根絶
        world.Component<PoolLeaseState>().AutoOverride<PoolLeaseState>();

        // 投射物基底原型
        BaseProjectile = world.Prefab(BaseProjectileName)
            .Set(new Transform(0, 0))
            .Set(new Velocity(0, 0));

        // 近接判定基底原型
        BaseMeleeHitbox = world.Prefab(BaseMeleeHitboxName)
            .Set(new Transform(0, 0))
            .Set(new HitHistory());

        // VFX 基底原型
        BaseVfx = world.Prefab(BaseVfxName)
            .Set(new Transform(0, 0))
            .Add<VfxTag>();

        // キャラクター基底原型
        BaseCharacter = world.Prefab(BaseCharacterName)
            .Set(new Transform(0, 0))
            .Set(new Velocity(0, 0));
    }
}
