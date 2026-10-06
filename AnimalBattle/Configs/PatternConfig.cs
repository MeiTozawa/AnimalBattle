using System.Drawing;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     俯衝突進・メテオダイブ技能の設定クラス。
/// </summary>
public sealed class DiveRamConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆ロックオン）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（突進突撃）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（着地硬直・反撃好機）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>突進速度倍率</summary>
    public float DashSpeed { get; set; }

    /// <summary>着地衝撃波半径</summary>
    public float ImpactRadius { get; set; }

    /// <summary>着地炸裂弾数</summary>
    public int ImpactBulletCount { get; set; }

    /// <summary>着地炸裂弾速度</summary>
    public float ImpactBulletSpeed { get; set; }

    /// <summary>着地炸裂弾半径</summary>
    public float ImpactBulletRadius { get; set; }

    /// <summary>着地炸裂弾塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color ImpactFillColor { get; set; }

    /// <summary>着地炸裂弾外枠カラー (<see cref="Color" />)</summary>
    public Color ImpactOutlineColor { get; set; }

    /// <summary>突進残影トレイルカラー (<see cref="Color" />)</summary>
    public Color TrailColor { get; set; }

    /// <summary>突進補間に使用するイージング種別</summary>
    public EaseType EaseType { get; set; }
}

/// <summary>
///     自機狙い 3 連射技能の設定クラス。
/// </summary>
public sealed class Aimed3ShotConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>拡散角度（ラジアン）</summary>
    public float SpreadAngle { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     16 方向全方位リング弾幕技能の設定クラス。
/// </summary>
public sealed class RingBurstConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾間の角度間隔（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     双螺旋回転弾幕技能の設定クラス。
/// </summary>
public sealed class DoubleSpiralConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>第 1 弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor1 { get; set; }

    /// <summary>第 1 弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor1 { get; set; }

    /// <summary>第 2 弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor2 { get; set; }

    /// <summary>第 2 弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor2 { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     降雨弾幕技能の設定クラス。
/// </summary>
public sealed class RainDropConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾生成の最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>弾生成の最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     全方位交差波弾幕技能の設定クラス。
/// </summary>
public sealed class CrossChaosConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射ごとの角度変化量（ラジアン）</summary>
    public float AngleStep { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     高速レーザースナイパー弾幕技能の設定クラス。
/// </summary>
public sealed class FastSniperConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（キャスト）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（リカバリー）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>技能の有効射程距離（ピクセル）</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾の塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別（"Normal", "Magenta", "Yellow", "Cyan", "Black"）</summary>
    public string BulletType { get; set; } = string.Empty;
}

/// <summary>
///     Boss 2: 重劈斬撃（近接大ダメージ）技能の設定クラス。
/// </summary>
public sealed class HeavySlashConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（蓄力予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（突進揮撃）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（振り抜き硬直）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>突進速度</summary>
    public float DashSpeed { get; set; }

    /// <summary>斬撃判定 X 半径</summary>
    public float SlashRangeX { get; set; }

    /// <summary>斬撃判定 Y 半径</summary>
    public float SlashRangeY { get; set; }

    /// <summary>斬撃円形半径</summary>
    public float SlashRadius { get; set; }

    /// <summary>斬撃命中ダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>斬撃攻撃判定の残留持続時間（秒）</summary>
    public float HitboxDuration { get; set; } = 0.28f;

    /// <summary>斬撃エフェクトカラー (<see cref="Color" />)</summary>
    public Color SlashColor { get; set; }
}

