using System;
using System.Collections.Generic;
using AnimalBattle.Abilities;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Events;
using AnimalBattle.Maps;
using AnimalBattle.Presentation.Rendering;
using AnimalBattle.Presentation.Vfx;
using Flecs.NET.Core;

namespace AnimalBattle.Player.Systems;

/// <summary>
///     World 単位で独立して保持されるキネマティック運動物理システム用コンテキスト。
/// </summary>
internal sealed class MovementSystemContext : IDisposable
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public TileMap? CachedTileMap;
    public readonly List<int> CandidatePlatforms = new(16);

    public void Dispose()
    {
        TileMapQuery.Dispose();
    }
}

/// <summary>
///     汎用キネマティック運動物理システム。
///     <see cref="MovementIntent" /> を消費し、重力、跳躍インパルス、足場衝突および画面境界を統一的に処理します。
/// </summary>
public static class MovementSystem {
    /// <summary>
    ///     キネマティック運動物理システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World" /></returns>
    public static World AddMovementSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();
        world.Set(new MovementSystemContext
        {
            TileMapQuery = tileMapQuery
        });

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
        var ctx = world.Get<MovementSystemContext>();
        ref readonly var cfg = ref world.Get<ArenaConfig>();
        var pCfg = world.GetActivePlayerConfig();

        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Iter(ExtractTileMap);
        var tileMap = ctx.CachedTileMap;
        ctx.CachedTileMap = null;

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
            ExecuteKinematics(entity, ref effect, ref tag, ref trans, ref vel, ref col, in intent, ref jump, ref dash, ref sprite, ref spring, dt, tileMap, cfg, pCfg, world, ctx.CandidatePlatforms);
        }
    }

    /// <summary>
    ///     TileMap インスタンスを無割当：Zero Allocationで抽出します。
    /// </summary>
    private static void ExtractTileMap(Iter it, Field<TileMap> tileMaps)
    {
        if (it.Count() == 0) return;
        it.World().Get<MovementSystemContext>().CachedTileMap = tileMaps[0];
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
        TileMap? tileMap,
        ArenaConfig arenaCfg,
        PlayerConfig pCfg,
        World? effectWorld,
        List<int> candidatePlatforms)
    {
        var wasGrounded = col.IsGrounded;

        // 0. ダッシュクールダウン減衰処理
        if (dash.Cooldown > 0.0f)
        {
            dash.Cooldown = MathF.Max(0.0f, dash.Cooldown - dt);
        }

        // 意図に基づくスプライト反転の単方向同期：Presentation Sync
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

            // 突進方向決定：移動入力優先、入力なし時は意図上の水平向き
            if (MathF.Abs(intent.MoveX) > 0.01f)
            {
                dash.DirectionX = MathF.Sign(intent.MoveX);
            }
            else
            {
                dash.DirectionX = intent.FacingX < 0.0f ? -1.0f : 1.0f;
            }

            // ダッシュ不可命中フレーム付与：弾幕・敵接触をすり抜け、点滅演出を注入
            TimedEffectService.ApplyEffect(ref timedEffects, ref tagState, GameplayTag.Untargetable, pCfg.DashInvincibleDuration, entity.Id);
            var blinkFreq = entity.CsWorld().Get<RenderConfig>().Player.InvincibleBlinkFrequency;
            entity.Set(new InvincibleBlinkEffect(pCfg.DashInvincibleDuration, blinkFreq));

            // スカッシュ＆ストレッチ突進変形演出
            spring.Current = pCfg.DashStretchScale;
            spring.Velocity = Vec2f.Zero;

            EventBus.Publish(new PlayerDashedEvent(dash.DirectionX));
        }

        var isDashing = dash.IsDashing;
        if (isDashing)
        {
            dash.Timer = MathF.Max(0.0f, dash.Timer - dt);

            // ダッシュ中速度維持：水平高速突進・重力無効化
            vel.Value = new Vec2f(dash.DirectionX * pCfg.DashSpeed, 0.0f);
            vel.Knockback = Vec2f.Zero;

            // 残像：Ghost Trail生成
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
                        // 停止：接地自然減速
                        accel = jump.GroundDeceleration;
                    }
                    else if (MathF.Sign(targetVx) != MathF.Sign(currentVx) && MathF.Abs(currentVx) > float.Epsilon)
                    {
                        // 反転：急停止制動
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
                    // 空中制御：入力有無に応じた加減速
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
                    // 地面初段ジャンプ：接地またはコヨーテ猶予内
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

            // キー解放時の上昇速度減衰：小ジャンプ制御
            if (currentVy < 0.0f && !intent.JumpHolding && jump.JumpCutDamping > 0.0f)
            {
                effectiveGravity /= jump.JumpCutDamping;
            }
            else if (MathF.Abs(currentVy) < jump.ApexFloatVelocityThreshold && jump.ApexFloatGravityMultiplier > 0.0f)
            {
                // 頂点付近の滞空時間延長：浮遊感・狙い撃ち猶予の付与
                effectiveGravity *= jump.ApexFloatGravityMultiplier;
            }

            PhysicsService.ApplyGravity(ref currentVy, effectiveGravity, jump.MaxFallSpeed, dt);
            vel.Value = new Vec2f(currentVx, currentVy);
        }

        // 外力ノックバック速度の減衰処理
        if (vel.Knockback != Vec2f.Zero)
        {
            var decay = col.IsGrounded ? jump.GroundDeceleration : jump.AirDeceleration;
            if (decay > 0.0f)
            {
                var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
                var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
                vel.Knockback = new Vec2f(kx, ky);
            }
            else
            {
                vel.Knockback = Vec2f.Zero;
            }
        }

        // 4. 次フレーム目標座標の算出：自主移動速度と外力ノックバックの合算
        var totalVel = vel.Value + vel.Knockback;
        var nextPos = trans.Position + totalVel * dt;

        if (arenaCfg != null)
            nextPos = nextPos with { X = PhysicsService.Clamp(nextPos.X, arenaCfg.BoundaryMinX, arenaCfg.BoundaryMaxX) };

        var currentY = trans.Position.Y;
        Box2D entityBox = new(nextPos, col.Size);

        // 5. 足場プラットフォームとの片道衝突判定
        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        if (tileMap != null)
        {
            var sweepTop = MathF.Min(currentY, nextPos.Y) - col.Size.Y * 0.5f;
            var sweepBottom = MathF.Max(currentY, nextPos.Y) + col.Size.Y * 0.5f;
            var sweepCenter = new Vec2f(nextPos.X, (sweepTop + sweepBottom) * 0.5f);
            var sweepSize = new Vec2f(col.Size.X, sweepBottom - sweepTop);
            Box2D sweepQueryBox = new(sweepCenter, sweepSize);

            candidatePlatforms.Clear();
            tileMap.SpatialHash.Query(sweepQueryBox, candidatePlatforms);

            foreach (var t in candidatePlatforms)
            {
                if (tileMap.SpatialHash.TryGetBox(t, out var platformBox))
                {
                    if (PhysicsService.CheckPlatformGround(
                        currentY,
                        nextPos.Y,
                        entityBox,
                        platformBox,
                        out var groundY,
                        out var vy))
                    {
                        if (!hitPlatform || groundY < platformGroundY)
                        {
                            hitPlatform = true;
                            platformGroundY = groundY;
                            platformVy = vy;
                        }
                    }
                }
            }
        }

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

        // 接地瞬間時の押し潰し演出：落下速度が設定閾値以上の場合のみ適用
        if (!wasGrounded && isGrounded && vel.Value.Y >= jump.LandSquashVelocityThreshold)
        {
            EventBus.Publish(new PlayerLandedEvent(vel.Value.Y));
            spring.Current = pCfg.LandSquashScale;
            spring.Velocity = Vec2f.Zero;
        }

        if (isGrounded)
        {
            jump.RemainingJumps = jump.MaxJumps;
            if (vel.Knockback.Y > 0.0f)
            {
                vel.Knockback = new Vec2f(vel.Knockback.X, 0.0f);
            }
        }

        trans.Position = new Vec2f(nextPos.X, adjustedPosY);
        vel.Value = new Vec2f(vel.Value.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }
}