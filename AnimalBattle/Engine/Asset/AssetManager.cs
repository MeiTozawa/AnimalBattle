using System.Collections.Concurrent;
using AnimalBattle.Configs;

namespace AnimalBattle.Asset;

/// <summary>
///     <see cref="AnimationRegistry" /> および <see cref="ResourcePath" /> を参照してキャラクタースプライトや各種テクスチャをロード・管理し、
///     純粋なスプライト UV 切り出し計算を提供するアセットサービス。
/// </summary>
public sealed class AssetManager : IDisposable {
    private static readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];
    private static readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];
    private static readonly Dictionary<string, int> _textureHandles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<int, Vec2i> _graphSizes = [];
    private bool _isDisposed;

    /// <summary>
    ///     キャラクタースプライトがロード済みかどうかを取得します。
    /// </summary>
    public static bool HasCharacterSprite => _characterSpriteHandles.Count > 0;

    /// <summary>
    ///     アセットマネージャーが初期化済みかどうかを取得します。
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    ///     ロード済みのキャラクタースプライトおよび汎用テクスチャハンドルをすべて解放します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            Shutdown();
            _isDisposed = true;
        }
    }

    /// <summary>
    ///     テスト用に静的状態をリセットします。
    /// </summary>
    public static void ResetStateForTest()
    {
        _characterSpriteHandles.Clear();
        _characterConfigs.Clear();
        _textureHandles.Clear();
        _graphSizes.Clear();
        IsInitialized = false;
    }

    /// <summary>
    ///     ロード済みの全テクスチャ・スプライトを非托管エンジンから破棄し、静的状態をリセットします。
    /// </summary>
    public static void Shutdown()
    {
        foreach (var handle in _characterSpriteHandles.Values) AssetService.DeleteGraph(handle);
        _characterSpriteHandles.Clear();

        foreach (var handle in _textureHandles.Values) AssetService.DeleteGraph(handle);
        _textureHandles.Clear();
        _graphSizes.Clear();
        _characterConfigs.Clear();
        IsInitialized = false;
    }

    /// <summary>
    ///     静的レジストリからのスプライトシート画像ロードおよび設定初期化を行います。
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized) return;

        var allLoaded = true;
        foreach (var (charType, config) in AnimationRegistry.CharacterConfigs)
        {
            _characterConfigs[charType] = config;
            LoadCharacterSprite(charType);
            LoadCharacterClips(charType, config);
        }

        ReadOnlySpan<string> commonTextures = [
            ResourcePath.Characters.Character031_3Png,
            ResourcePath.Characters.Character027_3Png,
            ResourcePath.Characters.SwordShieldEffectSpritesPng,
            ResourcePath.Platform.SheetPng
        ];

        foreach (var texPath in commonTextures)
        {
            var handle = LoadTexture(texPath);
            if (handle < 0) allLoaded = false;
        }

        IsInitialized = true;
        Debug.Assert(allLoaded, "[AssetManager 警告] 一部のアセットのロードに失敗しました。");
    }

    private static void LoadCharacterSprite(CharacterType charType)
    {
        if (!AnimationRegistry.CharacterSprites.TryGetValue(charType, out var relPath)) return;

        var fullPath = ResolveAssetPath(relPath);
        var handle = AssetService.LoadGraph(fullPath);
        Debug.Assert(handle >= 0, $"[AssetManager 警告] キャラクタースプライトのロードに失敗しました ({charType}): {fullPath}");
        if (handle >= 0)
        {
            var size = AssetService.GetGraphSize(handle);
            _graphSizes[handle] = size;
        }

        _characterSpriteHandles[charType] = handle;
        _textureHandles[relPath] = handle;
        Debug.WriteLine(
            $"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {handle})");
    }

    private static void LoadCharacterClips(CharacterType charType, AnimationConfig config)
    {
        foreach (var clip in config.Clips)
        {
            if (!string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                var handle = LoadTexture(clip.SpritePath);
                Debug.Assert(handle >= 0, $"[AssetManager 警告] クリップスプライトのロードに失敗しました ({charType} / {clip.Name}): {clip.SpritePath}");
            }
        }
    }

    /// <summary>
    ///     指定された相対パスまたは絶対パスのテクスチャ画像をロードし、ハンドルをキャッシュして返します。
    ///     既にロード済みの場合はキャッシュされたハンドルを返します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス。</param>
    /// <returns>正のグラフィックハンドル。失敗時は -1</returns>
    public static int LoadTexture(string relativePath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(relativePath), "[AssetManager 警告] 相対パスが null または空です。");
        if (string.IsNullOrWhiteSpace(relativePath)) return -1;

        if (_textureHandles.TryGetValue(relativePath, out var existingHandle)) return existingHandle;

        var fullPath = ResolveAssetPath(relativePath);
        var handle = AssetService.LoadGraph(fullPath);
        Debug.Assert(handle >= 0, $"[AssetManager 警告] テクスチャのロードに失敗しました ({relativePath}): {fullPath}");
        if (handle >= 0)
        {
            var size = AssetService.GetGraphSize(handle);
            _graphSizes[handle] = size;
        }

        _textureHandles[relativePath] = handle;
        Debug.WriteLine($"[C# アセット] テクスチャを正常にロードしました: {relativePath} -> {fullPath} (Handle: {handle})");

        return handle;
    }

    /// <summary>
    ///     キャッシュ済みのテクスチャハンドルの取得を試行します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス。</param>
    /// <param name="handle">グラフィックハンドル。</param>
    /// <returns>取得成功時は true、未キャッシュ時は false。</returns>
    public static bool TryGetTexture(string relativePath, out int handle)
    {
        if (!string.IsNullOrWhiteSpace(relativePath) &&
            _textureHandles.TryGetValue(relativePath, out handle)) return true;

        handle = -1;
        return false;
    }

    /// <summary>
    ///     テクスチャハンドルに対応する画像サイズを手動キャッシュ登録します。
    /// </summary>
    /// <param name="handle">グラフィックハンドル。</param>
    /// <param name="size">画像サイズ (<see cref="Vec2i" />)。</param>
    public static void CacheGraphSize(int handle, Vec2i size)
    {
        _graphSizes[handle] = size;
    }

    /// <summary>
    ///     キャッシュ済みのテクスチャ画像サイズの取得を試行します。
    /// </summary>
    /// <param name="handle">グラフィックハンドル。</param>
    /// <param name="size">画像サイズ (<see cref="Vec2i" />)。</param>
    /// <returns>キャッシュが存在する場合は true、未キャッシュ時は false。</returns>
    public static bool TryGetGraphSize(int handle, out Vec2i size)
    {
        return _graphSizes.TryGetValue(handle, out size);
    }

    /// <summary>
    ///     キャッシュ済みのテクスチャ画像サイズを取得します。
    ///     主スレッドロード時にキャッシュされた情報を返すため、並行論理スレッドから安全に呼び出し可能です。
    /// </summary>
    /// <param name="handle">グラフィックハンドル。</param>
    /// <returns>画像サイズ (<see cref="Vec2i" />)。未キャッシュ時は Vec2i.Zero。</returns>
    public static Vec2i GetGraphSize(int handle)
    {
        if (_graphSizes.TryGetValue(handle, out var size))
        {
            return size;
        }

        Debug.Fail($"[AssetManager 警告] ハンドル {handle} のテクスチャサイズがキャッシュされていません。主スレッドでの事前ロードを確認してください。");
        return Vec2i.Zero;
    }


    /// <summary>
    ///     キャッシュ済みのキャラクタースプライトハンドルの取得を試行します。
    /// </summary>
    /// <param name="type">キャラクター種別。</param>
    /// <param name="clipId">クリップ識別子。</param>
    /// <param name="handle">グラフィックハンドル。</param>
    /// <returns>取得成功時は true、失敗時は false。</returns>
    public static bool TryGetCharacterSpriteHandle(CharacterType type, StringId clipId, out int handle)
    {
        if (clipId != StringId.Empty && _characterConfigs.TryGetValue(type, out var config))
        {
            var clip = config.FindClip(clipId);
            if (clip != null && !string.IsNullOrWhiteSpace(clip.SpritePath) &&
                TryGetTexture(clip.SpritePath, out handle)) return true;
        }

        if (_characterSpriteHandles.TryGetValue(type, out handle)) return true;

        handle = -1;
        return false;
    }

    /// <summary>
    ///     キャッシュ済みのキャラクターアニメーション設定の取得を試行します。
    /// </summary>
    /// <param name="type">キャラクター種別。</param>
    /// <param name="config">取得された設定。</param>
    /// <returns>取得成功時は true、未登録時は false。</returns>
    public static bool TryGetCharacterConfig(CharacterType type, out AnimationConfig config)
    {
        if (_characterConfigs.TryGetValue(type, out var found))
        {
            config = found;
            return true;
        }

        config = null!;
        return false;
    }

    /// <summary>
    ///     アニメーションクリップ識別子と再生経過時間からスプライトシート上の切り出し矩形 UV SrcRect を定数時間で計算します。
    /// </summary>
    /// <param name="config">アニメーション設定</param>
    /// <param name="clipId">再生対象クリップ識別子</param>
    /// <param name="animTimer">再生経過時間秒数</param>
    /// <param name="isFinished">非ループ再生が完了したかどうかの出力</param>
    /// <returns>スプライトシート切り出し領域</returns>
    public static Rect CalculateSrcRect(AnimationConfig config, StringId clipId, float animTimer, out bool isFinished)
    {
        isFinished = false;
        var clip = config.FindClip(clipId);
        if (clip == null) return new Rect(0, 0, config.CellWidth, config.CellHeight);

        var totalFrames = clip.EndFrame - clip.StartFrame + 1;
        if (totalFrames <= 0) totalFrames = 1;

        var frameIndex = (int)(animTimer * clip.Fps);
        if (clip.IsLooping)
        {
            frameIndex %= totalFrames;
        }
        else if (frameIndex >= totalFrames)
        {
            frameIndex = totalFrames - 1;
            isFinished = true;
        }

        var col = clip.StartFrame + frameIndex;
        var row = clip.Row;

        var cellW = clip.CellWidth > 0 ? clip.CellWidth : config.CellWidth;
        var cellH = clip.CellHeight > 0 ? clip.CellHeight : config.CellHeight;

        var x1 = col * cellW;
        var y1 = row * cellH;
        var x2 = x1 + cellW;
        var y2 = y1 + cellH;

        return new Rect(x1, y1, x2, y2);
    }

    /// <summary>
    ///     リソース相対パスから実体ファイルの絶対パスを探索・解決します。
    /// </summary>
    /// <param name="relPath">リソース相対パス</param>
    /// <returns>解決された絶対パス、見つからない場合は入力パス</returns>
    public static string ResolveAssetPath(string relPath)
    {
        return AssetPathResolver.Instance.ResolveAssetPath(relPath);
    }
}