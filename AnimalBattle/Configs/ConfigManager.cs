using System;
using System.ComponentModel;
using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 各種 TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, patterns.toml, arena.toml, render.toml）を統括管理し、
/// 自動リロードおよびプロパティ参照を提供する静的設定マネージャークラス。
/// 全てのファイル読み込み結果（<see cref="Result{T}"/>）を検証し、エラー時は詳細な診断ログを出力します。
/// </summary>
public static class ConfigManager
{
    private static ConfigBase<PlayerConfig> _playerConfigContainer = new(new PlayerConfig());
    private static ConfigBase<Boss1Config> _boss1ConfigContainer = new(new Boss1Config());
    private static ConfigBase<Boss2Config> _boss2ConfigContainer = new(new Boss2Config());
    private static ConfigBase<PatternConfig> _patternConfigContainer = new(new PatternConfig());
    private static ConfigBase<ArenaConfig> _arenaConfigContainer = new(new ArenaConfig());
    private static ConfigBase<RenderConfig> _renderConfigContainer = new(new RenderConfig());

    /// <summary>プレイヤー設定オブジェクトを取得します。</summary>
    public static PlayerConfig Player => _playerConfigContainer.Config;

    /// <summary>Boss 1 (Ancient Cat Overlord) 設定オブジェクトを取得します。</summary>
    public static Boss1Config Boss1 => _boss1ConfigContainer.Config;

    /// <summary>Boss 2 (Ground Crusher) 設定オブジェクトを取得します。</summary>
    public static Boss2Config Boss2 => _boss2ConfigContainer.Config;

    /// <summary>弾幕・技能パターン設定オブジェクトを取得します。</summary>
    public static PatternConfig Pattern => _patternConfigContainer.Config;

    /// <summary>アリーナ足場設定オブジェクトを取得します。</summary>
    public static ArenaConfig Arena => _arenaConfigContainer.Config;

    /// <summary>描画および UI 設定オブジェクトを取得します。</summary>
    public static RenderConfig Render => _renderConfigContainer.Config;

    /// <summary>プレイヤー設定コンテナを取得します。</summary>
    public static ConfigBase<PlayerConfig> PlayerContainer => _playerConfigContainer;

    /// <summary>Boss 1 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss1Config> Boss1Container => _boss1ConfigContainer;

    /// <summary>Boss 2 設定コンテナを取得します。</summary>
    public static ConfigBase<Boss2Config> Boss2Container => _boss2ConfigContainer;

    /// <summary>弾幕・技能パターン設定コンテナを取得します。</summary>
    public static ConfigBase<PatternConfig> PatternContainer => _patternConfigContainer;

    /// <summary>アリーナ足場設定コンテナを取得します。</summary>
    public static ConfigBase<ArenaConfig> ArenaContainer => _arenaConfigContainer;

    /// <summary>描画設定コンテナを取得します。</summary>
    public static ConfigBase<RenderConfig> RenderContainer => _renderConfigContainer;

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

        Result<ConfigBase<Boss1Config>> boss1Res = Boss1Config.LoadFromFile(ResourcePath.Config.Boss1);
        if (boss1Res.IsOk && boss1Res.Value != null)
        {
            _boss1ConfigContainer = boss1Res.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Boss1} の読み込みに失敗しました ({boss1Res.Error?.Type}): {boss1Res.Error?.Description}");
            failureCount++;
        }

        Result<ConfigBase<Boss2Config>> boss2Res = Boss2Config.LoadFromFile(ResourcePath.Config.Boss2);
        if (boss2Res.IsOk && boss2Res.Value != null)
        {
            _boss2ConfigContainer = boss2Res.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {ResourcePath.Config.Boss2} の読み込みに失敗しました ({boss2Res.Error?.Type}): {boss2Res.Error?.Description}");
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
        _boss1ConfigContainer.Dispose();
        _boss2ConfigContainer.Dispose();
        _patternConfigContainer.Dispose();
        _arenaConfigContainer.Dispose();
        _renderConfigContainer.Dispose();
    }
}
