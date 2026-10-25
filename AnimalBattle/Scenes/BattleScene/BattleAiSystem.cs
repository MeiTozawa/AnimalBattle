using System;
using System.Collections.Generic;
using AnimalBattle.Ai;
using AnimalBattle.Ai.Boss1;
using AnimalBattle.Ai.Boss2;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Geometry;
using AnimalBattle.Maps;
using Flecs.NET.Core;

namespace AnimalBattle.Scenes.Battle;

/// <summary>
///     World 単位で独立して保持される Boss AI およびカメラ更新用コンテキスト。
/// </summary>
internal sealed class BattleAiSystemContext
{
    public required Query<TileMap> TileMapQuery { get; init; }
    public required Query<Transform, Velocity> PlayerQuery { get; init; }
    public required Query<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent> BossQuery { get; init; }

    public Vec2f CachedPlayerPos;
    public Vec2f CachedPlayerVel;
    public ulong CachedPlayerEntityId;
    public bool IsPlayerAlive;
    public bool HasPlayerEverSpawned;
    public Vec2f CachedBossPos;
    public bool HasBoss;
    public TileMap? CachedTileMap;
    public readonly List<int> CandidateBossPlatforms = new(16);
}

/// <summary>
///     戦闘シーンにおける Boss AI の意思決定および動的カメラ追従を処理する純粋 ECS システム。
///     GameState.Battle 状態時にアクティブとなり、OnUpdate フェーズで自機と Boss の状態同期・AI更新を実行します。
/// </summary>
public static class BattleAiSystem
{
    /// <summary>
    ///     Boss AI 思考および動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleAiSystem(this World world)
    {
        var tileMapQuery = world.QueryBuilder<TileMap>().Build();

        // プレイヤーエンティティの追跡用クエリ
        var playerQuery = world.QueryBuilder<Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        // Boss 更新用クエリ：HitstopState 中の凍結および DeadTag 除外を徹底
        var bossQuery = world.QueryBuilder<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Without<HitstopState>()
            .Without<DeadTag>()
            .Build();

        world.Set(new BattleAiSystemContext
        {
            TileMapQuery = tileMapQuery,
            PlayerQuery = playerQuery,
            BossQuery = bossQuery
        });

        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteBattleAi);

