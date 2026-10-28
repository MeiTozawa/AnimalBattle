using AnimalBattle.Ai;

namespace AnimalBattle.Bosses;

/// <summary>
///     Boss：Boss1 & Boss2の Prefab プレハブ登録およびインスタンス化。
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
    ///     Boss 共通基底および各 Boss の Prefab プレハブを登録します。
    /// </summary>
    public static void Register(World world)
    {
        // Boss 共通基底
        BossBase = world.Prefab(BossBaseName)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<BossTag>()
            .Add<EnemyTag>()
            .Set(new BossCombatState(BossType.Boss1));

        // --- Boss 1 ---
        var b1Cfg = world.Get<Boss1Config>();
        Debug.Assert(b1Cfg != null, "Boss1Config はワールドに登録されている必要があります。");

        Boss1 = world.Prefab(Boss1Name)
            .IsA(BossBase)
            .Add<Boss1Tag>()
            .Set(new BossCombatState(BossType.Boss1) { CanBeKnockedBack = b1Cfg.CanBeKnockedBack })
            .Set(EnemyCombatProfile.ForBoss(b1Cfg.CanBeKnockedBack))
            .Set(new Health(b1Cfg.MaxHp))
            .Set(new HitFlashSettings(b1Cfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, b1Cfg.Scale))
            .Set(new CollisionBox(b1Cfg.CollisionSize))
            .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b1Cfg.GetHitboxShape(), b1Cfg.BodyContactDamage, Faction.Hostile, false));

        // --- Boss 2 ---
        var b2Cfg = world.Get<Boss2Config>();
        Debug.Assert(b2Cfg != null, "Boss2Config はワールドに登録されている必要があります。");

        Boss2 = world.Prefab(Boss2Name)
            .IsA(BossBase)
            .Add<Boss2Tag>()
            .Set(new BossCombatState(BossType.Boss2) { CanBeKnockedBack = b2Cfg.CanBeKnockedBack })
            .Set(EnemyCombatProfile.ForBoss(b2Cfg.CanBeKnockedBack))
            .Set(new Health(b2Cfg.MaxHp))
            .Set(new HitFlashSettings(b2Cfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, b2Cfg.Scale))
            .Set(new CollisionBox(b2Cfg.CollisionSize))
            .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b2Cfg.GetHitboxShape(), b2Cfg.BodyContactDamage, Faction.Hostile, false));
    }

    /// <summary>
    ///     指定された BossType の Prefab プレハブを取得します。
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
