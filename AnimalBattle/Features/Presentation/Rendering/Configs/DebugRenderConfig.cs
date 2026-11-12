using System.Drawing;
using Microsoft.Extensions.Configuration;

namespace AnimalBattle.Presentation.Rendering;

/// <summary>
///     Config/debug.render.toml から読み込まれる Hitbox / Hurtbox / 衝突境界線デバッグ可視化描画の設定クラス。
/// </summary>
public sealed class DebugRenderConfig
{
    /// <summary>
    ///     デバッグモード全体の有効フラグ：Config/debug.render.toml 由来。
    ///     false の場合、F1～F4 キーを含む全てのデバッグ入力および描画が無効化されます。
    /// </summary>
    public bool DebugMode { get; set; }

    /// <summary>
    ///     F1 キーによってトグルされるデバッグオーバーレイの表示状態。
    /// </summary>
    public bool IsActive { get; set; }

    private Color _playerTerrainBoxColor;

    /// <summary>プレイヤー地形衝突ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerTerrainBoxColor")]
    public string PlayerTerrainBoxColorHex
    {
        get => string.Empty;
        set { _playerTerrainBoxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー地形衝突ボックス枠色 <see cref="Color" /></summary>
    public Color PlayerTerrainBoxColor => _playerTerrainBoxColor;

    private Color _playerHurtboxColor;

    /// <summary>プレイヤー被弾判定円色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerHurtboxColor")]
    public string PlayerHurtboxColorHex
    {
        get => string.Empty;
        set { _playerHurtboxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー被弾判定円色 <see cref="Color" /></summary>
    public Color PlayerHurtboxColor => _playerHurtboxColor;

    private Color _bossHitboxColor;

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("BossHitboxColor")]
    public string BossHitboxColorHex
    {
        get => string.Empty;
        set { _bossHitboxColor = value.ToColor(); }
    }

    /// <summary>Boss 攻撃・被弾衝突ボックス枠色 <see cref="Color" /></summary>
    public Color BossHitboxColor => _bossHitboxColor;

    private Color _enemyBulletHitboxColor;

    /// <summary>エネミー弾幕判定円枠色 Hex 文字列</summary>
    [ConfigurationKeyName("EnemyBulletHitboxColor")]
    public string EnemyBulletHitboxColorHex
    {
        get => string.Empty;
        set { _enemyBulletHitboxColor = value.ToColor(); }
    }

    [ConfigurationKeyName("BossBulletHitboxColor")]
    public string BossBulletHitboxColorHex
    {
        get => string.Empty;
        set { if (_enemyBulletHitboxColor == default) _enemyBulletHitboxColor = value.ToColor(); }
    }

    /// <summary>エネミー弾幕判定円枠色 <see cref="Color" /></summary>
    public Color EnemyBulletHitboxColor => _enemyBulletHitboxColor;
    public Color BossBulletHitboxColor => EnemyBulletHitboxColor;

    private Color _playerBulletHitboxColor;

    /// <summary>プレイヤー銃弾判定ボックス枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlayerBulletHitboxColor")]
    public string PlayerBulletHitboxColorHex
    {
        get => string.Empty;
        set { _playerBulletHitboxColor = value.ToColor(); }
    }

    /// <summary>プレイヤー銃弾判定ボックス枠色 <see cref="Color" /></summary>
    public Color PlayerBulletHitboxColor => _playerBulletHitboxColor;

    private Color _platformHitboxColor;

    /// <summary>足場衝突境界枠色 Hex 文字列</summary>
    [ConfigurationKeyName("PlatformHitboxColor")]
    public string PlatformHitboxColorHex
    {
        get => string.Empty;
        set { _platformHitboxColor = value.ToColor(); }
    }

    /// <summary>足場衝突境界枠色 <see cref="Color" /></summary>
    public Color PlatformHitboxColor => _platformHitboxColor;

    private Color _indicatorColor;

    /// <summary>デバッグ状態インジケータ文字色 Hex 文字列</summary>
    [ConfigurationKeyName("IndicatorColor")]
    public string IndicatorColorHex
    {
        get => string.Empty;
        set { _indicatorColor = value.ToColor(); }
    }

    /// <summary>デバッグ状態インジケータ文字色 <see cref="Color" /></summary>
    public Color IndicatorColor => _indicatorColor;

    /// <summary>デバッグ状態インジケータ表示座標</summary>
    public Vec2i IndicatorPos { get; set; }

    /// <summary>デバッグ状態インジケータ表示文字列</summary>
    public string IndicatorText { get; set; } = string.Empty;

    /// <summary>デバッグフェーズ切替操作案内テキスト</summary>
    public string PhaseSwitchGuideText { get; set; } = string.Empty;

    private Color _phaseSwitchGuideColor;

    /// <summary>デバッグフェーズ切替操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("PhaseSwitchGuideColor")]
    public string PhaseSwitchGuideColorHex
    {
        get => string.Empty;
        set { _phaseSwitchGuideColor = value.ToColor(); }
    }

    /// <summary>デバッグフェーズ切替操作案内文字色 <see cref="Color" /></summary>
    public Color PhaseSwitchGuideColor => _phaseSwitchGuideColor;

    /// <summary>デバッグ怪生成・機能操作案内テキスト</summary>
    public string SpawnGuideText { get; set; } = string.Empty;

    private Color _spawnGuideColor;

    /// <summary>デバッグ怪生成・機能操作案内文字色 Hex 文字列</summary>
    [ConfigurationKeyName("SpawnGuideColor")]
    public string SpawnGuideColorHex
    {
        get => string.Empty;
        set { _spawnGuideColor = value.ToColor(); }
    }

    /// <summary>デバッグ怪生成・機能操作案内文字色 <see cref="Color" /></summary>
    public Color SpawnGuideColor => _spawnGuideColor.A != 0 ? _spawnGuideColor : _phaseSwitchGuideColor;

    /// <summary>デバッグ操作案内表示 Y オフセット ピクセル単位</summary>
    public int GuideOffsetY { get; set; }

    private Color _neutralHitboxColor;

    /// <summary>中立陣営当たり判定色 Hex 文字列</summary>
    [ConfigurationKeyName("NeutralHitboxColor")]
    public string NeutralHitboxColorHex
    {
        get => string.Empty;
        set { _neutralHitboxColor = value.ToColor(); }
    }

    /// <summary>中立陣営当たり判定色 <see cref="Color" /></summary>
    public Color NeutralHitboxColor => _neutralHitboxColor;

    /// <summary>点衝突形状描画半径 ピクセル単位</summary>
    public int PointShapeRadius { get; set; }

    private Color _navNodeColor;

    /// <summary>ナビゲーションメッシュノード描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavNodeColor")]
    public string NavNodeColorHex
    {
        get => string.Empty;
        set { _navNodeColor = value.ToColor(); }
    }

    /// <summary>ナビゲーションメッシュノード描画色 <see cref="Color" /></summary>
    public Color NavNodeColor => _navNodeColor;

    private Color _navEdgeDropColor;

    /// <summary>ナビゲーション自由落下リンク描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavEdgeDropColor")]
    public string NavEdgeDropColorHex
    {
        get => string.Empty;
        set { _navEdgeDropColor = value.ToColor(); }
    }

    /// <summary>ナビゲーション自由落下リンク描画色 <see cref="Color" /></summary>
    public Color NavEdgeDropColor => _navEdgeDropColor;

    private Color _navEdgeJumpColor;

    /// <summary>ナビゲーションジャンプリンク描画色 Hex 文字列</summary>
    [ConfigurationKeyName("NavEdgeJumpColor")]
    public string NavEdgeJumpColorHex
    {
        get => string.Empty;
        set { _navEdgeJumpColor = value.ToColor(); }
    }

    /// <summary>ナビゲーションジャンプリンク描画色 <see cref="Color" /></summary>
    public Color NavEdgeJumpColor => _navEdgeJumpColor;

    /// <summary>ナビゲーションメッシュノード描画半径 ピクセル単位</summary>
    public int NavNodeRadius { get; set; }

    private Color _factionFriendlyColor;

    /// <summary>味方勢力 Gizmo 色 Hex 文字列</summary>
    [ConfigurationKeyName("FactionFriendlyColor")]
    public string FactionFriendlyColorHex
    {
        get => string.Empty;
        set { _factionFriendlyColor = value.ToColor(); }
    }

    /// <summary>味方勢力 Gizmo 色 <see cref="Color" /></summary>
    public Color FactionFriendlyColor => _factionFriendlyColor;

    private Color _factionHostileColor;

    /// <summary>敵勢力 Gizmo 色 Hex 文字列</summary>
    [ConfigurationKeyName("FactionHostileColor")]
    public string FactionHostileColorHex
    {
        get => string.Empty;
        set { _factionHostileColor = value.ToColor(); }
    }

    /// <summary>敵勢力 Gizmo 色 <see cref="Color" /></summary>
    public Color FactionHostileColor => _factionHostileColor;
}
