using AnimalBattle.Ai;

namespace AnimalBattle.Bosses;

/// <summary>
///     Boss：BossSnake & BossLionの Prefab プレハブ登録およびインスタンス化。
/// </summary>
public static class BossPrefabs
{
    public const string BossBaseName = "BossBasePrefab";
    public const string BossSnakeName = "BossSnakePrefab";
    public const string BossLionName = "BossLionPrefab";

    public static Entity BossBase { get; private set; }
    public static Entity BossSnake { get; private set; }
    public static Entity BossLion { get; private set; }

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
            .Set(new BossCombatState(BossType.BossSnake));

        // --- BossSnake ---
        var bSnakeCfg = world.Get<BossSnakeConfig>();
        Debug.Assert(bSnakeCfg != null, "BossSnakeConfig はワールドに登録されている必要があります。");

        BossSnake = world.Prefab(BossSnakeName)
            .IsA(BossBase)
            .Add<BossSnakeTag>()
            .Set(new BossCombatState(BossType.BossSnake) { CanBeKnockedBack = bSnakeCfg.CanBeKnockedBack })
            .Set(CombatProfile.ForBoss(bSnakeCfg.CanBeKnockedBack, bSnakeCfg.HitSquashScale))
            .Set(new SpringScale(Vec2f.One, bSnakeCfg.SpringFrequency, bSnakeCfg.SpringDampingRatio))
            .Set(new Health(bSnakeCfg.MaxHp))
            .Set(new HitFlashSettings(bSnakeCfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, bSnakeCfg.Scale))
            .Set(new CollisionBox(bSnakeCfg.CollisionSize))
            .Set(new Hurtbox(bSnakeCfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(bSnakeCfg.GetHitboxShape(), bSnakeCfg.BodyContactDamage, Faction.Hostile, false));

        // --- BossLion ---
        var bLionCfg = world.Get<BossLionConfig>();
        Debug.Assert(bLionCfg != null, "BossLionConfig はワールドに登録されている必要があります。");

        BossLion = world.Prefab(BossLionName)
            .IsA(BossBase)
            .Add<BossLionTag>()
            .Set(new BossCombatState(BossType.BossLion) { CanBeKnockedBack = bLionCfg.CanBeKnockedBack })
            .Set(CombatProfile.ForBoss(bLionCfg.CanBeKnockedBack, bLionCfg.HitSquashScale))
            .Set(new SpringScale(Vec2f.One, bLionCfg.SpringFrequency, bLionCfg.SpringDampingRatio))
            .Set(new Health(bLionCfg.MaxHp))
            .Set(new HitFlashSettings(bLionCfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, bLionCfg.Scale))
            .Set(new CollisionBox(bLionCfg.CollisionSize))
            .Set(new Hurtbox(bLionCfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(bLionCfg.GetHitboxShape(), bLionCfg.BodyContactDamage, Faction.Hostile, false));
    }

    /// <summary>
    ///     指定された BossType の Prefab プレハブを取得します。
    /// </summary>
    public static Entity GetPrefab(BossType type)
    {
        return type switch {
            BossType.BossSnake => BossSnake,
            BossType.BossLion => BossLion,
            _ => AssertHelper.FailWithDefault("", BossSnake)
        };
    }
}
