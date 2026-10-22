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
///     戦闘シーンにおける Boss AI の意思決定および動的カメラ追従を処理する純粋 ECS システム。
///     GameState.Battle 状態時にアクティブとなり、OnUpdate フェーズで自機と Boss の状態同期・AI更新を実行します。
/// </summary>
public static class BattleAiSystem
{
    private static Query<TileMap> _tileMapQuery;
    private static Query<Transform, Velocity> _playerQuery;
    private static Query<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent> _bossQuery;

    private static Vec2f _cachedPlayerPos;
    private static Vec2f _cachedPlayerVel;
    private static TileMap? _cachedTileMap;
    private static bool _hasPlayer;
    private static readonly List<int> _candidateBossPlatforms = new(16);

    /// <summary>
    ///     Boss AI 思考および動的カメラ追従システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddBattleAiSystem(this World world)
    {
        _tileMapQuery = world.QueryBuilder<TileMap>().Build();

        _playerQuery = world.QueryBuilder<Transform, Velocity>()
            .With<PlayerTag>()
            .Build();

        _bossQuery = world.QueryBuilder<Transform, Velocity, CollisionBox, Health, Sprite, BossCombatState, BossBrainComponent>()
            .With<BossTag>()
            .Build();

        world.System("BattleAiSystem")
            .Kind(Ecs.OnUpdate)
            .Iter(ExecuteBattleAi);

        return world;
    }

    private static void FindTileMap(ref TileMap tm) => _cachedTileMap = tm;

    private static void ExecuteBattleAi(Iter it)
    {
        var w = it.World();
        ref readonly var flow = ref w.Get<GameFlow>();
        if (flow.State != GameState.Battle) return;

        _hasPlayer = false;
        _playerQuery.Each(FindPlayer);
        if (!_hasPlayer) return;

        var dt = it.DeltaTime();
        var camCfg = w.Get<CameraConfig>();

        _cachedTileMap = null;
        _tileMapQuery.Each(FindTileMap);
        var tileMap = _cachedTileMap;
        _cachedTileMap = null;

        _bossQuery.Each((Entity e, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health hp, ref Sprite sprite, ref BossCombatState state, ref BossBrainComponent brainComp) =>
        {
            var brain = brainComp.Brain;

            // 死亡判定
            if (hp.Current <= 0.0f)
            {
                if (!brain.IsDead) brain.Kill();
                if (!e.Has<DeadTag>()) e.Add<DeadTag>();
                sprite.IsVisible = false;
                return;
            }

            if (state.BossType == BossType.Boss1 && brain is Boss1Brain b1Brain)
            {
                var b1Cfg = w.Get<Boss1Config>();
                b1Brain.Update(dt, _cachedPlayerPos, 1, true, hp.Current, hp.Max);

                // 外力ノックバック速度の減衰処理
                if (vel.Knockback != Vec2f.Zero)
                {
                    var fsmCfg = b1Brain.Phase switch {
                        Phase.Phase2 => b1Cfg.Phase2,
                        Phase.Phase3 => b1Cfg.Phase3,
                        _ => b1Cfg.Phase1
                    };
                    var decay = fsmCfg.RecoverFlybackSpeed;
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

                trans.Position = b1Brain.Position + vel.Knockback;
                sprite.FlipX = !b1Brain.FacingRight;

                var fx = w.Get<RenderConfig>().BossEffects;
                trans.Rotation = (float)(MathF.Sin(b1Brain.HoverTimer * fx.BossHoverAngleSpeed) * fx.BossHoverAngleAmp);
            }
            else if (state.BossType == BossType.Boss2 && brain is Boss2Brain b2Brain)
            {
                var isGrounded = col.IsGrounded;
                b2Brain.Update(dt, _cachedPlayerPos, _cachedPlayerVel, true, isGrounded, hp.Current, hp.Max, trans.Position);

                var currentLinear = vel.Value;
                currentLinear = new Vec2f(b2Brain.Velocity.X, currentLinear.Y);
                var isControlledAirborne = b2Brain.State == Boss2State.PlatformTraverse
                    || (b2Brain.AbilitySystem.IsCasting && b2Brain.CurrentSkillTag == b2Brain.Configuration.TargetLockSkillTag);

                if (isControlledAirborne || isGrounded)
                {
                    currentLinear = new Vec2f(currentLinear.X, b2Brain.Velocity.Y);
                }
                vel.Value = currentLinear;

                UpdateBoss2MovementAndCollision(e, b2Brain, ref trans, ref vel, ref col, dt, w, tileMap, isControlledAirborne);
                sprite.FlipX = !b2Brain.FacingRight;
            }

            // BossCombatState の同期
            state.Phase = brain.Phase;
            state.State = brain.CurrentState;
            state.CurrentSkillTag = brain.CurrentSkillTag;
            state.StateTimer = brain.StateTimer;
            state.TargetPosition = brain.TargetPosition;
            state.FacingRight = brain.FacingRight;

            // カメラ追従
            CameraService.Update(
                dt,
                _cachedPlayerPos,
                _cachedPlayerVel,
                trans.Position,
                camCfg);
        });
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
        bool isControlledAirborne)
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
            var decay = b2Cfg.Gravity > 0.0f ? b2Cfg.Gravity : b2Cfg.MoveSpeed;
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

        var totalLinear = linear + vel.Knockback;
        var nextX = Math.Clamp(trans.Position.X + totalLinear.X * dt, minX, maxX);
        var nextY = trans.Position.Y + totalLinear.Y * dt;
        var nextPos = new Vec2f(nextX, nextY);

        var currentY = trans.Position.Y;
        Box2D bossBox = new(nextPos, colSize);

        var hitPlatform = false;
        var platformGroundY = float.MaxValue;
        var platformVy = 0.0f;

        // 制御空中移動時かつ上昇・滞空（vel.Linear.Y <= 0）の間は足場着地判定をスキップ
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

            _candidateBossPlatforms.Clear();
            tileMap.SpatialHash.Query(sweepQueryBox, _candidateBossPlatforms);

            for (var p = 0; p < _candidateBossPlatforms.Count; p++)
            {
                if (tileMap.SpatialHash.TryGetBox(_candidateBossPlatforms[p], out var platformBox))
                {
                    var platTopY = platformBox.Top;

                    // ヒップドロップ（LeapSmash）急降下中：目標着地サーフェス（足場上面または床）より上層の足場は貫通（スキップ）
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

    private static void FindPlayer(ref Transform t, ref Velocity v)
    {
        _cachedPlayerPos = t.Position;
        _cachedPlayerVel = v.Value;
        _hasPlayer = true;
    }
}
