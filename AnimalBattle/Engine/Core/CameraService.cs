namespace AnimalBattle.Core;

/// <summary>
///     被弾衝撃度に応じた画面振動とプレイヤーおよびボス間の動的構図制御を提供するカメラサービスクラスです。
/// </summary>
public static class CameraService
{
    private static float _trauma;
    private static float _time;
    private static float _impulseX;
    private static float _impulseY;

    private static float _currentPanX;
    private static float _currentPanY;
    private static float _shakeOffsetX;
    private static float _shakeOffsetY;

    /// <summary>
    ///     現在アクティブなカメラ設定。
    /// </summary>
    public static AnimalBattle.Configs.CameraConfig CurrentConfig { get; set; } = new();

    /// <summary>
    ///     0.0fから1.0fの範囲に収まる現在のTrauma値。
    /// </summary>
    public static float Trauma => _trauma;

    /// <summary>
    ///     ピクセル単位のカメラ平滑パンX軸オフセット。
    /// </summary>
    public static float PanOffsetX => _currentPanX;

    /// <summary>
    ///     ピクセル単位のカメラ平滑パンY軸オフセット。
    /// </summary>
    public static float PanOffsetY => _currentPanY;

    /// <summary>
    ///     ピクセル単位の画面シェイクX軸オフセット。
    /// </summary>
    public static float ShakeOffsetX => _shakeOffsetX;

    /// <summary>
    ///     ピクセル単位の画面シェイクY軸オフセット。
    /// </summary>
    public static float ShakeOffsetY => _shakeOffsetY;

    /// <summary>
    ///     ピクセル単位の最終合成カメラ描画オフセット。
    /// </summary>
    public static Vec2i RenderOffset { get; private set; }

    /// <summary>
    ///     RenderOffsetへの参照を返却するカメラ描画オフセットエイリアス。
    /// </summary>
    public static Vec2i Offset => RenderOffset;

    /// <summary>
    ///     Traumaを加算して最大1.0fへクランプします。
    /// </summary>
    /// <param name="amount">加算量</param>
    public static void AddTrauma(float amount)
    {
        _trauma = Math.Clamp(_trauma + amount, 0.0f, 1.0f);
    }

    /// <summary>
    ///     受撃方向に応じた指向性インパルスを加算します。
    /// </summary>
    /// <param name="dir">方向ベクトル</param>
    /// <param name="power">強度係数</param>
    public static void AddDirectionalImpulse(Vec2f dir, float power)
    {
        var cfg = CurrentConfig;
        _impulseX = Math.Clamp(_impulseX + dir.X * power, -cfg.MaxOffset.X, cfg.MaxOffset.X);
        _impulseY = Math.Clamp(_impulseY + dir.Y * power, -cfg.MaxOffset.Y, cfg.MaxOffset.Y);
    }

    /// <summary>
    ///     単体更新またはテスト用のデフォルト更新処理。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public static void Update(float deltaTime)
    {
        var cfg = CurrentConfig;
        Update(deltaTime, cfg.ArenaCenter, Vec2f.Zero, cfg.ArenaCenter, cfg);
    }

