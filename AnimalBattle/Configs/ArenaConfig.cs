using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 個別プラットフォームの幾何パラメータ定義クラス。
/// </summary>
public sealed class PlatformData
{
    /// <summary>中心 X 座標</summary>
    public float X { get; set; }

    /// <summary>中心 Y 座標</summary>
    public float Y { get; set; }

    /// <summary>足場幅（ピクセル）</summary>
    public float Width { get; set; }

    /// <summary>足場高さ（ピクセル）</summary>
    public float Height { get; set; }
}

/// <summary>
/// Config/arena.toml から読み込まれる戦闘アリーナおよび足場設定クラス。
/// </summary>
public sealed class ArenaConfig
{
    /// <summary>底面メインフロア</summary>
    public PlatformData Floor { get; set; } = new() { X = 640.0f, Y = 600.0f, Width = 1240.0f, Height = 36.0f };

    /// <summary>左側浮遊足場</summary>
    public PlatformData PlatformLeft { get; set; } = new() { X = 280.0f, Y = 440.0f, Width = 240.0f, Height = 22.0f };

    /// <summary>右側浮遊足場</summary>
    public PlatformData PlatformRight { get; set; } = new() { X = 1000.0f, Y = 440.0f, Width = 240.0f, Height = 22.0f };

    /// <summary>中央高台足場</summary>
    public PlatformData PlatformCenter { get; set; } = new() { X = 640.0f, Y = 300.0f, Width = 200.0f, Height = 20.0f };

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{ArenaConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<ArenaConfig>> LoadFromFile(string path = "arena.toml")
    {
        return ConfigBase<ArenaConfig>.LoadFromFile(path);
    }
}
