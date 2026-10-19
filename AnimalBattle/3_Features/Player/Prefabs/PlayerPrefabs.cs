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
        Debug.Assert(catCfg != null, "CatConfig はワールドに登録されている必要があります。");

        CatPlayer = world.Prefab(CatPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(catCfg.MaxHp))
            .Set(new InvincibleOnHit(catCfg.InvincibleDuration))
            .Set(new HitFlashSettings(catCfg.HitFlashDuration))
            .Set(new CollisionBox(catCfg.CollisionWidth, catCfg.CollisionHeight))
            .Set(new Hurtbox(catCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(0.0f, 0.0f, catCfg.ScaleX, catCfg.ScaleY))
            .Set(new SpringScale(1.0f, 1.0f, catCfg.SpringFrequency, catCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(catCfg))
            .Set(CatWeaponState.FromConfig(catCfg));

        // --- Bunny プレイヤー ---
        var bunnyCfg = world.Get<BunnyConfig>();
        Debug.Assert(bunnyCfg != null, "BunnyConfig はワールドに登録されている必要があります。");

        BunnyPlayer = world.Prefab(BunnyPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(bunnyCfg.MaxHp))
            .Set(new InvincibleOnHit(bunnyCfg.InvincibleDuration))
            .Set(new HitFlashSettings(bunnyCfg.HitFlashDuration))
            .Set(new CollisionBox(bunnyCfg.CollisionWidth, bunnyCfg.CollisionHeight))
            .Set(new Hurtbox(bunnyCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(0.0f, 0.0f, bunnyCfg.ScaleX, bunnyCfg.ScaleY))
            .Set(new SpringScale(1.0f, 1.0f, bunnyCfg.SpringFrequency, bunnyCfg.SpringDampingRatio))
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
