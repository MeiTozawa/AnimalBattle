using System;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Maps.Navigation;
using AnimalBattle.Systems;
using Flecs.NET.Core;

namespace AnimalBattle.Characters.Boss;

/// <summary>
///     Boss 2 専用自治エージェントです。
///     地上巡回追跡、足場間自律跳躍、および近接攻撃・気絶を Boss2Brain と直接連携して管理します。
/// </summary>
public sealed class Boss2Agent : BaseBossAgent
{
    private readonly Boss2Brain _brain;

    /// <summary>
    ///     Boss 2 の Flecs エンティティ、テクスチャハンドル、およびナビゲーショングラフプロバイダーを指定して初期化します。
    /// </summary>
    /// <param name="entity">対応する Flecs エンティティ句柄。</param>
    /// <param name="spriteHandle">テクスチャハンドル。</param>
    /// <param name="navGraphProvider">足場尋路用ナビゲーショングラフプロバイダー。</param>
    public Boss2Agent(
        Entity entity,
        int spriteHandle,
        Func<PlatformNavigationGraph?>? navGraphProvider = null)
        : base(entity)
    {
        var world = entity.CsWorld();
        var b2Cfg = world.Get<Boss2Config>();
        _brain = new Boss2Brain(world, b2Cfg.Spawn, null, navGraphProvider);

        _brain.OnCreateHitbox += HandleCreateHitbox;
        BindBrainEvents(_brain);

        InitializeComponents(entity, spriteHandle);
    }

    /// <inheritdoc />
    public override IBossBrain Brain => _brain;

    /// <inheritdoc />
    public override BossType BossType => BossType.Boss2;

    /// <inheritdoc />
    public override (float Phase2Threshold, float Phase3Threshold) PhaseThresholds {
        get {
            var b2Cfg = Entity.CsWorld().Get<Boss2Config>();
            return (b2Cfg.Phase2.HpThreshold, b2Cfg.Phase3.HpThreshold);
        }
    }

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b2Cfg = entity.CsWorld().Get<Boss2Config>();
        InitializeComponents(
            entity,
            spriteHandle,
            b2Cfg.Spawn,
            b2Cfg.Scale);
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
        ref var col = ref Entity.GetMut<CollisionBox>();
        ref var trans = ref Entity.GetMut<Transform>();
        ref var vel = ref Entity.GetMut<Velocity>();
        var isGrounded = col.IsGrounded;

        _brain.Update(
            deltaTime,
            playerPosition,
            playerVelocity,
            true,
            isGrounded,
            health.Current,
            health.Max,
            trans.Position);

        var currentLinear = vel.Value;
        currentLinear = new Vec2f(_brain.Velocity.X, currentLinear.Y);
        var isControlledAirborne = _brain.State == Boss2State.PlatformTraverse
            || (_brain.AbilitySystem.IsCasting && _brain.CurrentSkillTag == _brain.Configuration.TargetLockSkillTag);

        if (isControlledAirborne || isGrounded)
        {
            currentLinear = new Vec2f(currentLinear.X, _brain.Velocity.Y);
        }
        vel.Value = currentLinear;

        UpdateMovementAndCollision(ref trans, ref vel, ref col, deltaTime, platformQuery, isControlledAirborne);

        ref var sprite = ref Entity.GetMut<Sprite>();
        sprite.FlipX = !_brain.FacingRight;

