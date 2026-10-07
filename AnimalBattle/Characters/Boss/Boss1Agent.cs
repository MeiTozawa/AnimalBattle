using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 1 専用自治エージェントです。
///     空中浮遊機動、全方位弾幕、および急降下攻撃を Boss1Brain と直接連携して管理します。
/// </summary>
public sealed class Boss1Agent : BaseBossAgent
{
    private readonly Boss1Brain _brain;
    private World _currentWorld;

    /// <summary>
    ///     Boss 1 の Flecs エンティティおよびテクスチャハンドルを指定してエージェントを初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    public Boss1Agent(Entity entity, int spriteHandle) : base(entity)
    {
        var b1Cfg = ConfigManager.Boss1;
        _brain = new Boss1Brain(b1Cfg.SpawnX, b1Cfg.SpawnY);

        _brain.OnEmitBullet += HandleEmitBullet;
        _brain.OnClearBullets += HandleClearBullets;

        InitializeComponents(entity, spriteHandle);
    }

    /// <inheritdoc />
    public override BossType BossType => BossType.Boss1;

    /// <inheritdoc />
    public override Phase Phase => _brain.Phase;

    /// <inheritdoc />
    public override BossState CurrentState => BossState.From(_brain.State);

    /// <inheritdoc />
    public override BossSkill CurrentSkill => _brain.CurrentSkill;

    /// <inheritdoc />
    public override float StateTimer => _brain.StateTimer;

    /// <inheritdoc />
    public override float StateDuration => _brain.StateDuration;

    /// <inheritdoc />
    public override float HoverTimer => _brain.HoverTimer;

    /// <inheritdoc />
    public override float TargetX => _brain.TargetX;

    /// <inheritdoc />
    public override float TargetY => _brain.TargetY;

    /// <inheritdoc />
    public override bool IsVulnerable => _brain.IsVulnerable;

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b1Cfg = ConfigManager.Boss1;
        entity.Set(new Health(b1Cfg.MaxHp))
            .Set(new InvincibleOnHit(0.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new Transform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY))
            .Set(new Velocity(0.0f))
            .Set(new Hurtbox(b1Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b1Cfg.GetHitboxShape(), b1Cfg.BodyContactDamage, Faction.Hostile, false))
            .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new BossCombatState(IsVulnerable, Phase))
            .Add<BossTag>()
            .Add<Boss1Tag>();
    }

    /// <inheritdoc />
    public override void Update(
        float deltaTime,
        Vec2f playerPosition,
        Vec2f playerVelocity,
        World world,
        Query<Transform, CollisionBox> platformQuery)
    {
        _currentWorld = world;

        ref var health = ref Entity.GetMut<Health>();
        if (health.Current <= 0.0f)
        {
            if (!_brain.IsDead) _brain.Kill();
            return;
        }

        _brain.Update(deltaTime, playerPosition, 1, true, health.Current, health.Max);

        ref var trans = ref Entity.GetMut<Transform>();
        trans.X = _brain.Position.X;
        trans.Y = _brain.Position.Y;

        OnPostUpdate(deltaTime, world);

        Entity.Set(new BossCombatState(IsVulnerable, Phase));
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = (float)(MathF.Sin(_brain.HoverTimer * ConfigManager.Render.BossEffects.BossHoverAngleSpeed)
            * ConfigManager.Render.BossEffects.BossHoverAngleAmp);
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b1Cfg = ConfigManager.Boss1;
        SetTransform(b1Cfg.SpawnX, b1Cfg.SpawnY, b1Cfg.ScaleX, b1Cfg.ScaleY);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;
        ResetHealth(b1Cfg.MaxHp);

        _brain.Reset(b1Cfg.SpawnX, b1Cfg.SpawnY);
    }

    private void HandleEmitBullet(BossSkill skill, float vx, float vy)
    {
        if (_currentWorld == default) return;
        var patternCfg = ConfigManager.Boss1Patterns;
        ref readonly var trans = ref Entity.Get<Transform>();

        if (skill == BossSkill.RainDrop)
        {
            var bType = Boss1PatternConfig.ParseBulletType(patternCfg.RainDrop.BulletType, BulletType.Cyan);
            CombatEffectHelper.SpawnBossSpecialBullet(
                _currentWorld,
                vx,
                0.0f,
                0.0f,
                vy,
                patternCfg.RainDrop.BulletRadius,
                bType,
                patternCfg.RainDrop.FillColor,
                patternCfg.RainDrop.OutlineColor);
            return;
        }

        float radius;
        Color fill;
        Color outline;
        var type = BulletType.Normal;

        if (skill == BossSkill.Aimed3Shot)
        {
            radius = patternCfg.Aimed3Shot.BulletRadius;
            fill = patternCfg.Aimed3Shot.FillColor;
            outline = patternCfg.Aimed3Shot.OutlineColor;
            type = Boss1PatternConfig.ParseBulletType(patternCfg.Aimed3Shot.BulletType, BulletType.Cyan);
        }
        else if (skill == BossSkill.RingBurst)
        {
            radius = patternCfg.RingBurst.BulletRadius;
            fill = patternCfg.RingBurst.FillColor;
            outline = patternCfg.RingBurst.OutlineColor;
            type = Boss1PatternConfig.ParseBulletType(patternCfg.RingBurst.BulletType, BulletType.Magenta);
        }
        else if (skill == BossSkill.DoubleSpiral)
        {
            radius = patternCfg.DoubleSpiral.BulletRadius;
            fill = patternCfg.DoubleSpiral.FillColor1;
            outline = patternCfg.DoubleSpiral.OutlineColor1;
            type = Boss1PatternConfig.ParseBulletType(patternCfg.DoubleSpiral.BulletType, BulletType.Yellow);
        }
        else if (skill == BossSkill.CrossChaos)
        {
            radius = patternCfg.CrossChaos.BulletRadius;
            fill = patternCfg.CrossChaos.FillColor;
            outline = patternCfg.CrossChaos.OutlineColor;
            type = Boss1PatternConfig.ParseBulletType(patternCfg.CrossChaos.BulletType, BulletType.Magenta);
        }
        else if (skill == BossSkill.FastSniper)
        {
            radius = patternCfg.FastSniper.BulletRadius;
            fill = patternCfg.FastSniper.FillColor;
            outline = patternCfg.FastSniper.OutlineColor;
            type = Boss1PatternConfig.ParseBulletType(patternCfg.FastSniper.BulletType, BulletType.Black);
        }
        else
        {
            radius = 6.0f;
            fill = ColorPalette.RedBright;
            outline = ColorPalette.White;
        }

        CombatEffectHelper.SpawnBossSpecialBullet(_currentWorld, trans.X, trans.Y, vx, vy, radius, type, fill, outline);
    }

    private void HandleClearBullets()
    {
        if (_currentWorld != default)
        {
            CombatEffectHelper.ClearAllBossBullets(_currentWorld);
        }
    }
}
