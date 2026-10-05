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
            .Kind(Ecs.PreUpdate)
            .Each((ref Velocity vel, ref CollisionBox col, ref Health hp, ref Animation anim) =>
            {
                if (hp.Current <= 0.0f) return;

                if (!col.IsGrounded)
                {
                    anim.Play(vel.Vy < 0.0f ? ClipJump : ClipFall);
                }
                else
                {
                    var threshold = ConfigManager.Player != null && ConfigManager.Player.WalkAnimVelocityThreshold > 0.0f
                        ? ConfigManager.Player.WalkAnimVelocityThreshold
                        : 0.1f;

                    if (MathF.Abs(vel.Vx) > threshold)
                    {
                        anim.Play(ClipRun);
                    }
                    else
                    {
                        anim.Play(ClipIdle);
                    }
                }
            });

        return world;
    }
}
