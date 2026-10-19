using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     照準器（AimReticle）のスプリング物理拡縮状態（反動・減衰）を更新する ECS システム。
///     射撃イベントを購読して反動拡縮を付与し、PreUpdate フェーズで物理方程式に従い静止状態へ減衰させます。
/// </summary>
public static class AimReticleUpdateSystem {
    private static bool _isSubscribed;

    /// <summary>
    ///     照準器物理更新システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddAimReticleUpdateSystem(this World world)
    {
        world.Set(new AimReticleState());

        if (!_isSubscribed)
        {
            EventBus.Subscribe<PlayerShotFiredEvent>(_ =>
            {
                if (world.Has<AimReticleState>())
                {
                    var cfg = world.Get<RenderConfig>().AimReticle;
                    ref var state = ref world.GetMut<AimReticleState>();
                    state.Expansion = cfg.RecoilExpansion;
                    state.ExpansionVelocity = 0.0f;
                }
            });
            _isSubscribed = true;
        }

        // レティクル拡縮スプリング物理更新 (PreUpdate)
        world.System("AimReticleUpdateSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it) =>
            {
                var dt = it.DeltaTime();
                if (dt <= 0.0f) return;

                var w = it.World();
                if (!w.Has<AimReticleState>()) return;

                ref var state = ref w.GetMut<AimReticleState>();
                UpdateSpring(ref state, dt, w.Get<RenderConfig>().AimReticle);
            });

        return world;
    }

    private static void UpdateSpring(ref AimReticleState state, float deltaTime, AimReticleRenderConfig cfg)
    {
        var safeDt = MathF.Min(deltaTime, 0.1f);
        var frequency = cfg.SpringFrequency;
        var dampingRatio = cfg.SpringDampingRatio;

        if (frequency <= 0.0f)
        {
            state.Expansion = 0.0f;
            state.ExpansionVelocity = 0.0f;
            return;
        }

        var omega = MathF.Tau * frequency;
        var p = state.Expansion; // target = 0.0f

        if (dampingRatio < 1.0f)
        {
            // 減衰不足（Underdamped）
            var omegaD = omega * MathF.Sqrt(1.0f - dampingRatio * dampingRatio);
            var gamma = dampingRatio * omega;
            var exp = MathF.Exp(-gamma * safeDt);
            var cos = MathF.Cos(omegaD * safeDt);
            var sin = MathF.Sin(omegaD * safeDt);

            var newP = exp * (p * cos + (state.ExpansionVelocity + gamma * p) / omegaD * sin);
            var newV = exp * (state.ExpansionVelocity * cos - (gamma * state.ExpansionVelocity + omega * omega * p) / omegaD * sin);

            state.Expansion = newP;
            state.ExpansionVelocity = newV;
        }
        else if (MathF.Abs(dampingRatio - 1.0f) < 0.0001f)
        {
            // 臨界減衰（Critically Damped）
            var exp = MathF.Exp(-omega * safeDt);
            var newP = exp * (p + (state.ExpansionVelocity + omega * p) * safeDt);
            var newV = exp * (state.ExpansionVelocity - omega * (state.ExpansionVelocity + omega * p) * safeDt);

            state.Expansion = newP;
            state.ExpansionVelocity = newV;
        }
        else
        {
            // 過減衰（Overdamped）
            var omegaD = omega * MathF.Sqrt(dampingRatio * dampingRatio - 1.0f);
            var gamma1 = -dampingRatio * omega + omegaD;
            var gamma2 = -dampingRatio * omega - omegaD;
            var exp1 = MathF.Exp(gamma1 * safeDt);
            var exp2 = MathF.Exp(gamma2 * safeDt);

            var c2 = (state.ExpansionVelocity - gamma1 * p) / (gamma2 - gamma1);
            var c1 = p - c2;

            state.Expansion = c1 * exp1 + c2 * exp2;
            state.ExpansionVelocity = c1 * gamma1 * exp1 + c2 * gamma2 * exp2;
        }

        if (MathF.Abs(state.Expansion) < 0.001f && MathF.Abs(state.ExpansionVelocity) < 0.001f)
        {
            state.Expansion = 0.0f;
            state.ExpansionVelocity = 0.0f;
        }

        // 異常発散やオーバーフローを確実に防止する防護クランプ
        state.Expansion = Math.Clamp(state.Expansion, -cfg.BaseRadius + 1.0f, cfg.RecoilExpansion * cfg.RecoilExpansionClampRatio);
    }
}
