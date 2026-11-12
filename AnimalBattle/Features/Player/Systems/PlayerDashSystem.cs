namespace AnimalBattle.Player.Systems;

using System;
using AnimalBattle.Configs;
using AnimalBattle.Events;
using AnimalBattle.Player.Components;
using AnimalBattle.Presentation.Rendering.Components;
using AnimalBattle.Presentation.Vfx.Components;

/// <summary>
///     自機の突進回避（ダッシュ）入力を検知し、突進速度の固定、無敵時間、冷却および演出トリガーを管理するシステム。
/// </summary>
public static class PlayerDashSystem
{
    /// <summary>
    ///     ダッシュ制御システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    public static World AddPlayerDashSystem(this World world)
    {
        world.System<Velocity, MovementIntent, DashCapability, DashState, Sprite, SpringScale, TimedEffectsState, GameplayTagState>("PlayerDashSystem")
            .Kind(Ecs.OnUpdate)
            .Without<DeadTag>()
            .Without<HitstopState>()
            .Iter(ExecutePlayerDash);

        return world;
    }

    private static void ExecutePlayerDash(
        Iter it,
        Field<Velocity> velocities,
        Field<MovementIntent> intents,
        Field<DashCapability> dashCaps,
        Field<DashState> dashStates,
        Field<Sprite> sprites,
        Field<SpringScale> springs,
        Field<TimedEffectsState> timedEffects,
        Field<GameplayTagState> tags)
    {
        var count = it.Count();
        if (count == 0) return;

        var dt = it.DeltaTime();

        for (var i = 0; i < count; i++)
        {
            ref var vel = ref velocities[i];
            ref readonly var intent = ref intents[i];
            ref readonly var cap = ref dashCaps[i];
            ref var dash = ref dashStates[i];
            ref var sprite = ref sprites[i];
            ref var spring = ref springs[i];
            ref var effect = ref timedEffects[i];
            ref var tag = ref tags[i];

            var entity = it.Entity(i);

            // 0. クールダウン減衰
            if (dash.Cooldown > 0.0f)
            {
                dash.Cooldown = MathF.Max(0.0f, dash.Cooldown - dt);
            }

            // スプライト向きの同期
            if (intent.FacingX != 0.0f)
            {
                sprite.FlipX = intent.FacingX < 0.0f;
            }

            // ダッシュ起動判定
            if (intent.DashTriggered && dash.Cooldown <= 0.0f && !dash.IsDashing)
            {
                dash.Timer = cap.Duration;
                dash.Cooldown = cap.Cooldown;
                dash.GhostTrailTimer = 0.0f;
                entity.Add<DashingTag>();

                // 突進方向決定：入力優先、未入力時は向いている方向
                if (MathF.Abs(intent.MoveX) > 0.01f)
                {
                    dash.DirectionX = MathF.Sign(intent.MoveX);
                }
                else
                {
                    dash.DirectionX = intent.FacingX < 0.0f ? -1.0f : 1.0f;
                }

                // 無敵時間および点滅演出の適用
                TimedEffectService.ApplyEffect(ref effect, ref tag, GameplayTag.Untargetable, cap.InvincibleDuration, entity.Id);
                var blinkFreq = entity.CsWorld().Get<PlayerVisualRenderConfig>().InvincibleBlinkFrequency;
                entity.Set(new InvincibleBlinkEffect(cap.InvincibleDuration, blinkFreq));

                // 突進時ストレッチ変形
                spring.Current = cap.StretchScale;
                spring.Velocity = Vec2f.Zero;

                EventBus.Publish(new PlayerDashedEvent());
            }

            // ダッシュ中速度維持：水平高速突進・重力無効化・ノックバック消去
            if (dash.IsDashing)
            {
                dash.Timer = MathF.Max(0.0f, dash.Timer - dt);
                vel.Value = new Vec2f(dash.DirectionX * cap.Speed, 0.0f);
                vel.Knockback = Vec2f.Zero;

                if (!dash.IsDashing)
                {
                    entity.Remove<DashingTag>();
                }
            }
        }
    }
}
