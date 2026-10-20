using System;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

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
    /// <param name="arenaCfg">アリーナ境界設定（省略時は World 単例）</param>
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
        var world = it.World();
        var cfg = _arenaCfg ?? world.Get<ArenaConfig>();
        var pCfg = world.GetActivePlayerConfig();
        var pQuery = _platformQuery!;

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
            ExecuteKinematics(entity, ref effect, ref tag, ref trans, ref vel, ref col, in intent, ref jump, ref dash, ref sprite, ref spring, dt, pQuery, cfg, pCfg, world);
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
        ArenaConfig arenaCfg,
        PlayerConfig pCfg,
        World? effectWorld)
    {
        var wasGrounded = col.IsGrounded;

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
            spring.Current = pCfg.DashStretchScale;
            spring.Velocity = Vec2f.Zero;

            EventBus.Publish(new PlayerDashedEvent(dash.DirectionX));
        }

        var isDashing = dash.IsDashing;
        if (isDashing)
        {
            dash.Timer = MathF.Max(0.0f, dash.Timer - dt);

            // ダッシュ中速度維持（水平高速突進・重力無効化）
            vel.Value = new Vec2f(dash.DirectionX * pCfg.DashSpeed, 0.0f);

            // 残像（Ghost Trail）生成
            dash.GhostTrailTimer += dt;
            if (dash.GhostTrailTimer >= pCfg.DashGhostTrailInterval)
            {
                dash.GhostTrailTimer = 0.0f;
                if (effectWorld.HasValue)
                {
                    EffectSpawner.SpawnGhostTrail(
                        effectWorld.Value,
                        trans.Position,
                        trans.Scale,
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
            var currentVx = vel.Value.X;
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
                    else if (MathF.Sign(targetVx) != MathF.Sign(currentVx) && MathF.Abs(currentVx) > float.Epsilon)
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

                currentVx = PhysicsService.MoveTowards(currentVx, targetVx, accel * dt);
            }
            else
            {
                currentVx = targetVx;
            }

            var currentVy = vel.Value.Y;

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
                    currentVy = jump.FirstJumpVelocity;
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
                    currentVy = jump.SecondJumpVelocity;
                    jump.RemainingJumps--;
                    jump.JumpBufferTimer = 0.0f;
                    didJump = true;
                    if (effectWorld.HasValue)
                        EffectSpawner.SpawnJumpRingEffect(effectWorld.Value, new Vec2f(trans.Position.X, trans.Position.Y + col.Size.Y / 2.0f));
                    EventBus.Publish(new PlayerDoubleJumpedEvent());
                }

                if (didJump)
                {
                    spring.Current = pCfg.JumpStretchScale;
                    spring.Velocity = Vec2f.Zero;
                }
            }

            // 3. 重力加速度および可変高度・頂点滞空の適用
            var effectiveGravity = jump.Gravity;

            // キー解放時の上昇速度減衰（小ジャンプ制御）
            if (currentVy < 0.0f && !intent.JumpHolding && jump.JumpCutDamping > 0.0f)
            {
                effectiveGravity /= jump.JumpCutDamping;
            }
            else if (MathF.Abs(currentVy) < jump.ApexFloatVelocityThreshold && jump.ApexFloatGravityMultiplier > 0.0f)
            {
                // 頂点付近の滞空時間延長（浮遊感・狙い撃ち猶予の付与）
                effectiveGravity *= jump.ApexFloatGravityMultiplier;
            }

            PhysicsService.ApplyGravity(ref currentVy, effectiveGravity, jump.MaxFallSpeed, dt);
            vel.Value = new Vec2f(currentVx, currentVy);
        }

        // 4. 次フレーム目標座標の算出
        var nextPos = trans.Position + vel.Value * dt;

        if (arenaCfg != null)
            nextPos = new Vec2f(PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX), nextPos.Y);

        var currentY = trans.Position.Y;
        Box2D entityBox = new(nextPos, col.Size);

        // 5. 足場プラットフォームとの片道衝突判定
        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
        {
            Box2D platformBox = new(platTrans.Position, platCol.Size);
            if (PhysicsService.CheckPlatformGround(
                currentY,
                nextPos.Y,
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
        var adjustedPosY = hitPlatform ? platformGroundY : nextPos.Y;
        var adjustedVy = hitPlatform ? platformVy : vel.Value.Y;

        if (arenaCfg != null)
        {
            var floorLimitY = arenaCfg.FloorLimitY - col.Size.Y / 2.0f;
            if (adjustedPosY >= floorLimitY)
            {
                isGrounded = true;
                adjustedPosY = floorLimitY;
                adjustedVy = 0.0f;
            }
        }

        // 接地瞬間時の押し潰し演出（落下速度が設定閾値以上の場合のみ適用）
        if (!wasGrounded && isGrounded && vel.Value.Y >= jump.LandSquashVelocityThreshold)
        {
            EventBus.Publish(new PlayerLandedEvent(vel.Value.Y));
            spring.Current = pCfg.LandSquashScale;
            spring.Velocity = Vec2f.Zero;
        }

        if (isGrounded) jump.RemainingJumps = jump.MaxJumps;

        trans.Position = new Vec2f(nextPos.X, adjustedPosY);
        vel.Value = new Vec2f(vel.Value.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }
}