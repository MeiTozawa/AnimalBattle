using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     汎用キネマティック運動物理システム。
///     <see cref="MovementIntent" /> を消費し、重力、跳躍インパルス、足場衝突および画面境界を統一的に処理します。
/// </summary>
public static class MovementSystem {
    /// <summary>
    ///     キネマティック運動物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <param name="platformQuery">足場プラットフォーム検索クエリ（省略時は内部生成）</param>
    /// <param name="arenaCfg">アリーナ境界設定（省略時は ConfigManager.Arena）</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddMovementSystem(
        this World world,
        Query<Transform, CollisionBox>? platformQuery = null,
        ArenaConfig? arenaCfg = null)
    {
        var pQuery = platformQuery ?? world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();

        world.System<Transform, Velocity, CollisionBox, MovementIntent, JumpCapability>("MovementSystem")
           .Kind(Ecs.OnUpdate)
           .Without<DeadTag>()
           .Iter((Iter it,
                Field<Transform> transforms,
                Field<Velocity> velocities,
                Field<CollisionBox> collisionBoxes,
                Field<MovementIntent> intents,
                Field<JumpCapability> jumps) =>
            {
                var dt = it.DeltaTime();
                var count = it.Count();
                var cfg = arenaCfg ?? ConfigManager.Arena;

                for (var i = 0; i < count; i++)
                {
                    ref var trans = ref transforms[i];
                    ref var vel = ref velocities[i];
                    ref var col = ref collisionBoxes[i];
                    ref var intent = ref intents[i];
                    ref var jump = ref jumps[i];

                    ExecuteKinematics(ref trans, ref vel, ref col, in intent, ref jump, dt, pQuery, cfg, world);
                }
            });

        return world;
    }

    /// <summary>
    ///     単一エンティティのキネマティック運動物理を 1 フレーム分更新します（テストおよび直接呼び出し互換用）。
    /// </summary>
    public static void UpdateEntity(
        Entity entity,
        float dt,
        Query<Transform, CollisionBox> platformQuery,
        ArenaConfig? arenaCfg = null,
        World? effectWorld = null)
    {
        if (!entity.IsAlive()) return;

        ref var trans = ref entity.GetMut<Transform>();
        ref var vel = ref entity.GetMut<Velocity>();
        ref var col = ref entity.GetMut<CollisionBox>();
        ref var intent = ref entity.GetMut<MovementIntent>();
        ref var jump = ref entity.GetMut<JumpCapability>();

        ExecuteKinematics(ref trans, ref vel, ref col, in intent, ref jump, dt, platformQuery, arenaCfg, effectWorld);
    }

    private static void ExecuteKinematics(
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        in MovementIntent intent,
        ref JumpCapability jump,
        float dt,
        Query<Transform, CollisionBox> platformQuery,
        ArenaConfig? arenaCfg,
        World? effectWorld)
    {
        // 1. 水平移動意図の適用
        vel.Vx = intent.MoveX * jump.MoveSpeed;

        // 2. 先行入力およびコヨーテタイムの更新
        if (intent.JumpTriggered)
        {
            jump.JumpBufferTimer = jump.JumpBufferDuration;
        }
        else if (jump.JumpBufferTimer > 0.0f)
        {
            jump.JumpBufferTimer = MathF.Max(0.0f, jump.JumpBufferTimer - dt);
        }

        if (col.IsGrounded)
        {
            jump.CoyoteTimer = jump.CoyoteTimeDuration;
        }
        else if (jump.CoyoteTimer > 0.0f)
        {
            jump.CoyoteTimer = MathF.Max(0.0f, jump.CoyoteTimer - dt);
        }

        // 跳躍の判定と実行
        if (jump.JumpBufferTimer > 0.0f)
        {
            if (col.IsGrounded || jump.CoyoteTimer > 0.0f)
            {
                // 地面初段ジャンプ（接地またはコヨーテ猶予内）
                vel.Vy = jump.FirstJumpVelocity;
                col.IsGrounded = false;
                jump.CoyoteTimer = 0.0f;
                jump.JumpBufferTimer = 0.0f;
                jump.RemainingJumps = jump.MaxJumps - 1;
            }
            else if (jump.RemainingJumps > 0)
            {
                // 空中二段ジャンプ
                vel.Vy = jump.SecondJumpVelocity;
                jump.RemainingJumps--;
                jump.JumpBufferTimer = 0.0f;
                if (effectWorld.HasValue)
                    CombatEffectHelper.SpawnJumpRingEffect(effectWorld.Value, trans.X, trans.Y + col.Height / 2.0f);
            }
        }

        // 3. 重力加速度および可変高度・頂点滞空の適用
        var effectiveGravity = jump.Gravity;

        // キー解放時の上昇速度減衰（小ジャンプ制御）
        if (vel.Vy < 0.0f && !intent.JumpHolding && jump.JumpCutDamping > 0.0f)
        {
            effectiveGravity /= jump.JumpCutDamping;
        }
        else if (MathF.Abs(vel.Vy) < jump.ApexFloatVelocityThreshold && jump.ApexFloatGravityMultiplier > 0.0f)
        {
            // 頂点付近の滞空時間延長（浮遊感・狙い撃ち猶予の付与）
            effectiveGravity *= jump.ApexFloatGravityMultiplier;
        }

        var currentVy = vel.Vy;
        PhysicsService.ApplyGravity(ref currentVy, effectiveGravity, jump.MaxFallSpeed, dt);
        vel.Vy = currentVy;

        // 4. 次フレーム目標座標の算出
        var nextX = trans.X + vel.Vx * dt;
        var nextY = trans.Y + vel.Vy * dt;

        if (arenaCfg != null) nextX = PhysicsService.Clamp(nextX, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX);

        var currentY = trans.Y;
        Box2D entityBox = new(nextX, nextY, col.Width, col.Height);

        // 5. 足場プラットフォームとの片道衝突判定
        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        platformQuery.Each((ref platTrans, ref platCol) =>
        {
            Box2D platformBox = new(platTrans.X, platTrans.Y, platCol.Width, platCol.Height);
            if (PhysicsService.CheckPlatformGround(
                currentY,
                nextY,
                entityBox,
                platformBox,
                out var groundY,
                out var vy))
                if (!hitPlatform || groundY < platformGroundY)
                {
                    hitPlatform = true;
                    platformGroundY = groundY;
                    platformVy = vy;
                }
        });

        var isGrounded = hitPlatform;
        var adjustedPosY = hitPlatform ? platformGroundY : nextY;
        var adjustedVy = hitPlatform ? platformVy : vel.Vy;

        if (arenaCfg != null)
        {
            var floorLimitY = arenaCfg.FloorLimitY - col.Height / 2.0f;
            if (adjustedPosY >= floorLimitY)
            {
                isGrounded = true;
                adjustedPosY = floorLimitY;
                adjustedVy = 0.0f;
            }
        }

        if (isGrounded) jump.RemainingJumps = jump.MaxJumps;

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;
        col.IsGrounded = isGrounded;
    }
}