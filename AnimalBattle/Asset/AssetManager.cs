using System;
using System.Collections.Generic;
using System.IO;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Core.Asset;

/// <summary>
/// <see cref="AnimationRegistry"/> および <see cref="ResourcePath"/> を参照してキャラクタースプライトや各種テクスチャをロード・管理し、
/// 純粋なスプライト UV 切り出し計算を提供するアセットサービス。
/// </summary>
public sealed class AssetManager : IDisposable
{
    private static readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];
    private static readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];
    private static readonly Dictionary<string, int> _textureHandles = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// キャラクタースプライトがロード済みかどうかを取得します。
    /// </summary>
    public static bool HasCharacterSprite => _characterSpriteHandles.Count > 0;

    /// <summary>
    /// 静的レジストリからのスプライトシート画像ロードおよび設定初期化を行います。
    /// </summary>
    /// <returns>全アセットが正常にロードされた場合は true、失敗した場合は false</returns>
    public static bool Initialize()
    {
        if (_isInitialized)
        {
            return true;
        }

        bool allLoaded = true;
        foreach ((CharacterType charType, AnimationConfig config) in AnimationRegistry.CharacterConfigs)
        {
            _characterConfigs[charType] = config;
            if (!LoadCharacterSprite(charType))
            {
                allLoaded = false;
            }

            if (!LoadCharacterClips(charType, config))
            {
                allLoaded = false;
            }
        }

        _isInitialized = true;
        return allLoaded;
    }

    private static bool LoadCharacterSprite(CharacterType charType)
    {
        if (!AnimationRegistry.CharacterSprites.TryGetValue(charType, out string? relPath))
        {
            return true;
        }

        string fullPath = ResolveAssetPath(relPath);
        Result<int> loadResult = AssetService.LoadGraph(fullPath);
        if (loadResult.IsOk)
        {
            _characterSpriteHandles[charType] = loadResult.Value;
            _textureHandles[relPath] = loadResult.Value;
            Console.WriteLine($"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {loadResult.Value})");
            return true;
        }

        Console.WriteLine($"[C# アニメーション警告] キャラクタースプライトのロードに失敗しました ({charType}): {loadResult.Error?.Description}");
        return false;
    }

    private static bool LoadCharacterClips(CharacterType charType, AnimationConfig config)
    {
        bool allClipsLoaded = true;
        foreach (AnimationClipConfig clip in config.Clips)
        {
            if (!string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                int handle = LoadTexture(clip.SpritePath);
                if (handle < 0)
                {
                    Console.WriteLine($"[C# アニメーション警告] クリップスプライトのロードに失敗しました ({charType} / {clip.Name}): {clip.SpritePath}");
                    allClipsLoaded = false;
                }
            }
        }
        return allClipsLoaded;
    }

    /// <summary>
    /// 指定された相対パスまたは絶対パスのテクスチャ画像をロードし、ハンドルをキャッシュして返します。
    /// 既にロード済みの場合はキャッシュされたハンドルを返します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: <see cref="ResourcePath.Platform.SheetPng"/>）</param>
    /// <returns>グラフィックハンドル（ロード失敗時は -1）</returns>
    public static int LoadTexture(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return -1;
        }

        if (_textureHandles.TryGetValue(relativePath, out int existingHandle))
        {
            return existingHandle;
        }

        string fullPath = ResolveAssetPath(relativePath);
        Result<int> loadResult = AssetService.LoadGraph(fullPath);
        if (loadResult.IsOk)
        {
            _textureHandles[relativePath] = loadResult.Value;
            Console.WriteLine($"[C# アセット] テクスチャを正常にロードしました: {relativePath} -> {fullPath} (Handle: {loadResult.Value})");
            return loadResult.Value;
        }

        Console.WriteLine($"[C# アセット警告] テクスチャのロードに失敗しました ({relativePath}): {loadResult.Error?.Description}");
        return -1;
    }

    /// <summary>
    /// ロード済みのテクスチャハンドルを取得します。未ロードの場合はロードを試行します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: <see cref="ResourcePath.Platform.SheetPng"/>）</param>
    /// <returns>グラフィックハンドル（未ロード・ロード失敗時は -1）</returns>
    public static int GetTexture(string relativePath)
    {
        return LoadTexture(relativePath);
    }

    /// <summary>
    /// 指定されたキャラクター種別およびクリップ名に対応するグラフィックハンドルを取得します。
    /// クリップ専用のスプライトシートが指定されている場合はそれを返し、未指定の場合はキャラクター標準スプライトを返します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <param name="clipName">再生クリップ名（null の場合は標準スプライトハンドル）</param>
    /// <returns>グラフィックハンドル（未ロード時は -1）</returns>
    public static int GetCharacterSpriteHandle(CharacterType type = CharacterType.Cat, string? clipName = null)
    {
        if (!string.IsNullOrWhiteSpace(clipName) &&
            _characterConfigs.TryGetValue(type, out AnimationConfig? config))
        {
            AnimationClipConfig? clip = config.Clips.Find(c => string.Equals(c.Name, clipName, StringComparison.OrdinalIgnoreCase));
            if (clip != null && !string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                int clipHandle = GetTexture(clip.SpritePath);
                if (clipHandle >= 0)
                {
                    return clipHandle;
                }
            }
        }

        return _characterSpriteHandles.TryGetValue(type, out int handle) ? handle : -1;
    }

    /// <summary>
    /// 指定されたキャラクター設定を取得します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <returns>アニメーション設定（未登録時は null）</returns>
    public static AnimationConfig? GetCharacterConfig(CharacterType type = CharacterType.Cat)
    {
        return _characterConfigs.TryGetValue(type, out AnimationConfig? config) ? config : null;
    }

    /// <summary>
    /// アニメーションクリップ名と再生経過時間から、スプライトシート上の切り出し矩形 (UV SrcRect) を計算します。
    /// </summary>
    /// <param name="config">アニメーション設定</param>
    /// <param name="clipName">再生対象クリップ名</param>
    /// <param name="animTimer">再生経過時間（秒）</param>
    /// <param name="isFinished">非ループ再生が完了したかどうかの出力</param>
    /// <returns>スプライトシート切り出し領域</returns>
    public static Rect<int> CalculateSrcRect(AnimationConfig config, string clipName, float animTimer, out bool isFinished)
    {
        isFinished = false;
        AnimationClipConfig? clip = config.Clips.Find(c => string.Equals(c.Name, clipName, StringComparison.OrdinalIgnoreCase));
        if (clip == null)
        {
            return new Rect<int>(0, 0, config.CellWidth, config.CellHeight);
        }

        int totalFrames = clip.EndFrame - clip.StartFrame + 1;
        if (totalFrames <= 0)
        {
            totalFrames = 1;
        }

        int frameIndex = (int)(animTimer * clip.Fps);
        if (clip.IsLooping)
        {
            frameIndex %= totalFrames;
        }
        else if (frameIndex >= totalFrames)
        {
            frameIndex = totalFrames - 1;
            isFinished = true;
        }

        int col = clip.StartFrame + frameIndex;
        int row = clip.Row;

        int cellW = clip.CellWidth > 0 ? clip.CellWidth : config.CellWidth;
        int cellH = clip.CellHeight > 0 ? clip.CellHeight : config.CellHeight;

        int x1 = col * cellW;
        int y1 = row * cellH;
        int x2 = x1 + cellW;
        int y2 = y1 + cellH;

        return new Rect<int>(x1, y1, x2, y2);
    }

    /// <summary>
    /// リソース相対パスから実体ファイルの絶対パスを探索・解決します。
    /// </summary>
    /// <param name="relPath">リソース相対パス</param>
    /// <returns>解決された絶対パス、見つからない場合は入力パス</returns>
    public static string ResolveAssetPath(string relPath)
    {
        try
        {
            string[] directRoots = [AppDomain.CurrentDomain.BaseDirectory, Environment.CurrentDirectory];
            foreach (string root in directRoots)
            {
                string? found = TryFindAssetFile(root, relPath);
                if (found != null)
                {
                    return found;
                }
            }

            DirectoryInfo? dir = new(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                string? found = TryFindAssetFile(dir.FullName, relPath);
                if (found != null)
                {
                    return found;
                }
                dir = dir.Parent;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[C# アセット警告] アセットパス探索中に例外が発生しました ({relPath}): {ex.Message}");
        }

        return relPath;
    }

    private static string? TryFindAssetFile(string rootDir, string relPath)
    {
        string directResource = Path.Combine(rootDir, "Resource", relPath);
        if (File.Exists(directResource))
        {
            return Path.GetFullPath(directResource);
        }

        string directCandidate = Path.Combine(rootDir, relPath);
        if (File.Exists(directCandidate))
        {
            return Path.GetFullPath(directCandidate);
        }

        return null;
    }

    /// <summary>
    /// ロード済みのキャラクタースプライトおよび汎用テクスチャハンドルをすべて解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            foreach (int handle in _characterSpriteHandles.Values)
            {
                AssetService.DeleteGraph(handle);
            }
            _characterSpriteHandles.Clear();

            foreach (int handle in _textureHandles.Values)
            {
                AssetService.DeleteGraph(handle);
            }
            _textureHandles.Clear();

            _isDisposed = true;
        }
    }
}

