using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// フェーズ 1 の自機狙い弾および全方位リング弾幕の設定クラス。
/// </summary>
public sealed class Phase1PatternConfig
{
    /// <summary>自機狙い 3 連射の発射周期（秒）</summary>
    public float AimedInterval { get; set; } = 0.90f;

    /// <summary>自機狙い弾の初速度</summary>
    public float AimedSpeed { get; set; } = 6.2f;

    /// <summary>自機狙い 3 連射の扇形拡散角（ラジアン）</summary>
    public float AimedSpreadAngle { get; set; } = 0.16f;

    /// <summary>自機狙い弾の判定半径</summary>
    public float AimedRadius { get; set; } = 8.0f;

    /// <summary>自機狙い弾の内部塗りつぶし色</summary>
    public uint AimedFillColor { get; set; } = 0xFFFF3333;

    /// <summary>自機狙い弾の外周発光色</summary>
    public uint AimedOutlineColor { get; set; } = 0xFFFF8888;

    /// <summary>16 方向全方位リング弾の発射周期（秒）</summary>
    public float RingInterval { get; set; } = 2.20f;

    /// <summary>リング弾の分割弾数</summary>
    public int RingBulletCount { get; set; } = 16;

    /// <summary>リング弾の初速度</summary>
    public float RingSpeed { get; set; } = 3.8f;

    /// <summary>リング弾発射ごとの回転角度オフセット加算値</summary>
    public float RingAngleStep { get; set; } = 0.25f;

    /// <summary>リング弾の判定半径</summary>
    public float RingRadius { get; set; } = 9.0f;

    /// <summary>リング弾の内部塗りつぶし色</summary>
    public uint RingFillColor { get; set; } = 0xFF00E5FF;

    /// <summary>リング弾の外周発光色</summary>
    public uint RingOutlineColor { get; set; } = 0xFF80FFFF;
}

/// <summary>
/// フェーズ 2 の双螺旋回転弾幕および降雨弾幕の設定クラス。
/// </summary>
public sealed class Phase2PatternConfig
{
    /// <summary>双螺旋弾の発射周期（秒）</summary>
    public float SpiralInterval { get; set; } = 0.075f;

    /// <summary>双螺旋弾の飛行速度</summary>
    public float SpiralSpeed { get; set; } = 4.8f;

    /// <summary>双螺旋弾の回転角加速度ステップ</summary>
    public float SpiralAngleStep { get; set; } = 0.22f;

    /// <summary>双螺旋弾の判定半径</summary>
    public float SpiralRadius { get; set; } = 7.5f;

    /// <summary>螺旋 1 枝目の塗りつぶし色</summary>
    public uint SpiralFillColor1 { get; set; } = 0xFFBA55D3;

    /// <summary>螺旋 1 枝目の発光枠色</summary>
    public uint SpiralOutlineColor1 { get; set; } = 0xFFE0B0FF;

    /// <summary>螺旋 2 枝目の塗りつぶし色</summary>
    public uint SpiralFillColor2 { get; set; } = 0xFFFFD700;

    /// <summary>螺旋 2 枝目の発光枠色</summary>
    public uint SpiralOutlineColor2 { get; set; } = 0xFFFFF275;

    /// <summary>降雨弾幕の発射周期（秒）</summary>
    public float RainInterval { get; set; } = 0.32f;

    /// <summary>降雨弾幕の垂直落下速度</summary>
    public float RainSpeed { get; set; } = 5.2f;

    /// <summary>降雨弾幕の判定半径</summary>
    public float RainRadius { get; set; } = 7.0f;

    /// <summary>降雨弾の出現最小 X 座標</summary>
    public float RainMinX { get; set; } = 60.0f;

    /// <summary>降雨弾の出現最大 X 座標</summary>
    public float RainMaxX { get; set; } = 1220.0f;

    /// <summary>降雨弾の塗りつぶし色</summary>
    public uint RainFillColor { get; set; } = 0xFF00E676;

    /// <summary>降雨弾の発光枠色</summary>
    public uint RainOutlineColor { get; set; } = 0xFFB9F6CA;
}

/// <summary>
/// フェーズ 3 の全方位交差波弾幕および高速スナイプ弾幕の設定クラス。
/// </summary>
public sealed class Phase3PatternConfig
{
    /// <summary>全方位交差波の発射周期（秒）</summary>
    public float CrossInterval { get; set; } = 0.10f;

    /// <summary>全方位交差波の飛行速度</summary>
    public float CrossSpeed { get; set; } = 5.2f;

    /// <summary>交差波の回転角ステップ</summary>
    public float CrossAngleStep { get; set; } = 0.18f;

    /// <summary>交差波弾の判定半径</summary>
    public float CrossRadius { get; set; } = 8.5f;

    /// <summary>交差波弾の塗りつぶし色</summary>
    public uint CrossFillColor { get; set; } = 0xFFFF1744;

    /// <summary>交差波弾の発光枠色</summary>
    public uint CrossOutlineColor { get; set; } = 0xFFFF8A80;

    /// <summary>高速スナイプ弾の発射周期（秒）</summary>
    public float SniperInterval { get; set; } = 0.40f;

    /// <summary>高速スナイプ弾の超高速初速度</summary>
    public float SniperSpeed { get; set; } = 8.5f;

    /// <summary>高速スナイプ弾の判定半径</summary>
    public float SniperRadius { get; set; } = 6.5f;

    /// <summary>高速スナイプ弾の塗りつぶし色</summary>
    public uint SniperFillColor { get; set; } = 0xFFFF5252;

    /// <summary>高速スナイプ弾の発光枠色</summary>
    public uint SniperOutlineColor { get; set; } = 0xFFFFFFFF;
}

/// <summary>
/// Config/patterns.toml から読み込まれる各フェーズの弾幕パターン設定クラス。
/// </summary>
public sealed class PatternConfig
{
    /// <summary>フェーズ 1 弾幕設定</summary>
    public Phase1PatternConfig Phase1 { get; set; } = new();

    /// <summary>フェーズ 2 弾幕設定</summary>
    public Phase2PatternConfig Phase2 { get; set; } = new();

    /// <summary>フェーズ 3 弾幕設定</summary>
    public Phase3PatternConfig Phase3 { get; set; } = new();

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
