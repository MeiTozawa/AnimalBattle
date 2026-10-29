namespace AnimalBattle.Enemies;

/// <summary>
/// 雑魚敵を動的に生成および配置するスポナークラス。
/// </summary>
public static class MinionSpawner
{
    private static int _groundMinionCounter;
    private static int _flyingMinionCounter;

    /// <summary>
    /// 地面近接追撃怪をワールドに生成します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    /// <param name="root">親ルートエンティティ</param>
    /// <param name="pos">初期配置座標</param>
    /// <returns>生成されたエンティティ</returns>
    public static Entity SpawnGroundMinion(World world, Entity root, Vec2f pos)
    {
        var assets = world.Get<IAssetManager>();
        var texPath = ResourcePath.Characters.Character027_3Png;
        assets.TryGetTexture(texPath, out var texHandle);

        var minionCfg = world.Get<MinionConfig>();
        var gCfg = minionCfg.GroundMinion;

        var name = $"GroundMinion_{Interlocked.Increment(ref _groundMinionCounter)}";
        var entity = world.Entity(name)
            .ChildOf(root)
            .IsA(MinionPrefabs.GroundMinion)
            .Set(new Transform(pos, gCfg.Scale))
            .Set(new Sprite(texHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new CollisionBox(gCfg.CollisionSize))
            .Set(new Health(gCfg.MaxHp))
            .Add<GroundMinionTag>()
            .Set(new GroundMinionComponent());

        return entity;
    }

    /// <summary>
    /// 空中浮遊弾幕怪をワールドに生成し、RingBurst 技能を注入します。
    /// </summary>
    /// <param name="world">対象の Flecs ワールド</param>
    /// <param name="root">親ルートエンティティ</param>
    /// <param name="pos">初期配置座標</param>
    /// <returns>生成されたエンティティ</returns>
    public static Entity SpawnFlyingMinion(World world, Entity root, Vec2f pos)
    {
        var assets = world.Get<IAssetManager>();
        var texPath = ResourcePath.Characters.Character031_3Png;
        assets.TryGetTexture(texPath, out var texHandle);

        var minionCfg = world.Get<MinionConfig>();
        var fCfg = minionCfg.FlyingMinion;
        var patternCfg = world.Get<Boss1PatternConfig>();

        var asc = new AbilitySystemComponent();
        var abilityResult = SkillBuilder.BuildAbility(fCfg.SkillTag, patternCfg, fCfg.SkillCooldown);
        if (abilityResult.IsError)
        {
            Debug.Assert(false, $"FlyingMinion のスキル構築に失敗しました: {fCfg.SkillTag}");
        }

        var name = $"FlyingMinion_{Interlocked.Increment(ref _flyingMinionCounter)}";
        var entity = world.Entity(name)
            .ChildOf(root)
            .IsA(MinionPrefabs.FlyingMinion)
            .Set(new Transform(pos, fCfg.Scale))
            .Set(new Sprite(texHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new Velocity(Vec2f.Zero))
            .Set(new CollisionBox(fCfg.CollisionSize))
            .Set(new Health(fCfg.MaxHp))
            .Add<FlyingMinionTag>()
            .Set(new FlyingMinionComponent(asc));

        if (!abilityResult.IsError)
        {
            var ability = abilityResult.Value;
            var angleOffset = 0.0f;
            ability.ActionDispatcher = (step, dir) =>
            {
                if (step.Effect != null)
                {
                    var origin = entity.IsAlive() && entity.Has<Transform>()
                        ? entity.Get<Transform>().Position
                        : pos;
                    var ctx = new SkillExecutionContext
                    {
                        World = world,
                        SkillTag = ability.AbilityTag,
                        Origin = origin,
                        TargetPos = Vec2f.Zero,
                        ForwardDir = dir,
                        CasterVelocity = Vec2f.Zero,
                        BodyHeight = fCfg.CollisionSize.Y,
                        SetVelocity = null
                    };
                    SkillEffectDispatcher.Execute(in ctx, step.Effect, ref angleOffset);
                }
            };
            asc.GiveAbility(ability);
        }

        asc.OnTagsChanged += tags =>
        {
            if (entity.IsAlive() && entity.Has<GameplayTagState>())
            {
                entity.GetMut<GameplayTagState>().ActionMask = tags;
            }
        };

        return entity;
    }
}
