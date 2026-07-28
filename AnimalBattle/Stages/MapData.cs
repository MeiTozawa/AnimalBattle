using System;
using System.Collections.Generic;
using AnimalBattle.Core;

namespace AnimalBattle.Stages;

/// <summary>
/// マップ（ステージ）全体の構成情報を保持するデータ構造クラス。
/// </summary>
public class MapData
{
    /// <summary>
    /// マップIDを取得または設定します。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// マップ名を取得または設定します。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// マップのサブタイトル・説明テキストを取得または設定します。
    /// </summary>
    public string Subtitle { get; set; } = "";

    /// <summary>
    /// マップテーマカラー (ARGB) を取得または設定します。
    /// </summary>
    public uint ThemeColor { get; set; }

    /// <summary>
    /// 背景カラー (ARGB) を取得または設定します。
    /// </summary>
    public uint BgColor { get; set; }

    /// <summary>
    /// Player 1 の初期出現座標を取得または設定します。
    /// </summary>
    public Point<float> P1Spawn { get; set; }

    /// <summary>
    /// Player 2 の初期出現座標を取得または設定します。
    /// </summary>
    public Point<float> P2Spawn { get; set; }

    /// <summary>
    /// バーストゾーン（撃墜判定境界）を取得または設定します。
    /// </summary>
    public Rect<float> BlastZone { get; set; }

    /// <summary>
    /// マップに配置されている足場のリストを取得または設定します。
    /// </summary>
    public List<PlatformData> Platforms { get; set; } = [];
}
