namespace AnimalBattle.Configs;

/// <summary>
///     BGM、効果音、マスター音量などの音響パラメータを保持する設定データモデル。
/// </summary>
public sealed class AudioConfig
{
    /// <summary>マスター音量 (0.0 ～ 1.0)</summary>
    public float MasterVolume { get; set; }

    /// <summary>効果音音量 (0.0 ～ 1.0)</summary>
    public float SfxVolume { get; set; }

    /// <summary>BGM 音量 (0.0 ～ 1.0)</summary>
    public float BgmVolume { get; set; }
}
