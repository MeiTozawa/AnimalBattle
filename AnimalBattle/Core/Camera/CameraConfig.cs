using System;
using AnimalBattle.Core;

namespace AnimalBattle.Core;

/// <summary>
/// カメラの初期ビューポートおよび制御パラメータを保持する設定クラス。
/// TOML 設定ファイルからの動的読み込みに対応します。
/// </summary>
public class CameraConfig
{
    /// <summary>
    /// カメラの初期 X 座標を取得または設定します。
    /// </summary>
    public float InitialX { get; set; } = 640.0f;

    /// <summary>
    /// カメラの初期 Y 座標を取得または設定します。
    /// </summary>
    public float InitialY { get; set; } = 360.0f;

    /// <summary>
    /// カメラの初期ズーム倍率を取得または設定します。
    /// </summary>
    public float InitialScale { get; set; } = 1.0f;

    /// <summary>
    /// 最小ズームスケールを取得または設定します。
    /// </summary>
    public float MinScale { get; set; } = 0.45f;

    /// <summary>
    /// 最大ズームスケールを取得または設定します。
    /// </summary>
    public float MaxScale { get; set; } = 1.0f;

    /// <summary>
    /// 補間追従速度を取得または設定します。
    /// </summary>
    public float LerpSpeed { get; set; } = 4.5f;

    /// <summary>
    /// 横方向余白パディングを取得または設定します。
    /// </summary>
    public float PaddingX { get; set; } = 280.0f;

    /// <summary>
    /// 縦方向余白パディングを取得または設定します。
    /// </summary>
    public float PaddingY { get; set; } = 180.0f;

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{CameraConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイルパスまたはファイル名</param>
    /// <returns>成功時は <see cref="ConfigBase{CameraConfig}"/> を含む <see cref="Result{T}"/>、失敗時は <see cref="SimpleError"/></returns>
    public static Result<ConfigBase<CameraConfig>> LoadFromFile(string path = "camera.toml")
    {
        return ConfigBase<CameraConfig>.LoadFromFile(path);
    }
}
