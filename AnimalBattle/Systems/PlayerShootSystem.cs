using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー自機の 8 方向瞄準および手銃射撃を制御する ECS System。
/// </summary>
public static class PlayerShootSystem
{
    private static bool _prevShootKey;

    /// <summary>
    /// プレイヤー射撃システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    public static void Register(World world)
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
}
