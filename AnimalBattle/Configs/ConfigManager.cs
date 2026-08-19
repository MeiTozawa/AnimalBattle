using System;
using System.ComponentModel;
using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 各種 TOML 設定ファイル（player.toml, boss.toml, patterns.toml, arena.toml, render.toml）を統括管理し、
/// 自動リロードおよびプロパティ参照を提供する静的設定マネージャークラス。
/// 全てのファイル読み込み結果（<see cref="Result{T}"/>）を検証し、エラー時は詳細な診断ログを出力します。
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
    /// <returns>全設定が正常にロードされた場合は true、1 つでも失敗した場合は false</returns>
    public static bool Initialize()
    {
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        int successCount = 0;
        int failureCount = 0;

        Result<ConfigBase<PlayerConfig>> playerRes = PlayerConfig.LoadFromFile(ResourcePath.Config.Player);
        if (playerRes.IsOk && playerRes.Value != null)
        {
            _playerConfigContainer = playerRes.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Player} の読み込みに失敗しました ({playerRes.Error?.Type}): {playerRes.Error?.Description}");
            failureCount++;
        }

        Result<ConfigBase<BossConfig>> bossRes = BossConfig.LoadFromFile(ResourcePath.Config.Boss);
        if (bossRes.IsOk && bossRes.Value != null)
        {
            _bossConfigContainer = bossRes.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Boss} の読み込みに失敗しました ({bossRes.Error?.Type}): {bossRes.Error?.Description}");
            failureCount++;
        }

        Result<ConfigBase<PatternConfig>> patternRes = PatternConfig.LoadFromFile(ResourcePath.Config.Patterns);
        if (patternRes.IsOk && patternRes.Value != null)
        {
            _patternConfigContainer = patternRes.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Patterns} の読み込みに失敗しました ({patternRes.Error?.Type}): {patternRes.Error?.Description}");
            failureCount++;
        }

        Result<ConfigBase<ArenaConfig>> arenaRes = ArenaConfig.LoadFromFile(ResourcePath.Config.Arena);
        if (arenaRes.IsOk && arenaRes.Value != null)
        {
            _arenaConfigContainer = arenaRes.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Arena} の読み込みに失敗しました ({arenaRes.Error?.Type}): {arenaRes.Error?.Description}");
            failureCount++;
        }

        Result<ConfigBase<RenderConfig>> renderRes = RenderConfig.LoadFromFile(ResourcePath.Config.Render);
        if (renderRes.IsOk && renderRes.Value != null)
        {
            _renderConfigContainer = renderRes.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Render} の読み込みに失敗しました ({renderRes.Error?.Type}): {renderRes.Error?.Description}");
            failureCount++;
        }

        if (failureCount > 0)
        {
            Console.WriteLine($"[C# 設定警告] TOML 設定読み込み完了 (成功: {successCount}, 失敗: {failureCount})。失敗した設定はデフォルト値を使用します。");
            return false;
        }

        Console.WriteLine($"[C# 設定初期化] 全ての TOML 設定ファイル（{successCount} 件）の読み込みが正常に完了しました！");
        return true;
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
