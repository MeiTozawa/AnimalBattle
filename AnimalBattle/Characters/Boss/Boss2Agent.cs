using System.Diagnostics;
using System.Drawing;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Ai.Contracts;
using AnimalBattle.Configs;
using AnimalBattle.Maps.Navigation;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 2（Ground Crusher）専用自治エージェント（Pawn）。
///     地上巡回追跡、足場間自律跳躍、および近接攻撃・気絶を Boss2Brain と直接連携して管理します。
/// </summary>
public sealed class Boss2Agent : BaseBossAgent
{
    private readonly Boss2Brain _brain;
    private World _currentWorld;

    /// <summary>
    ///     Boss 2 の Flecs エンティティ、テクスチャハンドル、およびナビゲーショングラフプロバイダーを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">足場尋路用ナビゲーショングラフプロバイダー（省略可能）。</param>
    public Boss2Agent(
        Entity entity,
        int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
        : base(entity)
    {
        var b2Cfg = ConfigManager.Boss2;
        _brain = new Boss2Brain(b2Cfg.SpawnX, b2Cfg.SpawnY, null, navGraphProvider);

        _brain.OnCreateHitbox += HandleCreateHitbox;
        _brain.OnEmitBullet += HandleEmitBullet;
        _brain.OnClearBullets += HandleClearBullets;

        InitializeComponents(entity, spriteHandle);
    }

    /// <inheritdoc />
    public override BossType BossType => BossType.Boss2;

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
    public override bool IsVulnerable => _brain.IsVulnerable;

    /// <inheritdoc />
    public override bool FacingRight => _brain.FacingRight;

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b2Cfg = ConfigManager.Boss2;
        entity.Set(new Health(b2Cfg.MaxHp))
            .Set(new InvincibleOnHit(0.0f))
            .Set(new HitFlashConfig(0.08f, Color.White))
            .Set(new Transform(b2Cfg.SpawnX, b2Cfg.SpawnY, b2Cfg.ScaleX, b2Cfg.ScaleY))
            .Set(new Velocity(0.0f))
            .Set(new CollisionBox(b2Cfg.CollisionWidth, b2Cfg.CollisionHeight))
            .Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile))
            .Set(new Hitbox(b2Cfg.GetHitboxShape(), b2Cfg.BodyContactDamage, Faction.Hostile, false))
            .Set(new Sprite(spriteHandle, false, true, new Rect(0, 0, 192, 192)))
            .Set(new BossCombatState(IsVulnerable, Phase))
            .Add<BossTag>()
            .Add<Boss2Tag>();
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

        try
        {
            ref var col = ref Entity.GetMut<CollisionBox>();
            var isGrounded = col.IsGrounded;
            var isOnWall = col.IsOnWall;

            _brain.Update(
                deltaTime,
                playerPosition,
                playerVelocity,
                true,
                isGrounded,
                isOnWall,
                health.Current,
                health.Max);

            ref var trans = ref Entity.GetMut<Transform>();
            ref var vel = ref Entity.GetMut<Velocity>();

            vel.Vx = _brain.Velocity.X;
            vel.Vy = _brain.Velocity.Y;

            UpdateMovementAndCollision(ref trans, ref vel, ref col, deltaTime, platformQuery);

            if (Entity.Has<Sprite>())
            {
                ref var sprite = ref Entity.GetMut<Sprite>();
                sprite.FlipX = !_brain.FacingRight;
            }

            OnPostUpdate(deltaTime, world);

            Entity.Set(new BossCombatState(IsVulnerable, Phase));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Boss2Agent 警告] Update 処理中に例外が発生しました: {ex.Message}");
        }
    }

    private void UpdateMovementAndCollision(
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        float dt,
        Query<Transform, CollisionBox> platformQuery)
    {
        var b2Cfg = ConfigManager.Boss2;
        var arenaCfg = ConfigManager.Arena;

        var nextX = trans.X + vel.Vx * dt;
        var nextY = trans.Y + vel.Vy * dt;
        nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

        var currentY = trans.Y;
        var colWidth = col.Width > 0 ? col.Width : b2Cfg.CollisionWidth;
        var colHeight = col.Height > 0 ? col.Height : b2Cfg.CollisionHeight;
        Box2D bossBox = new(nextX, nextY, colWidth, colHeight);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        var ignorePlatforms = _brain.CurrentSkill == BossSkill.LeapSmash;
        if (!ignorePlatforms)
        {
            platformQuery.Each((ref platTrans, ref platCol) =>
            {
                Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
                if (PhysicsService.CheckPlatformGround(currentY, nextY, bossBox, platformBox, out var groundY, out var pVy))
                {
                    if (!hitPlatform || groundY < platformGroundY)
                    {
                        hitPlatform = true;
                        platformGroundY = groundY;
                        platformVy = pVy;
                    }
                }
            });
        }

        var isGrounded = hitPlatform;
        var adjustedPosY = hitPlatform ? platformGroundY : nextY;
        var adjustedVy = hitPlatform ? platformVy : vel.Vy;

        var floorLimitY = arenaCfg.FloorLimitY - colHeight / 2.0f;
        if (adjustedPosY >= floorLimitY)
        {
            isGrounded = true;
            adjustedPosY = floorLimitY;
            adjustedVy = 0.0f;
        }

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;
        col.IsGrounded = isGrounded;
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b2Cfg = ConfigManager.Boss2;
        SetTransform(b2Cfg.SpawnX, b2Cfg.SpawnY, b2Cfg.ScaleX, b2Cfg.ScaleY);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;

        ResetHealth(b2Cfg.MaxHp);

        ref var col = ref Entity.GetMut<CollisionBox>();
        col.IsGrounded = false;
        col.IsOnWall = false;

        _brain.Reset(b2Cfg.SpawnX, b2Cfg.SpawnY);
    }

    private void HandleCreateHitbox(BossSkill skill, float offsetX, float offsetY)
    {
        if (_currentWorld == default) return;
        var patternCfg = ConfigManager.Boss2Patterns;
        var b2Cfg = ConfigManager.Boss2;
        ref readonly var trans = ref Entity.Get<Transform>();
        ref readonly var vel = ref Entity.Get<Velocity>();

        var forwardDir = _brain.FacingRight ? 1.0f : -1.0f;
        var dir = _brain.FacingRight ? SlashDirection.Right : SlashDirection.Left;

        if (skill == BossSkill.HeavySlash)
        {
            var offX = offsetX != 0.0f ? offsetX : forwardDir * b2Cfg.SlashSpawnOffsetX;
            var offY = offsetY != 0.0f ? offsetY : b2Cfg.SlashSpawnOffsetY;
            CombatEffectHelper.SpawnMeleeSlashHitbox(
                _currentWorld,
                trans.X + offX,
                trans.Y + offY,
                vel.Vx,
                0.0f,
                patternCfg.HeavySlash.SlashRangeX,
                patternCfg.HeavySlash.SlashRangeY,
                patternCfg.HeavySlash.SlashDamage,
                patternCfg.HeavySlash.HitboxDuration,
                patternCfg.HeavySlash.SlashColor,
                dir);
        }
        else if (skill == BossSkill.DoubleCleave)
        {
            var offX = offsetX != 0.0f ? offsetX : forwardDir * b2Cfg.SlashSpawnOffsetX;
            var offY = offsetY != 0.0f ? offsetY : b2Cfg.SlashSpawnOffsetY;
            CombatEffectHelper.SpawnMeleeSlashHitbox(
                _currentWorld,
                trans.X + offX,
                trans.Y + offY,
                vel.Vx,
                0.0f,
                patternCfg.DoubleCleave.SlashRangeX,
                patternCfg.DoubleCleave.SlashRangeY,
                patternCfg.DoubleCleave.SlashDamage,
                patternCfg.DoubleCleave.HitboxDuration,
                patternCfg.DoubleCleave.SlashColor,
                dir);
        }
        else if (skill == BossSkill.GroundStomp)
        {
            var colHeight = Entity.Has<CollisionBox>() ? Entity.Get<CollisionBox>().Height : b2Cfg.CollisionHeight;
            var groundY = trans.Y + colHeight / 2.0f - patternCfg.GroundStomp.GroundOffsetY;
            CombatEffectHelper.SpawnShockwave(_currentWorld, trans.X, groundY);
            CombatEffectHelper.SpawnGroundShockwave(
                _currentWorld,
                trans.X - patternCfg.GroundStomp.ShockwaveOffsetX,
                groundY,
                -patternCfg.GroundStomp.ShockwaveSpeed,
                patternCfg.GroundStomp.ShockwaveRadius,
                patternCfg.GroundStomp.ShockwaveDamage,
                patternCfg.GroundStomp.FillColor,
                patternCfg.GroundStomp.OutlineColor);
            CombatEffectHelper.SpawnGroundShockwave(
                _currentWorld,
                trans.X + patternCfg.GroundStomp.ShockwaveOffsetX,
                groundY,
                patternCfg.GroundStomp.ShockwaveSpeed,
                patternCfg.GroundStomp.ShockwaveRadius,
                patternCfg.GroundStomp.ShockwaveDamage,
                patternCfg.GroundStomp.FillColor,
                patternCfg.GroundStomp.OutlineColor);
        }
        else if (skill == BossSkill.LeapSmash)
        {
            var colHeight = Entity.Has<CollisionBox>() ? Entity.Get<CollisionBox>().Height : b2Cfg.CollisionHeight;
            var groundY = trans.Y + colHeight / 2.0f;
            CombatEffectHelper.SpawnShockwave(_currentWorld, trans.X, groundY);

            var leapCfg = patternCfg.LeapSmash;
            for (var k = 0; k < leapCfg.ShrapnelCount; k++)
            {
                var angle = -MathF.PI * (leapCfg.ShrapnelAngleStartRatio + leapCfg.ShrapnelAngleSpanRatio * (k / (float)(leapCfg.ShrapnelCount - 1)));
                CombatEffectHelper.SpawnBossBullet(
                    _currentWorld,
                    trans.X,
                    groundY - leapCfg.ShrapnelOffsetY,
                    MathF.Cos(angle) * leapCfg.ShrapnelSpeed,
                    MathF.Sin(angle) * leapCfg.ShrapnelSpeed,
                    leapCfg.ShrapnelRadius,
                    leapCfg.ShrapnelFillColor,
                    leapCfg.ShrapnelOutlineColor);
            }
        }
    }

    private void HandleEmitBullet(BossSkill skill, float vx, float vy)
    {
        if (_currentWorld == default) return;
        var patternCfg = ConfigManager.Boss2Patterns;

        if (skill == BossSkill.EarthquakeRoar)
        {
            var rand = Random.Shared;
            float rx = rand.Next((int)patternCfg.EarthquakeRoar.MinX, (int)patternCfg.EarthquakeRoar.MaxX);
            CombatEffectHelper.SpawnBossSpecialBullet(
                _currentWorld,
                rx,
                0.0f,
                0.0f,
                vy,
                patternCfg.EarthquakeRoar.RockRadius,
                BulletType.Normal,
                patternCfg.EarthquakeRoar.RockFillColor,
                patternCfg.EarthquakeRoar.RockOutlineColor);
        }
    }

    private void HandleClearBullets()
    {
        if (_currentWorld != default)
        {
            CombatEffectHelper.ClearAllBossBullets(_currentWorld);
        }
    }
}
