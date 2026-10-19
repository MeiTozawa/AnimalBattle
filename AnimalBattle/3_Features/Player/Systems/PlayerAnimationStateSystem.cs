using System;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using Flecs.NET.Core;

namespace AnimalBattle.Systems;

/// <summary>
///     自機の運動状態（接地・跳躍・落下・走行）に応じて適切なアニメーションクリップを選択するシステム。
/// </summary>
public static class PlayerAnimationStateSystem
{
    private static readonly StringId ClipIdle = "Idle";
    private static readonly StringId ClipRun = "Run";
    private static readonly StringId ClipJump = "Jump";
    private static readonly StringId ClipFall = "Fall";
    private static readonly StringId ClipAttack = "Attack";

    /// <summary>
    ///     自機アニメーション状態選択システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">登録先の Flecs.NET ワールド。</param>
    /// <returns>メソッドチェーン用の <see cref="World" />。</returns>
    public static World AddPlayerAnimationStateSystem(this World world)
    {
        world.System<Velocity, CollisionBox, Health, Animation>("PlayerAnimationStateSystem")
            .With<PlayerTag>()
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it, Field<Velocity> velocities, Field<CollisionBox> collisionBoxes, Field<Health> healths, Field<Animation> anims) =>
            {
                var count = it.Count();
                var w = it.World();
                ref readonly var catCfg = ref w.Get<CatConfig>();
                var threshold = catCfg.WalkAnimVelocityThreshold;

                for (var i = 0; i < count; i++)
                {
                    ref readonly var hp = ref healths[i];
                    if (hp.Current <= 0.0f) continue;

                    ref var anim = ref anims[i];
                    if (anim.CurrentClip == ClipAttack && !anim.IsFinished) continue;

                    ref readonly var col = ref collisionBoxes[i];
                    ref readonly var vel = ref velocities[i];

                    if (!col.IsGrounded)
                    {
                        anim.Play(vel.Vy < 0.0f ? ClipJump : ClipFall);
                    }
                    else
                    {
                        anim.Play(MathF.Abs(vel.Vx) > threshold ? ClipRun : ClipIdle);
                    }
                }
            });

        return world;
    }
}