/// <summary>
///     Boss 2: 撼地踏撃・貼地衝撃波技能の設定クラス。
/// </summary>
public sealed class GroundStompConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（持ち上げ予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（踏みつけ）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（硬直）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>判定生成オフセット Y（ピクセル）</summary>
    public float HitboxOffsetY { get; set; }

    /// <summary>地面接触面の Y 補正オフセット（ピクセル）</summary>
    public float GroundOffsetY { get; set; } = 10.0f;

    /// <summary>左右衝撃波生成の水平 X オフセット（ピクセル）</summary>
    public float ShockwaveOffsetX { get; set; } = 30.0f;

    /// <summary>衝撃波水平推進速度</summary>
    public float ShockwaveSpeed { get; set; }

    /// <summary>衝撃波弾半径</summary>
    public float ShockwaveRadius { get; set; }

    /// <summary>衝撃波ダメージ</summary>
    public float ShockwaveDamage { get; set; }

    /// <summary>衝撃波塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color FillColor { get; set; }

    /// <summary>衝撃波外枠カラー (<see cref="Color" />)</summary>
    public Color OutlineColor { get; set; }
}

/// <summary>
///     Boss 2: 泰山圧頂・巨力飛撲技能の設定クラス。
/// </summary>
public sealed class LeapSmashConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（かがみ込み予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動（跳躍・滞空・落下）持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙（着地硬直）時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>プレイヤー頭上への飛翔所要時間（秒）</summary>
    public float FlyDuration { get; set; }

    /// <summary>最高点ホバリング滞空時の Y 座標高度</summary>
    public float PeakAltitudeY { get; set; }

    /// <summary>最高点でのホバリング滞空時間（秒）</summary>
    public float ApexHangDuration { get; set; }

    /// <summary>垂直急降下叩きつけ速度</summary>
    public float DescentSpeed { get; set; }

    /// <summary>着地破片弾数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片飛散開始角度倍率（π に対する比率）</summary>
    public float ShrapnelAngleStartRatio { get; set; } = 0.15f;

    /// <summary>破片飛散扇状角度幅倍率（π に対する比率）</summary>
    public float ShrapnelAngleSpanRatio { get; set; } = 0.7f;

    /// <summary>破片生成位置の垂直 Y オフセット（ピクセル）</summary>
    public float ShrapnelOffsetY { get; set; } = 15.0f;

    /// <summary>破片飛散速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片弾半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー (<see cref="Color" />)</summary>
    public Color ShrapnelOutlineColor { get; set; }
}

/// <summary>
///     Boss 2: 二連重斬技能の設定クラス。
/// </summary>
public sealed class DoubleCleaveConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>2 回目の斬撃追加前方オフセット X（ピクセル）</summary>
    public float SecondSlashOffsetX { get; set; }

    /// <summary>前進突進速度</summary>
    public float DashSpeed { get; set; }

    /// <summary>2 回目の斬撃までのインターバル（秒）</summary>
    public float SlashInterval { get; set; }

    /// <summary>斬撃 X 範囲</summary>
    public float SlashRangeX { get; set; }

    /// <summary>斬撃 Y 範囲</summary>
    public float SlashRangeY { get; set; }

    /// <summary>斬撃ダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>斬撃攻撃判定の残留持続時間（秒）</summary>
    public float HitboxDuration { get; set; } = 0.24f;

    /// <summary>斬撃エフェクトカラー (<see cref="Color" />)</summary>
    public Color SlashColor { get; set; }
}

/// <summary>
///     Boss 2: 狂暴横衝直撞技能の設定クラス。
/// </summary>
public sealed class FrenzyChargeConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（咆哮構え予兆）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>突進持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>壁衝突後の後隙硬直時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>全画面高速突進速度</summary>
    public float ChargeSpeed { get; set; }

    /// <summary>突進攻撃判定幅（ピクセル）</summary>
    public float HitboxWidth { get; set; } = 100.0f;

    /// <summary>突進攻撃判定高さ（ピクセル）</summary>
    public float HitboxHeight { get; set; } = 60.0f;

    /// <summary>突進攻撃判定持続時間（秒）</summary>
    public float HitboxDuration { get; set; } = 0.25f;

    /// <summary>突進攻撃判定エフェクトカラー (<see cref="Color" />)</summary>
    public Color HitboxColor { get; set; } = Color.FromArgb(0xFF, 0xFF, 0x17, 0x44);

    /// <summary>壁激突時の破片数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー (<see cref="Color" />)</summary>
    public Color ShrapnelOutlineColor { get; set; }
}

