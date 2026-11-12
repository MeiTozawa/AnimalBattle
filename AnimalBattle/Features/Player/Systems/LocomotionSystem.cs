namespace AnimalBattle.Player.Systems;

using System;
using AnimalBattle.Core;
using AnimalBattle.Player.Components;

/// <summary>
///     自機の水平移動加減速、急停止制動、可変高度跳躍減衰および頂点滞空重力を統合処理するシステム。
/// </summary>
public static class LocomotionSystem
{
    /// <summary>
    ///     運動キネマティクスシステムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddLocomotionSystem(this World world)
    {
        world.System<Velocity, CollisionBox, MovementIntent, LocomotionCapability, JumpCapability>("LocomotionSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Without<DashingTag>()
            .Iter(ExecuteLocomotion);

        return world;
    }

    private static void ExecuteLocomotion(
        Iter it,
        Field<Velocity> velocities,
        Field<CollisionBox> collisionBoxes,
        Field<MovementIntent> intents,
        Field<LocomotionCapability> locos,
        Field<JumpCapability> jumps)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();

        for (var i = 0; i < count; i++)
        {
            ref var vel = ref velocities[i];
            ref readonly var col = ref collisionBoxes[i];
            ref readonly var intent = ref intents[i];
            ref readonly var loco = ref locos[i];
            ref readonly var jump = ref jumps[i];

            // 1. 水平移動の加減速・摩擦制動の適用
            var targetVx = intent.MoveX * loco.MoveSpeed;
            var currentVx = vel.Value.X;

            if (loco.GroundAcceleration > 0.0f)
            {
                float accel;
                if (col.IsGrounded)
                {
                    if (MathF.Abs(targetVx) < float.Epsilon)
                    {
                        // 停止：接地自然減速
                        accel = loco.GroundDeceleration;
                    }
                    else if (MathF.Sign(targetVx) != MathF.Sign(currentVx) && MathF.Abs(currentVx) > float.Epsilon)
                    {
                        // 反転：急停止制動
                        accel = loco.TurnaroundDeceleration;
                    }
                    else
                    {
                        // 通常地上加速
                        accel = loco.GroundAcceleration;
                    }
                }
                else
                {
                    // 空中制御：入力有無に応じた加減速
                    accel = MathF.Abs(targetVx) > float.Epsilon ? loco.AirAcceleration : loco.AirDeceleration;
                }

                currentVx = PhysicsService.MoveTowards(currentVx, targetVx, accel * dt);
            }
            else
            {
                currentVx = targetVx;
            }

            var currentVy = vel.Value.Y;

            // 2. 重力加速度および可変高度・頂点滞空の適用
            var effectiveGravity = loco.Gravity;

            // キー解放時の上昇速度減衰：小ジャンプ制御
            if (currentVy < 0.0f && !intent.JumpHolding && jump.JumpCutDamping > 0.0f)
            {
                effectiveGravity /= jump.JumpCutDamping;
            }
            else if (MathF.Abs(currentVy) < jump.ApexFloatVelocityThreshold && jump.ApexFloatGravityMultiplier > 0.0f)
            {
                // 頂点付近の滞空時間延長：浮遊感の付与
                effectiveGravity *= jump.ApexFloatGravityMultiplier;
            }

            PhysicsService.ApplyGravity(ref currentVy, effectiveGravity, loco.MaxFallSpeed, dt);

            vel.Value = new Vec2f(currentVx, currentVy);
        }
    }
}
