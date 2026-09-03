using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using AnimalBattle.Core;

namespace AnimalBattle.Configs;

/// <summary>
/// 各種 TOML 設定ファイル（player.toml, boss1.toml, boss2.toml, patterns.toml, arena.toml, render.toml）を統括管理し、
/// 自動リロードおよびプロパティ参照を提供する静的設定マネージャークラス。
/// 全てのファイル読み込み結果を検証し、エラー時は詳細な診断ログを出力します。
/// </summary>

public static class ConfigManager
{
    private static ConfigBase<PlayerConfig> _playerConfigContainer = new(BakedConfigs.CreatePlayerConfig());
    private static ConfigBase<Boss1Config> _boss1ConfigContainer = new(BakedConfigs.CreateBoss1Config());
    private static ConfigBase<Boss2Config> _boss2ConfigContainer = new(BakedConfigs.CreateBoss2Config());
    private static ConfigBase<PatternConfig> _patternConfigContainer = new(BakedConfigs.CreatePatternConfig());
    private static ConfigBase<ArenaConfig> _arenaConfigContainer = new(BakedConfigs.CreateArenaConfig());
    private static ConfigBase<RenderConfig> _renderConfigContainer = new(BakedConfigs.CreateRenderConfig());

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
    /// 全設定を初期化します。
    /// Debug ビルド時は TOML ファイル群を動的ロードしてホットリロード監視を開始します。
    /// Release ビルド時はコード内にベイクされた初期設定データをそのまま使用し、外部ファイル不要で独立動作します。
    /// </summary>
    public static void Initialize()
    {
#if DEBUG
        TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(FlexibleColorConverter)));

        int successCount = 0;
        int failureCount = 0;

        LoadConfig(ResourcePath.Config.Player, ref _playerConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss1, ref _boss1ConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Boss2, ref _boss2ConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Patterns, ref _patternConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Arena, ref _arenaConfigContainer, ref successCount, ref failureCount);
        LoadConfig(ResourcePath.Config.Render, ref _renderConfigContainer, ref successCount, ref failureCount);

        Debug.Assert(failureCount == 0, "一部の TOML 設定ファイルの読み込みに失敗しました。");

        if (failureCount > 0)
        {
            Console.WriteLine($"[C# 設定警告] TOML 設定読み込み完了 (成功: {successCount}, 失敗: {failureCount})。失敗した設定はデフォルト値を使用します。");
            return;
        }

        Console.WriteLine($"[C# 設定初期化] 全ての TOML 設定ファイル（{successCount} 件）の読み込みが正常に完了しました！");
#else
        Console.WriteLine("[C# 設定初期化] Release 版: 埋め込み (Baked) 設定データを使用して独立起動しました。");
#endif
    }

#if DEBUG
    private static void LoadConfig<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
        string configPath,
        ref ConfigBase<T> container,
        ref int successCount,
        ref int failureCount) where T : class, new()
    {
        ErrorOr.ErrorOr<ConfigBase<T>> res = ConfigBase<T>.LoadFromFile(configPath);
        if (!res.IsError && res.Value != null)
        {
            container = res.Value;
            successCount++;
        }
        else
        {
            Console.WriteLine($"[C# 設定警告] {configPath} の読み込みに失敗しました ({res.FirstError.Type}): {res.FirstError.Description}");
            failureCount++;
        }
    }
#endif

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
