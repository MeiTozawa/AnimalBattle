using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Configs;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 1 専用自治エージェントです。
///     空中浮遊機動、全方位弾幕、および急降下攻撃を Boss1Brain と直接連携して管理します。
/// </summary>
public sealed class Boss1Agent : BaseBossAgent
{
    private readonly Boss1Brain _brain;

    /// <summary>
    ///     Boss 1 の Flecs エンティティおよびテクスチャハンドルを指定してエージェントを初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    public Boss1Agent(Entity entity, int spriteHandle) : base(entity)
    {
        var world = entity.CsWorld();
        var b1Cfg = world.Get<Boss1Config>();
        _brain = new Boss1Brain(world, b1Cfg.SpawnX, b1Cfg.SpawnY);

        BindBrainEvents(_brain);
        InitializeComponents(entity, spriteHandle);
    }

    /// <inheritdoc />
    public override IBossBrain Brain => _brain;

    /// <inheritdoc />
    public override BossType BossType => BossType.Boss1;

    /// <inheritdoc />
    public override (float Phase2Threshold, float Phase3Threshold) PhaseThresholds {
        get {
            var b1Cfg = Entity.CsWorld().Get<Boss1Config>();
            return (b1Cfg.Phase2.HpThreshold, b1Cfg.Phase3.HpThreshold);
        }
    }

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b1Cfg = entity.CsWorld().Get<Boss1Config>();
        InitializeComponents(
            entity,
            spriteHandle,
            b1Cfg.SpawnX,
            b1Cfg.SpawnY,
            b1Cfg.ScaleX,
            b1Cfg.ScaleY);
    }

    /// <inheritdoc />
    public override void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        SetCurrentWorld(world);

        if (CheckAndHandleDeath()) return;

        ref readonly var health = ref Entity.Get<Health>();
        _brain.Update(deltaTime, playerPosition, 1, true, health.Current, health.Max);

        ref var trans = ref Entity.GetMut<Transform>();
        trans.X = _brain.Position.X;
        trans.Y = _brain.Position.Y;
        ref var sprite = ref Entity.GetMut<Sprite>();
        sprite.FlipX = !_brain.FacingRight;

        OnPostUpdate(deltaTime, world);
        SyncCombatState();
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
        ref var t = ref Entity.GetMut<Transform>();
        var fx = world.Get<RenderConfig>().BossEffects;
        t.Rotation = (float)(MathF.Sin(_brain.HoverTimer * fx.BossHoverAngleSpeed)
            * fx.BossHoverAngleAmp);
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b1Cfg = Entity.CsWorld().Get<Boss1Config>();
        SetTransform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;
        ResetHealth(b1Cfg.MaxHp);
        Entity.Set(new CollisionBox(b1Cfg.CollisionWidth, b1Cfg.CollisionHeight));
        Entity.Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile));
        Entity.Set(new Hitbox(b1Cfg.GetHitboxShape(), b1Cfg.BodyContactDamage, Faction.Hostile, false));
        Entity.Set(new HitFlashSettings(b1Cfg.HitFlashDuration));

        _brain.Reset(b1Cfg.SpawnX, b1Cfg.SpawnY);
    }

    /// <inheritdoc />
    protected override void HandleEmitBulletWave(StringId skill, Vec2f origin, Vec2f target)
    {
        if (CurrentWorld == default) return;
        BulletEmitter.EmitBoss1(CurrentWorld, skill, origin, target, ref EmitterAngleOffset);
    }
}
