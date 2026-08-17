using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 自機狙い 3 連射技能の設定クラス。
/// </summary>
public sealed class Aimed3ShotConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "Aimed 3-Way Shot";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.40f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x88, 0x88);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 0.90f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.50f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.45f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 6.2f;

    /// <summary>拡散角度（ラジアン）</summary>
    public float SpreadAngle { get; set; } = 0.16f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 8.0f;

    /// <summary>弾数</summary>
    public int BulletCount { get; set; } = 3;

    /// <summary>弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x33, 0x33);

    /// <summary>弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x88, 0x88);
}

/// <summary>
/// 16 方向全方位リング弾幕技能の設定クラス。
/// </summary>
public sealed class RingBurstConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "16-Way Ring Burst";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.55f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0x80, 0xFF, 0xFF);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 1.00f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.70f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.50f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 3.8f;

    /// <summary>弾間の角度間隔（ラジアン）</summary>
    public float AngleStep { get; set; } = 0.25f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 9.0f;

    /// <summary>弾数</summary>
    public int BulletCount { get; set; } = 16;

    /// <summary>弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0xE5, 0xFF);

    /// <summary>弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor { get; set; } = Color.FromArgb(0xFF, 0x80, 0xFF, 0xFF);
}

/// <summary>
/// 双螺旋回転弾幕技能の設定クラス。
/// </summary>
public sealed class DoubleSpiralConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "Twin Spiral Barrage";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.45f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0xE0, 0xB0, 0xFF);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 1.60f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.60f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 4.8f;

    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; } = 0.22f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.075f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 7.5f;

    /// <summary>第 1 弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor1 { get; set; } = Color.FromArgb(0xFF, 0xB2, 0x55, 0xD3);

    /// <summary>第 1 弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor1 { get; set; } = Color.FromArgb(0xFF, 0xE0, 0xB0, 0xFF);

    /// <summary>第 2 弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor2 { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);

    /// <summary>第 2 弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor2 { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xF2, 0x75);
}

/// <summary>
/// 降雨弾幕技能の設定クラス。
/// </summary>
public sealed class RainDropConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "Skyfall Rain Barrage";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.35f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0xB9, 0xF6, 0xCA);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 1.80f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.80f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 5.2f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.32f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 7.0f;

    /// <summary>弾生成の最小 X 座標</summary>
    public float MinX { get; set; } = 60.0f;

    /// <summary>弾生成の最大 X 座標</summary>
    public float MaxX { get; set; } = 1220.0f;

    /// <summary>弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0x01, 0x06, 0x76);

    /// <summary>弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor { get; set; } = Color.FromArgb(0xFF, 0xB9, 0xF6, 0xCA);
}

/// <summary>
/// 全方位交差波弾幕技能の設定クラス。
/// </summary>
public sealed class CrossChaosConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "Cross Chaos Waves";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.35f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 2.00f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.50f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 5.2f;

    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; } = 0.18f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.10f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 8.5f;

    /// <summary>弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x17, 0x44);

    /// <summary>弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x8A, 0x80);
}

/// <summary>
/// 高速レーザースナイパー弾幕技能の設定クラス。
/// </summary>
public sealed class FastSniperConfig
{
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = "Laser Fast Sniper";

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; } = 0.50f;

    /// <summary>予兆エフェクトカラー (<see cref="Color"/>)</summary>
    public Color TelegraphColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; } = 0.60f;

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; } = 0.60f;

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; } = 0.30f;

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; } = 8.5f;

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; } = 6.5f;

    /// <summary>弾の塗りつぶしカラー (<see cref="Color"/>)</summary>
    public Color FillColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x52, 0x52);

    /// <summary>弾の外枠カラー (<see cref="Color"/>)</summary>
    public Color OutlineColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
}

/// <summary>
/// Config/patterns.toml から読み込まれる各技能の FSM アクション設定クラス。
/// </summary>
public sealed class PatternConfig
{
    /// <summary>自機狙い 3 連射技能設定</summary>
    public Aimed3ShotConfig Aimed3Shot { get; set; } = new();

    /// <summary>16 方向全方位リング弾幕技能設定</summary>
    public RingBurstConfig RingBurst { get; set; } = new();

    /// <summary>双螺旋回転弾幕技能設定</summary>
    public DoubleSpiralConfig DoubleSpiral { get; set; } = new();

    /// <summary>降雨弾幕技能設定</summary>
    public RainDropConfig RainDrop { get; set; } = new();

    /// <summary>全方位交差波弾幕技能設定</summary>
    public CrossChaosConfig CrossChaos { get; set; } = new();

    /// <summary>高速レーザースナイパー弾幕技能設定</summary>
    public FastSniperConfig FastSniper { get; set; } = new();

    /// <summary>
    /// 指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{PatternConfig}"/> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static Result<ConfigBase<PatternConfig>> LoadFromFile(string path = "patterns.toml")
    {
        return ConfigBase<PatternConfig>.LoadFromFile(path);
    }
}
