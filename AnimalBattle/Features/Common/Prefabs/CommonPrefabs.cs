using AnimalBattle.Abilities;
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
        // 実行時インスタンスコンポーネントに AutoOverride を付与し、Archetype 安定化およびインスタンス固有メモリを保証
        world.Component<PoolLeaseState>().AutoOverride<PoolLeaseState>();
        world.Component<Transform>().AutoOverride<Transform>();
        world.Component<Velocity>().AutoOverride<Velocity>();
        world.Component<CollisionBox>().AutoOverride<CollisionBox>();
        world.Component<GameplayTagState>().AutoOverride<GameplayTagState>();
        world.Component<TimedEffectsState>().AutoOverride<TimedEffectsState>();
        world.Component<MovementIntent>().AutoOverride<MovementIntent>();
        world.Component<WeaponIntent>().AutoOverride<WeaponIntent>();
        world.Component<DashState>().AutoOverride<DashState>();
        world.Component<JumpCapability>().AutoOverride<JumpCapability>();
        world.Component<BunnyWeaponState>().AutoOverride<BunnyWeaponState>();
        world.Component<CatWeaponState>().AutoOverride<CatWeaponState>();
        world.Component<Health>().AutoOverride<Health>();
        world.Component<SpringScale>().AutoOverride<SpringScale>();

        // 投射物基底原型
        BaseProjectile = world.Prefab(BaseProjectileName)
            .Set(new Transform(Vec2f.Zero))
            .Set(new Velocity(Vec2f.Zero));

        // 近接判定基底原型
        BaseMeleeHitbox = world.Prefab(BaseMeleeHitboxName)
            .Set(new Transform(Vec2f.Zero))
            .Set(new HitHistory());

        // VFX 基底原型
        BaseVfx = world.Prefab(BaseVfxName)
            .Set(new Transform(Vec2f.Zero))
            .Add<VfxTag>();

        // キャラクター基底原型
        BaseCharacter = world.Prefab(BaseCharacterName)
            .Set(new Transform(Vec2f.Zero))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new GameplayTagState())
            .Set(new TimedEffectsState());
    }
}
