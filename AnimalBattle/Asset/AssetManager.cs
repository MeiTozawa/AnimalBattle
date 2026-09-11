using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Asset;

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
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
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
        Debug.Assert(allLoaded, "一部のアセットのロードに失敗しました。");
    }

    private static bool LoadCharacterSprite(CharacterType charType)
    {
        if (!AnimationRegistry.CharacterSprites.TryGetValue(charType, out string? relPath))
        {
            return true;
        }

        string fullPath = ResolveAssetPath(relPath);
        ErrorOr.ErrorOr<int> loadResult = AssetService.LoadGraph(fullPath);
        if (!loadResult.IsError)
        {
            _characterSpriteHandles[charType] = loadResult.Value;
            _textureHandles[relPath] = loadResult.Value;
            Console.WriteLine($"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {loadResult.Value})");
            return true;
        }

        Console.WriteLine($"[C# アニメーション警告] キャラクタースプライトのロードに失敗しました ({charType}): {loadResult.FirstError.Description}");
        return false;
    }

    private static bool LoadCharacterClips(CharacterType charType, AnimationConfig config)
    {
        bool allClipsLoaded = true;
        foreach (AnimationClipConfig clip in config.Clips)
        {
            if (!string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                ErrorOr.ErrorOr<int> loadResult = LoadTexture(clip.SpritePath);
                if (loadResult.IsError)
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
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}"/></returns>
    public static ErrorOr.ErrorOr<int> LoadTexture(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return ErrorOr.Error.Validation("Asset.EmptyPath", "相対パスが null または空です。");
        }

        if (_textureHandles.TryGetValue(relativePath, out int existingHandle))
        {
            return existingHandle;
        }

        string fullPath = ResolveAssetPath(relativePath);
        ErrorOr.ErrorOr<int> loadResult = AssetService.LoadGraph(fullPath);
        if (!loadResult.IsError)
        {
            _textureHandles[relativePath] = loadResult.Value;
            Console.WriteLine($"[C# アセット] テクスチャを正常にロードしました: {relativePath} -> {fullPath} (Handle: {loadResult.Value})");
            return loadResult;
        }

        Console.WriteLine($"[C# アセット警告] テクスチャのロードに失敗しました ({relativePath}): {loadResult.FirstError.Description}");
        return loadResult;
    }

    /// <summary>
    /// ロード済みのテクスチャハンドルを取得します。未ロードの場合はロードを試行します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: <see cref="ResourcePath.Platform.SheetPng"/>）</param>
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}"/></returns>
    public static ErrorOr.ErrorOr<int> GetTexture(string relativePath)
    {
        return LoadTexture(relativePath);
    }

    /// <summary>
    /// 指定されたキャラクター種別およびクリップ識別子に対応するグラフィックハンドルを取得します。
    /// クリップ専用のスプライトシートが指定されている場合はそれを返し、未指定の場合はキャラクター標準スプライトを返します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <param name="clipId">再生クリップ識別子（Empty の場合は標準スプライトハンドル）</param>
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}"/></returns>
    public static ErrorOr.ErrorOr<int> GetCharacterSpriteHandle(CharacterType type = CharacterType.Cat, StringId clipId = default)
    {
        if (clipId != StringId.Empty &&
            _characterConfigs.TryGetValue(type, out AnimationConfig? config))
        {
            AnimationClipConfig? clip = config.FindClip(clipId);
            if (clip != null && !string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                ErrorOr.ErrorOr<int> clipResult = GetTexture(clip.SpritePath);
                if (!clipResult.IsError)
                {
                    return clipResult;
                }
            }
        }

        if (_characterSpriteHandles.TryGetValue(type, out int handle))
        {
            return handle;
        }

        return ErrorOr.Error.NotFound("Asset.SpriteNotFound", $"キャラクター '{type}' のスプライトハンドルが見つかりません。");
    }

    /// <summary>
    /// 指定されたキャラクター設定を取得します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <returns>アニメーション設定を含む <see cref="ErrorOr.ErrorOr{T}"/></returns>
    public static ErrorOr.ErrorOr<AnimationConfig> GetCharacterConfig(CharacterType type = CharacterType.Cat)
    {
        if (_characterConfigs.TryGetValue(type, out AnimationConfig? config) && config != null)
        {
            return config;
        }

        return ErrorOr.Error.NotFound("Asset.ConfigNotFound", $"キャラクター '{type}' のアニメーション設定が見つかりません。");
    }

    /// <summary>
    /// アニメーションクリップ識別子と再生経過時間から、スプライトシート上の切り出し矩形 (UV SrcRect) を O(1) で計算します。
    /// </summary>
    /// <param name="config">アニメーション設定</param>
    /// <param name="clipId">再生対象クリップ識別子</param>
    /// <param name="animTimer">再生経過時間（秒）</param>
    /// <param name="isFinished">非ループ再生が完了したかどうかの出力</param>
    /// <returns>スプライトシート切り出し領域</returns>
    public static Rect CalculateSrcRect(AnimationConfig config, StringId clipId, float animTimer, out bool isFinished)
    {
        isFinished = false;
        AnimationClipConfig? clip = config.FindClip(clipId);
        if (clip == null)
        {
            return new Rect(0, 0, config.CellWidth, config.CellHeight);
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

        return new Rect(x1, y1, x2, y2);
    }

    /// <summary>
    /// リソース相対パスから実体ファイルの絶対パスを探索・解決します。
    /// </summary>
    /// <param name="relPath">リソース相対パス</param>
    /// <returns>解決された絶対パス、見つからない場合は入力パス</returns>
    public static string ResolveAssetPath(string relPath)
    {
        return AssetPathResolver.Instance.ResolveAssetPath(relPath);
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

