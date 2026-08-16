using System;
using Flecs.NET.Core;
using AnimalBattle.Components;
using AnimalBattle.Core;

namespace AnimalBattle.Systems;
/// <summary>
/// 重力、運動積分、地形（足場）との AABB 衝突判定、およびノックバック減衰を計算する Flecs.NET ECS システム。
/// </summary>
public static class PhysicsSystems
{
    private const float _gravity = 0.48f;
    private const float _friction = 0.88f;

    /// <summary>
    /// 全アクティブエンティティの重力加速、ノックバック運動、足場との接地衝突判定を毎フレーム実行します。
    /// </summary>
    public static readonly Action<World> PhysicsSystem = (world) =>
    {
        Query<Transform, CollisionBox> platformQuery =
            world.QueryBuilder<Transform, CollisionBox>()
                 .With<PlatformTag>()
                 .Build();

        world.QueryBuilder<Transform, Velocity, CollisionBox>()
             .With<PlayerTag>()
             .Build()
             .Each((ref Transform trans, ref Velocity vel, ref CollisionBox col) =>
             {
                 vel.KnockbackX *= _friction;
                 vel.KnockbackY += _gravity;

                 if (Math.Abs(vel.KnockbackX) < 2.0f)
                 {
                     vel.KnockbackX = 0.0f;
                 }
                 if (Math.Abs(vel.KnockbackY) < 2.0f)
                 {
                     vel.KnockbackY = 0.0f;
                 }

                 vel.Vy += _gravity;
                 float posX = trans.X + (vel.Vx + vel.KnockbackX);
                 float posY = trans.Y + (vel.Vy + vel.KnockbackY);

                 float playerBottom = posY + col.Height / 2.0f;
                 float playerLeft = posX - col.Width / 2.0f;
                 float playerRight = posX + col.Width / 2.0f;
                 float colHeight = col.Height;
                 float vy = vel.Vy;

                 bool isGrounded = false;
                 float adjustedPosY = posY;
                 float adjustedVy = vy;

                 platformQuery.Each((Entity pe, ref Transform pt, ref CollisionBox pc) =>
                 {
                     if (!pe.Has<PlatformTag>())
                     {
                         return;
                     }

                     float platTop = pt.Y - pc.Height / 2.0f;
                     float platBottom = pt.Y + pc.Height / 2.0f;
                     float platLeft = pt.X - pc.Width / 2.0f;
                     float platRight = pt.X + pc.Width / 2.0f;

                     if (playerRight > platLeft && playerLeft < platRight)
                     {
                         if (playerBottom >= platTop && playerBottom <= platBottom + 12.0f && adjustedVy >= 0.0f)
                         {
                             adjustedPosY = platTop - colHeight / 2.0f;
                             adjustedVy = 0.0f;
                             isGrounded = true;
                         }
                     }
                 });

                 trans.X = posX;
                 trans.Y = adjustedPosY;
                 vel.Vy = adjustedVy;
                 col.IsGrounded = isGrounded;
             });
    };
}
