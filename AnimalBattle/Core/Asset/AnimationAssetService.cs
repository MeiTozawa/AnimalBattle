using System;
using System.Collections.Generic;
using System.IO;
using AnimalBattle.Configs;
using AnimalBattle.Core;

namespace AnimalBattle.Core.Asset;

/// <summary>
/// <see cref="AnimationRegistry"/> を参照してキャラクタースプライトシート画像をロード・管理し、
/// 純粋なスプライト UV 切り出し計算を提供するアセットサービス。
/// </summary>
public sealed class AnimationAssetService : IDisposable
{
    private static readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];
    private static readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];
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
            if (AnimationRegistry.CharacterSprites.TryGetValue(charType, out string? relPath))
            {
                string fullPath = ResolveAssetPath(relPath);
                Result<int> loadResult = AssetManager.LoadGraph(fullPath);
                if (loadResult.IsOk)
                {
                    _characterSpriteHandles[charType] = loadResult.Value;
                    Console.WriteLine($"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {loadResult.Value})");
                }
                else
                {
                    Console.WriteLine($"[C# アニメーション警告] キャラクタースプライトのロードに失敗しました ({charType}): {loadResult.Error?.Description}");
                    allLoaded = false;
                }
            }
        }

        _isInitialized = true;
        return allLoaded;
    }

    /// <summary>
    /// 指定されたキャラクター種別のスプライトグラフィックハンドルを取得します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <returns>グラフィックハンドル（未ロード時は -1）</returns>
    public static int GetCharacterSpriteHandle(CharacterType type = CharacterType.CatSpriteSheet)
    {
        return _characterSpriteHandles.TryGetValue(type, out int handle) ? handle : -1;
    }

    /// <summary>
    /// 指定されたキャラクター設定を取得します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <returns>アニメーション設定（未登録時は null）</returns>
    public static AnimationConfig? GetCharacterConfig(CharacterType type = CharacterType.CatSpriteSheet)
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

    private static string ResolveAssetPath(string relPath)
    {
        try
        {
            string directResource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource", relPath);
            if (File.Exists(directResource))
            {
                return Path.GetFullPath(directResource);
            }

            string cwdResource = Path.Combine(Environment.CurrentDirectory, "Resource", relPath);
            if (File.Exists(cwdResource))
            {
                return Path.GetFullPath(cwdResource);
            }

            DirectoryInfo? dir = new(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                string directCandidate = Path.Combine(dir.FullName, "Resource", relPath);
                if (File.Exists(directCandidate))
                {
                    return Path.GetFullPath(directCandidate);
                }

                string altCandidate = Path.Combine(dir.FullName, relPath);
                if (File.Exists(altCandidate))
                {
                    return Path.GetFullPath(altCandidate);
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

    /// <summary>
    /// ロード済みのキャラクタースプライトハンドルを解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            foreach (int handle in _characterSpriteHandles.Values)
            {
                AssetManager.DeleteGraph(handle);
            }
            _characterSpriteHandles.Clear();
            _isDisposed = true;
        }
    }
}
