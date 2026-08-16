using System;
using Flecs.NET.Core;
using AnimalBattle.Animations;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤーの移動・二段跳躍・射撃、Boss AI（3 フェーズ）、弾物理、当たり判定、および R キー即時リセットを
/// TOML 設定ファイル（player.toml, boss.toml, patterns.toml, arena.toml）に基づいて駆動する ECS 常駐 Routine (System)。
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
        world.System<Transform, BossStats, Sprite>("BossAiSystem")
             .With<BossTag>()
             .Kind(Ecs.OnUpdate)
             .Each((ref Transform trans, ref BossStats stats, ref Sprite sprite) =>
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
                 int previousPhase = stats.Phase;

                 if (hpRatio > 0.66f)
                 {
                     stats.Phase = 1;
                 }
                 else if (hpRatio > 0.33f)
                 {
                     stats.Phase = 2;
                 }
                 else
                 {
                     stats.Phase = 3;
                 }

                 if (stats.Phase != previousPhase)
                 {
                     ClearAllBossBullets(world);
                     SpawnShockwave(world, trans.X, trans.Y);
                 }

                 stats.HoverTimer += 0.016f;

                 if (stats.Phase == 1)
                 {
                     trans.X = stats.BaseX + MathF.Sin(stats.HoverTimer * bCfg.Phase1HoverSpeedX) * bCfg.Phase1HoverAmpX;
                     trans.Y = stats.BaseY + MathF.Cos(stats.HoverTimer * bCfg.Phase1HoverSpeedY) * bCfg.Phase1HoverAmpY;
                 }
                 else if (stats.Phase == 2)
                 {
                     trans.X = stats.BaseX + MathF.Sin(stats.HoverTimer * bCfg.Phase2HoverSpeedX) * bCfg.Phase2HoverAmpX;
                     trans.Y = stats.BaseY + MathF.Cos(stats.HoverTimer * bCfg.Phase2HoverSpeedY) * bCfg.Phase2HoverAmpY;
                 }
                 else
                 {
                     trans.X = stats.BaseX + MathF.Sin(stats.HoverTimer * bCfg.Phase3HoverSpeedX) * bCfg.Phase3HoverAmpX;
                     trans.Y = stats.BaseY + MathF.Cos(stats.HoverTimer * bCfg.Phase3HoverSpeedY) * bCfg.Phase3HoverAmpY;
                 }

                 if (!isPlayerAlive)
                 {
                     return;
                 }

                 stats.AttackTimer1 -= 0.016f;
                 stats.AttackTimer2 -= 0.016f;

                 // Phase 1: 自機狙い 3 連射 + 16 方向放射弾
                 if (stats.Phase == 1)
                 {
                     Phase1PatternConfig p1 = patternCfg.Phase1;
                     if (stats.AttackTimer1 <= 0.0f)
                     {
                         stats.AttackTimer1 = p1.AimedInterval;
                         float baseAngle = MathF.Atan2(playerY - trans.Y, playerX - trans.X);
                         for (int i = -1; i <= 1; i++)
                         {
                             float angle = baseAngle + i * p1.AimedSpreadAngle;
                             SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(angle) * p1.AimedSpeed, MathF.Sin(angle) * p1.AimedSpeed, p1.AimedRadius, p1.AimedFillColor, p1.AimedOutlineColor);
                         }
                     }

                     if (stats.AttackTimer2 <= 0.0f)
                     {
                         stats.AttackTimer2 = p1.RingInterval;
                         int bulletCount = Math.Max(4, p1.RingBulletCount);
                         for (int i = 0; i < bulletCount; i++)
                         {
                             float angle = (i / (float)bulletCount) * MathF.PI * 2.0f + stats.AngleOffset;
                             SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(angle) * p1.RingSpeed, MathF.Sin(angle) * p1.RingSpeed, p1.RingRadius, p1.RingFillColor, p1.RingOutlineColor);
                         }
                         stats.AngleOffset += p1.RingAngleStep;
                     }
                 }
                 // Phase 2: 双螺旋回転弾 + 天降る雨弾
                 else if (stats.Phase == 2)
                 {
                     Phase2PatternConfig p2 = patternCfg.Phase2;
                     if (stats.AttackTimer1 <= 0.0f)
                     {
                         stats.AttackTimer1 = p2.SpiralInterval;
                         float angle1 = stats.AngleOffset;
                         float angle2 = stats.AngleOffset + MathF.PI;

                         SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(angle1) * p2.SpiralSpeed, MathF.Sin(angle1) * p2.SpiralSpeed, p2.SpiralRadius, p2.SpiralFillColor1, p2.SpiralOutlineColor1);
                         SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(angle2) * p2.SpiralSpeed, MathF.Sin(angle2) * p2.SpiralSpeed, p2.SpiralRadius, p2.SpiralFillColor2, p2.SpiralOutlineColor2);

                         stats.AngleOffset += p2.SpiralAngleStep;
                     }

                     if (stats.AttackTimer2 <= 0.0f)
                     {
                         stats.AttackTimer2 = p2.RainInterval;
                         Random random = Random.Shared;
                         int minX = (int)p2.RainMinX;
                         int maxX = (int)p2.RainMaxX;
                         float rainX1 = random.Next(minX, maxX);
                         float rainX2 = random.Next(minX, maxX);
                         SpawnBossBullet(world, rainX1, 0.0f, 0.0f, p2.RainSpeed, p2.RainRadius, p2.RainFillColor, p2.RainOutlineColor);
                         SpawnBossBullet(world, rainX2, 0.0f, 0.0f, p2.RainSpeed, p2.RainRadius, p2.RainFillColor, p2.RainOutlineColor);
                     }
                 }
                 // Phase 3: 狂暴終局（全方位交差波 + 高速スナイプ弾）
                 else
                 {
                     Phase3PatternConfig p3 = patternCfg.Phase3;
                     if (stats.AttackTimer1 <= 0.0f)
                     {
                         stats.AttackTimer1 = p3.CrossInterval;
                         for (int k = 0; k < 4; k++)
                         {
                             float angle = stats.AngleOffset + k * (MathF.PI / 2.0f);
                             SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(angle) * p3.CrossSpeed, MathF.Sin(angle) * p3.CrossSpeed, p3.CrossRadius, p3.CrossFillColor, p3.CrossOutlineColor);
                         }
                         stats.AngleOffset += p3.CrossAngleStep;
                     }

                     if (stats.AttackTimer2 <= 0.0f)
                     {
                         stats.AttackTimer2 = p3.SniperInterval;
                         float aimAngle = MathF.Atan2(playerY - trans.Y, playerX - trans.X);
                         SpawnBossBullet(world, trans.X, trans.Y, MathF.Cos(aimAngle) * p3.SniperSpeed, MathF.Sin(aimAngle) * p3.SniperSpeed, p3.SniperRadius, p3.SniperFillColor, p3.SniperOutlineColor);
                     }
                 }
             });
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

                 world.QueryBuilder<Transform, BossStats>()
                      .With<BossTag>()
                      .Build()
                      .Each((ref Transform bTrans, ref BossStats bStats) =>
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
                                  damageTaken += 1.0f;
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

                     world.QueryBuilder<Transform, BossStats>()
                          .With<BossTag>()
                          .Build()
                          .Each((ref Transform trans, ref BossStats stats) =>
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
             .Set(new Transform(x, y, scaleX: 3.0f, scaleY: 3.0f))
             .Set(FadeEffect.FadeOut(0.35f))
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
