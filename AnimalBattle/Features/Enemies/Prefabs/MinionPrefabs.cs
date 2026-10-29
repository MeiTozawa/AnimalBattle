namespace AnimalBattle.Enemies;

/// <summary>
/// 雑魚敵のプレハブ登録および管理クラス。
/// </summary>
public static class MinionPrefabs
{
    public const string BaseMinionName = "BaseMinionPrefab";
    public const string GroundMinionName = "GroundMinionPrefab";
    public const string FlyingMinionName = "FlyingMinionPrefab";

    public static Entity BaseMinion { get; private set; }
    public static Entity GroundMinion { get; private set; }
    public static Entity FlyingMinion { get; private set; }

    /// <summary>
    /// 雑魚敵共通基底および各小怪のプレハブを Flecs ワールドへ登録します。
    /// </summary>
    public static void Register(World world)
    {
        var minionCfg = world.Get<MinionConfig>();
        Debug.Assert(minionCfg != null, "MinionConfig はワールドに登録されている必要があります。");

        // 雑魚敵共通基底
        BaseMinion = world.Prefab(BaseMinionName)
            .IsA(CommonPrefabs.BaseCharacter)
            .Add<EnemyTag>()
            .Add<MinionTag>()
            .Set(CombatProfile.ForMinion());

        // 地面近接追撃怪
        var gCfg = minionCfg.GroundMinion;
        GroundMinion = world.Prefab(GroundMinionName)
            .IsA(BaseMinion)
            .Add<GroundMinionTag>()
            .Set(new Health(gCfg.MaxHp))
            .Set(new HitFlashSettings(gCfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, gCfg.Scale))
            .Set(new CollisionBox(gCfg.CollisionSize))
            .Set(new Hurtbox(new CollisionShape.Box(gCfg.CollisionSize), Faction.Hostile))
            .Set(new Hitbox(new CollisionShape.Box(gCfg.CollisionSize), gCfg.ContactDamage, Faction.Hostile, destroyOnHit: false, isActive: true));

        // 空中浮遊弾幕怪
        var fCfg = minionCfg.FlyingMinion;
        FlyingMinion = world.Prefab(FlyingMinionName)
            .IsA(BaseMinion)
            .Add<FlyingMinionTag>()
            .Set(new Health(fCfg.MaxHp))
            .Set(new HitFlashSettings(fCfg.HitFlashDuration))
            .Set(new Transform(Vec2f.Zero, fCfg.Scale))
            .Set(new CollisionBox(fCfg.CollisionSize))
            .Set(new Hurtbox(new CollisionShape.Box(fCfg.CollisionSize), Faction.Hostile))
            .Set(new Hitbox(new CollisionShape.Box(fCfg.CollisionSize), fCfg.ContactDamage, Faction.Hostile, destroyOnHit: false, isActive: true));
    }
}
