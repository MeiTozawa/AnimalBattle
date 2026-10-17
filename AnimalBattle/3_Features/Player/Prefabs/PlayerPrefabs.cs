using AnimalBattle.Ai;
using AnimalBattle.Combat;
using AnimalBattle.Common;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
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
        var catCfg = ConfigManager.CatPrefab;
        CatPlayer = world.Prefab(CatPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(catCfg.MaxHp))
            .Set(new InvincibleOnHit(catCfg.InvincibleDuration))
            .Set(new HitFlashSettings(catCfg.HitFlashDuration))
            .Set(new CollisionBox(catCfg.CollisionWidth, catCfg.CollisionHeight))
            .Set(new Hurtbox(new CollisionShape.Circle(catCfg.HurtboxRadius, 0.0f, catCfg.HurtboxOffsetY), Faction.Friendly))
            .Set(new Transform(0.0f, 0.0f, catCfg.ScaleX, catCfg.ScaleY))
            .Set(new SpringScale(1.0f, 1.0f, catCfg.SpringFrequency, catCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(ConfigManager.Cat))
            .Set(CatWeaponState.FromConfig(ConfigManager.Cat));

        // --- Bunny プレイヤー ---
        var bunnyCfg = ConfigManager.BunnyPrefab;
        BunnyPlayer = world.Prefab(BunnyPlayerName)
            .IsA(PlayerBase)
            .Set(new Health(bunnyCfg.MaxHp))
            .Set(new InvincibleOnHit(bunnyCfg.InvincibleDuration))
            .Set(new HitFlashSettings(bunnyCfg.HitFlashDuration))
            .Set(new CollisionBox(bunnyCfg.CollisionWidth, bunnyCfg.CollisionHeight))
            .Set(new Hurtbox(new CollisionShape.Circle(bunnyCfg.HurtboxRadius, 0.0f, bunnyCfg.HurtboxOffsetY), Faction.Friendly))
            .Set(new Transform(0.0f, 0.0f, bunnyCfg.ScaleX, bunnyCfg.ScaleY))
            .Set(new SpringScale(1.0f, 1.0f, bunnyCfg.SpringFrequency, bunnyCfg.SpringDampingRatio))
            .Set(JumpCapability.FromPlayerConfig(ConfigManager.Bunny))
            .Set(BunnyWeaponState.FromConfig(ConfigManager.Bunny));
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
