namespace AnimalBattle.Configs;

/// <summary>
///     BGMや効果音およびマスター音量の音響パラメータを保持する設定データモデルです。
/// </summary>
public sealed class AudioConfig
{
    /// <summary>ゼロから1.0の範囲で指定するマスター音量</summary>
    public float MasterVolume { get; set; } = 0.8f;

    /// <summary>ゼロから1.0の範囲で指定する効果音音量</summary>
    public float SfxVolume { get; set; } = 0.85f;

    /// <summary>ゼロから1.0の範囲で指定するBGM音量</summary>
    public float BgmVolume { get; set; } = 0.6f;
}
