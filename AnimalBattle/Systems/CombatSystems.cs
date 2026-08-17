using System;
using System.Collections.Generic;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤーの移動・二段跳躍・射撃、Boss 分層状態機 (HFSM: Phase -> Hovering -> Telegraphing -> Casting -> Recovering)、
/// 弾物理、当たり判定、および R キー即時リセットを TOML 設定に基づいて駆動する ECS 常駐 Routine (System)。
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

        world.System<Transform, Velocity, CollisionBox, PlayerState, Animation, Sprite>("PlayerMovementSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((ref Transform trans, ref Velocity vel, ref CollisionBox col, ref PlayerState playerState, ref Animation anim, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;

                 bool isLeft = GameEngine.CheckHitKey(0x1E) != 0 || GameEngine.CheckHitKey(0xCB) != 0;
                 bool isRight = GameEngine.CheckHitKey(0x20) != 0 || GameEngine.CheckHitKey(0xCD) != 0;
                 bool isJump = GameEngine.CheckHitKey(0x11) != 0 || GameEngine.CheckHitKey(0x39) != 0 || GameEngine.CheckHitKey(0xC8) != 0;

                 int pad = GameEngine.GetJoypadInputState(0);
                 isLeft = isLeft || (pad & 0x0001) != 0;
                 isRight = isRight || (pad & 0x0002) != 0;
                 isJump = isJump || (pad & 0x0010) != 0;

                 bool jumpJustPressed = isJump && !_prevJumpKey;
                 _prevJumpKey = isJump;

                 if (!playerState.IsAlive)
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

                 if (jumpJustPressed)
                 {
                     if (col.IsGrounded)
                     {
                         vel.Vy = pCfg.FirstJumpVelocity;
                         col.IsGrounded = false;
                         playerState.JumpCount = 1;
                     }
                     else if (playerState.JumpCount > 0)
                     {
                         vel.Vy = pCfg.SecondJumpVelocity;
                         playerState.JumpCount--;
                         SpawnJumpRingEffect(world, trans.X, trans.Y + col.Height / 2.0f);
                     }
                 }

                 vel.Vy += pCfg.Gravity;
                 if (vel.Vy > pCfg.MaxFallSpeed)
                 {
                     vel.Vy = pCfg.MaxFallSpeed;
                 }

                 float nextX = trans.X + vel.Vx;
                 float nextY = trans.Y + vel.Vy;

                 if (nextX < 25.0f)
                 {
                     nextX = 25.0f;
                 }
                 else if (nextX > 1255.0f)
                 {
                     nextX = 1255.0f;
                 }

                 float playerBottom = nextY + col.Height / 2.0f;
                 float playerLeft = nextX - col.Width / 2.0f;
                 float playerRight = nextX + col.Width / 2.0f;
                 float colHeight = col.Height;

                 bool isGrounded = false;
                 float adjustedPosY = nextY;
                 float adjustedVy = vel.Vy;

                 platformQuery.Each((Entity pe, ref Transform pt, ref CollisionBox pc) =>
                 {
                     float platTop = pt.Y - pc.Height / 2.0f;
                     float platBottom = pt.Y + pc.Height / 2.0f;
                     float platLeft = pt.X - pc.Width / 2.0f;
                     float platRight = pt.X + pc.Width / 2.0f;

                     if (playerRight > platLeft && playerLeft < platRight)
                     {
                         if (playerBottom >= platTop && playerBottom <= platBottom + 14.0f && adjustedVy >= 0.0f)
                         {
                             adjustedPosY = platTop - colHeight / 2.0f;
                             adjustedVy = 0.0f;
                             isGrounded = true;
                         }
                     }
                 });

                 if (isGrounded)
                 {
                     playerState.JumpCount = playerState.MaxJumps;
                 }

                 trans.X = nextX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;

                 anim.Timer += 0.016f;
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
        world.System<Transform, PlayerState, Sprite>("PlayerShootSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((ref Transform trans, ref PlayerState playerState, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;

                 bool isShoot = GameEngine.CheckHitKey(0x24) != 0 || GameEngine.CheckHitKey(0x2C) != 0;
                 int pad = GameEngine.GetJoypadInputState(0);
                 isShoot = isShoot || (pad & 0x0020) != 0;

                 bool shootJustPressed = isShoot && !_prevShootKey;
                 _prevShootKey = isShoot;

                 if (!playerState.IsAlive)
                 {
                     return;
                 }

                 if (playerState.ShootCooldown > 0.0f)
                 {
                     playerState.ShootCooldown -= 0.016f;
                 }

                 if (shootJustPressed && playerState.ShootCooldown <= 0.0f)
                 {
                     float bulletDir = sprite.FlipX ? -1.0f : 1.0f;
                     float spawnX = trans.X + bulletDir * pCfg.BulletSpawnOffset;
                     float spawnY = trans.Y - 4.0f;

                     world.Entity()
                          .Set(new Transform(spawnX, spawnY))
                          .Set(new Velocity(bulletDir * pCfg.BulletSpeed, 0.0f))
                          .Set(new CollisionBox(12.0f, 8.0f))
                          .Add<PlayerBulletTag>();

                     playerState.ShootCooldown = pCfg.ShootCooldown;
                 }
             });
    }

    private static void RegisterBossAiSystem(World world)
    {
        world.System<Transform, BossStats, BossStateMachine, Sprite>("BossAiSystem")
             .With<BossTag>()
             .Kind(Ecs.OnUpdate)
             .Each((ref Transform trans, ref BossStats stats, ref BossStateMachine fsm, ref Sprite sprite) =>
             {
                 BossConfig bCfg = ConfigManager.Boss;
                 PatternConfig patternCfg = ConfigManager.Pattern;

                 float playerX = 640.0f;
                 float playerY = 600.0f;
                 bool isPlayerAlive = false;

                 world.QueryBuilder<Transform, PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref PlayerState pState) =>
                      {
                          playerX = pTrans.X;
                          playerY = pTrans.Y;
                          isPlayerAlive = pState.IsAlive;
                      });

                 if (stats.HitFlashTimer > 0.0f)
                 {
                     stats.HitFlashTimer -= 0.016f;
                 }

                 if (stats.CurrentHp <= 0.0f)
                 {
                     stats.CurrentHp = 0.0f;
                     return;
                 }

                 float hpRatio = stats.CurrentHp / stats.MaxHp;
                 int targetPhase = hpRatio > bCfg.Phase2.HpThreshold ? 1 : (hpRatio > bCfg.Phase3.HpThreshold ? 2 : 3);

                 if (targetPhase != stats.Phase)
                 {
                     stats.Phase = targetPhase;
                     PhaseFsmConfig newPhaseCfg = stats.Phase == 2 ? bCfg.Phase2 : bCfg.Phase3;
                     fsm.CurrentState = BossStateKind.PhaseTransition;
                     fsm.StateTimer = 0.0f;
                     fsm.StateDuration = newPhaseCfg.TransitionDuration;
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

                 stats.HoverTimer += 0.016f;
                 trans.X = bCfg.BaseX + MathF.Sin(stats.HoverTimer * currentPhaseCfg.HoverSpeedX) * currentPhaseCfg.HoverAmpX;
                 trans.Y = bCfg.BaseY + MathF.Cos(stats.HoverTimer * currentPhaseCfg.HoverSpeedY) * currentPhaseCfg.HoverAmpY;

                 fsm.StateTimer += 0.016f;

                 switch (fsm.CurrentState)
                 {
                     case BossStateKind.PhaseTransition:
                         fsm.IsInvulnerable = true;
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.IsInvulnerable = false;
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                             fsm.CurrentSkillIndex = 0;
                         }
                         break;

                     case BossStateKind.Hovering:
                         if (isPlayerAlive && fsm.StateTimer >= fsm.StateDuration)
                         {
                             List<string> skills = currentPhaseCfg.Skills;
                             if (skills.Count > 0)
                             {
                                 fsm.CurrentSkillName = skills[fsm.CurrentSkillIndex % skills.Count];
                                 fsm.CurrentSkillIndex++;

                                 float windUp = GetSkillWindUpTime(patternCfg, fsm.CurrentSkillName);
                                 fsm.CurrentState = BossStateKind.Telegraphing;
                                 fsm.StateTimer = 0.0f;
                                 fsm.StateDuration = windUp;
                             }
                         }
                         break;

                     case BossStateKind.Telegraphing:
                         uint telegraphColor = GetSkillTelegraphColor(patternCfg, fsm.CurrentSkillName);
                         SpawnTelegraphSpark(world, trans.X, trans.Y, telegraphColor);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float castDuration = GetSkillCastDuration(patternCfg, fsm.CurrentSkillName);
                             fsm.CurrentState = BossStateKind.Casting;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = castDuration;
                             fsm.SubTimer = 0.0f;
                         }
                         break;

                     case BossStateKind.Casting:
                         ExecuteSkillPattern(world, trans.X, trans.Y, playerX, playerY, patternCfg, ref fsm);

                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             float recoveryTime = GetSkillRecoveryTime(patternCfg, fsm.CurrentSkillName);
                             fsm.CurrentState = BossStateKind.Recovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = recoveryTime;
                         }
                         break;

                     case BossStateKind.Recovering:
                         if (fsm.StateTimer >= fsm.StateDuration)
                         {
                             fsm.CurrentState = BossStateKind.Hovering;
                             fsm.StateTimer = 0.0f;
                             fsm.StateDuration = currentPhaseCfg.HoverDuration;
                         }
                         break;
                 }
             });
    }

    private static float GetSkillWindUpTime(PatternConfig config, string skillName) => skillName switch
    {
        "Aimed3Shot" => config.Aimed3Shot.WindUpTime,
        "RingBurst" => config.RingBurst.WindUpTime,
        "DoubleSpiral" => config.DoubleSpiral.WindUpTime,
        "RainDrop" => config.RainDrop.WindUpTime,
        "CrossChaos" => config.CrossChaos.WindUpTime,
        "FastSniper" => config.FastSniper.WindUpTime,
        _ => 0.5f
    };

    private static uint GetSkillTelegraphColor(PatternConfig config, string skillName) => skillName switch
    {
        "Aimed3Shot" => config.Aimed3Shot.TelegraphColor,
        "RingBurst" => config.RingBurst.TelegraphColor,
        "DoubleSpiral" => config.DoubleSpiral.TelegraphColor,
        "RainDrop" => config.RainDrop.TelegraphColor,
        "CrossChaos" => config.CrossChaos.TelegraphColor,
        "FastSniper" => config.FastSniper.TelegraphColor,
        _ => 0xFFFFFFFF
    };

    private static float GetSkillCastDuration(PatternConfig config, string skillName) => skillName switch
    {
        "Aimed3Shot" => config.Aimed3Shot.CastDuration,
        "RingBurst" => config.RingBurst.CastDuration,
        "DoubleSpiral" => config.DoubleSpiral.CastDuration,
        "RainDrop" => config.RainDrop.CastDuration,
        "CrossChaos" => config.CrossChaos.CastDuration,
        "FastSniper" => config.FastSniper.CastDuration,
        _ => 1.0f
    };

    private static float GetSkillRecoveryTime(PatternConfig config, string skillName) => skillName switch
    {
        "Aimed3Shot" => config.Aimed3Shot.RecoveryTime,
        "RingBurst" => config.RingBurst.RecoveryTime,
        "DoubleSpiral" => config.DoubleSpiral.RecoveryTime,
        "RainDrop" => config.RainDrop.RecoveryTime,
        "CrossChaos" => config.CrossChaos.RecoveryTime,
        "FastSniper" => config.FastSniper.RecoveryTime,
        _ => 0.5f
    };

    private static void ExecuteSkillPattern(World world, float bx, float by, float px, float py, PatternConfig patternCfg, ref BossStateMachine fsm)
    {
        fsm.SubTimer -= 0.016f;

        switch (fsm.CurrentSkillName)
        {
            case "Aimed3Shot":
                if (fsm.SubTimer <= 0.0f)
                {
                    fsm.SubTimer = 0.45f;
                    Aimed3ShotConfig p = patternCfg.Aimed3Shot;
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
                    fsm.SubTimer = 0.50f;
                    RingBurstConfig p = patternCfg.RingBurst;
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
                    fsm.SubTimer = 0.30f;
                    float aimAngle = MathF.Atan2(py - by, px - bx);
                    SpawnBossBullet(world, bx, by, MathF.Cos(aimAngle) * p.BulletSpeed, MathF.Sin(aimAngle) * p.BulletSpeed, p.BulletRadius, p.FillColor, p.OutlineColor);
                }
                break;
        }
    }

    private static void RegisterBulletPhysicsSystems(World world)
    {
        world.System<Transform, Velocity, Bullet>("BossBulletPhysicsSystem")
             .With<BossBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel, ref Bullet bullet) =>
             {
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;
                 bullet.Lifetime += 0.016f;

                 if (trans.X < -60.0f || trans.X > 1340.0f || trans.Y < -60.0f || trans.Y > 780.0f)
                 {
                     e.Destruct();
                 }
             });

        world.System<Transform, Velocity>("PlayerBulletPhysicsSystem")
             .With<PlayerBulletTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Entity e, ref Transform trans, ref Velocity vel) =>
             {
                 trans.X += vel.Vx;
                 trans.Y += vel.Vy;

                 if (trans.X < -30.0f || trans.X > 1310.0f)
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

                 world.QueryBuilder<Transform, BossStats, BossStateMachine>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform bTrans, ref BossStats bStats, ref BossStateMachine bFsm) =>
                      {
                          if (bStats.CurrentHp > 0.0f)
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
                         world.QueryBuilder<BossStats>()
                              .With<BossTag>()
                              .Build()
                              .Each((ref BossStats bStats) =>
                              {
                                  bStats.CurrentHp -= damageTaken;
                                  bStats.HitFlashTimer = bCfg.HitFlashDuration;
                                  if (bStats.CurrentHp <= 0.0f)
                                  {
                                      bStats.CurrentHp = 0.0f;
                                      ClearAllBossBullets(world);
                                      SpawnVictoryExplosions(world, bossX, bossY);
                                  }
                              });
                     }
                 }

                 // 2. 敵弾 vs プレイヤー中心判定点 (1-Hit 即死)
                 float playerX = 0.0f;
                 float playerY = 0.0f;
                 bool isPlayerAlive = false;

                 world.QueryBuilder<Transform, PlayerState>()
                      .With<PlayerTag>()
                      .Build()
                      .Each((ref Transform pTrans, ref PlayerState pState) =>
                      {
                          if (pState.IsAlive)
                          {
                              playerX = pTrans.X;
                              playerY = pTrans.Y;
                              isPlayerAlive = true;
                          }
                      });

                 if (isPlayerAlive)
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
                         world.QueryBuilder<PlayerState, Sprite>()
                              .With<PlayerTag>()
                              .Build()
                              .Each((ref PlayerState pState, ref Sprite pSprite) =>
                              {
                                  pState.IsAlive = false;
                                  pSprite.IsVisible = false;
                              });

                         SpawnPlayerDeathParticles(world, playerX, playerY);
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

                     world.QueryBuilder<Transform, Velocity, PlayerState, CollisionBox, Animation, Sprite>()
                          .With<PlayerTag>()
                          .Build()
                          .Each((ref Transform trans, ref Velocity vel, ref PlayerState playerState, ref CollisionBox col, ref Animation anim, ref Sprite sprite) =>
                          {
                              trans.X = pCfg.SpawnX;
                              trans.Y = pCfg.SpawnY;
                              vel.Vx = 0.0f;
                              vel.Vy = 0.0f;
                              playerState.IsAlive = true;
                              playerState.JumpCount = playerState.MaxJumps;
                              playerState.ShootCooldown = 0.0f;
                              col.IsGrounded = true;
                              anim.CurrentClip = "Idle";
                              anim.Timer = 0.0f;
                              sprite.IsVisible = true;
                              sprite.FlipX = false;
                          });

                     world.QueryBuilder<Transform, BossStats, BossStateMachine>()
                          .With<BossTag>()
                          .Build()
                          .Each((ref Transform trans, ref BossStats stats, ref BossStateMachine fsm) =>
                          {
                              trans.X = bCfg.BaseX;
                              trans.Y = bCfg.BaseY;
                              stats.CurrentHp = bCfg.MaxHp;
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

    private static void SpawnBossBullet(World world, float x, float y, float vx, float vy, float radius, uint fillColor, uint outlineColor)
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
             .Set(new HitFlashEffect(0.08f, 0xFFFFFF55))
             .Add<EffectTag>();
    }

    private static void SpawnTelegraphSpark(World world, float x, float y, uint color)
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
                 .Set(new Bullet(5.0f, 0xFFFF3333, 0xFFFFFFFF))
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
                 .Set(new Bullet(6.0f, 0xFFFFD700, 0xFFFFFFFF))
                 .Set(FadeEffect.FadeOut(1.2f))
                 .Add<EffectTag>();
        }
    }
}