    /// <summary>
    ///     プレイヤーとボスの動的座標および速度を考慮したフレーム更新を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    /// <param name="playerPos">プレイヤーのワールド座標</param>
    /// <param name="playerVel">プレイヤーの移動速度</param>
    /// <param name="bossPos">ボスのワールド座標</param>
    /// <param name="config">カメラ設定。省略時はCurrentConfigを使用します</param>
    public static void Update(
        float deltaTime,
        Vec2f playerPos,
        Vec2f playerVel,
        Vec2f bossPos,
        AnimalBattle.Configs.CameraConfig? config = null)
    {
        if (config != null)
        {
            CurrentConfig = config;
        }

        var cfg = CurrentConfig;

        // 1. 動的構図ターゲット計算としてプレイヤーとボスの加重中点に速度先読みを加算します
        var pWeight = cfg.PlayerFocusWeight;
        var bWeight = cfg.BossFocusWeight;
        var totalWeight = pWeight + bWeight;
        if (totalWeight <= 0.001f)
        {
            // ゼロ除算を防止するため重みの合計値が極小の場合はプレイヤーを優先します
            pWeight = 1.0f;
            bWeight = 0.0f;
            totalWeight = 1.0f;
        }

        var focus = (playerPos * pWeight + bossPos * bWeight) / totalWeight;
        var targetCam = focus + playerVel * cfg.LookAheadFactor;

        // 中心変位に対するカメラ描画逆オフセット
        var desiredPanX = Math.Clamp(cfg.ArenaCenter.X - targetCam.X, -cfg.MaxPan.X, cfg.MaxPan.X);
        var desiredPanY = Math.Clamp(cfg.ArenaCenter.Y - targetCam.Y, -cfg.MaxPan.Y, cfg.MaxPan.Y);

        // 指数平滑化 Lerp による追従
        var lerpFactor = 1.0f - MathF.Exp(-cfg.FollowSpeed * deltaTime);
        _currentPanX += (desiredPanX - _currentPanX) * lerpFactor;
        _currentPanY += (desiredPanY - _currentPanY) * lerpFactor;

        // 2. Trauma 減衰処理
        if (_trauma > 0.0f)
        {
            _trauma = Math.Max(0.0f, _trauma - cfg.TraumaDecaySpeed * deltaTime);
        }

        // 3. 指向性インパルス減衰処理
        if (MathF.Abs(_impulseX) > 0.001f)
        {
            _impulseX = PhysicsService.MoveTowards(_impulseX, 0.0f, cfg.ImpulseDecaySpeed * deltaTime);
        }
        else
        {
            _impulseX = 0.0f;
        }

        if (MathF.Abs(_impulseY) > 0.001f)
        {
            _impulseY = PhysicsService.MoveTowards(_impulseY, 0.0f, cfg.ImpulseDecaySpeed * deltaTime);
        }
        else
        {
            _impulseY = 0.0f;
        }

        // 4. 画面振動シェイク計算
        if (_trauma > 0.0f || _impulseX != 0.0f || _impulseY != 0.0f)
        {
            _time += deltaTime;
            // 小規模な衝撃では画面を静かに揺らし、大ダメージ時のみ激しく崩すため二乗カーブを採用します
            var shake = _trauma * _trauma;
            // 単一周期による規則的な往復揺れを防ぎ有機的な手ブレを表現するため、異なる周波数を7対3の比率で合成します
            var noiseX = MathF.Sin(_time * cfg.NoiseFrequency1) * 0.7f + MathF.Sin(_time * cfg.NoiseFrequency2) * 0.3f;
            var noiseY = MathF.Cos(_time * cfg.NoiseFrequency3) * 0.7f + MathF.Cos(_time * cfg.NoiseFrequency4) * 0.3f;

            _shakeOffsetX = shake * cfg.MaxOffset.X * noiseX + _impulseX;
            _shakeOffsetY = shake * cfg.MaxOffset.Y * noiseY + _impulseY;
        }
        else
        {
            _shakeOffsetX = 0.0f;
            _shakeOffsetY = 0.0f;
        }

        // 5. 最終描画オフセット合成
        RenderOffset = new Vec2i(
            (int)MathF.Round(_currentPanX + _shakeOffsetX),
            (int)MathF.Round(_currentPanY + _shakeOffsetY));
    }

    /// <summary>
    ///     カメラ振動およびパン状態を初期化します。
    /// </summary>
    public static void Reset()
    {
        _trauma = 0.0f;
        _time = 0.0f;
        _impulseX = 0.0f;
        _impulseY = 0.0f;
        _currentPanX = 0.0f;
        _currentPanY = 0.0f;
        _shakeOffsetX = 0.0f;
        _shakeOffsetY = 0.0f;
        RenderOffset = Vec2i.Zero;
    }
}
