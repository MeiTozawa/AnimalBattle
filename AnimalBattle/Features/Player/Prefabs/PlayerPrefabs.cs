using Flecs.NET.Core;

namespace AnimalBattle.Player;

/// <summary>
///     プレイヤー自機（Cat & Bunny）の Prefab 原型登録およびインスタンス化。
/// </summary>
public static class PlayerPrefabs
{
    public const string PlayerBaseName = "PlayerBasePrefab";
    public const string CatPlayerName = "CatPlayerPrefab";
    public const string BunnyPlayerName = "BunnyPlayerPrefab";

    public static Entity PlayerBase { get; private set; }
    public static Entity CatPlayer { get; private set; }
    public static Entity BunnyPlayer { get; private set; }

    /// <summary>
    ///     プレイヤー共通基底および各キャラクターの Prefab 原型を登録します。
    /// </summary>
    public static void Register(World world)
    {
        // プレイヤー共通基底
        PlayerBase = world.Prefab(PlayerBaseName)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<PlayerTag>()
            .Set(new MovementIntent())
            .Set(new WeaponIntent())
            .Set(new DashState());

        // --- Cat プレイヤー ---
        var catCfg = world.Get<CatConfig>();
        var catPrefabCfg = world.Get<CatPrefabConfig>();
        Debug.Assert(catCfg != null, "CatConfig はワールドに登録されている必要があります。");
        Debug.Assert(catPrefabCfg != null, "CatPrefabConfig はワールドに登録されている必要があります。");

        CatPlayer = world.Prefab(CatPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(catPrefabCfg.MaxHp))
            .Set(new InvincibleOnHit(catPrefabCfg.InvincibleDuration))
            .Set(new HitFlashSettings(catPrefabCfg.HitFlashDuration))
            .Set(new CollisionBox(catPrefabCfg.CollisionSize))
            .Set(new Hurtbox(catPrefabCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(Vec2f.Zero, catPrefabCfg.Scale))
            .Set(new SpringScale(Vec2f.One, catPrefabCfg.SpringFrequency, catPrefabCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(catCfg))
            .Set(CatWeaponState.FromConfig(catCfg));

        // --- Bunny プレイヤー ---
        var bunnyCfg = world.Get<BunnyConfig>();
        var bunnyPrefabCfg = world.Get<BunnyPrefabConfig>();
        Debug.Assert(bunnyCfg != null, "BunnyConfig はワールドに登録されている必要があります。");
        Debug.Assert(bunnyPrefabCfg != null, "BunnyPrefabConfig はワールドに登録されている必要があります。");

        BunnyPlayer = world.Prefab(BunnyPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(bunnyPrefabCfg.MaxHp))
            .Set(new InvincibleOnHit(bunnyPrefabCfg.InvincibleDuration))
            .Set(new HitFlashSettings(bunnyPrefabCfg.HitFlashDuration))
            .Set(new CollisionBox(bunnyPrefabCfg.CollisionSize))
            .Set(new Hurtbox(bunnyPrefabCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(Vec2f.Zero, bunnyPrefabCfg.Scale))
            .Set(new SpringScale(Vec2f.One, bunnyPrefabCfg.SpringFrequency, bunnyPrefabCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(bunnyCfg))
            .Set(BunnyWeaponState.FromConfig(bunnyCfg));
    }

    /// <summary>
    ///     指定された CharacterType の Prefab 原型を取得します。
    /// </summary>
    public static Entity GetPrefab(CharacterType type)
    {
        return type switch {
            CharacterType.Cat => CatPlayer,
            CharacterType.Bunny => BunnyPlayer,
            _ => AssertHelper.FailWithDefault("", CatPlayer)
        };
    }
}
