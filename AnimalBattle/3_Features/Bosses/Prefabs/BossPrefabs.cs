using AnimalBattle.Ai;
using AnimalBattle.Combat;
using AnimalBattle.Common;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core.Geometry;
using Flecs.NET.Core;

namespace AnimalBattle.Bosses;

/// <summary>
///     Boss（Boss1 & Boss2）の Prefab 原型登録およびインスタンス化。
/// </summary>
public static class BossPrefabs
{
    public const string BossBaseName = "BossBasePrefab";
    public const string Boss1Name = "Boss1Prefab";
    public const string Boss2Name = "Boss2Prefab";

    public static Entity BossBase { get; private set; }
    public static Entity Boss1 { get; private set; }
    public static Entity Boss2 { get; private set; }

    /// <summary>
    ///     Boss 共通基底および各 Boss の Prefab 原型を登録します。
    /// </summary>
    public static void Register(World world)
    {
        // Boss 共通基底
        BossBase = world.Prefab(BossBaseName)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<BossTag>()
            .Set(new BossCombatState(Phase.Phase1));

        // --- Boss 1 ---
        var boss1Cfg = world.Get<Boss1PrefabConfig>();
        Boss1 = world.Prefab(Boss1Name)
            .IsA(BossBase)
            .Add<Boss1Tag>()
            .Set(new Health(boss1Cfg.MaxHp))
            .Set(new HitFlashSettings(boss1Cfg.HitFlashDuration))
            .Set(new Transform(0.0f, 0.0f, boss1Cfg.ScaleX, boss1Cfg.ScaleY))
            .Set(new Hurtbox(new CollisionShape.Circle(boss1Cfg.HurtboxRadius, 0.0f, boss1Cfg.HurtboxOffsetY), Faction.Hostile))
            .Set(new Hitbox(new CollisionShape.Circle(boss1Cfg.BodyHitboxRadius, 0.0f, boss1Cfg.BodyHitboxOffsetY), boss1Cfg.BodyContactDamage, Faction.Hostile, false));

        // --- Boss 2 ---
        var boss2Cfg = world.Get<Boss2PrefabConfig>();
        Boss2 = world.Prefab(Boss2Name)
            .IsA(BossBase)
            .Add<Boss2Tag>()
            .Set(new Health(boss2Cfg.MaxHp))
            .Set(new HitFlashSettings(boss2Cfg.HitFlashDuration))
            .Set(new Transform(0.0f, 0.0f, boss2Cfg.ScaleX, boss2Cfg.ScaleY))
            .Set(new CollisionBox(boss2Cfg.CollisionWidth, boss2Cfg.CollisionHeight))
            .Set(new Hurtbox(new CollisionShape.Box(boss2Cfg.HurtboxWidth, boss2Cfg.HurtboxHeight, 0.0f, boss2Cfg.HurtboxOffsetY), Faction.Hostile))
            .Set(new Hitbox(new CollisionShape.Box(boss2Cfg.BodyHitboxWidth, boss2Cfg.BodyHitboxHeight, 0.0f, boss2Cfg.BodyHitboxOffsetY), boss2Cfg.BodyContactDamage, Faction.Hostile, false));
    }

    /// <summary>
    ///     指定された BossType の Prefab 原型を取得します。
    /// </summary>
    public static Entity GetPrefab(BossType type)
    {
        return type switch {
            BossType.Boss1 => Boss1,
            BossType.Boss2 => Boss2,
            _ => AssertHelper.FailWithDefault("", Boss1)
        };
    }
}