/// <summary>
///     Boss 2: 滅地狂吼・落石技能の設定クラス。
/// </summary>
public sealed class EarthquakeRoarConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙（聚気咆哮）時間（秒）</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー (<see cref="Color" />)</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間（秒）</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙時間（秒）</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>落石投下間隔（秒）</summary>
    public float Interval { get; set; }

    /// <summary>1 回あたりの落石投下数</summary>
    public int RockCount { get; set; }

    /// <summary>落石発生最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>落石発生最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>落石落下速度</summary>
    public float RockSpeed { get; set; }

    /// <summary>落石半径</summary>
    public float RockRadius { get; set; }

    /// <summary>落石塗りつぶしカラー (<see cref="Color" />)</summary>
    public Color RockFillColor { get; set; }

    /// <summary>落石外枠カラー (<see cref="Color" />)</summary>
    public Color RockOutlineColor { get; set; }
}

/// <summary>
///     特殊弾幕変調パラメータ設定クラス。
/// </summary>
public sealed class SpecialBulletConfig {
    /// <summary>青弾および黒弾の法線正弦波揺動振幅</summary>
    public float CyanWaveAmplitude { get; set; }

    /// <summary>青弾および黒弾の法線正弦波揺動周波数</summary>
    public float CyanWaveFrequency { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動振幅</summary>
    public float MagentaPulseAmplitude { get; set; }

    /// <summary>品紅弾および黒弾の呼吸拡縮脈動周波数</summary>
    public float MagentaPulseFrequency { get; set; }
}

/// <summary>
///     Config/patterns.toml から読み込まれる各技能の FSM アクション設定クラス。
/// </summary>
public sealed class PatternConfig {
    /// <summary>特殊弾幕変調パラメータ設定</summary>
    public SpecialBulletConfig SpecialBullet { get; set; } = new();

    /// <summary>俯衝突進・メテオダイブ技能設定</summary>
    public DiveRamConfig DiveRam { get; set; } = new();

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

    /// <summary>Boss 2 重劈斬撃技能設定</summary>
    public HeavySlashConfig HeavySlash { get; set; } = new();

    /// <summary>Boss 2 撼地踏撃技能設定</summary>
    public GroundStompConfig GroundStomp { get; set; } = new();

    /// <summary>Boss 2 泰山圧頂技能設定</summary>
    public LeapSmashConfig LeapSmash { get; set; } = new();

    /// <summary>Boss 2 二連重斬技能設定</summary>
    public DoubleCleaveConfig DoubleCleave { get; set; } = new();

    /// <summary>Boss 2 狂暴横衝直撞技能設定</summary>
    public FrenzyChargeConfig FrenzyCharge { get; set; } = new();

    /// <summary>Boss 2 滅地狂吼技能設定</summary>
    public EarthquakeRoarConfig EarthquakeRoar { get; set; } = new();

    /// <summary>
    ///     文字列で指定された弾幕種別を <see cref="BulletType" /> 列挙型へパースします。
    /// </summary>
    /// <param name="typeStr">弾幕種別文字列。</param>
    /// <param name="defaultType">パース失敗時のデフォルト種別。</param>
    /// <returns>パースされた <see cref="BulletType" />。</returns>
    public static BulletType ParseBulletType(string? typeStr, BulletType defaultType = BulletType.Normal)
    {
        if (!string.IsNullOrWhiteSpace(typeStr) && Enum.TryParse(typeStr, true, out BulletType result)) return result;
        return defaultType;
    }

    /// <summary>
    ///     指定された TOML ファイルから設定を読み込んだ <see cref="ConfigBase{PatternConfig}" /> インスタンスを生成します。
    /// </summary>
    /// <param name="path">TOML ファイル名</param>
    /// <returns>読み込み結果</returns>
    public static ErrorOr<ConfigBase<PatternConfig>> LoadFromFile(string path = ResourcePath.Config.Patterns)
    {
        return ConfigBase<PatternConfig>.LoadFromFile(path);
    }
}