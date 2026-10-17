using System.Diagnostics;
using AnimalBattle.Components;
using AnimalBattle.Configs;
using AnimalBattle.Events;
using Flecs.NET.Core;

namespace AnimalBattle.Systems.Render;

/// <summary>
///     照準器（AimReticle）のスプリング拡縮状態更新およびスクリーン描画を制御する ECS システム。
///     ワールド常駐コンポーネント <see cref="AimReticleState" /> を通じて物理状態を管理し、
///     戦闘状態（GameState.Battle）時にのみ戦術照準器を描画します。
/// </summary>
public static class AimReticleRenderSystem {
    private static bool _isSubscribed;

    /// <summary>
    ///     照準器システムを Flecs.NET ワールドへ登録します。
    /// </summary>
    /// <param name="world">対象の Flecs.NET ワールド</param>
    /// <returns>登録済みワールド</returns>
    public static World AddAimReticleRenderSystem(this World world)
    {
        world.Set(new AimReticleState());

        if (!_isSubscribed)
        {
            EventBus.Subscribe<PlayerShotFiredEvent>(_ =>
            {
                if (world.Has<AimReticleState>())
                {
                    var cfg = ConfigManager.Render.AimReticle;
                    ref var state = ref world.GetMut<AimReticleState>();
                    state.Expansion = cfg.RecoilExpansion;
                    state.ExpansionVelocity = 0.0f;
                }
            });
            _isSubscribed = true;
        }

        // 1. レティクル拡縮スプリング物理更新 (PreUpdate)
        world.System("AimReticleUpdateSystem")
            .Kind(Ecs.PreUpdate)
            .Iter((Iter it) =>
            {
                var dt = it.DeltaTime();
                if (dt <= 0.0f) return;

                var w = it.World();
                if (!w.Has<AimReticleState>()) return;

                ref var state = ref w.GetMut<AimReticleState>();
                UpdateSpring(ref state, dt);
            });

        // 2. レティクル画面描画 (OnStore)
        world.System("AimReticleRenderSystem")
            .Kind(Ecs.OnStore)
            .Iter((Iter it) =>
            {
                var w = it.World();
                ref readonly var flow = ref w.Get<GameFlow>();
                if (flow.State != GameState.Battle) return;

                ref readonly var state = ref w.Get<AimReticleState>();
                var mouseScreen = InputService.GetMousePosition();
                RenderReticle(mouseScreen.X, mouseScreen.Y, state.Expansion);
            });

        return world;
    }

    private static void UpdateSpring(ref AimReticleState state, float deltaTime)
    {
        var cfg = ConfigManager.Render.AimReticle;
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

    private static void RenderReticle(int x, int y, float expansion)
    {
        var cfg = ConfigManager.Render.AimReticle;
        var radius = Math.Clamp((int)MathF.Round(cfg.BaseRadius + expansion), cfg.MinRadius, cfg.MaxRadius);

        // 1. 中心高輝度ドット（照準中心コア）
        GameEngine.DrawCircle(x, y, cfg.CenterDotRadius, cfg.CenterDotColor, fill: true);

        // 2. 外周円環（タクティカルリング）
        GameEngine.DrawCircle(x, y, radius, cfg.RingColor, fill: false);

        // 3. 上下左右 4 方向の刻度ショートライン
        var inner = radius + cfg.TickGap;
        var outer = inner + cfg.TickLength;

        // 上
        GameEngine.DrawLine(x, y - inner, x, y - outer, cfg.TickColor);
        // 下
        GameEngine.DrawLine(x, y + inner, x, y + outer, cfg.TickColor);
        // 左
        GameEngine.DrawLine(x - inner, y, x - outer, y, cfg.TickColor);
        // 右
        GameEngine.DrawLine(x + inner, y, x + outer, y, cfg.TickColor);
    }
}
