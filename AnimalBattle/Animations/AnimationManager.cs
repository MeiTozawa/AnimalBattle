using System;
using System.Collections.Generic;
using System.IO;
using AnimalBattle.Core;

namespace AnimalBattle.Animations;

/// <summary>
/// <see cref="AnimationRegistry"/> を参照してキャラクターおよびエフェクトのスプライトを
/// ロード・初期化するマネージャークラス。
/// アニメーション設定はビルド時に TOML から生成された静的レジストリから取得します。
/// </summary>
public sealed class AnimationManager : IDisposable
{
    private readonly List<AnimationPlayer> _characterAnimPlayers = [];
    private readonly List<AnimationPlayer> _effectAnimPlayers = [];

    // ロード済みスプライトハンドル (キャラクター種別 → ハンドル)
    private readonly Dictionary<CharacterType, int> _characterSpriteHandles = [];

    // 各プレイヤーに割り当てられたキャラクター種別
    private readonly List<CharacterType> _playerCharacterTypes = [];

    // ロード済みアニメーション設定 (キャラクター種別 → 設定)
    private readonly Dictionary<CharacterType, AnimationConfig> _characterConfigs = [];

    // ロード済みエフェクトスプライトハンドル
    private readonly Dictionary<EffectType, int> _effectSpriteHandles = [];
    private readonly Dictionary<EffectType, AnimationConfig> _effectConfigs = [];
    private bool _isDisposed;

    /// <summary>
    /// キャラクタースプライトが1体以上ロードされているかを取得します。
    /// </summary>
    public bool HasCatSprite => _characterSpriteHandles.Count > 0;

    /// <summary>
    /// 攻撃エフェクトのスプライト画像が正常にロードされているかを取得します。
    /// </summary>
    public bool HasEffectSprite => _effectSpriteHandles.Count > 0;

    /// <summary>
    /// デストラクタ。
    /// </summary>
    ~AnimationManager()
    {
        Dispose(false);
    }

    /// <summary>
    /// <see cref="AnimationRegistry"/> からアセットをロードし、
    /// 指定されたプレイヤー人数 (1～4人) 分のアニメーションプレイヤーを初期化します。
    /// </summary>
    /// <param name="playerCount">参戦プレイヤー人数</param>
    public void Initialize(int playerCount = 4)
    {
        playerCount = Math.Clamp(playerCount, 1, 4);
        string resourceRoot = FindResourceRoot();

        // AnimationRegistry からキャラクターをロード
        foreach (var (charType, spritePath) in AnimationRegistry.CharacterSprites)
        {
            string fullPath = Path.Combine(resourceRoot, spritePath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[AnimationManager] スプライトが見つかりませんでした: {fullPath}");
                continue;
            }

            _characterSpriteHandles[charType] = AssetManager.LoadGraph(fullPath);
            _characterConfigs[charType] = AnimationRegistry.CharacterConfigs[charType];
            Console.WriteLine($"[AnimationManager] キャラクタースプライトをロードしました: {charType}");
        }

        // AnimationRegistry からエフェクトをロード
        foreach (var (effectType, spritePath) in AnimationRegistry.EffectSprites)
        {
            string fullPath = Path.Combine(resourceRoot, spritePath);
            if (!File.Exists(fullPath))
            {
                Console.WriteLine($"[AnimationManager] エフェクトスプライトが見つかりませんでした: {fullPath}");
                continue;
            }

            _effectSpriteHandles[effectType] = AssetManager.LoadGraph(fullPath);
            _effectConfigs[effectType] = AnimationRegistry.EffectConfigs[effectType];
            Console.WriteLine($"[AnimationManager] エフェクトスプライトをロードしました: {effectType}");
        }

        EnsurePlayerCount(playerCount);
    }

