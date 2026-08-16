using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Stages;
/// <summary>
/// マップ（ステージ）全体の構成情報を保持するデータ構造クラス。
/// </summary>
public class StageInfo
{
    /// <summary>マップID (1～4)</summary>
    public int Id { get; set; }

    /// <summary>マップ名（日・英・中表記）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>マップのサブタイトル・解説</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>マップテーマカラー (ARGB)</summary>
    public uint ThemeColor { get; set; }

    /// <summary>背景カラー (ARGB)</summary>
    public uint BgColor { get; set; }

    /// <summary>P1～P4 の初期スポーン座標 (4要素)</summary>
    public Point<float>[] Spawns { get; set; } = [];

    /// <summary>マップに配置されている足場のリスト</summary>
    public List<PlatformConfig> Platforms { get; set; } = [];
}
