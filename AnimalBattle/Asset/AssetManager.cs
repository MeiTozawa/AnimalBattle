using AnimalBattle.Configs;
using ErrorOr;

namespace AnimalBattle.Asset;

/// <summary>
///     <see cref="AnimationRegistry" /> および <see cref="ResourcePath" /> を参照してキャラクタースプライトや各種テクスチャをロード・管理し、
///     純粋なスプライト UV 切り出し計算を提供するアセットサービス。
/// </summary>
public sealed class AssetManager : IDisposable {
    private static readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];
    private static readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];
    private static readonly Dictionary<string, int> _textureHandles = new(StringComparer.OrdinalIgnoreCase);
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
            if (!LoadCharacterSprite(charType)) allLoaded = false;

            if (!LoadCharacterClips(charType, config)) allLoaded = false;
        }

        IsInitialized = true;
        if (!allLoaded) Console.WriteLine("[AssetManager 警告] 一部のアセットのロードに失敗しました。デフォルト/欠落テクスチャ状態で継続します。");
    }

    private static bool LoadCharacterSprite(CharacterType charType)
    {
        if (!AnimationRegistry.CharacterSprites.TryGetValue(charType, out var relPath)) return true;

        var fullPath = ResolveAssetPath(relPath);
        var loadResult = AssetService.LoadGraph(fullPath);
        if (!loadResult.IsError)
        {
            _characterSpriteHandles[charType] = loadResult.Value;
            _textureHandles[relPath] = loadResult.Value;
            Console.WriteLine(
                $"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {loadResult.Value})");
            return true;
        }

        Console.WriteLine($"[C# アニメーション警告] キャラクタースプライトのロードに失敗しました ({charType}): {loadResult.FirstError.Description}");
        return false;
    }

    private static bool LoadCharacterClips(CharacterType charType, AnimationConfig config)
    {
        var allClipsLoaded = true;
        foreach (var clip in config.Clips)
            if (!string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                var loadResult = LoadTexture(clip.SpritePath);
                if (loadResult.IsError)
                {
                    Console.WriteLine(
                        $"[C# アニメーション警告] クリップスプライトのロードに失敗しました ({charType} / {clip.Name}): {clip.SpritePath}");
                    allClipsLoaded = false;
                }
            }

        return allClipsLoaded;
    }

    /// <summary>
    ///     指定された相対パスまたは絶対パスのテクスチャ画像をロードし、ハンドルをキャッシュして返します。
    ///     既にロード済みの場合はキャッシュされたハンドルを返します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: <see cref="ResourcePath.Platform.SheetPng" />）</param>
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}" /></returns>
    public static ErrorOr<int> LoadTexture(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Error.Validation("Asset.EmptyPath", "相対パスが null または空です。");

        if (_textureHandles.TryGetValue(relativePath, out var existingHandle)) return existingHandle;

        var fullPath = ResolveAssetPath(relativePath);
        var loadResult = AssetService.LoadGraph(fullPath);
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
    ///     ロード済みのテクスチャハンドルを取得します。未ロードの場合はロードを試行します。
    /// </summary>
    /// <param name="relativePath">リソース相対パス（例: <see cref="ResourcePath.Platform.SheetPng" />）</param>
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}" /></returns>
    public static ErrorOr<int> GetTexture(string relativePath)
    {
        return LoadTexture(relativePath);
    }

    /// <summary>
    ///     指定されたキャラクター種別およびクリップ識別子に対応するグラフィックハンドルを取得します。
    ///     クリップ専用のスプライトシートが指定されている場合はそれを返し、未指定の場合はキャラクター標準スプライトを返します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <param name="clipId">再生クリップ識別子（Empty の場合は標準スプライトハンドル）</param>
    /// <returns>グラフィックハンドルを含む <see cref="ErrorOr.ErrorOr{T}" /></returns>
    public static ErrorOr<int> GetCharacterSpriteHandle(CharacterType type = CharacterType.Cat,
        StringId clipId = default)
    {
        if (clipId != StringId.Empty &&
            _characterConfigs.TryGetValue(type, out var config))
        {
            var clip = config.FindClip(clipId);
            if (clip != null && !string.IsNullOrWhiteSpace(clip.SpritePath))
            {
                var clipResult = GetTexture(clip.SpritePath);
                if (!clipResult.IsError) return clipResult;
            }
        }

        if (_characterSpriteHandles.TryGetValue(type, out var handle)) return handle;

        return Error.NotFound("Asset.SpriteNotFound", $"キャラクター '{type}' のスプライトハンドルが見つかりません。");
    }

    /// <summary>
    ///     指定されたキャラクター設定を取得します。
    /// </summary>
    /// <param name="type">キャラクター種別</param>
    /// <returns>アニメーション設定を含む <see cref="ErrorOr.ErrorOr{T}" /></returns>
    public static ErrorOr<AnimationConfig> GetCharacterConfig(CharacterType type = CharacterType.Cat)
    {
        if (_characterConfigs.TryGetValue(type, out var config) && config != null) return config;

        return Error.NotFound("Asset.ConfigNotFound", $"キャラクター '{type}' のアニメーション設定が見つかりません。");
    }

    /// <summary>
    ///     アニメーションクリップ識別子と再生経過時間から、スプライトシート上の切り出し矩形 (UV SrcRect) を O(1) で計算します。
    /// </summary>
    /// <param name="config">アニメーション設定</param>
    /// <param name="clipId">再生対象クリップ識別子</param>
    /// <param name="animTimer">再生経過時間（秒）</param>
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