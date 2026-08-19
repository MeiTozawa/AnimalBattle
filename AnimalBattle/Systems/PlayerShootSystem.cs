using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Core;
using AnimalBattle.Core.Input;

namespace AnimalBattle.Systems;

/// <summary>
/// プレイヤー自機の 8 方向瞄準および手銃射撃を制御する ECS System。
/// 生成される銃弾エンティティに TOML 設定ファイル駆動の <see cref="Hitbox"/>（味方陣営）を付与します。
/// </summary>
public static class PlayerShootSystem
{
    /// <summary>
    /// プレイヤー射撃システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド</param>
    /// <returns>メソッドチェーン用の <see cref="World"/></returns>
    public static World AddPlayerShootSystem(this World world)
    {
        world.System<Transform, Health, PlayerState, Sprite>("PlayerShootSystem")
             .With<PlayerTag>()
             .Kind(Ecs.OnUpdate)
             .Each((Iter it, int i, ref Transform trans, ref Health health, ref PlayerState playerState, ref Sprite sprite) =>
             {
                 PlayerConfig pCfg = ConfigManager.Player;
                 float dt = it.DeltaTime();

                 bool shootJustPressed = InputService.IsPressed(InputAction.Attack);

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
                     bool isLeft = InputService.IsHolding(InputAction.Left);
                     bool isRight = InputService.IsHolding(InputAction.Right);
                     bool isUp = InputService.IsHolding(InputAction.Up);
                     bool isDown = InputService.IsHolding(InputAction.Down);

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
                          .Set(new Hitbox(pCfg.GetBulletHitboxShape(), damage: pCfg.BulletDamage, faction: Faction.Friendly, destroyOnHit: true))
                          .Add<PlayerBulletTag>();

                     playerState.ShootCooldown = pCfg.ShootCooldown;
                 }
             });

        return world;
    }
}
