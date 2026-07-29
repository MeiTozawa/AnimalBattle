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
    /// Player 3 の初期出現座標を取得または設定します。
    /// </summary>
    public Point<float> P3Spawn { get; set; }

    /// <summary>
    /// Player 4 の初期出現座標を取得または設定します。
    /// </summary>
    public Point<float> P4Spawn { get; set; }

    /// <summary>
    /// 指定されたプレイヤーインデックス (1-indexed) のスポーン座標を取得します。
    /// </summary>
    public Point<float> GetSpawnPosition(int playerIndex)
    {
        return playerIndex switch
        {
            1 => P1Spawn,
            2 => P2Spawn,
            3 => (P3Spawn.X == 0 && P3Spawn.Y == 0) ? new Point<float>(P1Spawn.X - 100.0f, P1Spawn.Y - 50.0f) : P3Spawn,
            4 => (P4Spawn.X == 0 && P4Spawn.Y == 0) ? new Point<float>(P2Spawn.X + 100.0f, P2Spawn.Y - 50.0f) : P4Spawn,
            _ => P1Spawn
        };
    }

    /// <summary>
    /// バーストゾーン（撃墜判定境界）を取得または設定します。
    /// </summary>
    public Rect<float> BlastZone { get; set; }

    /// <summary>
    /// マップに配置されている足場のリストを取得または設定します。
    /// </summary>
    public List<PlatformData> Platforms { get; set; } = [];
}
