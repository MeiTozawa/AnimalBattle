using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     汎用キネマティック運動物理システム。
///     <see cref="MovementIntent" /> を消費し、重力、跳躍インパルス、足場衝突および画面境界を統一的に処理します。
/// </summary>
public static class MovementSystem {
    private static Query<Transform, CollisionBox> _platformQuery;
    private static ArenaConfig? _arenaCfg;

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
        _platformQuery = platformQuery ?? world.QueryBuilder<Transform, CollisionBox>().With<PlatformTag>().Build();
        _arenaCfg = arenaCfg;

        world.System<Transform, Velocity, CollisionBox, MovementIntent, JumpCapability, DashState, Sprite, SpringScale, TimedEffectsState, GameplayTagState>("MovementSystem")
           .Kind(Ecs.OnUpdate)
           .Without<DeadTag>()
           .Without<HitstopState>()
           .Iter(ExecuteMovement);

        return world;
    }

    private static void ExecuteMovement(
        Iter it,
        Field<Transform> transforms,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<MovementIntent> intents,
        Field<JumpCapability> jumps,
        Field<DashState> dashes,
        Field<Sprite> sprites,
        Field<SpringScale> springs,
        Field<TimedEffectsState> timedEffects,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();
        var cfg = _arenaCfg ?? ConfigManager.Arena;
        var pQuery = _platformQuery!;
        var world = it.World();

        for (var i = 0; i < count; i++)
        {
            ref var trans = ref transforms[i];
            ref var vel = ref velocities[i];
            ref var col = ref collisionBoxes[i];
            ref var intent = ref intents[i];
            ref var jump = ref jumps[i];
            ref var dash = ref dashes[i];
            ref var sprite = ref sprites[i];
            ref var spring = ref springs[i];
            ref var effect = ref timedEffects[i];
            ref var tag = ref tags[i];

            var entity = it.Entity(i);
            ExecuteKinematics(entity, ref effect, ref tag, ref trans, ref vel, ref col, in intent, ref jump, ref dash, ref sprite, ref spring, dt, pQuery, cfg, world);
        }
    }

    private static void ExecuteKinematics(
        Entity entity,
        ref TimedEffectsState timedEffects,
        ref GameplayTagState tagState,
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        in MovementIntent intent,
        ref JumpCapability jump,
        ref DashState dash,
        ref Sprite sprite,
        ref SpringScale spring,
        float dt,
        Query<Transform, CollisionBox> platformQuery,
        ArenaConfig? arenaCfg,
        World? effectWorld)
    {
        var wasGrounded = col.IsGrounded;
        var pCfg = ConfigManager.Player;

        // 0. ダッシュクールダウン減衰処理
        if (dash.Cooldown > 0.0f)
        {
            dash.Cooldown = MathF.Max(0.0f, dash.Cooldown - dt);
        }

        // 意図に基づくスプライト反転の単方向同期（Presentation Sync）
        if (intent.FacingX != 0.0f)
        {
            sprite.FlipX = intent.FacingX < 0.0f;
        }

        // ダッシュ起動判定
        if (intent.DashTriggered && dash.Cooldown <= 0.0f && !dash.IsDashing)
        {
            dash.Timer = pCfg.DashDuration;
            dash.Cooldown = pCfg.DashCooldown;
            dash.GhostTrailTimer = 0.0f;

            // 突進方向決定（移動入力優先、入力なし時は意図上の水平向き）
            if (MathF.Abs(intent.MoveX) > 0.01f)
            {
                dash.DirectionX = MathF.Sign(intent.MoveX);
            }
            else
            {
                dash.DirectionX = intent.FacingX < 0.0f ? -1.0f : 1.0f;
            }

            // ダッシュ不可命中フレーム付与（弾幕・敵接触をすり抜ける）
            TimedEffectService.ApplyEffect(ref timedEffects, ref tagState, GameplayTag.Untargetable, pCfg.DashInvincibleDuration, entity.Id);

            // スカッシュ＆ストレッチ突進変形演出
            spring.CurrentX = pCfg.DashStretchScaleX;
            spring.CurrentY = pCfg.DashStretchScaleY;
            spring.VelX = 0.0f;
            spring.VelY = 0.0f;

            EventBus.Publish(new PlayerDashedEvent(dash.DirectionX));
        }

        var isDashing = dash.IsDashing;
        if (isDashing)
        {
            dash.Timer = MathF.Max(0.0f, dash.Timer - dt);

            // ダッシュ中速度維持（水平高速突進・重力無効化）
            vel.Vx = dash.DirectionX * pCfg.DashSpeed;
            vel.Vy = 0.0f;

            // 残像（Ghost Trail）生成
            dash.GhostTrailTimer += dt;
            if (dash.GhostTrailTimer >= pCfg.DashGhostTrailInterval)
            {
                dash.GhostTrailTimer = 0.0f;
                if (effectWorld.HasValue)
                {
                    EffectSpawner.SpawnGhostTrail(
                        effectWorld.Value,
                        trans.X,
                        trans.Y,
                        trans.ScaleX,
                        trans.ScaleY,
                        trans.Rotation,
                        sprite.GraphHandle,
                        sprite.SrcRect,
                        sprite.FlipX,
                        ColorPalette.GhostTrailCyan,
                        pCfg.DashGhostTrailDuration);
                }
            }
        }
        else
        {
            // 1. 水平移動の加減速・摩擦制動の適用
            var targetVx = intent.MoveX * jump.MoveSpeed;
            if (jump.GroundAcceleration > 0.0f)
            {
                float accel;
                if (col.IsGrounded)
                {
                    if (MathF.Abs(targetVx) < float.Epsilon)
                    {
                        // 停止（接地自然減速）
                        accel = jump.GroundDeceleration;
                    }
                    else if (MathF.Sign(targetVx) != MathF.Sign(vel.Vx) && MathF.Abs(vel.Vx) > float.Epsilon)
                    {
                        // 反転（急停止制動）
                        accel = jump.TurnaroundDeceleration;
                    }
                    else
                    {
                        // 通常地上加速
                        accel = jump.GroundAcceleration;
                    }
                }
                else
                {
                    // 空中制御（入力有無に応じた加減速）
                    accel = MathF.Abs(targetVx) > float.Epsilon ? jump.AirAcceleration : jump.AirDeceleration;
                }

                vel.Vx = PhysicsService.MoveTowards(vel.Vx, targetVx, accel * dt);
            }
            else
            {
                vel.Vx = targetVx;
            }

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
                var didJump = false;
                if (col.IsGrounded || jump.CoyoteTimer > 0.0f)
                {
                    // 地面初段ジャンプ（接地またはコヨーテ猶予内）
                    vel.Vy = jump.FirstJumpVelocity;
                    col.IsGrounded = false;
                    jump.CoyoteTimer = 0.0f;
                    jump.JumpBufferTimer = 0.0f;
                    jump.RemainingJumps = jump.MaxJumps - 1;
                    didJump = true;
                    EventBus.Publish(new PlayerJumpedEvent());
                }
                else if (jump.RemainingJumps > 0)
                {
                    // 空中二段ジャンプ
                    vel.Vy = jump.SecondJumpVelocity;
                    jump.RemainingJumps--;
                    jump.JumpBufferTimer = 0.0f;
                    didJump = true;
                    if (effectWorld.HasValue)
                        EffectSpawner.SpawnJumpRingEffect(effectWorld.Value, trans.X, trans.Y + col.Height / 2.0f);
                    EventBus.Publish(new PlayerDoubleJumpedEvent());
                }

                if (didJump)
                {
                    spring.CurrentX = pCfg.JumpStretchScaleX;
                    spring.CurrentY = pCfg.JumpStretchScaleY;
                    spring.VelX = 0.0f;
                    spring.VelY = 0.0f;
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
        }

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

        // 接地瞬間時の押し潰し演出（落下速度が設定閾値以上の場合のみ適用）
        if (!wasGrounded && isGrounded && vel.Vy >= jump.LandSquashVelocityThreshold)
        {
            EventBus.Publish(new PlayerLandedEvent(vel.Vy));
            spring.CurrentX = pCfg.LandSquashScaleX;
            spring.CurrentY = pCfg.LandSquashScaleY;
            spring.VelX = 0.0f;
            spring.VelY = 0.0f;
        }

        if (isGrounded) jump.RemainingJumps = jump.MaxJumps;

        trans.X = nextX;
        trans.Y = adjustedPosY;
        vel.Vy = adjustedVy;
        col.IsGrounded = isGrounded;
    }
}