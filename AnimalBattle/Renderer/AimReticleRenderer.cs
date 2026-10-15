using AnimalBattle.Configs;
using AnimalBattle.Events;

namespace AnimalBattle.Renderer;

/// <summary>
///     マウス照準レティクル（准星）の動的スプリング拡縮およびスクリーン描画を統括するレンダラークラス。
///     設定ファイル（render.toml の [AimReticle]）から動的パラメータを読み込み、
///     射撃イベント（<see cref="PlayerShotFiredEvent" />）に連動した反動拡縮パルスと戦術照準器を描画します。
/// </summary>
public static class AimReticleRenderer
{
    private static float _expansion;
    private static float _expansionVelocity;
    private static bool _isSubscribed;

    /// <summary>
    ///     現在のスプリング拡張振幅（px）。
    /// </summary>
    public static float CurrentExpansion => _expansion;

    /// <summary>
    ///     レティクルレンダラーを初期化し、イベント購読を開始します。
    /// </summary>
    public static void Initialize()
    {
        if (_isSubscribed) return;
        EventBus.Subscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        _isSubscribed = true;
    }

    /// <summary>
    ///     射撃イベント発生時の反動パルスをトリガーします。
    /// </summary>
    private static void OnPlayerShotFired(PlayerShotFiredEvent e)
    {
        TriggerRecoilPulse();
    }

    /// <summary>
    ///     射撃開火時のスプリング拡張反動パルスを直接トリガーします。
    /// </summary>
    public static void TriggerRecoilPulse()
    {
        var cfg = ConfigManager.Render.AimReticle;
        _expansion = cfg.RecoilExpansion;
        _expansionVelocity = 0.0f;
    }

    /// <summary>
    ///     レティクルのスプリング拡縮状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過時間（秒）。</param>
    public static void Update(float deltaTime)
    {
        if (deltaTime <= 0.0f) return;

        var cfg = ConfigManager.Render.AimReticle;
        var safeDt = MathF.Min(deltaTime, 0.1f);
        var frequency = cfg.SpringFrequency;
        var dampingRatio = cfg.SpringDampingRatio;

        if (frequency <= 0.0f)
        {
            _expansion = 0.0f;
            _expansionVelocity = 0.0f;
            return;
        }

        var omega = MathF.Tau * frequency;
        var p = _expansion; // target = 0.0f

        if (dampingRatio < 1.0f)
        {
            // 減衰不足（Underdamped）
            var omegaD = omega * MathF.Sqrt(1.0f - dampingRatio * dampingRatio);
            var gamma = dampingRatio * omega;
            var exp = MathF.Exp(-gamma * safeDt);
            var cos = MathF.Cos(omegaD * safeDt);
            var sin = MathF.Sin(omegaD * safeDt);

            var newP = exp * (p * cos + (_expansionVelocity + gamma * p) / omegaD * sin);
            var newV = exp * (_expansionVelocity * cos - (gamma * _expansionVelocity + omega * omega * p) / omegaD * sin);

            _expansion = newP;
            _expansionVelocity = newV;
        }
        else if (MathF.Abs(dampingRatio - 1.0f) < 0.0001f)
        {
            // 臨界減衰（Critically Damped）
            var exp = MathF.Exp(-omega * safeDt);
            var newP = exp * (p + (_expansionVelocity + omega * p) * safeDt);
            var newV = exp * (_expansionVelocity - omega * (_expansionVelocity + omega * p) * safeDt);

            _expansion = newP;
            _expansionVelocity = newV;
        }
        else
        {
            // 過減衰（Overdamped）
            var omegaD = omega * MathF.Sqrt(dampingRatio * dampingRatio - 1.0f);
            var gamma1 = -dampingRatio * omega + omegaD;
            var gamma2 = -dampingRatio * omega - omegaD;
            var exp1 = MathF.Exp(gamma1 * safeDt);
            var exp2 = MathF.Exp(gamma2 * safeDt);

            var c2 = (_expansionVelocity - gamma1 * p) / (gamma2 - gamma1);
            var c1 = p - c2;

            _expansion = c1 * exp1 + c2 * exp2;
            _expansionVelocity = c1 * gamma1 * exp1 + c2 * gamma2 * exp2;
        }

        if (MathF.Abs(_expansion) < 0.001f && MathF.Abs(_expansionVelocity) < 0.001f)
        {
            _expansion = 0.0f;
            _expansionVelocity = 0.0f;
        }

        // 異常発散やオーバーフローを確実に防止する防護クランプ
        _expansion = Math.Clamp(_expansion, -cfg.BaseRadius + 1.0f, cfg.RecoilExpansion * cfg.RecoilExpansionClampRatio);
    }

    /// <summary>
    ///     HUD 画面空間（CameraOffset = 0）に戦術准星を描画します。
    /// </summary>
    public static void Render()
    {
        var mouseScreen = InputService.GetMousePosition();
        Render(mouseScreen.X, mouseScreen.Y);
    }

    /// <summary>
    ///     指定されたスクリーン座標に戦術准星を描画します。
    /// </summary>
    /// <param name="x">スクリーン X 座標。</param>
    /// <param name="y">スクリーン Y 座標。</param>
    public static void Render(int x, int y)
    {
        var cfg = ConfigManager.Render.AimReticle;
        var radius = Math.Clamp((int)MathF.Round(cfg.BaseRadius + _expansion), cfg.MinRadius, cfg.MaxRadius);

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

    /// <summary>
    ///     レティクルの反動・拡縮状態を初期状態にリセットします。
    /// </summary>
    public static void Reset()
    {
        _expansion = 0.0f;
        _expansionVelocity = 0.0f;
    }

    /// <summary>
    ///     レンダラーのイベント購読を解除します。
    /// </summary>
    public static void Shutdown()
    {
        if (!_isSubscribed) return;
        EventBus.Unsubscribe<PlayerShotFiredEvent>(OnPlayerShotFired);
        _isSubscribed = false;
        Reset();
    }
}