        OnPostUpdate(deltaTime, world);
        SyncCombatState();
    }

    private void UpdateMovementAndCollision(
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        float dt,
        Query<Transform, CollisionBox> platformQuery,
        bool isControlledAirborne)
    {
        var b2Cfg = Entity.CsWorld().Get<Boss2Config>();
        var arenaCfg = Entity.CsWorld().Get<ArenaConfig>();

        var linear = vel.Value;

        // 空中落下物理：PlatformTraverse や LeapSmash などの制御空中移動時を除き、通常重力加速度を適用
        if (!col.IsGrounded && !isControlledAirborne)
        {
            var curVy = linear.Y;
            PhysicsService.ApplyGravity(ref curVy, b2Cfg.Gravity, b2Cfg.MaxFallSpeed, dt);
            linear = new Vec2f(linear.X, curVy);
            vel.Value = linear;
        }

        var colSize = col.Size != Vec2f.Zero ? col.Size : b2Cfg.CollisionSize;

        var minX = arenaCfg.BoundaryMinX + colSize.X * 0.5f;
        var maxX = arenaCfg.BoundaryMaxX - colSize.X * 0.5f;

        var nextX = Math.Clamp(trans.Position.X + linear.X * dt, minX, maxX);
        var nextY = trans.Position.Y + linear.Y * dt;
        var nextPos = new Vec2f(nextX, nextY);

        var currentY = trans.Position.Y;
        Box2D bossBox = new(nextPos, colSize);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        // 制御空中移動時かつ上昇・滞空（vel.Linear.Y <= 0）の間は足場着地判定をスキップ
        var ignorePlatforms = isControlledAirborne && linear.Y <= 0.0f;
        if (!ignorePlatforms)
        {
            var isLeapSmash = _brain.CurrentSkillTag == _brain.Configuration.TargetLockSkillTag;
            var targetLandingSurfaceY = _brain.TargetLandingSurfaceY;

            platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
            {
                var platSize = platCol.Size;
                var platTopY = platTrans.Position.Y - platSize.Y * 0.5f;

                // 泰山圧頂急降下中：目標着地サーフェス（足場上面または床）より上層にある足場は貫通（スキップ）
                if (isLeapSmash && targetLandingSurfaceY > 0.0f && platTopY < targetLandingSurfaceY - 5.0f)
                {
                    return;
                }

                Box2D platformBox = new(platTrans.Position, platSize);
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
        var adjustedVy = hitPlatform ? platformVy : linear.Y;

        var floorLimitY = arenaCfg.FloorLimitY - colSize.Y * 0.5f;
        if (adjustedPosY >= floorLimitY)
        {
            isGrounded = true;
            adjustedPosY = floorLimitY;
            adjustedVy = 0.0f;
        }

        trans.Position = new Vec2f(nextX, adjustedPosY);
        vel.Value = new Vec2f(linear.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }

    /// <inheritdoc />
    protected override void OnPostUpdate(float deltaTime, World world)
    {
    }

    /// <inheritdoc />
    protected override void OnReset()
    {
        var b2Cfg = Entity.CsWorld().Get<Boss2Config>();
        SetTransform(b2Cfg.Spawn, b2Cfg.Scale);
        ref var t = ref Entity.GetMut<Transform>();
        t.Rotation = 0.0f;

        ResetHealth(b2Cfg.MaxHp);

        Entity.Set(new CollisionBox(b2Cfg.CollisionSize));
        Entity.Set(new Hurtbox(b2Cfg.GetHurtboxShape(), Faction.Hostile));
        Entity.Set(new Hitbox(b2Cfg.GetHitboxShape(), b2Cfg.BodyContactDamage, Faction.Hostile, false));
        Entity.Set(new HitFlashSettings(b2Cfg.HitFlashDuration));

        SetVelocity(Vec2f.Zero);

        _brain.Reset(b2Cfg.Spawn);
    }

    private void HandleCreateHitbox(StringId skill, float offsetX, float offsetY)
    {
        if (CurrentWorld == default) return;
        var patternCfg = CurrentWorld.Get<Boss2PatternConfig>();
        ref readonly var trans = ref Entity.Get<Transform>();

        var forwardDir = _brain.FacingRight ? 1.0f : -1.0f;
        var colHeight = Entity.Get<CollisionBox>().Size.Y;
        var vx = _brain.Velocity.X;

        if (patternCfg.TryGetSkill(skill, out var pattern))
        {
            pattern.SpawnHitbox(
                CurrentWorld,
                trans.Position,
                forwardDir,
                vx,
                colHeight,
                offsetX,
                offsetY,
                _brain.TargetPosition);
        }
    }

    /// <inheritdoc />
    protected override void HandleEmitBulletWave(StringId skill, Vec2f origin, Vec2f target)
    {
        if (CurrentWorld == default) return;
        var colHeight = Entity.Get<CollisionBox>().Size.Y;
        var forwardDir = _brain.FacingRight ? 1.0f : -1.0f;
        BulletEmitter.EmitBoss2(CurrentWorld, skill, origin, target, forwardDir, colHeight);
    }
}
