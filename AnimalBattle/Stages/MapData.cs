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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// マップのサブタイトル・説明テキストを取得または設定します。
    /// </summary>
    public string Subtitle { get; set; } = string.Empty;

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
    /// バーストゾーン（撃墜判定境界）を取得または設定します。
    /// </summary>
    public Rect<float> BlastZone { get; set; }

    /// <summary>
    /// マップに配置されている足場のリストを取得または設定します。
    /// </summary>
    public List<PlatformData> Platforms { get; set; } = [];

    /// <summary>
    /// デフォルト設定で <see cref="MapData"/> の新しいインスタンスを初期化します。
    /// </summary>
    public MapData()
    {
    }

    /// <summary>
    /// 主要パラメータを指定して <see cref="MapData"/> の新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="id">マップ ID</param>
    /// <param name="name">マップ名</param>
    /// <param name="subtitle">サブタイトル</param>
    /// <param name="themeColor">テーマカラー</param>
    /// <param name="bgColor">背景カラー</param>
    /// <param name="blastZone">撃墜境界</param>
    public MapData(int id, string name, string subtitle, uint themeColor, uint bgColor, Rect<float> blastZone)
    {
        Id = id;
        Name = name;
        Subtitle = subtitle;
        ThemeColor = themeColor;
        BgColor = bgColor;
        BlastZone = blastZone;
    }

    /// <summary>
    /// プラットフォームを追加します。
    /// </summary>
    /// <param name="platform">追加する足場データ</param>
    public void AddPlatform(PlatformData platform)
    {
        Platforms.Add(platform);
    }

    /// <summary>
    /// 各プレイヤーの初期出現座標を一括設定します。
    /// </summary>
    /// <param name="p1">P1 スポーン座標</param>
    /// <param name="p2">P2 スポーン座標</param>
    /// <param name="p3">P3 スポーン座標</param>
    /// <param name="p4">P4 スポーン座標</param>
    public void SetSpawnPositions(Point<float> p1, Point<float> p2, Point<float> p3, Point<float> p4)
    {
        P1Spawn = p1;
        P2Spawn = p2;
        P3Spawn = p3;
        P4Spawn = p4;
    }

    /// <summary>
    /// 指定されたプレイヤーインデックス (1-indexed) のスポーン座標を取得します。
    /// </summary>
    /// <param name="playerIndex">プレイヤー番号 (1～4)</param>
    /// <returns>スポーン座標</returns>
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
}
