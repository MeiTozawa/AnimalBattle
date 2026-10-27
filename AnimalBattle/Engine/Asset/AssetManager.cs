using AnimalBattle.Logging;

namespace AnimalBattle.Asset;

/// <summary>
/// <see cref="AnimationRegistry" /> および <see cref="ResourcePath" /> を参照してキャラクタースプライトや各種テクスチャをロード・管理し、
/// 画像サイズキャッシュおよび純粋なスプライト UV 切り出し計算を提供する具象アセットマネージャー。
/// </summary>
public sealed class AssetManager : IAssetManager {
    private readonly IAssetPathResolver _pathResolver;
    private readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];
    private readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];
    private readonly Dictionary<string, int> _textureHandles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, Vec2i> _graphSizes = [];
    private bool _isDisposed;

    /// <summary>
    /// <see cref="AssetManager"/> の新しいインスタンスを初期化します。
    /// </summary>
    public AssetManager()
    {
        _pathResolver = AssetPathResolver.Instance;
    }

    /// <inheritdoc />
    public bool HasCharacterSprite => _characterSpriteHandles.Count > 0;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            Shutdown();
            _isDisposed = true;
        }
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        foreach (var handle in _characterSpriteHandles.Values) AssetService.DeleteGraph(handle);
        _characterSpriteHandles.Clear();

        foreach (var handle in _textureHandles.Values) AssetService.DeleteGraph(handle);
        _textureHandles.Clear();
        _characterConfigs.Clear();
        _graphSizes.Clear();
    }

    /// <inheritdoc />
    public void Initialize()
    {
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

        Debug.Assert(allLoaded, "[AssetManager 警告] 一部のアセットのロードに失敗しました。");
    }

    private void LoadCharacterSprite(CharacterType charType)
    {
        if (!AnimationRegistry.CharacterSprites.TryGetValue(charType, out var relPath)) return;

        var fullPath = ResolveAssetPath(relPath);
        var handle = AssetService.LoadGraph(fullPath);
        Debug.Assert(handle >= 0, $"[AssetManager 警告] キャラクタースプライトのロードに失敗しました ({charType}): {fullPath}");

        _characterSpriteHandles[charType] = handle;
        _textureHandles[relPath] = handle;
        if (handle >= 0)
        {
            _graphSizes[handle] = AssetService.GetGraphSize(handle);
        }
        Log.Info("AssetManager", 
            $"[C# アニメーション] キャラクタースプライトを正常にロードしました ({charType}): {fullPath} (Handle: {handle})");
    }

    private void LoadCharacterClips(CharacterType charType, AnimationConfig config)
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

    /// <inheritdoc />
    public int LoadTexture(string relativePath)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(relativePath), "[AssetManager 警告] 相対パスが null または空です。");
        if (string.IsNullOrWhiteSpace(relativePath)) return -1;

        if (_textureHandles.TryGetValue(relativePath, out var existingHandle)) return existingHandle;

        var fullPath = ResolveAssetPath(relativePath);
        var handle = AssetService.LoadGraph(fullPath);
        Debug.Assert(handle >= 0, $"[AssetManager 警告] テクスチャのロードに失敗しました ({relativePath}): {fullPath}");

        _textureHandles[relativePath] = handle;
        if (handle >= 0)
        {
            _graphSizes[handle] = AssetService.GetGraphSize(handle);
        }
        Log.Info("AssetManager", $"[C# アセット] テクスチャを正常にロードしました: {relativePath} -> {fullPath} (Handle: {handle})");

        return handle;
    }

    /// <inheritdoc />
    public bool TryGetTexture(string relativePath, out int handle)
    {
        if (!string.IsNullOrWhiteSpace(relativePath) &&
            _textureHandles.TryGetValue(relativePath, out handle)) return true;

        handle = -1;
        return false;
    }

    /// <inheritdoc />
    public bool TryGetGraphSize(int handle, out Vec2i size)
    {
        return _graphSizes.TryGetValue(handle, out size);
    }

    /// <inheritdoc />
    public Vec2i GetGraphSize(int handle)
    {
        if (_graphSizes.TryGetValue(handle, out var size))
        {
            return size;
        }

        var nativeSize = AssetService.GetGraphSize(handle);
        if (handle >= 0)
        {
            _graphSizes[handle] = nativeSize;
        }
        return nativeSize;
    }

    /// <inheritdoc />
    public bool TryGetCharacterSpriteHandle(CharacterType type, StringId clipId, out int handle)
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

    /// <inheritdoc />
    public bool TryGetCharacterConfig(CharacterType type, out AnimationConfig config)
    {
        if (_characterConfigs.TryGetValue(type, out var found))
        {
            config = found;
            return true;
        }

        config = null!;
        return false;
    }

    /// <inheritdoc />
    public Rect CalculateSrcRect(AnimationConfig config, StringId clipId, float animTimer, out bool isFinished)
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

    /// <inheritdoc />
    public string ResolveAssetPath(string relativePath)
    {
        return _pathResolver.ResolveAssetPath(relativePath);
    }
}
