using System;
using System.Collections.Generic;
using System.Drawing;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤーの移動・二段跳躍・8方向射撃、Boss 分層状態機 (HFSM: Phase -> Hovering -> Telegraphing -> Casting -> Recovering)、
/// 弾物理、当たり判定、および R キー即時リセットを TOML 設定に基づいて駆動する ECS 常駐 Routine (System)。
/// 全ての物理・アニメーション・タイマー計算は外部から注入されたフレーム経過時間（delta_time）および TOML 設定値に基づきます。
/// </summary>
public static class CombatSystems
{
    private static bool _prevJumpKey;
    private static bool _prevShootKey;
    private static bool _prevRestartKey;

    /// <summary>
    /// 全戦闘関連の ECS 常駐 Routine (System) を Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void RegisterSystems(World world)
    {
        RegisterPlayerMovementSystem(world);
        RegisterPlayerShootSystem(world);
        RegisterBossAiSystem(world);
        RegisterBulletPhysicsSystems(world);
        RegisterCollisionSystems(world);
        RegisterRestartSystem(world);
    }

    private static void RegisterPlayerMovementSystem(World world)
    {
        Query<Transform, CollisionBox> platformQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build();

        world.System<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite>("PlayerMovementSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health health, ref DoubleJump doubleJump, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 float dt = it.DeltaTime();

                 if (health.InvincibleTimer > 0.0f)
                 {
                     health.InvincibleTimer = Math.Max(0.0f, health.InvincibleTimer - dt);
                 }

                 bool isLeft = GameEngine.CheckHitKey(0x1E) != 0 || GameEngine.CheckHitKey(0xCB) != 0;
                 bool isRight = GameEngine.CheckHitKey(0x20) != 0 || GameEngine.CheckHitKey(0xCD) != 0;
                 bool isJump = GameEngine.CheckHitKey(0x39) != 0 || GameEngine.CheckHitKey(0x25) != 0;

                 int pad = GameEngine.GetJoypadInputState(0);
                 isLeft = isLeft || (pad & GameEngine.PadInputLeft) != 0;
                 isRight = isRight || (pad & GameEngine.PadInputRight) != 0;
                 isJump = isJump || (pad & GameEngine.PadInputButton1) != 0;

                 bool jumpJustPressed = isJump && !_prevJumpKey;
                 _prevJumpKey = isJump;

                 if (!health.IsAlive)
                 {
                     vel.Vx = 0.0f;
                     vel.Vy = 0.0f;
                     return;
                 }

                 if (isLeft)
                 {
                     vel.Vx = -pCfg.MoveSpeed;
                     sprite.FlipX = true;
                 }
                 else if (isRight)
                 {
                     vel.Vx = pCfg.MoveSpeed;
                     sprite.FlipX = false;
                 }
                 else
                 {
                     vel.Vx = 0.0f;
                 }

                 float colWidth = col.Width;
                 float colHeight = col.Height;

                 if (jumpJustPressed)
                 {
                     if (col.IsGrounded)
                     {
                         vel.Vy = pCfg.FirstJumpVelocity;
                         col.IsGrounded = false;
                         doubleJump.RemainingJumps = 1;
                     }
                     else if (doubleJump.CanJump)
                     {
                         vel.Vy = pCfg.SecondJumpVelocity;
                         doubleJump.RemainingJumps--;
                         SpawnJumpRingEffect(world, trans.X, trans.Y + colHeight / 2.0f);
                     }
                 }

                 vel.Vy += pCfg.Gravity;
                 if (vel.Vy > pCfg.MaxFallSpeed)
                 {
                     vel.Vy = pCfg.MaxFallSpeed;
                 }

                 float nextX = trans.X + vel.Vx;
                 float nextY = trans.Y + vel.Vy;

                 if (nextX < arenaCfg.BoundaryMinX)
                 {
                     nextX = arenaCfg.BoundaryMinX;
                 }
                 else if (nextX > arenaCfg.BoundaryMaxX)
                 {
                     nextX = arenaCfg.BoundaryMaxX;
                 }

                 bool isGrounded = false;
                 float adjustedPosY = nextY;
                 float adjustedVy = vel.Vy;

                 float pLeft = nextX - colWidth / 2.0f;
                 float pRight = nextX + colWidth / 2.0f;
                 float pBottomPrev = trans.Y + colHeight / 2.0f;
                 float pBottomNext = nextY + colHeight / 2.0f;

                 platformQuery.Each((ref Transform platTrans, ref CollisionBox platCol) =>
                 {
                     float platLeft = platTrans.X - platCol.Width / 2.0f;
                     float platRight = platTrans.X + platCol.Width / 2.0f;
                     float platTop = platTrans.Y - platCol.Height / 2.0f;

                     if (pRight > platLeft && pLeft < platRight)
                     {
                         if (pBottomPrev <= platTop + 4.0f && pBottomNext >= platTop)
                         {
                             isGrounded = true;
                             adjustedPosY = platTop - colHeight / 2.0f;
                             adjustedVy = 0.0f;
                         }
                     }
                 });

                 if (isGrounded)
                 {
                     doubleJump.Reset();
                 }

                 trans.X = nextX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;

                 anim.Timer += dt;
                 if (!col.IsGrounded)
                 {
                     anim.CurrentClip = "Jump";
                 }
                 else if (Math.Abs(vel.Vx) > 0.1f)
                 {
                     anim.CurrentClip = "Run";
                 }
                 else
                 {
                     anim.CurrentClip = "Idle";
                 }

                 AnimationConfig? charConfig = AnimationManager.GetCharacterConfig(CharacterType.CatSpriteSheet);
                 if (charConfig != null)
                 {
                     sprite.SrcRect = AnimationManager.CalculateSrcRect(charConfig, anim.CurrentClip, anim.Timer, out _);
                 }
             });
    }

    private static void RegisterPlayerShootSystem(World world)
    {
        world.System<Transform, Health, PlayerState, Sprite>("PlayerShootSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Health health, ref PlayerState playerState, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;
                 float dt = it.DeltaTime();

                 bool isShoot = GameEngine.CheckHitKey(0x24) != 0 || GameEngine.CheckHitKey(0x2C) != 0;
                 int pad = GameEngine.GetJoypadInputState(0);
                 isShoot = isShoot || (pad & GameEngine.PadInputButton2) != 0;

                 bool shootJustPressed = isShoot && !_prevShootKey;
                 _prevShootKey = isShoot;

                 if (!health.IsAlive)
                 {
                     return;
                 }

                 if (playerState.ShootCooldown > 0.0f)
                 {
                     playerState.ShootCooldown = Math.Max(0.0f, playerState.ShootCooldown - dt);
                 }

                 if (shootJustPressed && playerState.ShootCooldown <= 0.0f)
                 {
                     bool isLeft = GameEngine.CheckHitKey(0x1E) != 0 || GameEngine.CheckHitKey(0xCB) != 0 || (pad & GameEngine.PadInputLeft) != 0;
                     bool isRight = GameEngine.CheckHitKey(0x20) != 0 || GameEngine.CheckHitKey(0xCD) != 0 || (pad & GameEngine.PadInputRight) != 0;
                     bool isUp = GameEngine.CheckHitKey(0x11) != 0 || GameEngine.CheckHitKey(0xC8) != 0 || (pad & GameEngine.PadInputUp) != 0;
                     bool isDown = GameEngine.CheckHitKey(0x1F) != 0 || GameEngine.CheckHitKey(0xD0) != 0 || (pad & GameEngine.PadInputDown) != 0;

                     float inputX = 0.0f;
                     float inputY = 0.0f;

                     if (isLeft)
                     {
                         inputX -= 1.0f;
                     }
                     if (isRight)
                     {
                         inputX += 1.0f;
                     }
                     if (isUp)
                     {
                         inputY -= 1.0f;
                     }
                     if (isDown)
                     {
                         inputY += 1.0f;
                     }

                     float dirX;
                     float dirY;

                     if (Math.Abs(inputX) > 0.001f || Math.Abs(inputY) > 0.001f)
                     {
                         float length = MathF.Sqrt(inputX * inputX + inputY * inputY);
                         dirX = inputX / length;
                         dirY = inputY / length;

                         if (inputX < 0.0f)
                         {
                             sprite.FlipX = true;
                         }
                         else if (inputX > 0.0f)
                         {
                             sprite.FlipX = false;
                         }
                     }
                     else
                     {
                         dirX = sprite.FlipX ? -1.0f : 1.0f;
                         dirY = 0.0f;
                     }

                     float spawnX = trans.X + dirX * pCfg.BulletSpawnOffset;
                     float spawnY = trans.Y + pCfg.BulletSpawnOffsetY + dirY * pCfg.BulletSpawnOffset;

                     world.Entity()
                          .Set(new Transform(spawnX, spawnY))
                          .Set(new Velocity(dirX * pCfg.BulletSpeed, dirY * pCfg.BulletSpeed))
                          .Set(new CollisionBox(pCfg.BulletWidth, pCfg.BulletHeight))
                          .Add<PlayerBulletTag>();

                     playerState.ShootCooldown = pCfg.ShootCooldown;
                 }
             });
    }

    private static void RegisterBossAiSystem(World world)
    {
        world.System<Transform, Health, BossStats, BossStateMachine, Sprite>("BossAiSystem")
             .With<BossTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Health bHealth, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PatternConfig patternCfg = ConfigManager.Pattern;
                 float dt = it.DeltaTime();

                 float playerX = 640.0f;
                 float playerY = 600.0f;
                 bool isPlayerAlive = false;

                 world.QueryBuilder<Transform, Health>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref Health pHealth) =>
                      {
                          playerX = pTrans.X;
                          playerY = pTrans.Y;
                          isPlayerAlive = pHealth.IsAlive;
                      });

                 if (stats.HitFlashTimer > 0.0f)
                 {
                     stats.HitFlashTimer = Math.Max(0.0f, stats.HitFlashTimer - dt);
                 }

                 if (!bHealth.IsAlive)
                 {
                     bHealth.Current = 0.0f;
                     return;
                 }

                 float hpRatio = bHealth.Ratio;
                 int targetPhase = hpRatio > bCfg.Phase2.HpThreshold ? 1 : (hpRatio > bCfg.Phase3.HpThreshold ? 2 : 3);

                 if (targetPhase > stats.Phase && fsm.CurrentState != BossStateKind.PhaseTransition)
                 {
                     stats.Phase = targetPhase;
                     fsm.CurrentState = BossStateKind.PhaseTransition;
                     fsm.StateTimer = 0.0f;
                     fsm.StateDuration = bCfg.Phase1.TransitionDuration;
                     fsm.IsInvulnerable = true;
                     ClearAllBossBullets(world);
                     SpawnShockwave(world, trans.X, trans.Y);
                 }

                 PhaseFsmConfig currentPhaseCfg = stats.Phase switch
                 {
                     1 => bCfg.Phase1,
                     2 => bCfg.Phase2,
                     _ => bCfg.Phase3
                 };

                 stats.HoverTimer += dt;
                 float targetBaseX = stats.BaseX + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedX) * currentPhaseCfg.HoverAmpX;
                 float targetBaseY = stats.BaseY + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedY) * currentPhaseCfg.HoverAmpY;
                 float lerpFactor = MathF.Min(1.0f, currentPhaseCfg.HoverLerpSpeed * dt);
                 trans.X += (targetBaseX - trans.X) * lerpFactor;
                 trans.Y += (targetBaseY - trans.Y) * lerpFactor;

                 fsm.StateTimer += dt;

                 switch (fsm.CurrentState)
                 {
                     case BossStateKind.Hovering:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration && isPlayerAlive)
                         {
                             if (currentPhaseCfg.Skills.Count > 0)
                             {
                                 fsm.CurrentSkillIndex = (fsm.CurrentSkillIndex + 1) % currentPhaseCfg.Skills.Count;
                                 fsm.CurrentSkillName = currentPhaseCfg.Skills[fsm.CurrentSkillIndex];
                             }
                             else
                             {
                                 fsm.CurrentSkillName = "Aimed3Shot";
                             }

                             float windUp = GetSkillWindUpTime(fsm.CurrentSkillName, patternCfg);
                             Color telegraphColor = GetSkillTelegraphColor(fsm.CurrentSkillName, patternCfg);

                             fsm.CurrentState = BossStateKind.Telegraphing;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = windUp;
                             fsm.SubTimer = 0.0f;
                             SpawnTelegraphSpark(world, trans.X, trans.Y, telegraphColor);
                         }
                         break;

                     case BossStateKind.Telegraphing:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float castDuration = GetSkillCastDuration(fsm.CurrentSkillName, patternCfg);
                             fsm.CurrentState = BossStateKind.Casting;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = castDuration;
                             fsm.SubTimer = 0.0f;
                             fsm.AngleOffset = 0.0f;
                         }
                         break;

                     case BossStateKind.Casting:
                         fsm.IsInvulnerable = false;
                         ExecuteBossSkill(world, ref fsm, patternCfg, trans.X, trans.Y, playerX, playerY, dt);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float recoveryTime = GetSkillRecoveryTime(fsm.CurrentSkillName, patternCfg);
                             fsm.CurrentState = BossStateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = recoveryTime;
                         }
                         break;

                     case BossStateKind.Recovering:
                         fsm.IsInvulnerable = false;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;

                     case BossStateKind.PhaseTransition:
                         fsm.IsInvulnerable = true;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.IsInvulnerable = false;
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;
                 }
             });
    }

    private static float GetSkillWindUpTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.WindUpTime,
        "RingBurst" => patternCfg.RingBurst.WindUpTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.WindUpTime,
        "RainDrop" => patternCfg.RainDrop.WindUpTime,
        "CrossChaos" => patternCfg.CrossChaos.WindUpTime,
        "FastSniper" => patternCfg.FastSniper.WindUpTime,
        _ => 0.5f
    };

    private static Color GetSkillTelegraphColor(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.TelegraphColor,
        "RingBurst" => patternCfg.RingBurst.TelegraphColor,
        "DoubleSpiral" => patternCfg.DoubleSpiral.TelegraphColor,
        "RainDrop" => patternCfg.RainDrop.TelegraphColor,
        "CrossChaos" => patternCfg.CrossChaos.TelegraphColor,
        "FastSniper" => patternCfg.FastSniper.TelegraphColor,
        _ => Color.FromArgb(0xFF, 0xFF, 0x88, 0x88)
    };

    private static float GetSkillCastDuration(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.CastDuration,
        "RingBurst" => patternCfg.RingBurst.CastDuration,
        "DoubleSpiral" => patternCfg.DoubleSpiral.CastDuration,
        "RainDrop" => patternCfg.RainDrop.CastDuration,
        "CrossChaos" => patternCfg.CrossChaos.CastDuration,
        "FastSniper" => patternCfg.FastSniper.CastDuration,
        _ => 1.0f
    };

    private static float GetSkillRecoveryTime(string skillName, PatternConfig patternCfg) => skillName switch
    {
        "Aimed3Shot" => patternCfg.Aimed3Shot.RecoveryTime,
        "RingBurst" => patternCfg.RingBurst.RecoveryTime,
        "DoubleSpiral" => patternCfg.DoubleSpiral.RecoveryTime,
        "RainDrop" => patternCfg.RainDrop.RecoveryTime,
        "CrossChaos" => patternCfg.CrossChaos.RecoveryTime,
        "FastSniper" => patternCfg.FastSniper.RecoveryTime,
        _ => 0.5f
    };

    private static void ExecuteBossSkill(World world, ref BossStateMachine fsm, PatternConfig patternCfg, float bx, float by, float px, float py, float dt)
    {
        if (fsm.SubTimer > 0.0f)
        {
            fsm.SubTimer = Math.Max(0.0f, fsm.SubTimer - dt);
        }

        switch (fsm.CurrentSkillName)
        {
            case "Aimed3Shot":
                if (fsm.SubTimer <= 0.0f)
                {
                    Aimed3ShotConfig p = patternCfg.Aimed3Shot;
                    fsm.SubTimer = p.Interval;
                    float baseAngle = MathF.Atan2(py - by, px - bx);
                    for (int i = -1; i <= 1; i++)
                    {
                        float angle = baseAngle + i * p.SpreadAngle;
                        SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                }
                break;

            case "RingBurst":
                if (fsm.SubTimer <= 0.0f)
                {
                    RingBurstConfig p = patternCfg.RingBurst;
                    fsm.SubTimer = p.Interval;
                    int count = Math.Max(4, p.BulletCount);
                    for (int i = 0; i < count; i++)
                    {
                        float angle = (i / (float)count) * MathF.PI * 2.0f + fsm.AngleOffset;
                        SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "DoubleSpiral":
                if (fsm.SubTimer <= 0.0f)
                {
                    DoubleSpiralConfig p = patternCfg.DoubleSpiral;
                    fsm.SubTimer = p.Interval;
                    float angle1 = fsm.AngleOffset;
                    float angle2 = fsm.AngleOffset + MathF.PI;

                    SpawnBossBullet(world, bx, by, MathF.Cos(angle1) * p.BulletSpeed, MathF.Sin(angle1) * p.BulletSpeed, p.BulletRadius, p.FillColor1, p.OutlineColor1);
                    SpawnBossBullet(world, bx, by, MathF.Cos(angle2) * p.BulletSpeed, MathF.Sin(angle2) * p.BulletSpeed, p.BulletRadius, p.FillColor2, p.OutlineColor2);

                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "RainDrop":
                if (fsm.SubTimer <= 0.0f)
                {
                    RainDropConfig p = patternCfg.RainDrop;
                    fsm.SubTimer = p.Interval;
                    Random random = Random.Shared;
                    int minX = (int)p.MinX;
                    int maxX = (int)p.MaxX;
                    float rainX1 = random.Next(minX, maxX);
                    float rainX2 = random.Next(minX, maxX);
                    SpawnBossBullet(world, rainX1, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    SpawnBossBullet(world, rainX2, 0.0f, 0.0f, p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                }
                break;

            case "CrossChaos":
                if (fsm.SubTimer <= 0.0f)
                {
                    CrossChaosConfig p = patternCfg.CrossChaos;
                    fsm.SubTimer = p.Interval;
                    for (int k = 0; k < 4; k++)
                    {
                        float angle = fsm.AngleOffset + k * (MathF.PI / 2.0f);
                        SpawnBossBullet(world, bx, by, MathF.Cos(angle) * p.BulletSpeed, MathF.Sin(angle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                    }
                    fsm.AngleOffset += p.AngleStep;
                }
                break;

            case "FastSniper":
                if (fsm.SubTimer <= 0.0f)
                {
                    FastSniperConfig p = patternCfg.FastSniper;
                    fsm.SubTimer = p.Interval;
                    float aimAngle = MathF.Atan2(py - by, px - bx);
                    SpawnBossBullet(world, bx, by, MathF.Cos(aimAngle) * p.BulletSpeed, MathF.Sin(aimAngle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                }
                break;
        }
    }

    private static void RegisterBulletPhysicsSystems(World world)
    {
        world.System<Transform, Velocity>("PlayerBulletMovementSystem")
             .With<PlayerBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel) =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;
                 if (trans.X < arenaCfg.BulletDespawnMinX || trans.X > arenaCfg.BulletDespawnMaxX ||
                     trans.Y < arenaCfg.BulletDespawnMinY || trans.Y > arenaCfg.BulletDespawnMaxY)
                 {
                     e.Destruct();
                 }
             });

        world.System<Transform, Velocity>("BossBulletMovementSystem")
             .With<BossBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel) =>
             {
                 ArenaConfig arenaCfg = ConfigManager.Arena;
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;
                 if (trans.X < arenaCfg.BulletDespawnMinX || trans.X > arenaCfg.BulletDespawnMaxX ||
                     trans.Y < arenaCfg.BulletDespawnMinY || trans.Y > arenaCfg.BulletDespawnMaxY)
                 {
                     e.Destruct();
                 }
             });
    }

    private static void RegisterCollisionSystems(World world)
    {
        world.System("CombatCollisionSystem")
             .Kind(Ecs.OnUpdate)
             .Run((Iter it) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PlayerConfig pCfg = ConfigManager.Player;

                 // 1. プレイヤー銃弾 vs Boss
                 float bossX = 0.0f;
                 float bossY = 0.0f;
                 float bossLeft = 0.0f;
                 float bossRight = 0.0f;
                 float bossTop = 0.0f;
                 float bossBottom = 0.0f;
                 bool hasBoss = false;
                 bool isInvulnerable = false;

                 world.QueryBuilder<Transform, Health, BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform bTrans, ref Health bHealth, ref BossStats bStats, ref BossStateMachine bFsm) =>
                      {
                          if (bHealth.IsAlive)
                          {
                              bossX = bTrans.X;
                              bossY = bTrans.Y;
                              bossLeft = bTrans.X - bCfg.HitboxWidth / 2.0f;
                              bossRight = bTrans.X + bCfg.HitboxWidth / 2.0f;
                              bossTop = bTrans.Y - bCfg.HitboxHeight / 2.0f;
                              bossBottom = bTrans.Y + bCfg.HitboxHeight / 2.0f;
                              hasBoss = true;
                              isInvulnerable = bFsm.IsInvulnerable;
                          }
                      });

                 if (hasBoss)
                 {
                     float damageTaken = 0.0f;
                     world.QueryBuilder<Transform>()
                          .With<PlayerBulletTag>()
                          .Build()
                          .Each((Entity bulletEntity, ref Transform bulletTrans) =>
                          {
                              if (bulletTrans.X >= bossLeft && bulletTrans.X <= bossRight &&
                                  bulletTrans.Y >= bossTop && bulletTrans.Y <= bossBottom)
                              {
                                  if (!isInvulnerable)
                                  {
                                      damageTaken += 1.0f;
                                  }
                                  SpawnHitSparkEffect(world, bulletTrans.X, bulletTrans.Y);
                                  bulletEntity.Destruct();
                              }
                          });

                     if (damageTaken > 0.0f)
                     {
                         world.QueryBuilder<Health, BossStats>()
                              .With<BossTag>()
                              .Build()
                              .Each((ref Health bHealth, ref BossStats bStats) =>
                              {
                                  bHealth.Current -= damageTaken;
                                  bStats.HitFlashTimer = bCfg.HitFlashDuration;
                                  if (!bHealth.IsAlive)
                                  {
                                      bHealth.Current = 0.0f;
                                      ClearAllBossBullets(world);
                                      SpawnVictoryExplosions(world, bossX, bossY);
                                  }
                              });
                     }
                 }

                 // 2. 敵弾 vs プレイヤー中心判定点 (HP 減算 & 無敵時間判定)
                 float playerX = 0.0f;
                 float playerY = 0.0f;
                 bool isPlayerAlive = false;
                 bool isPlayerInvincible = false;

                 world.QueryBuilder<Transform, Health, PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref Health pHealth, ref PlayerState pState) =>
                      {
                          if (pHealth.IsAlive)
                          {
                              playerX = pTrans.X;
                              playerY = pTrans.Y;
                              isPlayerAlive = true;
                              isPlayerInvincible = pHealth.IsInvincible;
                          }
                      });

                 if (isPlayerAlive && !isPlayerInvincible)
                 {
                     bool playerHit = false;
                     float pRadius = pCfg.HitboxRadius;

                     world.QueryBuilder<Transform, Bullet>()
                          .With<BossBulletTag>()
                          .Build()
                          .Each((ref Transform bulletTrans, ref Bullet bullet) =>
                          {
                              if (playerHit)
                              {
                                  return;
                              }

                              float dx = bulletTrans.X - playerX;
                              float dy = bulletTrans.Y - playerY;
                              float distanceSq = dx * dx + dy * dy;
                              float combinedRadius = bullet.Radius + pRadius;

                              if (distanceSq <= combinedRadius * combinedRadius)
                              {
                                  playerHit = true;
                              }
                          });

                     if (playerHit)
                     {
                         world.QueryBuilder<Health, PlayerState, Sprite>()
                              .With<PlayerTag>()
                              .Build()
                              .Each((ref Health pHealth, ref PlayerState pState, ref Sprite pSprite) =>
                              {
                                  if (!pHealth.IsInvincible)
                                  {
                                      pHealth.Current -= 1.0f;
                                      pHealth.InvincibleTimer = pCfg.InvincibleDuration;
                                      SpawnHitSparkEffect(world, playerX, playerY);

                                      if (!pHealth.IsAlive)
                                      {
                                          pHealth.Current = 0.0f;
                                          pSprite.IsVisible = false;
                                          SpawnPlayerDeathParticles(world, playerX, playerY);
                                      }
                                  }
                              });
                     }
                 }
             });
    }

    private static void RegisterRestartSystem(World world)
    {
        world.System("RestartSystem")
             .Kind(Ecs.PostUpdate)
             .Run((Iter it) =>
             {
                 bool isRestart = GameEngine.CheckHitKey(0x13) != 0; // R key
                 bool restartJustPressed = isRestart && !_prevRestartKey;
                 _prevRestartKey = isRestart;

                 if (restartJustPressed)
                 {
                     PlayerConfig pCfg = ConfigManager.Player;
                     BossConfig bCfg = ConfigManager.Boss;

                     ClearAllBossBullets(world);

                     world.QueryBuilder()
                          .With<PlayerBulletTag>()
                          .Build()
                          .Each((Entity e) => e.Destruct());

                     world.QueryBuilder()
                          .With<EffectTag>()
                          .Build()
                          .Each((Entity e) => e.Destruct());

                     world.QueryBuilder<Transform, Velocity, CollisionBox, Health, DoubleJump, PlayerState, Animation, Sprite>()
                          .With<PlayerTag>()
                          .Build()
                          .Each((ref Transform trans, ref Velocity vel, ref CollisionBox col, ref Health health, ref DoubleJump doubleJump, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
                          {
                              trans.X = pCfg.SpawnX;
                              trans.Y = pCfg.SpawnY;
                              vel.Vx = 0.0f;
                              vel.Vy = 0.0f;
                              health.Current = pCfg.MaxHp;
                              health.Max = pCfg.MaxHp;
                              health.InvincibleTimer = 0.0f;
                              doubleJump.Reset();
                              playerState.ShootCooldown = 0.0f;
                              col.IsGrounded = true;
                              anim.CurrentClip = "Idle";
                              anim.Timer = 0.0f;
                              sprite.IsVisible = true;
                              sprite.FlipX = false;
                          });

                     world.QueryBuilder<Transform, Health, BossStats, BossStateMachine>()
                          .With<BossTag>()
                          .Build()
                          .Each((ref Transform trans, ref Health health, ref BossStats stats, ref BossStateMachine fsm) =>
                          {
                              trans.X = bCfg.BaseX;
                              trans.Y = bCfg.BaseY;
                              health.Current = bCfg.MaxHp;
                              health.Max = bCfg.MaxHp;
                              health.InvincibleTimer = 0.0f;
                              stats.Phase = 1;
                              stats.AttackTimer1 = 0.5f;
                              stats.AttackTimer2 = 1.0f;
                              stats.AngleOffset = 0.0f;
                              stats.HitFlashTimer = 0.0f;
                              stats.HoverTimer = 0.0f;

                              fsm.CurrentState = BossStateKind.Hovering;
                              fsm.StateTimer = 0.0f;
                              fsm.StateDuration = bCfg.Phase1.HoverDuration;
                              fsm.CurrentSkillIndex = 0;
                              fsm.SubTimer = 0.0f;
                              fsm.AngleOffset = 0.0f;
                              fsm.IsInvulnerable = false;
                          });
                 }
             });
    }

    private static void SpawnBossBullet(World world, float x, float y, float vx, float vy, float radius, Color fillColor, Color outlineColor)
    {
        world.Entity()
             .Set(new Transform(x, y))
             .Set(new Velocity(vx, vy))
             .Set(new Bullet(radius, fillColor, outlineColor))
             .Add<BossBulletTag>();
    }

    private static void ClearAllBossBullets(World world)
    {
        world.QueryBuilder()
             .With<BossBulletTag>()
             .Build()
             .Each((Entity e) => e.Destruct());
    }

    private static void SpawnJumpRingEffect(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 1.0f, scaleY: 1.0f))
             .Set(FadeEffect.FadeOut(0.18f))
             .Add<EffectTag>();
    }

    private static void SpawnHitSparkEffect(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 1.5f, scaleY: 1.5f))
             .Set(FadeEffect.FadeOut(0.12f))
             .Set(new HitFlashEffect(0.08f, Color.FromArgb(0xFF, 0xFF, 0xFF, 0x55)))
             .Add<EffectTag>();
    }

    private static void SpawnTelegraphSpark(World world, float x, float y, Color color)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 2.2f, scaleY: 2.2f))
             .Set(FadeEffect.FadeOut(0.08f))
             .Set(new HitFlashEffect(0.06f, color))
             .Add<EffectTag>();
    }

    private static void SpawnPlayerDeathParticles(World world, float x, float y)
    {
        for (int i = 0; i < 16; i++)
        {
            float angle = (i / 16.0f) * MathF.PI * 2.0f;
            float speed = 4.0f + (i % 3) * 2.0f;
            world.Entity()
                 .Set(new Transform(x, y))
                 .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
                 .Set(new Bullet(5.0f, Color.FromArgb(0xFF, 0xFF, 0x33, 0x33), Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)))
                 .Set(FadeEffect.FadeOut(0.60f))
                 .Add<EffectTag>();
        }
    }

    private static void SpawnShockwave(World world, float x, float y)
    {
        world.Entity()
             .Set(new Transform(x, y, scaleX: 3.5f, scaleY: 3.5f))
             .Set(FadeEffect.FadeOut(0.40f))
             .Add<EffectTag>();
    }

    private static void SpawnVictoryExplosions(World world, float x, float y)
    {
        for (int i = 0; i < 32; i++)
        {
            float angle = (i / 32.0f) * MathF.PI * 2.0f;
            float speed = 3.0f + (i % 5) * 2.5f;
            world.Entity()
                 .Set(new Transform(x, y))
                 .Set(new Velocity(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed))
                 .Set(new Bullet(6.0f, Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00), Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75)))
                 .Set(FadeEffect.FadeOut(0.90f))
                 .Add<EffectTag>();
        }
    }
}
