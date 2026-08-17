using System;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 各種 TOML 設定ファイル（player.toml, boss.toml, patterns.toml, arena.toml, render.toml）を統括管理し、
/// 自動リロードおよびプロパティ参照を提供する静的設定マネージャークラス。
/// </summary>
public static class ConfigManager
{
    private static ConfigBase<PlayerConfig> _playerConfigContainer = new(new PlayerConfig());
    private static ConfigBase<BossConfig> _bossConfigContainer = new(new BossConfig());
    private static ConfigBase<PatternConfig> _patternConfigContainer = new(new PatternConfig());
    private static ConfigBase<ArenaConfig> _arenaConfigContainer = new(new ArenaConfig());
    private static ConfigBase<RenderConfig> _renderConfigContainer = new(new RenderConfig());

    /// <summary>プレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Player => _playerConfigContainer.Config;

    /// <summary>Boss 設定オブジェクトを取得します。</summary>
    public static BossConfig Boss => _bossConfigContainer.Config;

    /// <summary>弾幕パターン設定オブジェクトを取得します。</summary>
    public static PatternConfig Pattern => _patternConfigContainer.Config;

    /// <summary>アリーナ足場設定オブジェクトを取得します。</summary>
    public static ArenaConfig Arena => _arenaConfigContainer.Config;

    /// <summary>描画および UI 設定オブジェクトを取得します。</summary>
    public static RenderConfig Render => _renderConfigContainer.Config;

    /// <summary>
    /// 全 TOML 設定ファイルをロードし、自動リロード監視を開始します。
    /// </summary>
    public static void Initialize()
    {
        Result<ConfigBase<PlayerConfig>> playerRes = PlayerConfig.LoadFromFile("player.toml");
        if (playerRes.IsOk && playerRes.Value != null)
        {
            _playerConfigContainer = playerRes.Value;
        }

        Result<ConfigBase<BossConfig>> bossRes = BossConfig.LoadFromFile("boss.toml");
        if (bossRes.IsOk && bossRes.Value != null)
        {
            _bossConfigContainer = bossRes.Value;
        }

        Result<ConfigBase<PatternConfig>> patternRes = PatternConfig.LoadFromFile("patterns.toml");
        if (patternRes.IsOk && patternRes.Value != null)
        {
            _patternConfigContainer = patternRes.Value;
        }

        Result<ConfigBase<ArenaConfig>> arenaRes = ArenaConfig.LoadFromFile("arena.toml");
        if (arenaRes.IsOk && arenaRes.Value != null)
        {
            _arenaConfigContainer = arenaRes.Value;
        }

        Result<ConfigBase<RenderConfig>> renderRes = RenderConfig.LoadFromFile("render.toml");
        if (renderRes.IsOk && renderRes.Value != null)
        {
            _renderConfigContainer = renderRes.Value;
        }

        Console.WriteLine("[C# 設定初期化] 各種 TOML 設定ファイル（player, boss, patterns, arena, render）の読み込みが完了しました！");
    }

    /// <summary>
    /// 全設定コンテナのファイル監視リソースを安全に解放します。
    /// </summary>
    public static void Dispose()
    {
        _playerConfigContainer.Dispose();
        _bossConfigContainer.Dispose();
        _patternConfigContainer.Dispose();
        _arenaConfigContainer.Dispose();
        _renderConfigContainer.Dispose();
    }
}
