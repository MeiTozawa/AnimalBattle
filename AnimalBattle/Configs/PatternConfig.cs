using System.Drawing;
using ErrorOr;

namespace AnimalBattle.Configs;

/// <summary>
///     TOML で定義される単一キーフレームイベント設定クラス。
/// </summary>
public sealed class SkillEventConfig {
    /// <summary>発動遅延時間。</summary>
    public float Time { get; set; }

    /// <summary>イベント種別。</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>パラメータ 0。</summary>
    public float Param0 { get; set; }

    /// <summary>パラメータ 1。</summary>
    public float Param1 { get; set; }

    /// <summary>パラメータ 2。</summary>
    public float Param2 { get; set; }

    /// <summary>文字パラメータ。</summary>
    public string StrParam { get; set; } = string.Empty;
}

/// <summary>
///     俯衝突進・メテオダイブ技能の設定クラス。
/// </summary>
public sealed class DiveRamConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
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

    /// <summary>着地炸裂弾塗りつぶしカラー</summary>
    public Color ImpactFillColor { get; set; }

    /// <summary>着地炸裂弾外枠カラー</summary>
    public Color ImpactOutlineColor { get; set; }

    /// <summary>着地炸裂弾の発生 Y オフセット (px)</summary>
    public float ImpactOffsetY { get; set; }

    /// <summary>着地炸裂弾の特殊弾幕種別（Magenta, Cyan, Yellow, Black, Normal）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>突進残影トレイルカラー</summary>
    public Color TrailColor { get; set; }

    /// <summary>突進補間に使用するイージング種別</summary>
    public EaseType EaseType { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     自機狙い 3 連射技能の設定クラス。
/// </summary>
public sealed class Aimed3ShotConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>拡散角度</summary>
    public float SpreadAngle { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     16 方向全方位リング弾幕技能の設定クラス。
/// </summary>
public sealed class RingBurstConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾間の角度間隔</summary>
    public float AngleStep { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾数</summary>
    public int BulletCount { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     双螺旋回転弾幕技能の設定クラス。
/// </summary>
public sealed class DoubleSpiralConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射ごとの角度変化量</summary>
    public float AngleStep { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>第 1 弾の塗りつぶしカラー</summary>
    public Color FillColor1 { get; set; }

    /// <summary>第 1 弾の外枠カラー</summary>
    public Color OutlineColor1 { get; set; }

    /// <summary>第 2 弾の塗りつぶしカラー</summary>
    public Color FillColor2 { get; set; }

    /// <summary>第 2 弾の外枠カラー</summary>
    public Color OutlineColor2 { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     降雨弾幕技能の設定クラス。
/// </summary>
public sealed class RainDropConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾生成の最小 X 座標</summary>
    public float MinX { get; set; }

    /// <summary>弾生成の最大 X 座標</summary>
    public float MaxX { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     全方位交差波弾幕技能の設定クラス。
/// </summary>
public sealed class CrossChaosConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>発射ごとの角度変化量</summary>
    public float AngleStep { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     高速レーザースナイパー弾幕技能の設定クラス。
/// </summary>
public sealed class FastSniperConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>デフォルトクールダウン時間（秒）</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>技能の有効射程距離</summary>
    public float EffectiveRange { get; set; }

    /// <summary>発射間隔</summary>
    public float Interval { get; set; }

    /// <summary>弾速</summary>
    public float BulletSpeed { get; set; }

    /// <summary>弾の半径</summary>
    public float BulletRadius { get; set; }

    /// <summary>弾の塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>弾の外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>弾幕種別</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 重劈斬撃技能の設定クラス。
/// </summary>
public sealed class HeavySlashConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
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

    /// <summary>斬撃攻撃判定の残留持続時間</summary>
    public float HitboxDuration { get; set; }

    /// <summary>斬撃エフェクトカラー</summary>
    public Color SlashColor { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 撼地踏撃技能の設定クラス。
/// </summary>
public sealed class GroundStompConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>判定生成オフセット Y</summary>
    public float HitboxOffsetY { get; set; }

    /// <summary>地面接触面の Y 補正オフセット</summary>
    public float GroundOffsetY { get; set; }

    /// <summary>左右衝撃波生成の水平 X オフセット</summary>
    public float ShockwaveOffsetX { get; set; }

    /// <summary>衝撃波水平推進速度</summary>
    public float ShockwaveSpeed { get; set; }

    /// <summary>衝撃波弾半径</summary>
    public float ShockwaveRadius { get; set; }

    /// <summary>衝撃波ダメージ</summary>
    public float ShockwaveDamage { get; set; }

    /// <summary>衝撃波塗りつぶしカラー</summary>
    public Color FillColor { get; set; }

    /// <summary>衝撃波外枠カラー</summary>
    public Color OutlineColor { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 泰山圧頂技能の設定クラス。
/// </summary>
public sealed class LeapSmashConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>跳躍初速</summary>
    public float JumpForce { get; set; }

    /// <summary>プレイヤー頭上への飛翔所要時間</summary>
    public float FlyDuration { get; set; }

    /// <summary>最高点ホバリング滞空時の Y 座標高度</summary>
    public float PeakAltitudeY { get; set; }

    /// <summary>最高点でのホバリング滞空時間</summary>
    public float ApexHangDuration { get; set; }

    /// <summary>垂直急降下叩きつけ速度</summary>
    public float DescentSpeed { get; set; }

    /// <summary>着地破片弾数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片飛散開始角度倍率</summary>
    public float ShrapnelAngleStartRatio { get; set; }

    /// <summary>破片飛散扇状角度幅倍率</summary>
    public float ShrapnelAngleSpanRatio { get; set; }

    /// <summary>破片生成位置の垂直 Y オフセット</summary>
    public float ShrapnelOffsetY { get; set; }

    /// <summary>破片飛散速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片弾半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー</summary>
    public Color ShrapnelOutlineColor { get; set; }

    /// <summary>着地叩きつけ近接判定半径</summary>
    public float SlashRadius { get; set; }

    /// <summary>着地叩きつけダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 二連重斬技能の設定クラス。
/// </summary>
public sealed class DoubleCleaveConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>2 回目の斬撃追加前方オフセット X</summary>
    public float SecondSlashOffsetX { get; set; }

    /// <summary>前進突進速度</summary>
    public float DashSpeed { get; set; }

    /// <summary>2 回目の斬撃までのインターバル</summary>
    public float SlashInterval { get; set; }

    /// <summary>斬撃 X 範囲</summary>
    public float SlashRangeX { get; set; }

    /// <summary>斬撃 Y 範囲</summary>
    public float SlashRangeY { get; set; }

    /// <summary>斬撃ダメージ</summary>
    public float SlashDamage { get; set; }

    /// <summary>斬撃攻撃判定の残留持続時間</summary>
    public float HitboxDuration { get; set; } = 0.24f;

    /// <summary>斬撃エフェクトカラー</summary>
    public Color SlashColor { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 狂暴横衝直撞技能の設定クラス。
/// </summary>
public sealed class FrenzyChargeConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>突進持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>壁衝突後の後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>全画面高速突進速度</summary>
    public float ChargeSpeed { get; set; }

    /// <summary>突進攻撃判定幅</summary>
    public float HitboxWidth { get; set; }

    /// <summary>突進攻撃判定高さ</summary>
    public float HitboxHeight { get; set; }

    /// <summary>突進攻撃判定持続時間</summary>
    public float HitboxDuration { get; set; }

    /// <summary>突進攻撃判定エフェクトカラー</summary>
    public Color HitboxColor { get; set; }

    /// <summary>壁激突時の破片数</summary>
    public int ShrapnelCount { get; set; }

    /// <summary>破片速度</summary>
    public float ShrapnelSpeed { get; set; }

    /// <summary>破片半径</summary>
    public float ShrapnelRadius { get; set; }

    /// <summary>破片塗りつぶしカラー</summary>
    public Color ShrapnelFillColor { get; set; }

    /// <summary>破片外枠カラー</summary>
    public Color ShrapnelOutlineColor { get; set; }

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
}

/// <summary>
///     Boss 2 滅地狂吼技能の設定クラス。
/// </summary>
public sealed class EarthquakeRoarConfig {
    /// <summary>技能名</summary>
    public string SkillName { get; set; } = string.Empty;

    /// <summary>前隙予兆時間</summary>
    public float WindUpTime { get; set; }

    /// <summary>予兆エフェクトカラー</summary>
    public Color TelegraphColor { get; set; }

    /// <summary>発動持続時間</summary>
    public float CastDuration { get; set; }

    /// <summary>後隙硬直時間</summary>
    public float RecoveryTime { get; set; }

    /// <summary>基本冷却時間</summary>
    public float DefaultCooldown { get; set; }

    /// <summary>落石投下間隔</summary>
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

    /// <summary>落石塗りつぶしカラー</summary>
    public Color RockFillColor { get; set; }

    /// <summary>落石外枠カラー</summary>
    public Color RockOutlineColor { get; set; }

    /// <summary>落石の特殊弾幕種別（Magenta, Cyan, Yellow, Black, Normal）</summary>
    public string BulletType { get; set; } = string.Empty;

    /// <summary>時序キーフレームイベント一覧</summary>
    public List<SkillEventConfig> Events { get; set; } = [];
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

    /// <summary>黄弾公転衛星の回転角速度ラジアン毎秒</summary>
    public float YellowOrbitSpeed { get; set; }

    /// <summary>黄弾公転衛星の軌道半径倍率</summary>
    public float YellowOrbitDistanceMultiplier { get; set; }

    /// <summary>黄弾公転衛星の半径倍率</summary>
    public float YellowSatelliteRadiusMultiplier { get; set; }

    /// <summary>黄弾公転衛星の接触ダメージ</summary>
    public float YellowSatelliteDamage { get; set; }

    /// <summary>
    ///     指定された TOML ファイルから特殊弾幕変調設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<SpecialBulletConfig>> LoadFromFile(string path = ResourcePath.Config.SpecialBullets)
    {
        return ConfigBase<SpecialBulletConfig>.LoadFromFile(path);
    }
}

/// <summary>
///     Boss 1 専用の技能・弾幕パターン設定クラス。
/// </summary>
public sealed class Boss1PatternConfig {
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

    /// <summary>
    ///     文字列で指定された弾幕種別をパースします。
    /// </summary>
    public static BulletType ParseBulletType(string? typeStr, BulletType defaultType = BulletType.Normal)
    {
        if (!string.IsNullOrWhiteSpace(typeStr) && Enum.TryParse(typeStr, true, out BulletType result)) return result;
        return defaultType;
    }

    /// <summary>
    ///     指定された TOML ファイルから Boss 1 パターン設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<Boss1PatternConfig>> LoadFromFile(string path = ResourcePath.Config.Boss1Patterns)
    {
        return ConfigBase<Boss1PatternConfig>.LoadFromFile(path);
    }
}

/// <summary>
///     Boss 2 専用の近接・広域技能パターン設定クラス。
/// </summary>
public sealed class Boss2PatternConfig {
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
    ///     指定された TOML ファイルから Boss 2 パターン設定を読み込みます。
    /// </summary>
    public static ErrorOr<ConfigBase<Boss2PatternConfig>> LoadFromFile(string path = ResourcePath.Config.Boss2Patterns)
    {
        return ConfigBase<Boss2PatternConfig>.LoadFromFile(path);
    }
}