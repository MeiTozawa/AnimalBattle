using Flecs.NET.Core;

namespace AnimalBattle.Player;

/// <summary>
///     プレイヤー自機：Cat & Bunnyの Prefab プレハブ登録およびインスタンス化。
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
    ///     プレイヤー共通基底および各キャラクターの Prefab プレハブを登録します。
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
            .Set(new CollisionBox(catCfg.CollisionSize))
            .Set(new Hurtbox(catCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(Vec2f.Zero, catCfg.Scale))
            .Set(new SpringScale(Vec2f.One, catCfg.SpringFrequency, catCfg.SpringDampingRatio))
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
            .Set(new CollisionBox(bunnyCfg.CollisionSize))
            .Set(new Hurtbox(bunnyCfg.GetHurtboxShape(), Faction.Friendly))
            .Set(new Transform(Vec2f.Zero, bunnyCfg.Scale))
            .Set(new SpringScale(Vec2f.One, bunnyCfg.SpringFrequency, bunnyCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(bunnyCfg))
            .Set(BunnyWeaponState.FromConfig(bunnyCfg));
    }

    /// <summary>
    ///     指定された CharacterType の Prefab プレハブを取得します。
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