    /// <summary>
    /// プレイヤー数に応じて AnimationPlayer のリストサイズを拡張・確保します。
    /// </summary>
    public void EnsurePlayerCount(int playerCount)
    {
        playerCount = Math.Clamp(playerCount, 1, 4);
        while (_characterAnimPlayers.Count < playerCount)
        {
            int playerIndex = _characterAnimPlayers.Count;

            // キャラクター種別を順番に割り当て（複数キャラがいれば順番に、1体だけなら全員同じ）
            CharacterType charType = AssignCharacterType(playerIndex);
            _playerCharacterTypes.Add(charType);

            var charPlayer = new AnimationPlayer();
            if (HasCatSprite && _characterConfigs.TryGetValue(charType, out var config))
            {
                SetupClips(charPlayer, config.Clips, config.CellWidth, config.CellHeight);
            }

            _characterAnimPlayers.Add(charPlayer);

            var effectPlayer = new AnimationPlayer();
            if (HasEffectSprite && _effectConfigs.Count > 0)
            {
                var effConfig = new List<AnimationConfig>(_effectConfigs.Values)[0];
                SetupClips(effectPlayer, effConfig.Clips, effConfig.CellWidth, effConfig.CellHeight);
            }

            _effectAnimPlayers.Add(effectPlayer);
        }
    }