        return world;
    }

    private static void ExecuteBattleAi(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        var ctx = w.Get<BattleAiSystemContext>();

        // プレイヤーの生存・座標状態を更新
        ctx.IsPlayerAlive = false;
        var foundPlayerThisFrame = false;

        ctx.PlayerQuery.Each((Entity pe, ref Transform t, ref Velocity v) =>
        {
            foundPlayerThisFrame = true;
            ctx.HasPlayerEverSpawned = true;
            ctx.CachedPlayerPos = t.Position;
            ctx.CachedPlayerVel = v.Value;
            ctx.CachedPlayerEntityId = pe.Id;
            // プレイヤーが DeadTag または GameOverTag を保持していなければ生存と判定
            ctx.IsPlayerAlive = !pe.Has<DeadTag>() && !w.Has<GameOverTag>();
        });

        if (!foundPlayerThisFrame && !ctx.HasPlayerEverSpawned) return;

        var dt = it.DeltaTime();
        var camCfg = w.Get<CameraConfig>();

        ctx.CachedTileMap = null;
        ctx.TileMapQuery.Each((ref TileMap tm) => ctx.CachedTileMap = tm);
        var tileMap = ctx.CachedTileMap;
        ctx.CachedTileMap = null;

        ctx.HasBoss = false;

        ctx.BossQuery.Each((Entity e, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health hp, ref Sprite sprite, ref BossCombatState state, ref BossBrainComponent brainComp) =>
        {
            var brain = brainComp.Brain;

            // 死亡判定：クエリで Without<DeadTag> が保証されているため Has ガードは不要
            if (hp.Current <= 0.0f)
            {
                if (!brain.IsDead) brain.Kill();
                e.Add<DeadTag>();
                sprite.IsVisible = false;
                return;
            }

            ctx.HasBoss = true;
            ctx.CachedBossPos = trans.Position;

            if (state.BossType == BossType.Boss1 && brain is Boss1Brain b1Brain)
            {
                var b1Cfg = w.Get<Boss1Config>();
                var arenaCfg = w.Get<ArenaConfig>();

                // 生存フラグおよびプレイヤーエンティティIDを動的に渡す
                b1Brain.Update(dt, ctx.CachedPlayerPos, ctx.CachedPlayerEntityId, ctx.IsPlayerAlive, hp.Current, hp.Max);

                // 外力ノックバック変位の適用および減衰処理
                if (vel.Knockback != Vec2f.Zero)
                {
                    b1Brain.ApplyKnockback(vel.Knockback * dt);
                    var decay = b1Cfg.KnockbackDeceleration > 0.0f ? b1Cfg.KnockbackDeceleration : 1200.0f;
                    var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
                    var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
                    vel.Knockback = new Vec2f(kx, ky);
                }

                var colSize = col.Size != Vec2f.Zero ? col.Size : b1Cfg.CollisionSize;
                var minX = arenaCfg.BoundaryMinX + colSize.X * 0.5f;
                var maxX = arenaCfg.BoundaryMaxX - colSize.X * 0.5f;
                var floorY = arenaCfg.FloorLimitY - colSize.Y * 0.5f;
                var minY = b1Cfg.CeilingLimitY > 0.0f ? b1Cfg.CeilingLimitY : colSize.Y * 0.5f;
                b1Brain.ClampPosition(minX, maxX, minY, floorY);

                trans.Position = b1Brain.Position;
                sprite.FlipX = !b1Brain.FacingRight;

                var fx = w.Get<RenderConfig>().BossEffects;
                trans.Rotation = (float)(MathF.Sin(b1Brain.HoverTimer * fx.BossHoverAngleSpeed) * fx.BossHoverAngleAmp);
            }
            else if (state.BossType == BossType.Boss2 && brain is Boss2Brain b2Brain)
            {
                var isGrounded = col.IsGrounded;
                // 生存フラグを動的に渡す
                b2Brain.Update(dt, ctx.CachedPlayerPos, ctx.CachedPlayerVel, ctx.IsPlayerAlive, isGrounded, hp.Current, hp.Max, trans.Position);

                var currentLinear = vel.Value;
                currentLinear = new Vec2f(b2Brain.Velocity.X, currentLinear.Y);
                var isControlledAirborne = b2Brain.State == Boss2State.PlatformTraverse
                    || (b2Brain.AbilitySystem.IsCasting && b2Brain.CurrentSkillTag == b2Brain.Configuration.TargetLockSkillTag);

                if (isControlledAirborne || isGrounded)
                {
                    currentLinear = new Vec2f(currentLinear.X, b2Brain.Velocity.Y);
                }
                vel.Value = currentLinear;

                UpdateBoss2MovementAndCollision(e, b2Brain, ref trans, ref vel, ref col, dt, w, tileMap, isControlledAirborne, ctx.CandidateBossPlatforms);
                sprite.FlipX = !b2Brain.FacingRight;
            }

            // BossCombatState の同期
            state.Phase = brain.Phase;
            state.CurrentSkillTag = brain.CurrentSkillTag;
            state.StateTimer = brain.StateTimer;
            state.TargetPosition = brain.TargetPosition;
            state.FacingRight = brain.FacingRight;
        });

        // カメラ追従および振動の更新：Boss が Hitstop 中や死亡後であっても振動減衰・構図追従を滞りなく継続
        if (ctx.HasBoss)
        {
            CameraService.Update(
                dt,
                ctx.CachedPlayerPos,
                ctx.CachedPlayerVel,
                ctx.CachedBossPos,
                camCfg);
        }
    }

    private static void UpdateBoss2MovementAndCollision(
        Entity entity,
        Boss2Brain brain,
        ref Transform trans,
        ref Velocity vel,
        ref CollisionBox col,
        float dt,
        World world,
        TileMap? tileMap,
        bool isControlledAirborne,
        List<int> candidateBossPlatforms)
    {
        var b2Cfg = world.Get<Boss2Config>();
        var arenaCfg = world.Get<ArenaConfig>();

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

        // 外力ノックバック速度の減衰処理
        if (vel.Knockback != Vec2f.Zero)
        {
            var decay = b2Cfg.KnockbackDeceleration > 0.0f ? b2Cfg.KnockbackDeceleration : 1200.0f;
            var kx = PhysicsService.MoveTowards(vel.Knockback.X, 0.0f, decay * dt);
            var ky = PhysicsService.MoveTowards(vel.Knockback.Y, 0.0f, decay * dt);
            vel.Knockback = new Vec2f(kx, ky);
        }

        var totalLinear = linear + vel.Knockback;
        var nextX = Math.Clamp(trans.Position.X + totalLinear.X * dt, minX, maxX);
        var nextY = trans.Position.Y + totalLinear.Y * dt;
        var nextPos = new Vec2f(nextX, nextY);

        var currentY = trans.Position.Y;
        Box2D bossBox = new(nextPos, colSize);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        // 制御空中移動時かつ上昇・滞空：vel.Linear.Y <= 0の間は足場着地判定をスキップ
        var ignorePlatforms = isControlledAirborne && linear.Y <= 0.0f;
        if (!ignorePlatforms && tileMap != null)
        {
            var isLeapSmash = brain.CurrentSkillTag == brain.Configuration.TargetLockSkillTag;
            var targetLandingSurfaceY = brain.TargetLandingSurfaceY;

            var sweepTop = MathF.Min(currentY, nextY) - colSize.Y * 0.5f;
            var sweepBottom = MathF.Max(currentY, nextY) + colSize.Y * 0.5f;
            var sweepCenter = new Vec2f(nextX, (sweepTop + sweepBottom) * 0.5f);
            var sweepSize = new Vec2f(colSize.X, sweepBottom - sweepTop);
            Box2D sweepQueryBox = new(sweepCenter, sweepSize);

            candidateBossPlatforms.Clear();
            tileMap.SpatialHash.Query(sweepQueryBox, candidateBossPlatforms);

            for (var p = 0; p < candidateBossPlatforms.Count; p++)
            {
                if (tileMap.SpatialHash.TryGetBox(candidateBossPlatforms[p], out var platformBox))
                {
                    var platTopY = platformBox.Top;

                    // ヒップドロップ：LeapSmash急降下中：目標着地サーフェス：足場上面または床より上層の足場は貫通：スキップ
                    if (isLeapSmash && targetLandingSurfaceY > 0.0f && platTopY < targetLandingSurfaceY - 5.0f)
                    {
                        continue;
                    }

                    if (PhysicsService.CheckPlatformGround(currentY, nextY, bossBox, platformBox, out var groundY, out var pVy))
                    {
                        if (!hitPlatform || groundY < platformGroundY)
                        {
                            hitPlatform = true;
                            platformGroundY = groundY;
                            platformVy = pVy;
                        }
                    }
                }
            }
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

        if (isGrounded && vel.Knockback.Y > 0.0f)
        {
            vel.Knockback = new Vec2f(vel.Knockback.X, 0.0f);
        }

        trans.Position = new Vec2f(nextX, adjustedPosY);
        vel.Value = new Vec2f(linear.X, adjustedVy);
        col.IsGrounded = isGrounded;
    }
}
