using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Configs;
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
        var b2Cfg = ConfigManager.Boss2;
        _brain = new Boss2Brain(b2Cfg.SpawnX, b2Cfg.SpawnY, null, navGraphProvider);

        _brain.OnCreateHitbox += HandleCreateHitbox;
        BindBrainEvents(_brain);

        InitializeComponents(entity, spriteHandle);
    }

    /// <inheritdoc />
    public override IBossBrain Brain => _brain;

    /// <inheritdoc />
    public override BossType BossType => BossType.Boss2;

    private void InitializeComponents(Entity entity, int spriteHandle)
    {
        var b2Cfg = ConfigManager.Boss2;
        InitializeComponents(
            entity,
            spriteHandle,
            b2Cfg.SpawnX,
            b2Cfg.SpawnY,
            b2Cfg.ScaleX,
            b2Cfg.ScaleY);
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
            new Vec2f(trans.X, trans.Y));

        vel.Vx = _brain.Velocity.X;
        var isControlledAirborne = _brain.State == Boss2State.PlatformTraverse
            || (_brain.AbilitySystem.IsCasting && _brain.CurrentSkillTag == _brain.Configuration.TargetLockSkillTag);

        if (isControlledAirborne || isGrounded)
        {
            vel.Vy = _brain.Velocity.Y;
        }

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
        var b2Cfg = ConfigManager.Boss2;
        var arenaCfg = ConfigManager.Arena;

        // 空中落下物理：PlatformTraverse や LeapSmash などの制御空中移動時を除き、通常重力加速度を適用
        if (!col.IsGrounded && !isControlledAirborne)
        {
            var curVy = vel.Vy;
            PhysicsService.ApplyGravity(ref curVy, b2Cfg.Gravity, b2Cfg.MaxFallSpeed, dt);
            vel.Vy = curVy;
        }

        var colWidth = col.Width > 0 ? col.Width : b2Cfg.CollisionWidth;
        var colHeight = col.Height > 0 ? col.Height : b2Cfg.CollisionHeight;

        var minX = arenaCfg.BoundaryMinX + colWidth / 2.0f;
        var maxX = arenaCfg.BoundaryMaxX - colWidth / 2.0f;

        var nextX = trans.X + vel.Vx * dt;
        var nextY = trans.Y + vel.Vy * dt;

        nextX = Math.Clamp(nextX, minX, maxX);

        var currentY = trans.Y;
        Box2D bossBox = new(nextX, nextY, colWidth, colHeight);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        // 制御空中移動時かつ上昇・滞空（vel.Vy <= 0）の間は足場着地判定をスキップ
        var ignorePlatforms = isControlledAirborne && vel.Vy <= 0.0f;
        if (!ignorePlatforms)
        {
            var isLeapSmash = _brain.CurrentSkillTag == _brain.Configuration.TargetLockSkillTag;
            var targetLandingSurfaceY = _brain.TargetLandingSurfaceY;

            platformQuery.Each((ref platTrans, ref platCol) =>
            {
                var platTopY = platTrans.Y - platCol.Height / 2.0f;

                // 泰山圧頂急降下中：目標着地サーフェス（足場上面または床）より上層にある足場は貫通（スキップ）
                if (isLeapSmash && targetLandingSurfaceY > 0.0f && platTopY < targetLandingSurfaceY - 5.0f)
                {
                    return;
                }

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

        ref var vel = ref Entity.GetMut<Velocity>();
        vel.Vx = 0.0f;
        vel.Vy = 0.0f;

        _brain.Reset(b2Cfg.SpawnX, b2Cfg.SpawnY);
    }

    private void HandleCreateHitbox(StringId skill, float offsetX, float offsetY)
    {
        if (CurrentWorld == default) return;
        var patternCfg = ConfigManager.Boss2Patterns;
        ref readonly var trans = ref Entity.Get<Transform>();

        var forwardDir = _brain.FacingRight ? 1.0f : -1.0f;
        var colHeight = Entity.Get<CollisionBox>().Height;
        var vx = _brain.Velocity.X;

        if (patternCfg.TryGetSkill(skill, out var pattern))
        {
            pattern.SpawnHitbox(
                CurrentWorld,
                new Vec2f(trans.X, trans.Y),
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
        var colHeight = Entity.Get<CollisionBox>().Height;
        var forwardDir = _brain.FacingRight ? 1.0f : -1.0f;
        BulletEmitter.EmitBoss2(CurrentWorld, skill, origin, target, forwardDir, colHeight);
    }
}