    /// <summary>
    /// 全プレイヤーおよびエフェクトのアニメーション状態を更新します。
    /// </summary>
    /// <param name="deltaTime">前フレームからの経過秒数</param>
    public void Update(float deltaTime)
    {
        for (int i = 0; i < _characterAnimPlayers.Count; i++)
        {
            _characterAnimPlayers[i].Update(deltaTime);
            _effectAnimPlayers[i].Update(deltaTime);
        }
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターに指定のアニメーションクリップが存在するかを取得します。
    /// </summary>
    public bool HasCharacterAnimation(int playerIndex, CharacterAnimationType animType)
    {
        return HasCharacterAnimation(playerIndex, animType.ToString());
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターに指定のアニメーションクリップが存在するかを取得します。
    /// </summary>
    public bool HasCharacterAnimation(int playerIndex, string animName)
    {
        int zeroIndex = playerIndex - 1;
        if (zeroIndex < 0 || zeroIndex >= _playerCharacterTypes.Count) return false;

        CharacterType charType = _playerCharacterTypes[zeroIndex];
        if (_characterConfigs.TryGetValue(charType, out var config))
        {
            return config.Clips.Exists(c => c.Name.Equals(animName, StringComparison.OrdinalIgnoreCase));
        }
        return false;
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターのアニメーション再生を開始します。
    /// </summary>
    public bool PlayCharacterAnimation(int playerIndex, CharacterAnimationType animType, bool restartIfSame = false)
    {
        return PlayCharacterAnimation(playerIndex, animType.ToString(), restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターのアニメーション再生を開始します。
    /// 存在するアニメーションのみ再生を試みます。
    /// </summary>
    public bool PlayCharacterAnimation(int playerIndex, string animName, bool restartIfSame = false)
    {
        AnimationPlayer player = GetCharacterAnimPlayer(playerIndex);
        return player.Play(animName, restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーのエフェクトアニメーション再生を開始します。
    /// </summary>
    public bool PlayEffectAnimation(int playerIndex, EffectAnimationType effectType, bool restartIfSame = false)
    {
        return PlayEffectAnimation(playerIndex, effectType.ToString(), restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーのエフェクトアニメーション再生を開始します。
    /// </summary>
    public bool PlayEffectAnimation(int playerIndex, string effectName, bool restartIfSame = false)
    {
        AnimationPlayer player = GetEffectAnimPlayer(playerIndex);
        return player.Play(effectName, restartIfSame);
    }

    /// <summary>
    /// 指定されたプレイヤーキャラクターのスプライトを描画します。
    /// </summary>
    public void DrawCharacter(RenderService render, int playerIndex, Point<int> position, bool facingLeft, float scale = 2.5f)
    {
        if (!HasCatSprite) return;
        int zeroIndex = playerIndex - 1;
        if (zeroIndex < 0 || zeroIndex >= _playerCharacterTypes.Count) return;

        CharacterType charType = _playerCharacterTypes[zeroIndex];
        if (!_characterSpriteHandles.TryGetValue(charType, out int handle)) return;

        AnimationPlayer player = GetCharacterAnimPlayer(playerIndex);
        player.Draw(render, position, handle, flipX: facingLeft, scale: scale);
    }

    /// <summary>
    /// 指定されたプレイヤーの攻撃エフェクトスプライトを描画します。
    /// </summary>
    public void DrawEffect(RenderService render, int playerIndex, Point<int> position, bool facingLeft, float scale = 2.0f)
    {
        if (!HasEffectSprite) return;
        AnimationPlayer player = GetEffectAnimPlayer(playerIndex);
        int handle = new List<int>(_effectSpriteHandles.Values)[0];
        player.Draw(render, position, handle, flipX: facingLeft, scale: scale);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            foreach (var p in _characterAnimPlayers) p.Dispose();
            foreach (var p in _effectAnimPlayers) p.Dispose();
            _characterAnimPlayers.Clear();
            _effectAnimPlayers.Clear();

            foreach (var handle in _characterSpriteHandles.Values)
            {
                AssetManager.DeleteGraph(handle);
            }
            _characterSpriteHandles.Clear();

            foreach (var handle in _effectSpriteHandles.Values)
            {
                AssetManager.DeleteGraph(handle);
            }
            _effectSpriteHandles.Clear();

            _isDisposed = true;
        }
    }

    private AnimationPlayer GetCharacterAnimPlayer(int playerIndex)
    {
        EnsurePlayerCount(playerIndex);
        return _characterAnimPlayers[playerIndex - 1];
    }

    private AnimationPlayer GetEffectAnimPlayer(int playerIndex)
    {
        EnsurePlayerCount(playerIndex);
        return _effectAnimPlayers[playerIndex - 1];
    }

    /// <summary>
    /// resource/ フォルダのルートパスを解決します。
    /// </summary>
    private static string FindResourceRoot()
    {
        string candidate = Path.Combine(Directory.GetCurrentDirectory(), "resource");
        if (Directory.Exists(candidate)) return candidate;

        string parent = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "resource"));
        if (Directory.Exists(parent)) return parent;

        DirectoryInfo? dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null)
        {
            string path = Path.Combine(dir.FullName, "resource");
            if (Directory.Exists(path)) return path;
            dir = dir.Parent;
        }

        Console.WriteLine("[AnimationManager] resource/ フォルダが見つかりませんでした。");
        return Path.Combine(Directory.GetCurrentDirectory(), "resource");
    }

    /// <summary>
    /// AnimationClipConfig リストをもとに AnimationPlayer にクリップを登録します。
    /// </summary>
    private static void SetupClips(AnimationPlayer player, List<AnimationClipConfig> clips, int defaultCellWidth, int defaultCellHeight)
    {
        foreach (var clip in clips)
        {
            int cw = clip.CellWidth > 0 ? clip.CellWidth : defaultCellWidth;
            int ch = clip.CellHeight > 0 ? clip.CellHeight : defaultCellHeight;
            player.AddGridClip(clip.Name, clip.Row, clip.StartFrame, clip.EndFrame, cw, ch, clip.Fps, clip.IsLooping);
        }

        // デフォルト再生クリップは最初のクリップ
        if (clips.Count > 0)
        {
            player.Play(clips[0].Name);
        }
    }

    /// <summary>
    /// プレイヤーインデックスに対してキャラクター種別を割り当てます。
    /// キャラクターが複数いる場合は順番に割り当て、1体のみの場合は全員同じにします。
    /// </summary>
    private CharacterType AssignCharacterType(int zeroBasedIndex)
    {
        if (_characterSpriteHandles.Count == 0) return default;

        var types = new List<CharacterType>(_characterSpriteHandles.Keys);
        return types[zeroBasedIndex % types.Count];
    }
}
