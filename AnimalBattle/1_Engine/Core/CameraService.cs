namespace AnimalBattle.Core;

/// <summary>
///     Trauma（心的外傷）モデルに基づく画面振動および
///     プレイヤー・Boss 間の中点加重・速度前瞻による動的構図制御を提供するカメラサービスクラス。
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
    ///     現在の Trauma 値（0.0f ～ 1.0f）。
    /// </summary>
    public static float Trauma => _trauma;

    /// <summary>
    ///     現在のカメラ平滑パン X 軸オフセット（ピクセル）。
    /// </summary>
    public static float PanOffsetX => _currentPanX;

    /// <summary>
    ///     現在のカメラ平滑パン Y 軸オフセット（ピクセル）。
    /// </summary>
    public static float PanOffsetY => _currentPanY;

    /// <summary>
    ///     現在の画面シェイク X 軸オフセット（ピクセル）。
    /// </summary>
    public static float ShakeOffsetX => _shakeOffsetX;

    /// <summary>
    ///     現在の画面シェイク Y 軸オフセット（ピクセル）。
    /// </summary>
    public static float ShakeOffsetY => _shakeOffsetY;

    /// <summary>
    ///     最終合成カメラ描画オフセット（ピクセル）。
    /// </summary>
    public static Vec2i RenderOffset { get; private set; }

    /// <summary>
    ///     カメラ描画オフセット（RenderOffset へのエイリアス）。
    /// </summary>
    public static Vec2i Offset => RenderOffset;

    /// <summary>
    ///     Trauma を加算します（最大 1.0f にクランプ）。
    /// </summary>
    /// <param name="amount">加算量</param>
    public static void AddTrauma(float amount)
    {
        _trauma = Math.Clamp(_trauma + amount, 0.0f, 1.0f);
    }

    /// <summary>
    ///     受撃方向に応じた指向性インパルスを加算します。
    /// </summary>
    /// <param name="dir">方向ベクトル (<see cref="Vec2f" />)</param>
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
    ///     プレイヤーと Boss の動的座標および速度を考慮したフレーム更新を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    /// <param name="playerPos">プレイヤーワールド座標 (<see cref="Vec2f" />)</param>
    /// <param name="playerVel">プレイヤー速度 (<see cref="Vec2f" />)</param>
    /// <param name="bossPos">Boss ワールド座標 (<see cref="Vec2f" />)</param>
    /// <param name="config">カメラ設定（null の場合は CurrentConfig を使用）</param>
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

        // 1. 動的構図ターゲット計算 (加権中点 + 前瞻速度)
        var pWeight = cfg.PlayerFocusWeight;
        var bWeight = cfg.BossFocusWeight;
        var totalWeight = pWeight + bWeight;
        if (totalWeight <= 0.001f)
        {
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
            var shake = _trauma * _trauma;
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
