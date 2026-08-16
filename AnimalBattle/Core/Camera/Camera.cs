using System;
using AnimalBattle.Bindings;

namespace AnimalBattle.Core;
/// <summary>
/// Native C++ カメラノード (AnimalBattleCore.dll) を P/Invoke 経由で操作し、
/// マルチプレイヤーのスムーズなターゲット追従およびズーム計算を管理するクラス。
/// </summary>
public unsafe class Camera : IDisposable
{
    private IntPtr _nativeCamera;
    private bool _isDisposed;

    /// <summary>カメラ中心 X 座標</summary>
    public float X { get; set; }

    /// <summary>カメラ中心 Y 座標</summary>
    public float Y { get; set; }

    /// <summary>カメラのズーム拡大率</summary>
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// カメラの初期設定コンテナを取得または設定します。
    /// </summary>
    public ConfigBase<CameraConfig> ConfigHolder { get; set; }

    /// <summary>
    /// カメラパラメータ設定を取得します。
    /// </summary>
    public CameraConfig Config => ConfigHolder.Config;

    /// <summary>
    /// 指定された TOML ファイルパスからカメラ設定を読み込み、<see cref="Camera"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="configPath">読み込む TOML ファイルパス</param>
    public Camera(string configPath = "camera.toml")
    {
        Result<ConfigBase<CameraConfig>> configResult = CameraConfig.LoadFromFile(configPath);
        ConfigHolder = configResult.IsOk ? configResult.Value! : new ConfigBase<CameraConfig>();

        _nativeCamera = NativeBindings.Camera_Create(
            640.0f, 360.0f, 1.0f,
            Config.MinScale, Config.MaxScale, Config.LerpSpeed, Config.PaddingX, Config.PaddingY);

        ConfigHolder.OnConfigReloaded += ApplyConfigToNative;
    }

    /// <summary>
    /// デフォルト設定オブジェクトインスタンスで <see cref="Camera"/> を初期化します。
    /// </summary>
    /// <param name="config">カメラ設定オブジェクト</param>
    public Camera(CameraConfig config)
    {
        ConfigHolder = new ConfigBase<CameraConfig>(config);

        _nativeCamera = NativeBindings.Camera_Create(
            640.0f, 360.0f, 1.0f,
            Config.MinScale, Config.MaxScale, Config.LerpSpeed, Config.PaddingX, Config.PaddingY);

        ConfigHolder.OnConfigReloaded += ApplyConfigToNative;
    }

    /// <summary>
    /// カメラ設定パラメータを Native C++ カメラノードへ同期・反映します。
    /// </summary>
    public void ApplyConfigToNative()
    {
        if (_nativeCamera == IntPtr.Zero)
        {
            return;
        }

        NativeBindings.Camera_SetConfig(
            _nativeCamera,
            Config.MinScale,
            Config.MaxScale,
            Config.LerpSpeed,
            Config.PaddingX,
            Config.PaddingY);
    }

    /// <summary>
    /// カメラの中心座標および拡大率を即座にリセットします。
    /// </summary>
    /// <param name="x">リセット中心 X 座標</param>
    /// <param name="y">リセット中心 Y 座標</param>
    /// <param name="scale">リセット拡大率</param>
    public void Reset(float x = 640.0f, float y = 360.0f, float scale = 1.0f)
    {
        NativeBindings.Camera_Reset(_nativeCamera, x, y, scale);
        X = x;
        Y = y;
        Scale = scale;
    }

    /// <summary>
    /// 複数のターゲットプレイヤー座標を追従するようにカメラ位置とズーム倍率を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    /// <param name="targets">追従対象の座標配列</param>
    /// <param name="viewportWidth">ビューポート幅</param>
    /// <param name="viewportHeight">ビューポート高さ</param>
    public void Update(float deltaTime, ReadOnlySpan<Point<float>> targets, float viewportWidth = 1280.0f, float viewportHeight = 720.0f)
    {
        fixed (Point<float>* ptr = targets)
        {
            NativeBindings.Camera_Update(_nativeCamera, deltaTime, ptr, targets.Length, viewportWidth, viewportHeight);
        }

        float outX, outY, outScale;
        NativeBindings.Camera_GetState(_nativeCamera, &outX, &outY, &outScale);
        X = outX;
        Y = outY;
        Scale = outScale;
    }

    /// <summary>
    /// ネイティブカメラリソースおよび設定コンテナを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            if (_nativeCamera != IntPtr.Zero)
            {
                NativeBindings.Camera_Destroy(_nativeCamera);
                _nativeCamera = IntPtr.Zero;
            }
            ConfigHolder.Dispose();
            _isDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
