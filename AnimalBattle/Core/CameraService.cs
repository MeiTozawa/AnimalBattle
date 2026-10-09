using AnimalBattle.Configs;

namespace AnimalBattle.Core;

/// <summary>
///     Trauma（心的外傷）モデルに基づく画面振動・カメラシェイクサービスクラス。
///     揺れ強度 Shake = Trauma^2 による非線形減衰特性を提供し、
///     急激な衝撃と速やかな静定を実現します。
/// </summary>
public static class CameraService
{
    private static float _trauma;
    private static float _time;
    private static float _impulseX;
    private static float _impulseY;

    /// <summary>
    ///     現在の Trauma 値（0.0f ～ 1.0f）。
    /// </summary>
    public static float Trauma => _trauma;

    /// <summary>
    ///     現在のカメラ X 軸描画オフセット（ピクセル）。
    /// </summary>
    public static int OffsetX { get; private set; }

    /// <summary>
    ///     現在のカメラ Y 軸描画オフセット（ピクセル）。
    /// </summary>
    public static int OffsetY { get; private set; }

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
    /// <param name="dirX">X 軸方向</param>
    /// <param name="dirY">Y 軸方向</param>
    /// <param name="power">強度係数</param>
    public static void AddDirectionalImpulse(float dirX, float dirY, float power)
    {
        var cfg = ConfigManager.Camera;
        _impulseX = Math.Clamp(_impulseX + dirX * power, -cfg.MaxOffsetX, cfg.MaxOffsetX);
        _impulseY = Math.Clamp(_impulseY + dirY * power, -cfg.MaxOffsetY, cfg.MaxOffsetY);
    }

    /// <summary>
    ///     フレームごとの減衰およびオフセット計算を実行します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public static void Update(float deltaTime)
    {
        var cfg = ConfigManager.Camera;

        if (_trauma > 0.0f)
        {
            _trauma = Math.Max(0.0f, _trauma - cfg.TraumaDecaySpeed * deltaTime);
        }

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

        if (_trauma <= 0.0f && _impulseX == 0.0f && _impulseY == 0.0f)
        {
            OffsetX = 0;
            OffsetY = 0;
            return;
        }

        _time += deltaTime;

        // 二乗則による非線形振幅スケーリング
        var shake = _trauma * _trauma;
        var noiseX = MathF.Sin(_time * cfg.NoiseFrequency1) * 0.7f + MathF.Sin(_time * cfg.NoiseFrequency2) * 0.3f;
        var noiseY = MathF.Cos(_time * cfg.NoiseFrequency3) * 0.7f + MathF.Cos(_time * cfg.NoiseFrequency4) * 0.3f;

        OffsetX = (int)MathF.Round(shake * cfg.MaxOffsetX * noiseX + _impulseX);
        OffsetY = (int)MathF.Round(shake * cfg.MaxOffsetY * noiseY + _impulseY);
    }

    /// <summary>
    ///     カメラ振動状態を初期化します。
    /// </summary>
    public static void Reset()
    {
        _trauma = 0.0f;
        _time = 0.0f;
        _impulseX = 0.0f;
        _impulseY = 0.0f;
        OffsetX = 0;
        OffsetY = 0;
    }
}
